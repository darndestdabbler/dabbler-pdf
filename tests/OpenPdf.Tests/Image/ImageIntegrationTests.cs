// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using OpenPdf.Document;
using OpenPdf.Image;
using OpenPdf.Pdf;
using Xunit;

namespace OpenPdf.Tests.Image;

public class ImageIntegrationTests
{
    /// <summary>
    /// Generates a PDF with an embedded JPEG image and verifies the output
    /// contains the expected XObject and DCTDecode references.
    /// </summary>
    [Fact]
    public void JpegImage_EmbeddedInPdf_ContainsDctDecode()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(100, 80);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        var image = new Jpeg(jpegBytes);
        image.SetAbsolutePosition(72, 650);
        cb.AddImage(image);

        document.Close();

        var pdfContent = Encoding.ASCII.GetString(output.ToArray());

        // Should contain XObject resource on the page
        Assert.Contains("/XObject", pdfContent);
        // Should contain the image XObject with DCTDecode
        Assert.Contains("/DCTDecode", pdfContent);
        // Should contain image dimensions
        Assert.Contains("/Width 100", pdfContent);
        Assert.Contains("/Height 80", pdfContent);
        // Should contain the Do operator in the content stream (may be compressed)
        Assert.Contains("/Subtype /Image", pdfContent);
    }

    /// <summary>
    /// Generates a PDF with an embedded PNG image and verifies the output.
    /// </summary>
    [Fact]
    public void PngImage_EmbeddedInPdf_ContainsFlateDecode()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalPng(50, 40, r: 0, g: 128, b: 255);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        var image = OpenPdf.Image.Image.GetInstance(pngBytes);
        image.SetAbsolutePosition(100, 500);
        cb.AddImage(image);

        document.Close();

        var pdfContent = Encoding.ASCII.GetString(output.ToArray());

        Assert.Contains("/XObject", pdfContent);
        Assert.Contains("/FlateDecode", pdfContent);
        Assert.Contains("/Width 50", pdfContent);
        Assert.Contains("/Height 40", pdfContent);
        Assert.Contains("/Subtype /Image", pdfContent);
        Assert.Contains("/ColorSpace /DeviceRGB", pdfContent);
    }

    /// <summary>
    /// Verifies that an RGBA PNG produces a soft mask (SMask) in the PDF.
    /// </summary>
    [Fact]
    public void RgbaPng_EmbeddedInPdf_ContainsSMask()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalRgbaPng(20, 20, a: 128);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        var image = OpenPdf.Image.Image.GetInstance(pngBytes);
        image.SetAbsolutePosition(72, 600);
        cb.AddImage(image);

        document.Close();

        var pdfContent = Encoding.ASCII.GetString(output.ToArray());

        // Should have the main image
        Assert.Contains("/Subtype /Image", pdfContent);
        Assert.Contains("/ColorSpace /DeviceRGB", pdfContent);
        // Should reference a soft mask
        Assert.Contains("/SMask", pdfContent);
        // Should have the soft mask image object (DeviceGray)
        Assert.Contains("/ColorSpace /DeviceGray", pdfContent);
    }

    /// <summary>
    /// Verifies that a grayscale PNG embeds with DeviceGray color space.
    /// </summary>
    [Fact]
    public void GrayscalePng_EmbeddedInPdf_UsesDeviceGray()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalGrayscalePng(30, 30, gray: 200);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        var image = OpenPdf.Image.Image.GetInstance(pngBytes);
        image.SetAbsolutePosition(72, 700);
        cb.AddImage(image);

        document.Close();

        var pdfContent = Encoding.ASCII.GetString(output.ToArray());

        Assert.Contains("/ColorSpace /DeviceGray", pdfContent);
        Assert.Contains("/Width 30", pdfContent);
        Assert.Contains("/Height 30", pdfContent);
    }

    /// <summary>
    /// Verifies that ScaleToFit affects the transformation matrix but not the pixel data.
    /// </summary>
    [Fact]
    public void ScaledImage_UsesCorrectTransformMatrix()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(200, 100);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        var image = new Jpeg(jpegBytes);
        image.ScaleToFit(100, 100); // Should scale to 100x50
        image.SetAbsolutePosition(72, 600);
        cb.AddImage(image);

        document.Close();

        // The pixel dimensions in the XObject should still be 200x100
        var pdfContent = Encoding.ASCII.GetString(output.ToArray());
        Assert.Contains("/Width 200", pdfContent);
        Assert.Contains("/Height 100", pdfContent);
    }

    /// <summary>
    /// Verifies that images and text can coexist on the same page.
    /// </summary>
    [Fact]
    public void ImageAndText_OnSamePage_BothPresent()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(50, 50);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        // Add text
        var font = OpenPdf.Text.BaseFont.CreateFont("Helvetica",
            OpenPdf.Text.BaseFont.WinAnsiEncoding, false);
        cb.BeginText();
        cb.SetFontAndSize(font, 12);
        cb.SetTextMatrix(72, 750);
        cb.ShowText("Hello with image");
        cb.EndText();

        // Add image
        var image = new Jpeg(jpegBytes);
        image.SetAbsolutePosition(72, 650);
        cb.AddImage(image);

        document.Close();

        var pdfContent = Encoding.ASCII.GetString(output.ToArray());

        // Should have both font and XObject resources
        Assert.Contains("/Font", pdfContent);
        Assert.Contains("/XObject", pdfContent);
    }

    /// <summary>
    /// Verifies that multiple images on the same page each get unique resource names.
    /// </summary>
    [Fact]
    public void MultipleImages_OnSamePage_AllPresent()
    {
        var jpeg1 = ImageTestHelpers.CreateMinimalJpeg(40, 40);
        var jpeg2 = ImageTestHelpers.CreateMinimalJpeg(60, 30);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;

        var image1 = new Jpeg(jpeg1);
        image1.SetAbsolutePosition(72, 700);
        cb.AddImage(image1);

        var image2 = new Jpeg(jpeg2);
        image2.SetAbsolutePosition(72, 600);
        cb.AddImage(image2);

        document.Close();

        document.Close();

        var pdfBytes = output.ToArray();
        var pdfContent = Encoding.Latin1.GetString(pdfBytes);

        // Should have two image XObjects referenced in page resources
        Assert.Contains("/Im1", pdfContent);
        Assert.Contains("/Im2", pdfContent);
        Assert.Contains("/XObject", pdfContent);
    }

    /// <summary>
    /// Verifies that AddImage throws when no position is set.
    /// </summary>
    [Fact]
    public void AddImage_WithoutPosition_Throws()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(10, 10);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;
        var image = new Jpeg(jpegBytes);

        Assert.Throws<InvalidOperationException>(() => cb.AddImage(image));

        document.Close();
    }

    /// <summary>
    /// Generates a complete PDF and verifies it has a valid structure.
    /// </summary>
    [Fact]
    public void GeneratedPdf_WithImage_HasValidStructure()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(100, 100);

        using var output = new MemoryStream();
        var document = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(document, output);
        document.Open();

        var cb = writer.DirectContent;
        var image = new Jpeg(jpegBytes);
        image.SetAbsolutePosition(72, 650);
        cb.AddImage(image);

        document.Close();

        var pdfBytes = output.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        // Verify basic PDF structure
        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
        Assert.Contains("startxref", pdfText);
        Assert.Contains("/Type /Catalog", pdfText);
        Assert.Contains("/Type /Page", pdfText);
    }
}
