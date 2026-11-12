// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Text;

/// <summary>
/// Registry of the 14 standard PDF fonts and their metrics.
/// Width values are from the Adobe Font Metrics (AFM) files, in units of 1/1000 em.
/// </summary>
/// <remarks>
/// The Helvetica, Times and Courier tables are indexed by Windows-1252 code. Each code's
/// width is the WX of the glyph Windows-1252 names for it in the Adobe Core 14 AFM file
/// OpenPDF ships (openpdf-core/src/main/resources/org/openpdf/text/pdf/fonts/*.afm),
/// including glyphs listed unencoded (C -1), such as Euro. Codes 129, 141, 143, 144 and
/// 157, which Windows-1252 leaves undefined, are 0.
/// </remarks>
internal static class BuiltinFonts
{
    private static readonly HashSet<string> StandardFontNames =
    [
        BaseFont.Helvetica,
        BaseFont.HelveticaBold,
        BaseFont.HelveticaOblique,
        BaseFont.HelveticaBoldOblique,
        BaseFont.TimesRoman,
        BaseFont.TimesBold,
        BaseFont.TimesItalic,
        BaseFont.TimesBoldItalic,
        BaseFont.Courier,
        BaseFont.CourierBold,
        BaseFont.CourierOblique,
        BaseFont.CourierBoldOblique,
        BaseFont.Symbol,
        BaseFont.ZapfDingbats,
    ];

    public static bool IsStandardFont(string name) => StandardFontNames.Contains(name);

    /// <summary>
    /// Gets the font descriptor values for a standard font.
    /// Returns (Ascent, CapHeight, Descent, ItalicAngle, BBoxLLX, BBoxLLY, BBoxURX, BBoxURY).
    /// Values are in 1/1000 em units (unscaled).
    /// </summary>
    public static FontDescriptor GetDescriptor(string fontName) => fontName switch
    {
        BaseFont.Helvetica => new(718, 718, -207, 0, -166, -225, 1000, 931),
        BaseFont.HelveticaBold => new(718, 718, -207, 0, -170, -228, 1003, 962),
        BaseFont.HelveticaOblique => new(718, 718, -207, -12, -170, -225, 1116, 931),
        BaseFont.HelveticaBoldOblique => new(718, 718, -207, -12, -174, -228, 1114, 962),
        BaseFont.TimesRoman => new(683, 662, -217, 0, -168, -218, 1000, 898),
        BaseFont.TimesBold => new(683, 676, -217, 0, -168, -218, 1000, 935),
        BaseFont.TimesItalic => new(683, 653, -217, -15.5, -169, -217, 1010, 883),
        BaseFont.TimesBoldItalic => new(683, 669, -217, -15, -200, -218, 996, 921),
        BaseFont.Courier => new(629, 562, -157, 0, -23, -250, 715, 805),
        BaseFont.CourierBold => new(629, 562, -157, 0, -113, -250, 749, 801),
        BaseFont.CourierOblique => new(629, 562, -157, -12, -27, -250, 849, 805),
        BaseFont.CourierBoldOblique => new(629, 562, -157, -12, -57, -250, 869, 801),
        BaseFont.Symbol => new(0, 0, 0, 0, -180, -293, 1090, 1010),
        BaseFont.ZapfDingbats => new(0, 0, 0, 0, -1, -143, 981, 820),
        _ => throw new ArgumentException($"Unknown standard font: {fontName}")
    };

    /// <summary>
    /// Gets the character width table for a standard font.
    /// Index is the WinAnsi character code (0–255), value is width in 1/1000 em.
    /// </summary>
    public static int[] GetWidths(string fontName) => fontName switch
    {
        BaseFont.Helvetica => HelveticaWidths,
        BaseFont.HelveticaBold => HelveticaBoldWidths,
        BaseFont.HelveticaOblique => HelveticaWidths, // Same widths as Helvetica
        BaseFont.HelveticaBoldOblique => HelveticaBoldWidths, // Same widths as Helvetica-Bold
        BaseFont.TimesRoman => TimesRomanWidths,
        BaseFont.TimesBold => TimesBoldWidths,
        BaseFont.TimesItalic => TimesItalicWidths,
        BaseFont.TimesBoldItalic => TimesBoldItalicWidths,
        BaseFont.Courier or BaseFont.CourierBold
            or BaseFont.CourierOblique or BaseFont.CourierBoldOblique => CourierWidths,
        BaseFont.Symbol => SymbolWidths,
        BaseFont.ZapfDingbats => ZapfDingbatsWidths,
        _ => throw new ArgumentException($"Unknown standard font: {fontName}")
    };

    // ── Helvetica (from AFM data) ──────────────────────────────────
    // Characters 0–31 are control characters with 0 width.
    // Printable ASCII starts at 32 (space).
    private static readonly int[] HelveticaWidths = CreateHelveticaWidths();
    private static int[] CreateHelveticaWidths()
    {
        var w = new int[256];
        // Core printable ASCII (32–126) from Helvetica AFM
        w[32] = 278;  // space
        w[33] = 278;  // exclam
        w[34] = 355;  // quotedbl
        w[35] = 556;  // numbersign
        w[36] = 556;  // dollar
        w[37] = 889;  // percent
        w[38] = 667;  // ampersand
        w[39] = 191;  // quotesingle
        w[40] = 333;  // parenleft
        w[41] = 333;  // parenright
        w[42] = 389;  // asterisk
        w[43] = 584;  // plus
        w[44] = 278;  // comma
        w[45] = 333;  // hyphen
        w[46] = 278;  // period
        w[47] = 278;  // slash
        w[48] = 556;  // zero
        w[49] = 556;  // one
        w[50] = 556;  // two
        w[51] = 556;  // three
        w[52] = 556;  // four
        w[53] = 556;  // five
        w[54] = 556;  // six
        w[55] = 556;  // seven
        w[56] = 556;  // eight
        w[57] = 556;  // nine
        w[58] = 278;  // colon
        w[59] = 278;  // semicolon
        w[60] = 584;  // less
        w[61] = 584;  // equal
        w[62] = 584;  // greater
        w[63] = 556;  // question
        w[64] = 1015; // at
        w[65] = 667;  // A
        w[66] = 667;  // B
        w[67] = 722;  // C
        w[68] = 722;  // D
        w[69] = 667;  // E
        w[70] = 611;  // F
        w[71] = 778;  // G
        w[72] = 722;  // H
        w[73] = 278;  // I
        w[74] = 500;  // J
        w[75] = 667;  // K
        w[76] = 556;  // L
        w[77] = 833;  // M
        w[78] = 722;  // N
        w[79] = 778;  // O
        w[80] = 667;  // P
        w[81] = 778;  // Q
        w[82] = 722;  // R
        w[83] = 667;  // S
        w[84] = 611;  // T
        w[85] = 722;  // U
        w[86] = 667;  // V
        w[87] = 944;  // W
        w[88] = 667;  // X
        w[89] = 667;  // Y
        w[90] = 611;  // Z
        w[91] = 278;  // bracketleft
        w[92] = 278;  // backslash
        w[93] = 278;  // bracketright
        w[94] = 469;  // asciicircum
        w[95] = 556;  // underscore
        w[96] = 333;  // grave
        w[97] = 556;  // a
        w[98] = 556;  // b
        w[99] = 500;  // c
        w[100] = 556; // d
        w[101] = 556; // e
        w[102] = 278; // f
        w[103] = 556; // g
        w[104] = 556; // h
        w[105] = 222; // i
        w[106] = 222; // j
        w[107] = 500; // k
        w[108] = 222; // l
        w[109] = 833; // m
        w[110] = 556; // n
        w[111] = 556; // o
        w[112] = 556; // p
        w[113] = 556; // q
        w[114] = 333; // r
        w[115] = 500; // s
        w[116] = 278; // t
        w[117] = 556; // u
        w[118] = 500; // v
        w[119] = 722; // w
        w[120] = 500; // x
        w[121] = 500; // y
        w[122] = 500; // z
        w[123] = 334; // braceleft
        w[124] = 260; // bar
        w[125] = 334; // braceright
        w[126] = 584; // asciitilde

        // Windows-1252 128–159
        w[128] = 556; // Euro
        w[130] = 222; // quotesinglbase
        w[131] = 556; // florin
        w[132] = 333; // quotedblbase
        w[133] = 1000; // ellipsis
        w[134] = 556; // dagger
        w[135] = 556; // daggerdbl
        w[136] = 333; // circumflex
        w[137] = 1000; // perthousand
        w[138] = 667; // Scaron
        w[139] = 333; // guilsinglleft
        w[140] = 1000; // OE
        w[142] = 611; // Zcaron
        w[145] = 222; // quoteleft
        w[146] = 222; // quoteright
        w[147] = 333; // quotedblleft
        w[148] = 333; // quotedblright
        w[149] = 350; // bullet
        w[150] = 556; // endash
        w[151] = 1000; // emdash
        w[152] = 333; // tilde
        w[153] = 1000; // trademark
        w[154] = 500; // scaron
        w[155] = 333; // guilsinglright
        w[156] = 944; // oe
        w[158] = 500; // zcaron
        w[159] = 667; // Ydieresis

        // Extended Latin (Windows-1252 / WinAnsi, 160–255)
        w[160] = 278; // nbspace
        w[161] = 333; // exclamdown
        w[162] = 556; // cent
        w[163] = 556; // sterling
        w[164] = 556; // currency
        w[165] = 556; // yen
        w[166] = 260; // brokenbar
        w[167] = 556; // section
        w[168] = 333; // dieresis
        w[169] = 737; // copyright
        w[170] = 370; // ordfeminine
        w[171] = 556; // guillemotleft
        w[172] = 584; // logicalnot
        w[173] = 333; // softhyphen
        w[174] = 737; // registered
        w[175] = 333; // macron
        w[176] = 400; // degree
        w[177] = 584; // plusminus
        w[178] = 333; // twosuperior
        w[179] = 333; // threesuperior
        w[180] = 333; // acute
        w[181] = 556; // mu
        w[182] = 537; // paragraph
        w[183] = 278; // periodcentered
        w[184] = 333; // cedilla
        w[185] = 333; // onesuperior
        w[186] = 365; // ordmasculine
        w[187] = 556; // guillemotright
        w[188] = 834; // onequarter
        w[189] = 834; // onehalf
        w[190] = 834; // threequarters
        w[191] = 611; // questiondown
        w[192] = 667; // Agrave
        w[193] = 667; // Aacute
        w[194] = 667; // Acircumflex
        w[195] = 667; // Atilde
        w[196] = 667; // Adieresis
        w[197] = 667; // Aring
        w[198] = 1000; // AE
        w[199] = 722; // Ccedilla
        w[200] = 667; // Egrave
        w[201] = 667; // Eacute
        w[202] = 667; // Ecircumflex
        w[203] = 667; // Edieresis
        w[204] = 278; // Igrave
        w[205] = 278; // Iacute
        w[206] = 278; // Icircumflex
        w[207] = 278; // Idieresis
        w[208] = 722; // Eth
        w[209] = 722; // Ntilde
        w[210] = 778; // Ograve
        w[211] = 778; // Oacute
        w[212] = 778; // Ocircumflex
        w[213] = 778; // Otilde
        w[214] = 778; // Odieresis
        w[215] = 584; // multiply
        w[216] = 778; // Oslash
        w[217] = 722; // Ugrave
        w[218] = 722; // Uacute
        w[219] = 722; // Ucircumflex
        w[220] = 722; // Udieresis
        w[221] = 667; // Yacute
        w[222] = 667; // Thorn
        w[223] = 611; // germandbls
        w[224] = 556; // agrave
        w[225] = 556; // aacute
        w[226] = 556; // acircumflex
        w[227] = 556; // atilde
        w[228] = 556; // adieresis
        w[229] = 556; // aring
        w[230] = 889; // ae
        w[231] = 500; // ccedilla
        w[232] = 556; // egrave
        w[233] = 556; // eacute
        w[234] = 556; // ecircumflex
        w[235] = 556; // edieresis
        w[236] = 278; // igrave (corrected from 222)
        w[237] = 278; // iacute (corrected from 222)
        w[238] = 278; // icircumflex (corrected from 222)
        w[239] = 278; // idieresis (corrected from 222)
        w[240] = 556; // eth
        w[241] = 556; // ntilde
        w[242] = 556; // ograve
        w[243] = 556; // oacute
        w[244] = 556; // ocircumflex
        w[245] = 556; // otilde
        w[246] = 556; // odieresis
        w[247] = 584; // divide
        w[248] = 611; // oslash
        w[249] = 556; // ugrave
        w[250] = 556; // uacute
        w[251] = 556; // ucircumflex
        w[252] = 556; // udieresis
        w[253] = 500; // yacute
        w[254] = 556; // thorn
        w[255] = 500; // ydieresis
        return w;
    }

    // ── Helvetica-Bold ─────────────────────────────────────────────
    private static readonly int[] HelveticaBoldWidths = CreateHelveticaBoldWidths();
    private static int[] CreateHelveticaBoldWidths()
    {
        var w = new int[256];
        w[32] = 278; w[33] = 333; w[34] = 474; w[35] = 556; w[36] = 556; w[37] = 889;
        w[38] = 722; w[39] = 238; w[40] = 333; w[41] = 333; w[42] = 389; w[43] = 584;
        w[44] = 278; w[45] = 333; w[46] = 278; w[47] = 278;
        w[48] = 556; w[49] = 556; w[50] = 556; w[51] = 556; w[52] = 556;
        w[53] = 556; w[54] = 556; w[55] = 556; w[56] = 556; w[57] = 556;
        w[58] = 333; w[59] = 333; w[60] = 584; w[61] = 584; w[62] = 584;
        w[63] = 611; w[64] = 975;
        w[65] = 722; w[66] = 722; w[67] = 722; w[68] = 722; w[69] = 667;
        w[70] = 611; w[71] = 778; w[72] = 722; w[73] = 278; w[74] = 556;
        w[75] = 722; w[76] = 611; w[77] = 833; w[78] = 722; w[79] = 778;
        w[80] = 667; w[81] = 778; w[82] = 722; w[83] = 667; w[84] = 611;
        w[85] = 722; w[86] = 667; w[87] = 944; w[88] = 667; w[89] = 667;
        w[90] = 611; w[91] = 333; w[92] = 278; w[93] = 333; w[94] = 584;
        w[95] = 556; w[96] = 333;
        w[97] = 556; w[98] = 611; w[99] = 556; w[100] = 611; w[101] = 556;
        w[102] = 333; w[103] = 611; w[104] = 611; w[105] = 278; w[106] = 278;
        w[107] = 556; w[108] = 278; w[109] = 889; w[110] = 611; w[111] = 611;
        w[112] = 611; w[113] = 611; w[114] = 389; w[115] = 556; w[116] = 333;
        w[117] = 611; w[118] = 556; w[119] = 778; w[120] = 556; w[121] = 556;
        w[122] = 500; w[123] = 389; w[124] = 280; w[125] = 389; w[126] = 584;
        // Windows-1252 128–159
        w[128] = 556; w[130] = 278; w[131] = 556; w[132] = 500; w[133] = 1000; w[134] = 556;
        w[135] = 556; w[136] = 333; w[137] = 1000; w[138] = 667; w[139] = 333; w[140] = 1000;
        w[142] = 611; w[145] = 278; w[146] = 278; w[147] = 500; w[148] = 500; w[149] = 350;
        w[150] = 556; w[151] = 1000; w[152] = 333; w[153] = 1000; w[154] = 556; w[155] = 333;
        w[156] = 944; w[158] = 500; w[159] = 667;
        // Extended Latin (160-255 from AFM)
        w[160] = 278; w[161] = 333; w[162] = 556; w[163] = 556; w[164] = 556;
        w[165] = 556; w[166] = 280; w[167] = 556; w[168] = 333; w[169] = 737;
        w[170] = 370; w[171] = 556; w[172] = 584; w[173] = 333; w[174] = 737;
        w[175] = 333; w[176] = 400; w[177] = 584; w[178] = 333; w[179] = 333;
        w[180] = 333; w[181] = 611; w[182] = 556; w[183] = 278; w[184] = 333;
        w[185] = 333; w[186] = 365; w[187] = 556; w[188] = 834; w[189] = 834;
        w[190] = 834; w[191] = 611;
        w[192] = 722; w[193] = 722; w[194] = 722; w[195] = 722; w[196] = 722;
        w[197] = 722; w[198] = 1000; w[199] = 722; w[200] = 667; w[201] = 667;
        w[202] = 667; w[203] = 667; w[204] = 278; w[205] = 278; w[206] = 278;
        w[207] = 278; w[208] = 722; w[209] = 722; w[210] = 778; w[211] = 778;
        w[212] = 778; w[213] = 778; w[214] = 778; w[215] = 584; w[216] = 778;
        w[217] = 722; w[218] = 722; w[219] = 722; w[220] = 722; w[221] = 667;
        w[222] = 667; w[223] = 611;
        w[224] = 556; w[225] = 556; w[226] = 556; w[227] = 556; w[228] = 556;
        w[229] = 556; w[230] = 889; w[231] = 556; w[232] = 556; w[233] = 556;
        w[234] = 556; w[235] = 556; w[236] = 278; w[237] = 278; w[238] = 278;
        w[239] = 278; w[240] = 611; w[241] = 611; w[242] = 611; w[243] = 611;
        w[244] = 611; w[245] = 611; w[246] = 611; w[247] = 584; w[248] = 611;
        w[249] = 611; w[250] = 611; w[251] = 611; w[252] = 611; w[253] = 556;
        w[254] = 611; w[255] = 556;
        return w;
    }

    // ── Times-Roman ────────────────────────────────────────────────
    private static readonly int[] TimesRomanWidths = CreateTimesRomanWidths();
    private static int[] CreateTimesRomanWidths()
    {
        var w = new int[256];
        w[32] = 250; w[33] = 333; w[34] = 408; w[35] = 500; w[36] = 500; w[37] = 833;
        w[38] = 778; w[39] = 180; w[40] = 333; w[41] = 333; w[42] = 500; w[43] = 564;
        w[44] = 250; w[45] = 333; w[46] = 250; w[47] = 278;
        w[48] = 500; w[49] = 500; w[50] = 500; w[51] = 500; w[52] = 500;
        w[53] = 500; w[54] = 500; w[55] = 500; w[56] = 500; w[57] = 500;
        w[58] = 278; w[59] = 278; w[60] = 564; w[61] = 564; w[62] = 564;
        w[63] = 444; w[64] = 921;
        w[65] = 722; w[66] = 667; w[67] = 667; w[68] = 722; w[69] = 611;
        w[70] = 556; w[71] = 722; w[72] = 722; w[73] = 333; w[74] = 389;
        w[75] = 722; w[76] = 611; w[77] = 889; w[78] = 722; w[79] = 722;
        w[80] = 556; w[81] = 722; w[82] = 667; w[83] = 556; w[84] = 611;
        w[85] = 722; w[86] = 722; w[87] = 944; w[88] = 722; w[89] = 722;
        w[90] = 611; w[91] = 333; w[92] = 278; w[93] = 333; w[94] = 469;
        w[95] = 500; w[96] = 333;
        w[97] = 444; w[98] = 500; w[99] = 444; w[100] = 500; w[101] = 444;
        w[102] = 333; w[103] = 500; w[104] = 500; w[105] = 278; w[106] = 278;
        w[107] = 500; w[108] = 278; w[109] = 778; w[110] = 500; w[111] = 500;
        w[112] = 500; w[113] = 500; w[114] = 333; w[115] = 389; w[116] = 278;
        w[117] = 500; w[118] = 500; w[119] = 722; w[120] = 500; w[121] = 500;
        w[122] = 444; w[123] = 480; w[124] = 200; w[125] = 480; w[126] = 541;
        // Windows-1252 128–159
        w[128] = 500; w[130] = 333; w[131] = 500; w[132] = 444; w[133] = 1000; w[134] = 500;
        w[135] = 500; w[136] = 333; w[137] = 1000; w[138] = 556; w[139] = 333; w[140] = 889;
        w[142] = 611; w[145] = 333; w[146] = 333; w[147] = 444; w[148] = 444; w[149] = 350;
        w[150] = 500; w[151] = 1000; w[152] = 333; w[153] = 980; w[154] = 389; w[155] = 333;
        w[156] = 722; w[158] = 444; w[159] = 722;
        // Extended Latin (160-255 from AFM)
        w[160] = 250; w[161] = 333; w[162] = 500; w[163] = 500; w[164] = 500;
        w[165] = 500; w[166] = 200; w[167] = 500; w[168] = 333; w[169] = 760;
        w[170] = 276; w[171] = 500; w[172] = 564; w[173] = 333; w[174] = 760;
        w[175] = 333; w[176] = 400; w[177] = 564; w[178] = 300; w[179] = 300;
        w[180] = 333; w[181] = 500; w[182] = 453; w[183] = 250; w[184] = 333;
        w[185] = 300; w[186] = 310; w[187] = 500; w[188] = 750; w[189] = 750;
        w[190] = 750; w[191] = 444;
        w[192] = 722; w[193] = 722; w[194] = 722; w[195] = 722; w[196] = 722;
        w[197] = 722; w[198] = 889; w[199] = 667; w[200] = 611; w[201] = 611;
        w[202] = 611; w[203] = 611; w[204] = 333; w[205] = 333; w[206] = 333;
        w[207] = 333; w[208] = 722; w[209] = 722; w[210] = 722; w[211] = 722;
        w[212] = 722; w[213] = 722; w[214] = 722; w[215] = 564; w[216] = 722;
        w[217] = 722; w[218] = 722; w[219] = 722; w[220] = 722; w[221] = 722;
        w[222] = 556; w[223] = 500;
        w[224] = 444; w[225] = 444; w[226] = 444; w[227] = 444; w[228] = 444;
        w[229] = 444; w[230] = 667; w[231] = 444; w[232] = 444; w[233] = 444;
        w[234] = 444; w[235] = 444; w[236] = 278; w[237] = 278; w[238] = 278;
        w[239] = 278; w[240] = 500; w[241] = 500; w[242] = 500; w[243] = 500;
        w[244] = 500; w[245] = 500; w[246] = 500; w[247] = 564; w[248] = 500;
        w[249] = 500; w[250] = 500; w[251] = 500; w[252] = 500; w[253] = 500;
        w[254] = 500; w[255] = 500;
        return w;
    }

    // ── Times-Bold ─────────────────────────────────────────────────
    private static readonly int[] TimesBoldWidths = CreateTimesBoldWidths();
    private static int[] CreateTimesBoldWidths()
    {
        var w = new int[256];
        w[32] = 250; w[33] = 333; w[34] = 555; w[35] = 500; w[36] = 500; w[37] = 1000;
        w[38] = 833; w[39] = 278; w[40] = 333; w[41] = 333; w[42] = 500; w[43] = 570;
        w[44] = 250; w[45] = 333; w[46] = 250; w[47] = 278;
        w[48] = 500; w[49] = 500; w[50] = 500; w[51] = 500; w[52] = 500;
        w[53] = 500; w[54] = 500; w[55] = 500; w[56] = 500; w[57] = 500;
        w[58] = 333; w[59] = 333; w[60] = 570; w[61] = 570; w[62] = 570;
        w[63] = 500; w[64] = 930;
        w[65] = 722; w[66] = 667; w[67] = 722; w[68] = 722; w[69] = 667;
        w[70] = 611; w[71] = 778; w[72] = 778; w[73] = 389; w[74] = 500;
        w[75] = 778; w[76] = 667; w[77] = 944; w[78] = 722; w[79] = 778;
        w[80] = 611; w[81] = 778; w[82] = 722; w[83] = 556; w[84] = 667;
        w[85] = 722; w[86] = 722; w[87] = 1000; w[88] = 722; w[89] = 722;
        w[90] = 667; w[91] = 333; w[92] = 278; w[93] = 333; w[94] = 581;
        w[95] = 500; w[96] = 333;
        w[97] = 500; w[98] = 556; w[99] = 444; w[100] = 556; w[101] = 444;
        w[102] = 333; w[103] = 500; w[104] = 556; w[105] = 278; w[106] = 333;
        w[107] = 556; w[108] = 278; w[109] = 833; w[110] = 556; w[111] = 500;
        w[112] = 556; w[113] = 556; w[114] = 444; w[115] = 389; w[116] = 333;
        w[117] = 556; w[118] = 500; w[119] = 722; w[120] = 500; w[121] = 500;
        w[122] = 444; w[123] = 394; w[124] = 220; w[125] = 394; w[126] = 520;
        // Windows-1252 128–159
        w[128] = 500; w[130] = 333; w[131] = 500; w[132] = 500; w[133] = 1000; w[134] = 500;
        w[135] = 500; w[136] = 333; w[137] = 1000; w[138] = 556; w[139] = 333; w[140] = 1000;
        w[142] = 667; w[145] = 333; w[146] = 333; w[147] = 500; w[148] = 500; w[149] = 350;
        w[150] = 500; w[151] = 1000; w[152] = 333; w[153] = 1000; w[154] = 389; w[155] = 333;
        w[156] = 722; w[158] = 444; w[159] = 722;
        // Extended Latin (160-255 from AFM)
        w[160] = 250; w[161] = 333; w[162] = 500; w[163] = 500; w[164] = 500;
        w[165] = 500; w[166] = 220; w[167] = 500; w[168] = 333; w[169] = 747;
        w[170] = 300; w[171] = 500; w[172] = 570; w[173] = 333; w[174] = 747;
        w[175] = 333; w[176] = 400; w[177] = 570; w[178] = 300; w[179] = 300;
        w[180] = 333; w[181] = 556; w[182] = 540; w[183] = 250; w[184] = 333;
        w[185] = 300; w[186] = 330; w[187] = 500; w[188] = 750; w[189] = 750;
        w[190] = 750; w[191] = 500;
        w[192] = 722; w[193] = 722; w[194] = 722; w[195] = 722;
        w[196] = 722; w[197] = 722; w[198] = 1000; w[199] = 722;
        w[200] = 667; w[201] = 667; w[202] = 667; w[203] = 667;
        w[204] = 389; w[205] = 389; w[206] = 389; w[207] = 389;
        w[208] = 722; w[209] = 722; w[210] = 778; w[211] = 778;
        w[212] = 778; w[213] = 778; w[214] = 778; w[215] = 570; w[216] = 778;
        w[217] = 722; w[218] = 722; w[219] = 722; w[220] = 722;
        w[221] = 722; w[222] = 611; w[223] = 556;
        w[224] = 500; w[225] = 500; w[226] = 500; w[227] = 500; w[228] = 500;
        w[229] = 500; w[230] = 722; w[231] = 444; w[232] = 444; w[233] = 444;
        w[234] = 444; w[235] = 444; w[236] = 278; w[237] = 278; w[238] = 278;
        w[239] = 278; w[240] = 500; w[241] = 556; w[242] = 500; w[243] = 500;
        w[244] = 500; w[245] = 500; w[246] = 500; w[247] = 570; w[248] = 500;
        w[249] = 556; w[250] = 556; w[251] = 556; w[252] = 556; w[253] = 500;
        w[254] = 556; w[255] = 500;
        return w;
    }

    // ── Times-Italic ───────────────────────────────────────────────
    private static readonly int[] TimesItalicWidths = CreateTimesItalicWidths();
    private static int[] CreateTimesItalicWidths()
    {
        var w = new int[256];
        w[32] = 250; w[33] = 333; w[34] = 420; w[35] = 500; w[36] = 500; w[37] = 833;
        w[38] = 778; w[39] = 214; w[40] = 333; w[41] = 333; w[42] = 500; w[43] = 675;
        w[44] = 250; w[45] = 333; w[46] = 250; w[47] = 278;
        w[48] = 500; w[49] = 500; w[50] = 500; w[51] = 500; w[52] = 500;
        w[53] = 500; w[54] = 500; w[55] = 500; w[56] = 500; w[57] = 500;
        w[58] = 333; w[59] = 333; w[60] = 675; w[61] = 675; w[62] = 675;
        w[63] = 500; w[64] = 920;
        w[65] = 611; w[66] = 611; w[67] = 667; w[68] = 722; w[69] = 611;
        w[70] = 611; w[71] = 722; w[72] = 722; w[73] = 333; w[74] = 444;
        w[75] = 667; w[76] = 556; w[77] = 833; w[78] = 667; w[79] = 722;
        w[80] = 611; w[81] = 722; w[82] = 611; w[83] = 500; w[84] = 556;
        w[85] = 722; w[86] = 611; w[87] = 833; w[88] = 611; w[89] = 556;
        w[90] = 556; w[91] = 389; w[92] = 278; w[93] = 389; w[94] = 422;
        w[95] = 500; w[96] = 333;
        w[97] = 500; w[98] = 500; w[99] = 444; w[100] = 500; w[101] = 444;
        w[102] = 278; w[103] = 500; w[104] = 500; w[105] = 278; w[106] = 278;
        w[107] = 444; w[108] = 278; w[109] = 722; w[110] = 500; w[111] = 500;
        w[112] = 500; w[113] = 500; w[114] = 389; w[115] = 389; w[116] = 278;
        w[117] = 500; w[118] = 444; w[119] = 667; w[120] = 444; w[121] = 444;
        w[122] = 389; w[123] = 400; w[124] = 275; w[125] = 400; w[126] = 541;
        // Windows-1252 128–159
        w[128] = 500; w[130] = 333; w[131] = 500; w[132] = 556; w[133] = 889; w[134] = 500;
        w[135] = 500; w[136] = 333; w[137] = 1000; w[138] = 500; w[139] = 333; w[140] = 944;
        w[142] = 556; w[145] = 333; w[146] = 333; w[147] = 556; w[148] = 556; w[149] = 350;
        w[150] = 500; w[151] = 889; w[152] = 333; w[153] = 980; w[154] = 389; w[155] = 333;
        w[156] = 667; w[158] = 389; w[159] = 556;
        // Extended Latin (160-255 from AFM)
        w[160] = 250; w[161] = 389; w[162] = 500; w[163] = 500; w[164] = 500;
        w[165] = 500; w[166] = 275; w[167] = 500; w[168] = 333; w[169] = 760;
        w[170] = 276; w[171] = 500; w[172] = 675; w[173] = 333; w[174] = 760;
        w[175] = 333; w[176] = 400; w[177] = 675; w[178] = 300; w[179] = 300;
        w[180] = 333; w[181] = 500; w[182] = 523; w[183] = 250; w[184] = 333;
        w[185] = 300; w[186] = 310; w[187] = 500; w[188] = 750; w[189] = 750;
        w[190] = 750; w[191] = 500;
        w[192] = 611; w[193] = 611; w[194] = 611; w[195] = 611; w[196] = 611;
        w[197] = 611; w[198] = 889; w[199] = 667;
        w[200] = 611; w[201] = 611; w[202] = 611; w[203] = 611;
        w[204] = 333; w[205] = 333; w[206] = 333; w[207] = 333;
        w[208] = 722; w[209] = 667; w[210] = 722; w[211] = 722;
        w[212] = 722; w[213] = 722; w[214] = 722; w[215] = 675; w[216] = 722;
        w[217] = 722; w[218] = 722; w[219] = 722; w[220] = 722;
        w[221] = 556; w[222] = 611; w[223] = 500;
        w[224] = 500; w[225] = 500; w[226] = 500; w[227] = 500; w[228] = 500;
        w[229] = 500; w[230] = 667; w[231] = 444; w[232] = 444; w[233] = 444;
        w[234] = 444; w[235] = 444; w[236] = 278; w[237] = 278; w[238] = 278;
        w[239] = 278; w[240] = 500; w[241] = 500; w[242] = 500; w[243] = 500;
        w[244] = 500; w[245] = 500; w[246] = 500; w[247] = 675; w[248] = 500;
        w[249] = 500; w[250] = 500; w[251] = 500; w[252] = 500; w[253] = 444;
        w[254] = 500; w[255] = 444;
        return w;
    }

    // ── Times-BoldItalic ───────────────────────────────────────────
    private static readonly int[] TimesBoldItalicWidths = CreateTimesBoldItalicWidths();
    private static int[] CreateTimesBoldItalicWidths()
    {
        var w = new int[256];
        w[32] = 250; w[33] = 389; w[34] = 555; w[35] = 500; w[36] = 500; w[37] = 833;
        w[38] = 778; w[39] = 278; w[40] = 333; w[41] = 333; w[42] = 500; w[43] = 570;
        w[44] = 250; w[45] = 333; w[46] = 250; w[47] = 278;
        w[48] = 500; w[49] = 500; w[50] = 500; w[51] = 500; w[52] = 500;
        w[53] = 500; w[54] = 500; w[55] = 500; w[56] = 500; w[57] = 500;
        w[58] = 333; w[59] = 333; w[60] = 570; w[61] = 570; w[62] = 570;
        w[63] = 500; w[64] = 832;
        w[65] = 667; w[66] = 667; w[67] = 667; w[68] = 722; w[69] = 667;
        w[70] = 667; w[71] = 722; w[72] = 778; w[73] = 389; w[74] = 500;
        w[75] = 667; w[76] = 611; w[77] = 889; w[78] = 722; w[79] = 722;
        w[80] = 611; w[81] = 722; w[82] = 667; w[83] = 556; w[84] = 611;
        w[85] = 722; w[86] = 667; w[87] = 889; w[88] = 667; w[89] = 611;
        w[90] = 611; w[91] = 333; w[92] = 278; w[93] = 333; w[94] = 570;
        w[95] = 500; w[96] = 333;
        w[97] = 500; w[98] = 500; w[99] = 444; w[100] = 500; w[101] = 444;
        w[102] = 333; w[103] = 500; w[104] = 556; w[105] = 278; w[106] = 278;
        w[107] = 500; w[108] = 278; w[109] = 778; w[110] = 556; w[111] = 500;
        w[112] = 500; w[113] = 500; w[114] = 389; w[115] = 389; w[116] = 278;
        w[117] = 556; w[118] = 444; w[119] = 667; w[120] = 500; w[121] = 444;
        w[122] = 389; w[123] = 348; w[124] = 220; w[125] = 348; w[126] = 570;
        // Windows-1252 128–159
        w[128] = 500; w[130] = 333; w[131] = 500; w[132] = 500; w[133] = 1000; w[134] = 500;
        w[135] = 500; w[136] = 333; w[137] = 1000; w[138] = 556; w[139] = 333; w[140] = 944;
        w[142] = 611; w[145] = 333; w[146] = 333; w[147] = 500; w[148] = 500; w[149] = 350;
        w[150] = 500; w[151] = 1000; w[152] = 333; w[153] = 1000; w[154] = 389; w[155] = 333;
        w[156] = 722; w[158] = 389; w[159] = 611;
        // Extended Latin (160-255 from AFM)
        w[160] = 250; w[161] = 389; w[162] = 500; w[163] = 500; w[164] = 500;
        w[165] = 500; w[166] = 220; w[167] = 500; w[168] = 333; w[169] = 747;
        w[170] = 266; w[171] = 500; w[172] = 606; w[173] = 333; w[174] = 747;
        w[175] = 333; w[176] = 400; w[177] = 570; w[178] = 300; w[179] = 300;
        w[180] = 333; w[181] = 576; w[182] = 500; w[183] = 250; w[184] = 333;
        w[185] = 300; w[186] = 300; w[187] = 500; w[188] = 750; w[189] = 750;
        w[190] = 750; w[191] = 500;
        w[192] = 667; w[193] = 667; w[194] = 667; w[195] = 667; w[196] = 667;
        w[197] = 667; w[198] = 944; w[199] = 667;
        w[200] = 667; w[201] = 667; w[202] = 667; w[203] = 667;
        w[204] = 389; w[205] = 389; w[206] = 389; w[207] = 389;
        w[208] = 722; w[209] = 722; w[210] = 722; w[211] = 722;
        w[212] = 722; w[213] = 722; w[214] = 722; w[215] = 570; w[216] = 722;
        w[217] = 722; w[218] = 722; w[219] = 722; w[220] = 722;
        w[221] = 611; w[222] = 611; w[223] = 500;
        w[224] = 500; w[225] = 500; w[226] = 500; w[227] = 500; w[228] = 500;
        w[229] = 500; w[230] = 722; w[231] = 444; w[232] = 444; w[233] = 444;
        w[234] = 444; w[235] = 444; w[236] = 278; w[237] = 278; w[238] = 278;
        w[239] = 278; w[240] = 500; w[241] = 556; w[242] = 500; w[243] = 500;
        w[244] = 500; w[245] = 500; w[246] = 500; w[247] = 570; w[248] = 500;
        w[249] = 556; w[250] = 556; w[251] = 556; w[252] = 556; w[253] = 444;
        w[254] = 500; w[255] = 444;
        return w;
    }

    // ── Courier (all characters are 600 units wide) ────────────────
    private static readonly int[] CourierWidths = CreateCourierWidths();
    private static int[] CreateCourierWidths()
    {
        var w = new int[256];
        for (int i = 32; i < 256; i++)
            w[i] = 600;
        // The codes Windows-1252 leaves undefined
        foreach (int i in new[] { 129, 141, 143, 144, 157 })
            w[i] = 0;
        return w;
    }

    // ── Symbol (partial — printable range) ─────────────────────────
    private static readonly int[] SymbolWidths = CreateSymbolWidths();
    private static int[] CreateSymbolWidths()
    {
        var w = new int[256];
        w[32] = 250; w[33] = 333; w[34] = 713; w[35] = 500; w[36] = 549;
        w[37] = 833; w[38] = 778; w[39] = 439; w[40] = 333; w[41] = 333;
        w[42] = 500; w[43] = 549; w[44] = 250; w[45] = 549; w[46] = 250;
        w[47] = 278;
        w[48] = 500; w[49] = 500; w[50] = 500; w[51] = 500; w[52] = 500;
        w[53] = 500; w[54] = 500; w[55] = 500; w[56] = 500; w[57] = 500;
        // Most Symbol characters use default widths — this is a minimal set
        return w;
    }

    // ── ZapfDingbats (partial — printable range) ───────────────────
    private static readonly int[] ZapfDingbatsWidths = CreateZapfDingbatsWidths();
    private static int[] CreateZapfDingbatsWidths()
    {
        var w = new int[256];
        w[32] = 278;
        // Dingbats characters 33-126 have varied widths; use a common default
        for (int i = 33; i <= 126; i++)
            w[i] = 816; // Approximate average
        return w;
    }
}

/// <summary>
/// Font descriptor metrics (all in 1/1000 em units, unscaled).
/// </summary>
internal readonly record struct FontDescriptor(
    double Ascent,
    double CapHeight,
    double Descent,
    double ItalicAngle,
    double BBoxLLX,
    double BBoxLLY,
    double BBoxURX,
    double BBoxURY);
