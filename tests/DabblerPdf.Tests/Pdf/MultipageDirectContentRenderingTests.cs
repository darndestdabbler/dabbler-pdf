// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using UglyToad.PdfPig;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

/// <summary>
/// Regression coverage for the multi-page <c>DirectContent</c> rendering pattern.
///
/// The canonical consumer pattern — <c>Dabbler.Reports.Export.PdfExporter</c> —
/// writes N logical pages via the loop:
/// <code>
/// for (i = 0; i &lt; N; i++) { if (i &gt; 0) doc.NewPage(); render(writer.DirectContent, pages[i]); }
/// </code>
/// This produced stacked content on physical page 1 before the fix: composer-page 1
/// and composer-page 2 both rendered into the same content stream, and composer-page 2's
/// full-page background fill overdrew composer-page 1's visible output. These tests
/// drive the pattern end-to-end, then assert per-physical-page text via a
/// rendering-aware parser (PdfPig) so "string-in-bytes" false positives are impossible.
/// </summary>
public class MultipageDirectContentRenderingTests
{
    /// <summary>
    /// Drives the exact PdfExporter loop shape with a distinct marker per page.
    /// Each iteration writes to <c>DirectContent</c>; <c>NewPage()</c> is called
    /// only between pages (never before the first iteration).
    /// </summary>
    private static byte[] BuildPdfExporterPatternPdf(int pageCount)
    {
        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

        for (int i = 0; i < pageCount; i++)
        {
            if (i > 0)
                doc.NewPage();

            var cb = writer.DirectContent;
            cb.BeginText();
            cb.SetFontAndSize(font, 14);
            cb.SetTextMatrix(72, 720);
            cb.ShowText($"MARKER_PAGE_{i + 1}");
            cb.EndText();
        }

        doc.Close();
        return ms.ToArray();
    }

    [Fact]
    public void PdfExporterPattern_ProducesOnePhysicalPagePerLogicalPage()
    {
        const int pageCount = 3;
        var pdfBytes = BuildPdfExporterPatternPdf(pageCount);

        using var pdf = PdfDocument.Open(pdfBytes);
        Assert.Equal(pageCount, pdf.NumberOfPages);
    }

    [Fact]
    public void PdfExporterPattern_EachPhysicalPageTextIsIsolated()
    {
        const int pageCount = 3;
        var pdfBytes = BuildPdfExporterPatternPdf(pageCount);

        using var pdf = PdfDocument.Open(pdfBytes);
        for (int physicalPage = 1; physicalPage <= pageCount; physicalPage++)
        {
            var text = pdf.GetPage(physicalPage).Text;
            Assert.Contains($"MARKER_PAGE_{physicalPage}", text);
            for (int otherPage = 1; otherPage <= pageCount; otherPage++)
            {
                if (otherPage != physicalPage)
                    Assert.DoesNotContain($"MARKER_PAGE_{otherPage}", text);
            }
        }
    }

    [Fact]
    public void PdfExporterPattern_SevenLogicalPages_ProducesSevenPhysicalPages()
    {
        // Mirrors the field report: a 7-composer-page export that produced a 6-page
        // PDF with pages 1 + 2 stacked on the first physical page. After the fix, the
        // count must match exactly.
        const int pageCount = 7;
        var pdfBytes = BuildPdfExporterPatternPdf(pageCount);

        using var pdf = PdfDocument.Open(pdfBytes);
        Assert.Equal(pageCount, pdf.NumberOfPages);

        for (int physicalPage = 1; physicalPage <= pageCount; physicalPage++)
        {
            var text = pdf.GetPage(physicalPage).Text;
            Assert.Contains($"MARKER_PAGE_{physicalPage}", text);
        }
    }

    [Fact]
    public void SinglePageDirectContent_NoExplicitNewPage_ProducesOneValidPage()
    {
        // Guards the single-page convenience pattern used by several Samples and
        // MVP scenarios: write to DirectContent once, then Close without calling
        // NewPage. The fix must not turn this into two pages.
        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var cb = writer.DirectContent;
        cb.BeginText();
        cb.SetFontAndSize(font, 14);
        cb.SetTextMatrix(72, 720);
        cb.ShowText("SINGLE_PAGE_CONTENT");
        cb.EndText();

        doc.Close();

        using var pdf = PdfDocument.Open(ms.ToArray());
        Assert.Equal(1, pdf.NumberOfPages);
        Assert.Contains("SINGLE_PAGE_CONTENT", pdf.GetPage(1).Text);
    }

    [Fact]
    public void ExplicitNewPagePerPagePattern_StillProducesOnePhysicalPagePerLogicalPage()
    {
        // The inverse loop shape used by MvpValidationTests.Scenario1: NewPage() is
        // the FIRST call on every iteration, including the first. This pattern was
        // already correct before the fix; verify the fix did not regress it.
        const int pageCount = 4;

        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

        for (int i = 0; i < pageCount; i++)
        {
            doc.NewPage();
            var cb = writer.DirectContent;
            cb.BeginText();
            cb.SetFontAndSize(font, 14);
            cb.SetTextMatrix(72, 720);
            cb.ShowText($"MARKER_PAGE_{i + 1}");
            cb.EndText();
        }

        doc.Close();

        using var pdf = PdfDocument.Open(ms.ToArray());
        Assert.Equal(pageCount, pdf.NumberOfPages);
        for (int physicalPage = 1; physicalPage <= pageCount; physicalPage++)
        {
            var text = pdf.GetPage(physicalPage).Text;
            Assert.Contains($"MARKER_PAGE_{physicalPage}", text);
            for (int otherPage = 1; otherPage <= pageCount; otherPage++)
            {
                if (otherPage != physicalPage)
                    Assert.DoesNotContain($"MARKER_PAGE_{otherPage}", text);
            }
        }
    }
}
