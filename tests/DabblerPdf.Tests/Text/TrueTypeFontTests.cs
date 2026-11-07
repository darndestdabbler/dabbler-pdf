// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Text;

/// <summary>
/// Tests for TrueType font loading, text measurement, and PDF embedding.
/// Uses the vendored Liberation Sans Regular fixture (Fonts/LiberationSans-Regular.ttf,
/// SIL OFL 1.1, copied to the test output directory), which is metric-compatible
/// with Arial, so the tests run on any OS.
/// </summary>
public class TrueTypeFontTests
{
    private static readonly string FontPath =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");

    private static void RequireFontFixture()
    {
        if (!File.Exists(FontPath))
            Assert.Fail($"Font fixture not found at {FontPath}; the checkout or test output is broken.");
    }

    public class FontLoadingTests
    {
        [Fact]
        public void CreateFont_ArialTtf_LoadsSuccessfully()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            Assert.NotNull(font);
            // Liberation Sans Regular's PostScript name is "LiberationSans"
            Assert.Equal("LiberationSans", font.PostscriptName);
        }

        [Fact]
        public void CreateFont_ArialTtf_CachesInstances()
        {
            RequireFontFixture();

            var font1 = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            var font2 = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            Assert.Same(font1, font2);
        }

        [Fact]
        public void CreateFont_NonExistentTtf_Throws()
        {
            Assert.ThrowsAny<Exception>(() =>
                BaseFont.CreateFont(@"C:\nonexistent\font.ttf", BaseFont.WinAnsiEncoding, true));
        }
    }

    public class TextMeasurementTests
    {
        [Fact]
        public void GetWidthPoint_HelloWorld_Arial12()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            double width = font.GetWidthPoint("Hello World", 12.0);

            // Arial is metrically very similar to Helvetica.
            // "Hello World" should be approximately 60-64 points at 12pt.
            Assert.InRange(width, 55.0, 70.0);
        }

        [Fact]
        public void GetWidthPoint_EmptyString_ReturnsZero()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            Assert.Equal(0.0, font.GetWidthPoint("", 12.0));
        }

        [Fact]
        public void GetCharWidth_Space_NonZero()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            int spaceWidth = font.GetCharWidth(32);
            // Space should have a positive width (typically 200-300 units)
            Assert.True(spaceWidth > 0, $"Space width should be positive, got {spaceWidth}");
        }

        [Fact]
        public void GetCharWidth_UpperA_NonZero()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            int aWidth = font.GetCharWidth('A');
            // 'A' should have a positive width (typically 600-700 units)
            Assert.True(aWidth > 0, $"'A' width should be positive, got {aWidth}");
        }

        [Fact]
        public void GetCharWidth_OutOfRange_ReturnsZero()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            Assert.Equal(0, font.GetCharWidth(300));
            Assert.Equal(0, font.GetCharWidth(-1));
        }
    }

    public class FontDescriptorTests
    {
        [Fact]
        public void Ascent_IsPositive()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
            Assert.True(ascent > 0, $"Ascent should be positive, got {ascent}");
        }

        [Fact]
        public void Descent_IsNegative()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
            Assert.True(descent < 0, $"Descent should be negative, got {descent}");
        }

        [Fact]
        public void CapHeight_IsPositive()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            double capHeight = font.GetFontDescriptor(BaseFont.CAPHEIGHT, 12.0);
            Assert.True(capHeight > 0, $"CapHeight should be positive, got {capHeight}");
        }

        [Fact]
        public void LineHeight_CanBeComputed()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
            double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
            double lineHeight = ascent - descent;
            // Line height at 12pt should be approximately 11-14 points
            Assert.InRange(lineHeight, 9.0, 18.0);
        }

        [Fact]
        public void ItalicAngle_IsZeroForArial()
        {
            RequireFontFixture();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            double angle = font.GetFontDescriptor(BaseFont.ITALICANGLE, 1000.0);
            // Arial (non-italic) should have italic angle of 0
            Assert.Equal(0.0, angle, 1);
        }
    }

    public class PdfGenerationTests
    {
        [Fact]
        public void TrueTypeText_ProducesValidPdf()
        {
            RequireFontFixture();

            using var stream = new MemoryStream();
            var document = new DabblerPdf.Document.Document(PageSize.Letter);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();
            document.NewPage();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            var cb = writer.DirectContent;

            cb.BeginText();
            cb.SetFontAndSize(font, 14);
            cb.SetTextMatrix(72, 720);
            cb.ShowText("Hello from TrueType!");
            cb.EndText();

            document.Close();

            byte[] pdf = stream.ToArray();
            string pdfText = System.Text.Encoding.ASCII.GetString(pdf);

            // Verify PDF structure
            Assert.StartsWith("%PDF-1.4", pdfText);
            Assert.Contains("/Type /Font", pdfText);
            Assert.Contains("/Subtype /TrueType", pdfText);
            Assert.Contains("/BaseFont /LiberationSans", pdfText);
            Assert.Contains("/FontDescriptor", pdfText);
            Assert.Contains("/FontFile2", pdfText);
            Assert.Contains("/Widths", pdfText);
            Assert.Contains("BT", pdfText);
            Assert.Contains("Tj", pdfText);
            Assert.Contains("ET", pdfText);
        }

        [Fact]
        public void TrueTypeAndType1_MixedOnPage()
        {
            RequireFontFixture();

            using var stream = new MemoryStream();
            var document = new DabblerPdf.Document.Document(PageSize.Letter);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();
            document.NewPage();

            var arial = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            var helvetica = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            var cb = writer.DirectContent;

            // Write text with TrueType font
            cb.BeginText();
            cb.SetFontAndSize(arial, 12);
            cb.SetTextMatrix(72, 720);
            cb.ShowText("Arial TrueType text");
            cb.EndText();

            // Write text with built-in font
            cb.BeginText();
            cb.SetFontAndSize(helvetica, 12);
            cb.SetTextMatrix(72, 700);
            cb.ShowText("Helvetica Type1 text");
            cb.EndText();

            document.Close();

            byte[] pdf = stream.ToArray();
            string pdfText = System.Text.Encoding.ASCII.GetString(pdf);

            // Both font types should be present
            Assert.Contains("/Subtype /TrueType", pdfText);
            Assert.Contains("/Subtype /Type1", pdfText);
            Assert.Contains("/LiberationSans", pdfText);
            Assert.Contains("/Helvetica", pdfText);
        }

        [Fact]
        public void TrueType_MultiPage_ProducesValidPdf()
        {
            RequireFontFixture();

            using var stream = new MemoryStream();
            var document = new DabblerPdf.Document.Document(PageSize.Letter);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);

            // Page 1
            document.NewPage();
            var cb = writer.DirectContent;
            cb.BeginText();
            cb.SetFontAndSize(font, 12);
            cb.SetTextMatrix(72, 720);
            cb.ShowText("Page 1 - TrueType text");
            cb.EndText();

            // Page 2
            document.NewPage();
            cb.BeginText();
            cb.SetFontAndSize(font, 12);
            cb.SetTextMatrix(72, 720);
            cb.ShowText("Page 2 - TrueType text");
            cb.EndText();

            document.Close();

            byte[] pdf = stream.ToArray();
            string pdfText = System.Text.Encoding.ASCII.GetString(pdf);

            Assert.Contains("/Count 2", pdfText);
        }

        [Fact]
        public void TrueType_WithDrawing_CombinedOnPage()
        {
            RequireFontFixture();

            using var stream = new MemoryStream();
            var document = new DabblerPdf.Document.Document(PageSize.Letter);
            var writer = PdfWriter.GetInstance(document, stream);
            document.Open();

            var font = BaseFont.CreateFont(FontPath, BaseFont.WinAnsiEncoding, true);
            document.NewPage();
            var cb = writer.DirectContent;

            // Draw a filled rectangle
            cb.SetColorFill(new BaseColor(230, 230, 230));
            cb.Rectangle(72, 700, 468, 20);
            cb.Fill();

            // Draw text on top
            cb.BeginText();
            cb.SetFontAndSize(font, 12);
            cb.SetTextMatrix(76, 705);
            cb.ShowText("Text over a gray rectangle");
            cb.EndText();

            // Draw a line below
            cb.SetColorStroke(BaseColor.Black);
            cb.MoveTo(72, 698);
            cb.LineTo(540, 698);
            cb.Stroke();

            document.Close();

            byte[] pdf = stream.ToArray();
            Assert.True(pdf.Length > 100, "PDF should have meaningful content");
            string pdfText = System.Text.Encoding.ASCII.GetString(pdf);
            Assert.StartsWith("%PDF-1.4", pdfText);
            Assert.Contains("/Subtype /TrueType", pdfText);
        }
    }
}
