// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using UglyToad.PdfPig;
using Xunit;

namespace DabblerPdf.Tests.Text;

/// <summary>
/// Tests for <see cref="TrueTypeFontSubSet"/>, the font program an Identity-H font embeds,
/// using the vendored Liberation Sans Regular fixture. The program is read back from the
/// document's <c>FontFile2</c> stream.
/// </summary>
public class TrueTypeFontSubSetTests
{
    private static readonly string FontPath =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");

    /// <summary>Accented letters, among which Liberation Sans draws some as composite glyphs.</summary>
    private const string Accented = "ÁÉÍÓÚáéíóúÄÖÜäöüÅåÇçÑñ";

    private static TrueTypeFontUnicode UnicodeFont()
    {
        if (!File.Exists(FontPath))
            Assert.Fail($"Font fixture not found at {FontPath}; the checkout or test output is broken.");
        return Assert.IsType<TrueTypeFontUnicode>(BaseFont.CreateFont(FontPath, BaseFont.IdentityH, true));
    }

    private static List<int> GlyphsOf(TrueTypeFontUnicode font, string text)
    {
        var glyphs = new SortedDictionary<int, char>();
        font.EncodeHex(text, glyphs);
        return glyphs.Keys.ToList();
    }

    /// <summary>A one-page document drawing <paramref name="text"/> in <paramref name="font"/>.</summary>
    private static byte[] BuildPdf(BaseFont font, string text)
    {
        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document();
        var writer = PdfWriter.GetInstance(doc, ms);
        writer.CompressContentStreams = false;
        doc.Open();
        var cb = writer.DirectContent;
        cb.BeginText();
        cb.SetFontAndSize(font, 12);
        cb.SetTextMatrix(72, 720);
        cb.ShowText(text);
        cb.EndText();
        doc.Close();
        return ms.ToArray();
    }

    /// <summary>The decompressed font program of the document's one <c>FontFile2</c> stream.</summary>
    private static byte[] EmbeddedProgram(byte[] pdf)
    {
        string text = Encoding.Latin1.GetString(pdf);
        var length1 = Regex.Matches(text, @"/Length1 (\d+)");
        Assert.Single(length1);
        int at = length1[0].Index;
        int dictStart = text.LastIndexOf("<<", at, StringComparison.Ordinal);
        int dataStart = text.IndexOf("\nstream\n", at, StringComparison.Ordinal) + "\nstream\n".Length;
        var length = Regex.Match(text[dictStart..dataStart], @"/Length (\d+)");
        Assert.True(length.Success, "The font stream has no /Length.");

        using var zlib = new ZLibStream(
            new MemoryStream(pdf, dataStart, int.Parse(length.Groups[1].Value, CultureInfo.InvariantCulture)),
            CompressionMode.Decompress);
        using var program = new MemoryStream();
        zlib.CopyTo(program);
        Assert.Equal(int.Parse(length1[0].Groups[1].Value, CultureInfo.InvariantCulture), program.Length);
        return program.ToArray();
    }

    /// <summary>Each glyph's offset into <c>glyf</c> from <paramref name="font"/>'s <c>loca</c>, and the <c>glyf</c> offset.</summary>
    private static (int[] Loca, int Glyf) ReadOutlines(byte[] font)
    {
        var reader = new TrueTypeFont.TtfReader(font);
        var directory = reader.ReadTableDirectory();
        bool shortLoca = reader.ReadHead(directory["head"]).IndexToLocFormat == 0;
        var loca = directory["loca"];
        var offsets = new int[loca.Length / (shortLoca ? 2 : 4)];
        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = shortLoca
                ? BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(loca.Offset + i * 2)) * 2
                : (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(loca.Offset + i * 4));
        return (offsets, directory["glyf"].Offset);
    }

    private static int OutlineLength(byte[] font, int glyph)
    {
        var (loca, _) = ReadOutlines(font);
        return loca[glyph + 1] - loca[glyph];
    }

    /// <summary>The components of a composite glyph, or none for a simple one.</summary>
    private static List<int> Components(byte[] font, int glyph)
    {
        var (loca, glyf) = ReadOutlines(font);
        var components = new List<int>();
        int p = glyf + loca[glyph];
        if (loca[glyph + 1] == loca[glyph] || BinaryPrimitives.ReadInt16BigEndian(font.AsSpan(p)) >= 0)
            return components;
        p += 10;
        int flags;
        do
        {
            flags = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(p));
            components.Add(BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(p + 2)));
            p += 4 + ((flags & 0x1) != 0 ? 4 : 2) + ((flags & 0x8) != 0 ? 2 : (flags & 0x40) != 0 ? 4 : (flags & 0x80) != 0 ? 8 : 0);
        } while ((flags & 0x20) != 0);
        return components;
    }

    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 4)
        {
            uint word = 0;
            for (int j = 0; j < 4; j++)
                word = (word << 8) | (i + j < data.Length ? data[i + j] : 0u);
            sum += word;
        }
        return sum;
    }

    [Fact]
    public void Create_HelloDocument_EmbedsUnderATenthOfTheFontFile()
    {
        var program = EmbeddedProgram(BuildPdf(UnicodeFont(), "Hello"));

        Assert.True(program.Length < File.ReadAllBytes(FontPath).Length / 10,
            $"The embedded program is {program.Length} bytes.");
    }

    [Fact]
    public void Create_SubsetParsesBack_KeepingEachUsedGlyphsAdvanceAndOutline()
    {
        var font = UnicodeFont();
        byte[] full = File.ReadAllBytes(FontPath);
        byte[] subset = EmbeddedProgram(BuildPdf(font, "Hello"));

        var fullReader = new TrueTypeFont.TtfReader(full);
        var fullTables = fullReader.ReadTableDirectory();
        var fullAdvances = fullReader.ReadHmtx(fullTables["hmtx"], fullReader.ReadHhea(fullTables["hhea"]).NumOfLongHorMetrics);

        var reader = new TrueTypeFont.TtfReader(subset);
        var tables = reader.ReadTableDirectory();
        Assert.Equal(new[] { "cvt ", "fpgm", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "prep" },
            tables.Keys.Order(StringComparer.Ordinal));
        var advances = reader.ReadHmtx(tables["hmtx"], reader.ReadHhea(tables["hhea"]).NumOfLongHorMetrics);

        foreach (int glyph in GlyphsOf(font, "Hello").Prepend(0))
        {
            Assert.Equal(fullAdvances[glyph], advances[glyph]);
            Assert.Equal(OutlineLength(full, glyph), OutlineLength(subset, glyph));
            Assert.NotEqual(0, OutlineLength(subset, glyph));
        }
        int unused = GlyphsOf(font, "Z").Single();
        Assert.NotEqual(0, OutlineLength(full, unused));
        Assert.Equal(0, OutlineLength(subset, unused));
    }

    [Fact]
    public void Create_EachTableChecksumAndTheFileChecksum_AreCorrect()
    {
        byte[] subset = EmbeddedProgram(BuildPdf(UnicodeFont(), "Hello"));

        int count = BinaryPrimitives.ReadUInt16BigEndian(subset.AsSpan(4));
        for (int i = 0; i < count; i++)
        {
            int record = 12 + i * 16;
            string tag = Encoding.ASCII.GetString(subset, record, 4);
            int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(subset.AsSpan(record + 8));
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(subset.AsSpan(record + 12));
            byte[] table = subset.AsSpan(offset, length).ToArray();
            if (tag == "head")
                BinaryPrimitives.WriteUInt32BigEndian(table.AsSpan(8), 0);
            Assert.Equal(BinaryPrimitives.ReadUInt32BigEndian(subset.AsSpan(record + 4)), Checksum(table));
        }
        Assert.Equal(0xB1B0AFBAu, Checksum(subset));
    }

    [Fact]
    public void Create_CompositeGlyph_KeepsItsComponents_AndItsTextIsExtracted()
    {
        var font = UnicodeFont();
        byte[] full = File.ReadAllBytes(FontPath);
        string? composite = Accented.Select(c => c.ToString())
            .FirstOrDefault(c => Components(full, GlyphsOf(font, c).Single()).Count > 0);
        Assert.True(composite != null, "Liberation Sans draws none of the accented letters as a composite glyph.");

        byte[] pdf = BuildPdf(font, composite);
        byte[] subset = EmbeddedProgram(pdf);

        foreach (int component in Components(full, GlyphsOf(font, composite).Single()))
            Assert.Equal(OutlineLength(full, component), OutlineLength(subset, component));
        using var document = PdfDocument.Open(pdf);
        Assert.Equal(composite, document.GetPage(1).Text);
    }

    [Fact]
    public void SubsetTag_NamesTheFont_WithSixUppercaseLettersAndPlus()
    {
        string pdf = Encoding.Latin1.GetString(BuildPdf(UnicodeFont(), "Hello"));

        var fontName = Regex.Match(pdf, @"/FontName /([A-Z]{6})\+LiberationSans\b");
        Assert.True(fontName.Success, "The font descriptor's /FontName has no subset tag.");
        string tag = fontName.Groups[1].Value;
        Assert.Contains($"/BaseFont /{tag}+LiberationSans-Identity-H", pdf);
        Assert.Matches($@"/BaseFont /{tag}\+LiberationSans\b(?!-)", pdf);
        Assert.Matches(@"^[A-Z]{6}\+$", TrueTypeFontSubSet.SubsetTag([0, 43, 72]));
    }

    [Fact]
    public void Create_DocumentsDrawingDifferentText_EmbedDifferentSubsets()
    {
        var font = UnicodeFont();
        byte[] hello = BuildPdf(font, "Hello");
        byte[] world = BuildPdf(font, "World");

        Assert.NotEqual(EmbeddedProgram(hello), EmbeddedProgram(world));
        Assert.NotEqual(TagOf(hello), TagOf(world));
    }

    private static string TagOf(byte[] pdf) =>
        Regex.Match(Encoding.Latin1.GetString(pdf), @"/FontName /([A-Z]{6})\+").Groups[1].Value;
}
