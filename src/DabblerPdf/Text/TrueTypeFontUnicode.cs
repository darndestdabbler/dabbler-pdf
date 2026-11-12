// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Globalization;
using System.Text;
using DabblerPdf.Pdf;

namespace DabblerPdf.Text;

/// <summary>
/// A TrueType font drawn with Identity-H encoding: each character is written as its
/// 2-byte glyph ID, so any character the font has a glyph for is drawn. Ported from
/// OpenPDF's <c>TrueTypeFontUnicode</c>, without subsetting.
/// </summary>
/// <remarks>
/// A character the font has no glyph for, and each surrogate pair (a character beyond
/// U+FFFF), is drawn and measured as glyph 0 (<c>.notdef</c>). The glyphs a document draws
/// are recorded by its <see cref="PdfWriter"/>, not here, because <see cref="BaseFont.CreateFont"/>
/// shares one instance across documents.
/// </remarks>
internal sealed class TrueTypeFontUnicode : TrueTypeFont
{
    internal TrueTypeFontUnicode(string postscriptName, byte[] fontData)
        : base(postscriptName, fontData)
    {
    }

    /// <summary>The width of the glyph for code point <paramref name="c"/>, <c>.notdef</c>'s when there is none.</summary>
    public override int GetCharWidth(int c) =>
        WidthOf(c is >= 0 and <= 0xFFFF ? CharToGlyph.GetValueOrDefault((char)c) : 0);

    /// <inheritdoc />
    public override double GetWidthPoint(string text, double fontSize)
    {
        double total = 0;
        for (int i = 0; i < text.Length;)
            total += WidthOf(NextGlyph(text, ref i));
        return total * fontSize / 1000.0;
    }

    /// <summary>True exactly when the font has a glyph for <paramref name="c"/>.</summary>
    public override bool CanEncode(char c) => CharToGlyph.ContainsKey(c);

    /// <summary>
    /// Encodes <paramref name="text"/> as hex glyph IDs, four digits each, and records each
    /// glyph and the character it was drawn for in <paramref name="usedGlyphs"/>.
    /// </summary>
    internal string EncodeHex(string text, SortedDictionary<int, char> usedGlyphs)
    {
        var hex = new StringBuilder(text.Length * 4);
        for (int i = 0; i < text.Length;)
        {
            char c = text[i];
            int glyph = NextGlyph(text, ref i);
            usedGlyphs.TryAdd(glyph, c);
            hex.Append(glyph.ToString("X4", CultureInfo.InvariantCulture));
        }
        return hex.ToString();
    }

    /// <summary>
    /// Writes the Type0 font at <paramref name="fontRef"/>, with its CIDFontType2 descendant,
    /// font descriptor, font file and ToUnicode CMap, for the glyphs one document drew.
    /// </summary>
    internal void WriteFont(PdfWriter writer, PdfIndirectReference fontRef, SortedDictionary<int, char> usedGlyphs)
    {
        var descriptorRef = AddFontDescriptor(writer);

        var systemInfo = new PdfDictionary();
        systemInfo.Put(new PdfName("Registry"), new PdfString("Adobe"));
        systemInfo.Put(new PdfName("Ordering"), new PdfString("Identity"));
        systemInfo.Put(new PdfName("Supplement"), new PdfNumber(0));

        var widths = new PdfArray();
        foreach (int glyph in usedGlyphs.Keys)
        {
            widths.Add(new PdfNumber(glyph));
            widths.Add(new PdfArray(WidthOf(glyph)));
        }

        var cidFont = new PdfDictionary(PdfName.Font);
        cidFont.Put(PdfName.Subtype, new PdfName("CIDFontType2"));
        cidFont.Put(PdfName.BaseFont, new PdfName(PostscriptName));
        cidFont.Put(new PdfName("CIDSystemInfo"), systemInfo);
        cidFont.Put(PdfName.FontDescriptor, descriptorRef);
        cidFont.Put(new PdfName("CIDToGIDMap"), new PdfName("Identity"));
        cidFont.Put(new PdfName("DW"), new PdfNumber(1000));
        cidFont.Put(new PdfName("W"), widths);
        var cidFontRef = writer.AddIndirectObject(cidFont);

        var toUnicode = new PdfStream(Encoding.ASCII.GetBytes(ToUnicodeCMap(usedGlyphs)));
        toUnicode.FlateCompress();
        var toUnicodeRef = writer.AddIndirectObject(toUnicode);

        var type0 = new PdfDictionary(PdfName.Font);
        type0.Put(PdfName.Subtype, new PdfName("Type0"));
        type0.Put(PdfName.BaseFont, new PdfName($"{PostscriptName}-{IdentityH}"));
        type0.Put(PdfName.Encoding, new PdfName(IdentityH));
        type0.Put(new PdfName("DescendantFonts"), new PdfArray(cidFontRef));
        type0.Put(new PdfName("ToUnicode"), toUnicodeRef);
        writer.AddIndirectObject(type0, fontRef);
    }

    /// <summary>
    /// The ToUnicode CMap mapping each used glyph back to its character. <c>.notdef</c>
    /// stands for every missing character, so it maps to none.
    /// </summary>
    private static string ToUnicodeCMap(SortedDictionary<int, char> usedGlyphs)
    {
        var mapped = usedGlyphs.Where(g => g.Key != 0).ToList();
        var cmap = new StringBuilder();
        cmap.Append("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n");
        cmap.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
        cmap.Append("/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n");
        cmap.Append("1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");
        // A bfchar block holds at most 100 entries
        foreach (var block in mapped.Chunk(100))
        {
            cmap.Append(CultureInfo.InvariantCulture, $"{block.Length} beginbfchar\n");
            foreach (var (glyph, c) in block)
                cmap.Append(CultureInfo.InvariantCulture, $"<{glyph:X4}> <{(int)c:X4}>\n");
            cmap.Append("endbfchar\n");
        }
        cmap.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n");
        return cmap.ToString();
    }

    /// <summary>The glyph at <paramref name="i"/>, advancing past it: a surrogate pair is one <c>.notdef</c>.</summary>
    private int NextGlyph(string text, ref int i)
    {
        if (char.IsSurrogatePair(text, i))
        {
            i += 2;
            return 0;
        }
        return CharToGlyph.GetValueOrDefault(text[i++]);
    }

    private int WidthOf(int glyph) => glyph < GlyphWidths.Length ? GlyphWidths[glyph] : 0;
}
