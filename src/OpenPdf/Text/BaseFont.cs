// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Text;

/// <summary>
/// Provides font metrics and PDF font dictionaries for the 14 standard PDF fonts
/// and (in future) TrueType fonts. This is the primary text measurement API.
/// </summary>
public abstract class BaseFont
{
    // Encoding constants
    public const string WinAnsiEncoding = "Cp1252";
    public const string IdentityH = "Identity-H";

    // Font descriptor constants (used with GetFontDescriptor)
    public const int ASCENT = 1;
    public const int CAPHEIGHT = 2;
    public const int DESCENT = 3;
    public const int ITALICANGLE = 4;
    public const int BBOXLLX = 5;
    public const int BBOXLLY = 6;
    public const int BBOXURX = 7;
    public const int BBOXURY = 8;

    // The 14 standard PDF font names
    public const string Helvetica = "Helvetica";
    public const string HelveticaBold = "Helvetica-Bold";
    public const string HelveticaOblique = "Helvetica-Oblique";
    public const string HelveticaBoldOblique = "Helvetica-BoldOblique";
    public const string TimesRoman = "Times-Roman";
    public const string TimesBold = "Times-Bold";
    public const string TimesItalic = "Times-Italic";
    public const string TimesBoldItalic = "Times-BoldItalic";
    public const string Courier = "Courier";
    public const string CourierBold = "Courier-Bold";
    public const string CourierOblique = "Courier-Oblique";
    public const string CourierBoldOblique = "Courier-BoldOblique";
    public const string Symbol = "Symbol";
    public const string ZapfDingbats = "ZapfDingbats";

    private static readonly Dictionary<string, BaseFont> _fontCache = new();

    /// <summary>The PostScript name of this font.</summary>
    public string PostscriptName { get; }

    protected BaseFont(string postscriptName)
    {
        PostscriptName = postscriptName;
    }

    /// <summary>
    /// Creates a BaseFont for the given font name and encoding.
    /// For the 14 standard PDF fonts, no external font file is needed.
    /// For TrueType fonts, pass the .ttf file path as <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Font name (e.g., "Helvetica") or a .ttf file path.</param>
    /// <param name="encoding">Encoding (e.g., <see cref="WinAnsiEncoding"/>).</param>
    /// <param name="embedded">Whether to embed the font (always true for TrueType).</param>
    public static BaseFont CreateFont(string name, string encoding, bool embedded)
    {
        string key = $"{name}-{encoding}";
        if (_fontCache.TryGetValue(key, out var cached))
            return cached;

        if (BuiltinFonts.IsStandardFont(name))
        {
            var font = new Type1Font(name);
            _fontCache[key] = font;
            return font;
        }

        // Try loading as a TrueType font file
        if (IsTrueTypeFontFile(name))
        {
            byte[] fontData = File.ReadAllBytes(name);
            string psName = TrueTypeFont.ExtractPostscriptName(fontData);
            var ttFont = new TrueTypeFont(psName, fontData);
            _fontCache[key] = ttFont;
            return ttFont;
        }

        throw new ArgumentException(
            $"Font '{name}' is not a standard PDF font and is not a valid TrueType font file path.",
            nameof(name));
    }

    private static bool IsTrueTypeFontFile(string name)
    {
        return name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".TTF", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Measures the width of a string at the given font size, in points.
    /// This is the key method for the reporting engine's text measurement.
    /// </summary>
    public double GetWidthPoint(string text, double fontSize)
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
    public abstract int GetCharWidth(int c);

    /// <summary>
    /// Gets a font descriptor value scaled to the given font size.
    /// </summary>
    /// <param name="key">One of the descriptor constants (ASCENT, DESCENT, etc.).</param>
    /// <param name="fontSize">The font size in points.</param>
    public abstract double GetFontDescriptor(int key, double fontSize);
}
