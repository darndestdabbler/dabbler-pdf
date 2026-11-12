// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using UglyToad.PdfPig;
using Xunit;

namespace DabblerPdf.Tests.Text;

/// <summary>
/// Tests for TrueType fonts drawn with Identity-H (<see cref="TrueTypeFontUnicode"/>), using
/// the vendored Liberation Sans Regular fixture. Drawn text is checked by extracting it with
/// PdfPig, which reads it back through the font's ToUnicode CMap.
/// </summary>
public class TrueTypeFontUnicodeTests
{
    private static readonly string FontPath =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");

    private const string Polish = "Zażółć gęślą jaźń";
    private const string Cyrillic = "Съешь же ещё этих булок";
    private const string Greek = "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία";
    private const string Ideograph = "中"; // 中, which Liberation Sans lacks

    private static TrueTypeFontUnicode UnicodeFont()
    {
        if (!File.Exists(FontPath))
            Assert.Fail($"Font fixture not found at {FontPath}; the checkout or test output is broken.");
        return Assert.IsType<TrueTypeFontUnicode>(BaseFont.CreateFont(FontPath, BaseFont.IdentityH, true));
    }

    private static int GlyphOf(TrueTypeFontUnicode font, string text) =>
        int.Parse(font.EncodeHex(text, new SortedDictionary<int, char>()), NumberStyles.HexNumber);

    /// <summary>Builds a one-document PDF, uncompressed so its content can be read.</summary>
    private static byte[] BuildPdf(Action<PdfWriter> draw)
    {
        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document();
        var writer = PdfWriter.GetInstance(doc, ms);
        writer.CompressContentStreams = false;
        doc.Open();
        draw(writer);
        doc.Close();
        return ms.ToArray();
    }

    private static void DrawLine(PdfContentByte cb, BaseFont font, string text, double y)
    {
        cb.BeginText();
        cb.SetFontAndSize(font, 12);
        cb.SetTextMatrix(72, y);
        cb.ShowText(text);
        cb.EndText();
    }

    /// <summary>The glyph IDs in the document's /W array (each entry is <c>gid [width]</c>).</summary>
    private static List<int> WGlyphs(byte[] pdf)
    {
        var match = Regex.Match(Encoding.Latin1.GetString(pdf), @"/W \[((?:\d+ \[\d+\] ?)*)\]");
        Assert.True(match.Success, "The PDF has no /W array.");
        return Regex.Matches(match.Groups[1].Value, @"(\d+) \[\d+\]")
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();
    }

    [Fact]
    public void CreateFont_IdentityH_IsNotTheWinAnsiFont()
    {
        var unicode = UnicodeFont();
        var winAnsi = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
        Assert.IsNotType<TrueTypeFontUnicode>(winAnsi);
        Assert.Equal(winAnsi.PostscriptName, unicode.PostscriptName);
    }

    [Theory]
    [InlineData(Polish)]
    [InlineData(Cyrillic)]
    [InlineData(Greek)]
    public void ShowText_NonLatinText_IsExtractedExactly(string text)
    {
        var font = UnicodeFont();
        var pdf = BuildPdf(w => DrawLine(w.DirectContent, font, text, 720));

        using var document = PdfDocument.Open(pdf);
        Assert.Equal(text, document.GetPage(1).Text);
    }

    [Fact]
    public void ShowText_WritesType0FontWithIdentityHDescendant()
    {
        var font = UnicodeFont();
        var output = Encoding.Latin1.GetString(BuildPdf(w => DrawLine(w.DirectContent, font, Cyrillic, 720)));

        Assert.Contains("/Subtype /Type0", output);
        Assert.Contains("/Encoding /Identity-H", output);
        Assert.Contains("/Subtype /CIDFontType2", output);
        Assert.Contains("/CIDToGIDMap /Identity", output);
        Assert.Contains("/Registry (Adobe)", output);
        Assert.Contains("/Ordering (Identity)", output);
        Assert.Contains("/FontFile2", output);
        Assert.Contains("/ToUnicode", output);
    }

    [Fact]
    public void GetWidthPoint_IsTheSumOfGlyphAdvances()
    {
        var font = UnicodeFont();
        foreach (var text in new[] { Polish, Cyrillic, Greek })
        {
            double expected = text.Sum(c => font.GetCharWidth(c)) * 12 / 1000.0;
            Assert.Equal(expected, font.GetWidthPoint(text, 12), 6);
        }
    }

    [Fact]
    public void GetCharWidth_LatinGlyph_MatchesTheWinAnsiFont()
    {
        var font = UnicodeFont();
        var winAnsi = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
        foreach (char c in "AWgi é€")
            Assert.Equal(winAnsi.GetCharWidth(c), font.GetCharWidth(c));
    }

    [Fact]
    public void GetCharWidth_NonLatinGlyph_IsItsOwnAdvance()
    {
        var font = UnicodeFont();
        int notdef = font.GetCharWidth(Ideograph[0]);
        Assert.True(font.GetCharWidth('Ж') > 0);
        Assert.NotEqual(notdef, font.GetCharWidth('Ж'));
        Assert.NotEqual(font.GetCharWidth('і'), font.GetCharWidth('Ж'));
    }

    [Fact]
    public void MissingGlyph_MeasuresAsNotdef_AndCannotEncode()
    {
        var font = UnicodeFont();
        Assert.False(font.CanEncode(Ideograph[0]));
        Assert.Equal(0, GlyphOf(font, Ideograph));
        // A code point beyond U+FFFF is also .notdef
        Assert.Equal(font.GetCharWidth(0x1F600), font.GetCharWidth(Ideograph[0]));
        Assert.Equal(font.GetCharWidth(Ideograph[0]) * 10 / 1000.0, font.GetWidthPoint(Ideograph, 10), 6);
    }

    [Fact]
    public void CanEncode_IsTrueForGlyphsTheFontHas()
    {
        var font = UnicodeFont();
        foreach (char c in Polish + Cyrillic + Greek)
            Assert.True(font.CanEncode(c), $"U+{(int)c:X4}");
    }

    [Fact]
    public void SurrogatePair_IsOneNotdef()
    {
        var font = UnicodeFont();
        const string emoji = "\U0001F600";
        Assert.Equal("0000", font.EncodeHex(emoji, new SortedDictionary<int, char>()));
        Assert.Equal(font.GetCharWidth(Ideograph[0]) * 10 / 1000.0, font.GetWidthPoint(emoji, 10), 6);
    }

    [Fact]
    public void OneFontInTwoDocuments_EachDocumentHasItsOwnW()
    {
        var font = UnicodeFont();
        var first = BuildPdf(w => DrawLine(w.DirectContent, font, "AAA", 720));
        var second = BuildPdf(w => DrawLine(w.DirectContent, font, "Ж", 720));

        Assert.Equal([GlyphOf(font, "A")], WGlyphs(first));
        Assert.Equal([GlyphOf(font, "Ж")], WGlyphs(second));
    }

    [Fact]
    public void OneFileWithBothEncodings_InOneDocument_DrawsBothTexts()
    {
        var unicode = UnicodeFont();
        var winAnsi = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
        var pdf = BuildPdf(w =>
        {
            DrawLine(w.DirectContent, winAnsi, "Hello", 720);
            DrawLine(w.DirectContent, unicode, "Привет", 700);
        });

        using var document = PdfDocument.Open(pdf);
        var text = document.GetPage(1).Text;
        Assert.Contains("Hello", text);
        Assert.Contains("Привет", text);
        var output = Encoding.Latin1.GetString(pdf);
        Assert.Contains("/Subtype /TrueType", output);
        Assert.Contains("/Subtype /Type0", output);
    }

    [Fact]
    public void TextInTemplate_IsExtracted()
    {
        var font = UnicodeFont();
        var pdf = BuildPdf(w =>
        {
            var template = w.CreateTemplate(300, 40);
            w.DirectContent.AddTemplate(template, 72, 700);
            // Drawn after placement: the font is written at Close, so its glyphs are known
            DrawLine(template.Content, font, Greek, 10);
        });

        using var document = PdfDocument.Open(pdf);
        Assert.Equal(Greek, document.GetPage(1).Text);
    }
}
