// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Text;

/// <summary>
/// Provides font metrics and PDF font dictionaries for the 14 standard PDF fonts
/// and TrueType fonts. This is the primary text measurement API.
/// </summary>
public abstract class BaseFont
{
    /// <summary>
    /// Windows-1252 encoding. Supports ASCII plus Western European characters
    /// including accented letters (e, n, u), the Euro sign, em/en dashes,
    /// and typographic quotes. Characters outside this range are drawn and measured as '?'.
    /// This is the only encoding of the 14 standard fonts.
    ///
    /// For other scripts (Polish, Cyrillic, Greek, CJK and the rest of the Basic
    /// Multilingual Plane), use <see cref="IdentityH"/> with a TrueType font.
    /// </summary>
    public const string WinAnsiEncoding = "Cp1252";

    /// <summary>
    /// Identity-H encoding: with a TrueType (.ttf) font, every character the font has a
    /// glyph for is drawn, and copying and text extraction give the text back. The whole
    /// font file is embedded. A character the font lacks, and any character beyond U+FFFF,
    /// is drawn as the font's <c>.notdef</c> glyph. There is no shaping and no right-to-left
    /// layout, so scripts that need them (Arabic, Hebrew, Indic, Thai) do not draw correctly.
    /// The 14 standard fonts have no glyphs to embed and cannot use it.
    /// </summary>
    public const string IdentityH = "Identity-H";

    /// <summary>Font descriptor key for the ascent (distance above baseline).</summary>
    public const int ASCENT = 1;

    /// <summary>Font descriptor key for the cap height.</summary>
    public const int CAPHEIGHT = 2;

    /// <summary>Font descriptor key for the descent (distance below baseline, typically negative).</summary>
    public const int DESCENT = 3;

    /// <summary>Font descriptor key for the italic angle in degrees.</summary>
    public const int ITALICANGLE = 4;

    /// <summary>Font descriptor key for the bounding box lower-left X.</summary>
    public const int BBOXLLX = 5;

    /// <summary>Font descriptor key for the bounding box lower-left Y.</summary>
    public const int BBOXLLY = 6;

    /// <summary>Font descriptor key for the bounding box upper-right X.</summary>
    public const int BBOXURX = 7;

    /// <summary>Font descriptor key for the bounding box upper-right Y.</summary>
    public const int BBOXURY = 8;

    /// <summary>Helvetica (regular).</summary>
    public const string Helvetica = "Helvetica";

    /// <summary>Helvetica Bold.</summary>
    public const string HelveticaBold = "Helvetica-Bold";

    /// <summary>Helvetica Oblique (italic).</summary>
    public const string HelveticaOblique = "Helvetica-Oblique";

    /// <summary>Helvetica Bold Oblique (bold italic).</summary>
    public const string HelveticaBoldOblique = "Helvetica-BoldOblique";

    /// <summary>Times Roman (regular).</summary>
    public const string TimesRoman = "Times-Roman";

    /// <summary>Times Bold.</summary>
    public const string TimesBold = "Times-Bold";

    /// <summary>Times Italic.</summary>
    public const string TimesItalic = "Times-Italic";

    /// <summary>Times Bold Italic.</summary>
    public const string TimesBoldItalic = "Times-BoldItalic";

    /// <summary>Courier (regular, monospaced).</summary>
    public const string Courier = "Courier";

    /// <summary>Courier Bold.</summary>
    public const string CourierBold = "Courier-Bold";

    /// <summary>Courier Oblique (italic).</summary>
    public const string CourierOblique = "Courier-Oblique";

    /// <summary>Courier Bold Oblique (bold italic).</summary>
    public const string CourierBoldOblique = "Courier-BoldOblique";

    /// <summary>Symbol font (Greek letters and mathematical symbols).</summary>
    public const string Symbol = "Symbol";

    /// <summary>ZapfDingbats font (decorative symbols and icons).</summary>
    public const string ZapfDingbats = "ZapfDingbats";

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BaseFont> _fontCache = new();

    /// <summary>The PostScript name of this font.</summary>
    public string PostscriptName { get; }

    /// <summary>Initializes the base font with the given PostScript name.</summary>
    /// <param name="postscriptName">The PostScript name of the font.</param>
    protected BaseFont(string postscriptName)
    {
        PostscriptName = postscriptName;
    }

    /// <summary>
    /// Creates a BaseFont for the given font name and encoding.
    /// For the 14 standard PDF fonts, no external font file is needed.
    /// For TrueType fonts, pass the .ttf file path as <paramref name="name"/>.
    /// Use <see cref="CanEncode"/> to check whether a specific character is supported
    /// by the font's encoding before rendering.
    /// </summary>
    /// <param name="name">Font name (e.g., "Helvetica") or a .ttf file path.</param>
    /// <param name="encoding"><see cref="WinAnsiEncoding"/> (Windows-1252: ASCII and Western
    /// European characters) for any font, or <see cref="IdentityH"/> (every character the
    /// font has a glyph for) for a TrueType font. Any other value is treated as
    /// <see cref="WinAnsiEncoding"/>. The font is cached per name and encoding, so one .ttf
    /// can be created with both, and they are two fonts.</param>
    /// <param name="embedded">Whether to embed the font (always true for TrueType).</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is neither a standard font nor a
    /// .ttf path, or it is a standard font and <paramref name="encoding"/> is <see cref="IdentityH"/>.</exception>
    public static BaseFont CreateFont(string name, string encoding, bool embedded)
    {
        string key = $"{name}-{encoding}";
        return _fontCache.GetOrAdd(key, _ =>
        {
            bool identityH = encoding == IdentityH;
            if (BuiltinFonts.IsStandardFont(name))
            {
                if (identityH)
                    throw new ArgumentException(
                        $"Font '{name}' is a standard PDF font, which has no glyphs to embed; {IdentityH} needs a TrueType font.",
                        nameof(encoding));
                return new Type1Font(name);
            }

            if (IsTrueTypeFontFile(name))
            {
                byte[] fontData = File.ReadAllBytes(name);
                string psName = TrueTypeFont.ExtractPostscriptName(fontData);
                return identityH
                    ? new TrueTypeFontUnicode(psName, fontData)
                    : new TrueTypeFont(psName, fontData);
            }

            throw new ArgumentException(
                $"Font '{name}' is not a standard PDF font and is not a valid TrueType font file path.",
                nameof(name));
        });
    }

    private static bool IsTrueTypeFontFile(string name)
    {
        return name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Measures the width of a string at the given font size, in points.
    /// This is the key method for the reporting engine's text measurement.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontSize">The font size in points.</param>
    /// <returns>The width of the text in points.</returns>
    public virtual double GetWidthPoint(string text, double fontSize)
    {
        double totalWidth = 0;
        foreach (char c in text)
        {
            totalWidth += GetCharWidth(c);
        }
        return totalWidth * fontSize / 1000.0;
    }

    /// <summary>
    /// Gets the width of a single character in 1/1000 of a unit (unscaled).
    /// </summary>
    /// <param name="c">The character code.</param>
    /// <returns>The character width in thousandths of a unit.</returns>
    public abstract int GetCharWidth(int c);

    /// <summary>
    /// Gets a font descriptor value scaled to the given font size.
    /// </summary>
    /// <param name="key">One of the descriptor constants (ASCENT, DESCENT, etc.).</param>
    /// <param name="fontSize">The font size in points.</param>
    public abstract double GetFontDescriptor(int key, double fontSize);

    /// <summary>
    /// Returns whether the given character can be represented in the current encoding.
    /// For WinAnsi fonts, returns true for characters in the Windows-1252 range; for an
    /// <see cref="IdentityH"/> font, true exactly when the font has a glyph for it.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><c>true</c> if the character can be encoded; otherwise, <c>false</c>.</returns>
    public virtual bool CanEncode(char c)
    {
        return PdfEncodings.CharToWinAnsi(c) >= 0;
    }
}
