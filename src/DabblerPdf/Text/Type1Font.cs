// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Pdf;

namespace DabblerPdf.Text;

/// <summary>
/// Represents one of the 14 standard PDF Type 1 fonts. These fonts are built into
/// every PDF viewer and do not require embedding.
/// </summary>
internal sealed class Type1Font : BaseFont
{
    private readonly int[] _widths;
    private readonly FontDescriptor _descriptor;

    public Type1Font(string fontName) : base(fontName)
    {
        _widths = BuiltinFonts.GetWidths(fontName);
        _descriptor = BuiltinFonts.GetDescriptor(fontName);
    }

    /// <summary>
    /// Measures <paramref name="c"/> the way <see cref="PdfContentByte.ShowText"/> writes it:
    /// through its Windows-1252 code, or as '?' when it is outside Windows-1252.
    /// </summary>
    public override int GetCharWidth(int c)
    {
        int code = PdfEncodings.MeasuredWinAnsiCode(c);
        return code >= 0 ? _widths[code] : 0;
    }

    public override double GetFontDescriptor(int key, double fontSize)
    {
        double raw = key switch
        {
            ASCENT => _descriptor.Ascent,
            CAPHEIGHT => _descriptor.CapHeight,
            DESCENT => _descriptor.Descent,
            ITALICANGLE => _descriptor.ItalicAngle,
            BBOXLLX => _descriptor.BBoxLLX,
            BBOXLLY => _descriptor.BBoxLLY,
            BBOXURX => _descriptor.BBoxURX,
            BBOXURY => _descriptor.BBoxURY,
            _ => 0
        };
        return raw * fontSize / 1000.0;
    }

    /// <summary>
    /// Creates a PDF font dictionary for embedding in page resources.
    /// Standard Type 1 fonts only need /Type, /Subtype, /BaseFont, and /Encoding.
    /// </summary>
    internal PdfDictionary CreateFontDictionary()
    {
        var dict = new PdfDictionary(PdfName.Font);
        dict.Put(PdfName.Subtype, PdfName.Type1);
        dict.Put(PdfName.BaseFont, new PdfName(PostscriptName));
        // Symbol and ZapfDingbats use their own built-in encodings per the PDF spec
        if (PostscriptName != Symbol && PostscriptName != ZapfDingbats)
            dict.Put(PdfName.Encoding, PdfName.WinAnsiEncoding);
        return dict;
    }
}
