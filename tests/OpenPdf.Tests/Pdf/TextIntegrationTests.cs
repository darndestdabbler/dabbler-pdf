// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using OpenPdf.Document;
using OpenPdf.Pdf;
using OpenPdf.Text;
using Xunit;

namespace OpenPdf.Tests.Pdf;

public class TextIntegrationTests
{
    [Fact]
    public void TextAtPosition_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var cb = writer.DirectContent;
        cb.BeginText();
        cb.SetFontAndSize(font, 12);
        cb.SetTextMatrix(72, 720);
        cb.ShowText("Hello World");
        cb.EndText();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
        // Should have font resource
        Assert.Contains("/Font", pdfText);
        Assert.Contains("/Helvetica", pdfText);
        // Should have text operators in content stream
        Assert.Contains("BT", pdfText);
        Assert.Contains("Tj", pdfText);
        Assert.Contains("ET", pdfText);
    }

    [Fact]
    public void MultipleTextBlocks_WithDifferentFonts()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();

        var helvetica = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var times = BaseFont.CreateFont(BaseFont.TimesRoman, BaseFont.WinAnsiEncoding, false);
        var cb = writer.DirectContent;

        // First text block with Helvetica
        cb.BeginText();
        cb.SetFontAndSize(helvetica, 12);
        cb.SetTextMatrix(72, 720);
        cb.ShowText("Helvetica text");
        cb.EndText();

        // Second text block with Times
        cb.BeginText();
        cb.SetFontAndSize(times, 14);
        cb.SetTextMatrix(72, 700);
        cb.ShowText("Times text");
        cb.EndText();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.Contains("/Helvetica", pdfText);
        Assert.Contains("/Times-Roman", pdfText);
    }

    [Fact]
    public void TextWithDrawing_CombinedOnPage()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var cb = writer.DirectContent;

        // Draw background rectangle
        cb.SetColorFill(new BaseColor(230, 230, 230));
        cb.Rectangle(72, 700, 468, 20);
        cb.Fill();

        // Draw text over it
        cb.BeginText();
        cb.SetFontAndSize(font, 12);
        cb.SetColorFill(BaseColor.Black);
        cb.SetTextMatrix(76, 704);
        cb.ShowText("Text over gray background");
        cb.EndText();

        // Draw a border
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(0.5);
        cb.Rectangle(72, 700, 468, 20);
        cb.Stroke();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
    }

    [Fact]
    public void MultiPage_WithText_ProducesValidStructure()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        var font = BaseFont.CreateFont(BaseFont.CourierBold, BaseFont.WinAnsiEncoding, false);

        for (int i = 1; i <= 3; i++)
        {
            doc.NewPage();
            var cb = writer.DirectContent;
            cb.BeginText();
            cb.SetFontAndSize(font, 10);
            cb.SetTextMatrix(72, 720);
            cb.ShowText($"Page {i}");
            cb.EndText();
        }

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.Contains("/Count 3", pdfText);
        Assert.Contains("/Courier-Bold", pdfText);
    }
}
