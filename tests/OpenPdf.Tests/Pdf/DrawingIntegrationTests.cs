// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using OpenPdf.Document;
using OpenPdf.Pdf;
using Xunit;

namespace OpenPdf.Tests.Pdf;

public class DrawingIntegrationTests
{
    [Fact]
    public void DrawRectangle_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();

        var cb = writer.DirectContent;
        cb.SetColorFill(new BaseColor(230, 230, 230));
        cb.SetColorStroke(BaseColor.Black);
        cb.Rectangle(72, 700, 468, 20);
        cb.FillStroke();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        // Should be a valid PDF
        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);

        // Should contain the rectangle operator in a content stream
        Assert.Contains("re", pdfText);
        Assert.Contains("B", pdfText); // FillStroke
    }

    [Fact]
    public void DrawLine_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();

        var cb = writer.DirectContent;
        cb.SetLineWidth(1.5);
        cb.SetColorStroke(new BaseColor(255, 0, 0));
        cb.MoveTo(72, 695);
        cb.LineTo(540, 695);
        cb.Stroke();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
    }

    [Fact]
    public void MultipleDrawingOps_OnSinglePage()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();

        var cb = writer.DirectContent;

        // Draw a filled rectangle
        cb.SaveState();
        cb.SetColorFill(new BaseColor(200, 200, 255));
        cb.Rectangle(72, 700, 468, 50);
        cb.Fill();
        cb.RestoreState();

        // Draw a border
        cb.SaveState();
        cb.SetLineWidth(2);
        cb.SetColorStroke(BaseColor.Black);
        cb.Rectangle(72, 700, 468, 50);
        cb.Stroke();
        cb.RestoreState();

        // Draw a diagonal line
        cb.SetColorStroke(new BaseColor(255, 0, 0));
        cb.MoveTo(72, 700);
        cb.LineTo(540, 750);
        cb.Stroke();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
    }

    [Fact]
    public void TwoPages_WithDrawing_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        // Page 1 — rectangle
        doc.NewPage();
        writer.DirectContent.Rectangle(72, 700, 200, 50);
        writer.DirectContent.Stroke();

        // Page 2 — line
        doc.NewPage();
        writer.DirectContent.MoveTo(72, 36);
        writer.DirectContent.LineTo(540, 756);
        writer.DirectContent.Stroke();

        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
        // Should have 2 pages
        Assert.Contains("/Count 2", pdfText);
    }

    [Fact]
    public void EmptyPage_StillProducesValidPdf()
    {
        // A page with no drawing operations should still work
        using var ms = new MemoryStream();
        using var doc = new OpenPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        doc.NewPage();
        doc.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.StartsWith("%PDF-1.4", pdfText);
        Assert.Contains("%%EOF", pdfText);
        Assert.Contains("/Count 1", pdfText);
    }
}
