// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Text;

/// <summary>
/// Character encoding support for PDF fonts. Provides mappings between
/// WinAnsi (Windows-1252) byte values and Unicode code points.
/// </summary>
internal static class PdfEncodings
{
    /// <summary>
    /// Maps WinAnsi byte values (0–255) to Unicode code points.
    /// For most values this is identity; bytes 128–159 have special Windows-1252 mappings.
    /// </summary>
    private static readonly char[] WinAnsiToUnicode = CreateWinAnsiToUnicodeMap();

    /// <summary>
    /// Reverse mapping: Unicode code point → WinAnsi byte value.
    /// Only populated for the non-identity mappings in the 128–159 range.
    /// </summary>
    private static readonly Dictionary<char, byte> UnicodeToWinAnsi = CreateUnicodeToWinAnsiMap();

    /// <summary>
    /// Gets the Unicode code point for a WinAnsi byte value.
    /// </summary>
    internal static char WinAnsiToChar(int byteValue)
    {
        if (byteValue >= 0 && byteValue < 256)
            return WinAnsiToUnicode[byteValue];
        return '\0';
    }

    /// <summary>
    /// Converts a Unicode character to its WinAnsi byte value.
    /// Returns -1 if the character cannot be encoded in WinAnsi.
    /// </summary>
    internal static int CharToWinAnsi(char c)
    {
        // Identity range: 0–127 and 160–255 map directly
        if (c < 128)
            return c;
        if (c >= 160 && c <= 255)
            return c;

        // Check special mappings for 128–159
        if (UnicodeToWinAnsi.TryGetValue(c, out byte b))
            return b;

        return -1;
    }

    private static char[] CreateWinAnsiToUnicodeMap()
    {
        var map = new char[256];

        // 0–127: ASCII (identity)
        for (int i = 0; i < 128; i++)
            map[i] = (char)i;

        // 128–159: Windows-1252 special mappings
        map[128] = '\u20AC'; // Euro sign
        map[129] = '\uFFFD'; // undefined → replacement char
        map[130] = '\u201A'; // Single low-9 quotation mark
        map[131] = '\u0192'; // Latin small letter f with hook
        map[132] = '\u201E'; // Double low-9 quotation mark
        map[133] = '\u2026'; // Horizontal ellipsis
        map[134] = '\u2020'; // Dagger
        map[135] = '\u2021'; // Double dagger
        map[136] = '\u02C6'; // Modifier letter circumflex accent
        map[137] = '\u2030'; // Per mille sign
        map[138] = '\u0160'; // Latin capital letter S with caron
        map[139] = '\u2039'; // Single left-pointing angle quotation mark
        map[140] = '\u0152'; // Latin capital ligature OE
        map[141] = '\uFFFD'; // undefined
        map[142] = '\u017D'; // Latin capital letter Z with caron
        map[143] = '\uFFFD'; // undefined
        map[144] = '\uFFFD'; // undefined
        map[145] = '\u2018'; // Left single quotation mark
        map[146] = '\u2019'; // Right single quotation mark
        map[147] = '\u201C'; // Left double quotation mark
        map[148] = '\u201D'; // Right double quotation mark
        map[149] = '\u2022'; // Bullet
        map[150] = '\u2013'; // En dash
        map[151] = '\u2014'; // Em dash
        map[152] = '\u02DC'; // Small tilde
        map[153] = '\u2122'; // Trade mark sign
        map[154] = '\u0161'; // Latin small letter s with caron
        map[155] = '\u203A'; // Single right-pointing angle quotation mark
        map[156] = '\u0153'; // Latin small ligature oe
        map[157] = '\uFFFD'; // undefined
        map[158] = '\u017E'; // Latin small letter z with caron
        map[159] = '\u0178'; // Latin capital letter Y with diaeresis

        // 160–255: Latin-1 supplement (identity)
        for (int i = 160; i < 256; i++)
            map[i] = (char)i;

        return map;
    }

    private static Dictionary<char, byte> CreateUnicodeToWinAnsiMap()
    {
        var map = new Dictionary<char, byte>();
        // Only need reverse mappings for non-identity range (128–159)
        var winAnsiMap = WinAnsiToUnicode;
        for (int i = 128; i < 160; i++)
        {
            char c = winAnsiMap[i];
            if (c != '\uFFFD')
                map[c] = (byte)i;
        }
        return map;
    }
}
