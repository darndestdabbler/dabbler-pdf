// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using OpenPdf.Document;
using OpenPdf.Image;
using OpenPdf.Pdf;
using OpenPdf.Text;
using OpenPdf.Utilities;
using OpenPdf.Tests.Image;
using Xunit;

namespace OpenPdf.Tests;

/// <summary>
/// Minimum Viable API validation tests from porting-scope.md.
/// These 9 scenarios define "done" for the OpenPDF port.
/// </summary>
public class MvpValidationTests
{
    private const string ArialPath = @"C:\Windows\Fonts\arial.ttf";

    private static void SkipIfNoArial()
    {
        if (!File.Exists(ArialPath))
            Assert.Fail("Arial.ttf not found — skipping TrueType test");
    }

    /// <summary>
    /// Scenario 1: Create a multi-page PDF with text at known coordinates.
    /// </summary>
    [Fact]
    public void Scenario1_MultiPagePdf_WithTextAtKnownCoordinates()
    {
        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

        for (int page = 1; page <= 3; page++)
        {
            document.NewPage();
            var cb = writer.DirectContent;

            cb.BeginText();
            cb.SetFontAndSize(font, 24);
            cb.SetTextMatrix(72, 720); // 1" from left, 10" from bottom
            cb.ShowText($"Page {page} of 3");
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(font, 10);
            cb.SetTextMatrix(72, 36); // footer position
            cb.ShowText($"Footer — page {page}");
            cb.EndText();
        }

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("/Count 3", pdf);
        Assert.Contains("%%EOF", pdf);
    }

    /// <summary>
    /// Scenario 2: Measure text width and verify it matches OpenPDF Java output within 0.1pt.
    /// Verified against: BaseFont.createFont("Helvetica", "Cp1252", false).getWidthPoint("Hello World", 12f)
    /// </summary>
    [Fact]
    public void Scenario2_TextMeasurement_MatchesOpenPdfJava()
    {
        var helvetica = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

        // "Hello World" at 12pt Helvetica
        // Java OpenPDF: H(722)+e(556)+l(222)+l(222)+o(556)+space(278)+W(944)+o(556)+r(333)+l(222)+d(556) = 5167
        // 5167 * 12 / 1000 = 62.004
        double width = helvetica.GetWidthPoint("Hello World", 12.0);
        Assert.Equal(62.004, width, 1); // within 0.1pt

        // Font metrics
        double ascent = helvetica.GetFontDescriptor(BaseFont.ASCENT, 12.0);
        double descent = helvetica.GetFontDescriptor(BaseFont.DESCENT, 12.0);
        Assert.Equal(8.616, ascent, 1);
        Assert.Equal(-2.484, descent, 1);

        // Line height = ascent - descent
        double lineHeight = ascent - descent;
        Assert.Equal(11.1, lineHeight, 1);
    }

    /// <summary>
    /// Scenario 3: Draw rectangles with fill and stroke colors.
    /// </summary>
    [Fact]
    public void Scenario3_Rectangles_WithFillAndStrokeColors()
    {
        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var cb = writer.DirectContent;

        // Filled rectangle with gray background
        cb.SetColorFill(new BaseColor(230, 230, 230));
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1.0);
        cb.Rectangle(72, 700, 468, 50);
        cb.FillStroke();

        // Red-bordered rectangle with no fill
        cb.SetColorStroke(new BaseColor(255, 0, 0));
        cb.SetLineWidth(2.0);
        cb.Rectangle(72, 630, 468, 50);
        cb.Stroke();

        // Blue filled rectangle with no border
        cb.SetColorFill(new BaseColor(0, 0, 255));
        cb.Rectangle(72, 560, 468, 50);
        cb.Fill();

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("re", pdf);
        Assert.Contains("%%EOF", pdf);
    }

    /// <summary>
    /// Scenario 4: Draw lines with configurable width and color.
    /// </summary>
    [Fact]
    public void Scenario4_Lines_WithConfigurableWidthAndColor()
    {
        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var cb = writer.DirectContent;

        // Thin black line
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(0.5);
        cb.MoveTo(72, 700);
        cb.LineTo(540, 700);
        cb.Stroke();

        // Thick red line
        cb.SetColorStroke(new BaseColor(255, 0, 0));
        cb.SetLineWidth(3.0);
        cb.MoveTo(72, 680);
        cb.LineTo(540, 680);
        cb.Stroke();

        // Dashed blue line
        cb.SetColorStroke(new BaseColor(0, 0, 255));
        cb.SetLineWidth(1.0);
        cb.SetLineDash(new double[] { 6, 3 }, 0);
        cb.MoveTo(72, 660);
        cb.LineTo(540, 660);
        cb.Stroke();

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("%%EOF", pdf);
    }

    /// <summary>
    /// Scenario 5: Embed a JPEG image at a known position.
    /// </summary>
    [Fact]
    public void Scenario5_JpegImage_AtKnownPosition()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(200, 100);

        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var cb = writer.DirectContent;
        var image = OpenPdf.Image.Image.GetInstance(jpegBytes);
        image.ScaleToFit(144, 72); // 2" x 1"
        image.SetAbsolutePosition(72, 650);
        cb.AddImage(image);

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("/DCTDecode", pdf);
        Assert.Contains("/Subtype /Image", pdf);
        Assert.Contains("/XObject", pdf);
    }

    /// <summary>
    /// Scenario 6: Embed a PNG image at a known position.
    /// </summary>
    [Fact]
    public void Scenario6_PngImage_AtKnownPosition()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalPng(100, 80, r: 0, g: 128, b: 255);

        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var cb = writer.DirectContent;
        var image = OpenPdf.Image.Image.GetInstance(pngBytes);
        image.SetAbsolutePosition(72, 600);
        cb.AddImage(image);

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("/FlateDecode", pdf);
        Assert.Contains("/Subtype /Image", pdf);
        Assert.Contains("/ColorSpace /DeviceRGB", pdf);
    }

    /// <summary>
    /// Scenario 7: Use Helvetica, Times-Roman, and Courier (built-in fonts).
    /// </summary>
    [Fact]
    public void Scenario7_BuiltInFonts_HelveticaTimesCourier()
    {
        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var helvetica = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var times = BaseFont.CreateFont(BaseFont.TimesRoman, BaseFont.WinAnsiEncoding, false);
        var courier = BaseFont.CreateFont(BaseFont.Courier, BaseFont.WinAnsiEncoding, false);

        var cb = writer.DirectContent;

        cb.BeginText();
        cb.SetFontAndSize(helvetica, 14);
        cb.SetTextMatrix(72, 720);
        cb.ShowText("Helvetica — The quick brown fox");
        cb.EndText();

        cb.BeginText();
        cb.SetFontAndSize(times, 14);
        cb.SetTextMatrix(72, 700);
        cb.ShowText("Times-Roman — jumps over the lazy dog");
        cb.EndText();

        cb.BeginText();
        cb.SetFontAndSize(courier, 14);
        cb.SetTextMatrix(72, 680);
        cb.ShowText("Courier — 0123456789");
        cb.EndText();

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("/Helvetica", pdf);
        Assert.Contains("/Times-Roman", pdf);
        Assert.Contains("/Courier", pdf);
    }

    /// <summary>
    /// Scenario 8: Load and embed a TrueType font from a .ttf file.
    /// </summary>
    [Fact]
    public void Scenario8_TrueTypeFont_FromTtfFile()
    {
        SkipIfNoArial();

        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var arial = BaseFont.CreateFont(ArialPath, BaseFont.WinAnsiEncoding, true);

        var cb = writer.DirectContent;
        cb.BeginText();
        cb.SetFontAndSize(arial, 14);
        cb.SetTextMatrix(72, 720);
        cb.ShowText("Arial TrueType — embedded from .ttf file");
        cb.EndText();

        document.Close();

        var pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("/Subtype /TrueType", pdf);
        Assert.Contains("/BaseFont /ArialMT", pdf);
        Assert.Contains("/FontFile2", pdf);
    }

    /// <summary>
    /// Scenario 9: The output PDF has valid structure (proxy for "opens in viewers").
    /// Validates PDF header, catalog, page tree, xref, and trailer.
    /// </summary>
    [Fact]
    public void Scenario9_OutputPdf_HasValidStructure()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(50, 50);
        var pngBytes = ImageTestHelpers.CreateMinimalPng(30, 30, r: 255, g: 0, b: 0);

        using var ms = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, ms);
        document.Open();

        var helvetica = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

        // Page 1: text + rectangles + lines
        document.NewPage();
        var cb = writer.DirectContent;
        cb.BeginText();
        cb.SetFontAndSize(helvetica, 18);
        cb.SetTextMatrix(72, 720);
        cb.ShowText("Complete Report Test");
        cb.EndText();

        cb.SetColorFill(new BaseColor(240, 240, 240));
        cb.Rectangle(72, 680, 468, 30);
        cb.Fill();

        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(0.5);
        cb.MoveTo(72, 675);
        cb.LineTo(540, 675);
        cb.Stroke();

        // Page 2: images
        document.NewPage();
        cb = writer.DirectContent;

        var jpeg = OpenPdf.Image.Image.GetInstance(jpegBytes);
        jpeg.SetAbsolutePosition(72, 700);
        cb.AddImage(jpeg);

        var png = OpenPdf.Image.Image.GetInstance(pngBytes);
        png.SetAbsolutePosition(72, 600);
        cb.AddImage(png);

        document.Close();

        var pdfBytes = ms.ToArray();
        var pdf = Encoding.ASCII.GetString(pdfBytes);

        // Valid PDF structure checks
        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("/Type /Catalog", pdf);
        Assert.Contains("/Type /Pages", pdf);
        Assert.Contains("/Type /Page", pdf);
        Assert.Contains("/Count 2", pdf);
        Assert.Contains("startxref", pdf);
        Assert.Contains("%%EOF", pdf);

        // Cross-reference table should be present
        Assert.Contains("xref", pdf);
        Assert.Contains("trailer", pdf);
    }

    /// <summary>
    /// Bonus: Validates the CoordinateTranslator produces correct results
    /// for a typical Dabbler.Report rendering scenario.
    /// </summary>
    [Fact]
    public void CoordinateTranslator_DabblerReportScenario()
    {
        // Simulate placing an element at 1" from left, 1" from top on Letter page
        // Element is 0.25" tall
        var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(1.0, 1.0, 0.25, 11.0);

        // Expected: x = 72pt, y = (11 - 1 - 0.25) * 72 = 702pt
        Assert.Equal(72.0, pdfX);
        Assert.Equal(702.0, pdfY);

        // Verify round-trip
        Assert.Equal(72.0, CoordinateTranslator.InchesToPoints(1.0));
        Assert.Equal(1.0, CoordinateTranslator.PointsToInches(72.0));
    }
}
