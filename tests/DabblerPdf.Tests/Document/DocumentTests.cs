// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Document;

public class RectangleTests
{
    [Fact]
    public void TwoArgConstructor_SetsOriginAtZero()
    {
        var rect = new Rectangle(612, 792);
        Assert.Equal(0, rect.Left);
        Assert.Equal(0, rect.Bottom);
        Assert.Equal(612, rect.Right);
        Assert.Equal(792, rect.Top);
    }

    [Fact]
    public void FourArgConstructor_SetsAllCoordinates()
    {
        var rect = new Rectangle(10, 20, 110, 220);
        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Bottom);
        Assert.Equal(110, rect.Right);
        Assert.Equal(220, rect.Top);
    }

    [Fact]
    public void Width_And_Height_AreCorrect()
    {
        var rect = new Rectangle(10, 20, 110, 220);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void CopyConstructor_CopiesValues()
    {
        var original = new Rectangle(1, 2, 3, 4);
        var copy = new Rectangle(original);
        Assert.Equal(original.Left, copy.Left);
        Assert.Equal(original.Bottom, copy.Bottom);
        Assert.Equal(original.Right, copy.Right);
        Assert.Equal(original.Top, copy.Top);
    }

    [Fact]
    public void CopyConstructor_CreatesIndependentInstance()
    {
        var original = new Rectangle(1, 2, 3, 4);
        var copy = new Rectangle(original);
        Assert.NotSame(original, copy);
    }

    [Fact]
    public void ToString_IncludesCoordinates()
    {
        var rect = new Rectangle(10.5, 20.5, 110.5, 220.5);
        var str = rect.ToString();
        Assert.Contains("10.5", str);
        Assert.Contains("20.5", str);
        Assert.Contains("110.5", str);
        Assert.Contains("220.5", str);
    }

    [Fact]
    public void ZeroDimensions_AreValid()
    {
        var rect = new Rectangle(5, 5, 5, 5);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
    }
}

public class PageSizeTests
{
    [Fact]
    public void Letter_HasCorrectDimensions()
    {
        Assert.Equal(612, PageSize.Letter.Width);
        Assert.Equal(792, PageSize.Letter.Height);
    }

    [Fact]
    public void A4_HasCorrectDimensions()
    {
        Assert.Equal(595.28, PageSize.A4.Width);
        Assert.Equal(841.89, PageSize.A4.Height);
    }

    [Fact]
    public void Legal_HasCorrectDimensions()
    {
        Assert.Equal(612, PageSize.Legal.Width);
        Assert.Equal(1008, PageSize.Legal.Height);
    }

    [Fact]
    public void A3_HasCorrectDimensions()
    {
        Assert.Equal(841.89, PageSize.A3.Width);
        Assert.Equal(1190.55, PageSize.A3.Height);
    }

    [Fact]
    public void A5_HasCorrectDimensions()
    {
        Assert.Equal(419.53, PageSize.A5.Width);
        Assert.Equal(595.28, PageSize.A5.Height);
    }

    [Fact]
    public void Tabloid_HasCorrectDimensions()
    {
        Assert.Equal(792, PageSize.Tabloid.Width);
        Assert.Equal(1224, PageSize.Tabloid.Height);
    }

    [Fact]
    public void Executive_HasCorrectDimensions()
    {
        Assert.Equal(522, PageSize.Executive.Width);
        Assert.Equal(756, PageSize.Executive.Height);
    }

    [Fact]
    public void AllPageSizes_HaveOriginAtZero()
    {
        var sizes = new[] {
            PageSize.Letter, PageSize.Legal, PageSize.A4,
            PageSize.A3, PageSize.A5, PageSize.Tabloid, PageSize.Executive
        };

        foreach (var size in sizes)
        {
            Assert.Equal(0, size.Left);
            Assert.Equal(0, size.Bottom);
        }
    }
}

public class DocumentIntegrationTests
{
    [Fact]
    public void EmptyDocument_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.Close();

        var pdfBytes = ms.ToArray();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        // Must start with PDF header
        Assert.StartsWith("%PDF-1.4", pdfText);

        // Must end with %%EOF
        Assert.Contains("%%EOF", pdfText);

        // Must contain xref table
        Assert.Contains("xref", pdfText);

        // Must contain trailer
        Assert.Contains("trailer", pdfText);

        // Must contain startxref
        Assert.Contains("startxref", pdfText);

        // Must have at least one page (auto-added)
        Assert.Contains("/Type /Page", pdfText);
        Assert.Contains("/Type /Pages", pdfText);
        Assert.Contains("/Type /Catalog", pdfText);

        // Must have MediaBox for Letter size
        Assert.Contains("[0 0 612 792]", pdfText);
    }

    [Fact]
    public void MultiplePages_AllWritten()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.NewPage();
        document.NewPage();
        document.NewPage();
        document.Close();

        var pdfText = Encoding.ASCII.GetString(ms.ToArray());

        // Should have 3 pages — /Count 3 in the pages dictionary
        Assert.Contains("/Count 3", pdfText);
    }

    [Fact]
    public void Document_WithA4PageSize()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.A4);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.NewPage();
        document.Close();

        var pdfText = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("[0 0 595.28 841.89]", pdfText);
    }

    [Fact]
    public void Dispose_ClosesDocument()
    {
        using var ms = new MemoryStream();
        using (var document = new DabblerPdf.Document.Document(PageSize.Letter))
        {
            PdfWriter.GetInstance(document, ms);
            document.Open();
            document.NewPage();
        } // Dispose calls Close

        var pdfText = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("%%EOF", pdfText);
    }

    [Fact]
    public void Open_WithoutWriter_Throws()
    {
        var document = new DabblerPdf.Document.Document();
        Assert.Throws<InvalidOperationException>(() => document.Open());
    }

    [Fact]
    public void NewPage_BeforeOpen_Throws()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document();
        PdfWriter.GetInstance(document, ms);

        Assert.Throws<InvalidOperationException>(() => document.NewPage());
    }

    [Fact]
    public void InfoDictionary_ContainsProducer()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.Close();

        var pdfText = Encoding.ASCII.GetString(ms.ToArray());
        Assert.Contains("/Producer", pdfText);
        Assert.Contains("DabblerPdf.NET", pdfText);
    }

    [Fact]
    public void PdfStructure_HasValidObjectReferences()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.NewPage();
        document.Close();

        var pdfText = Encoding.ASCII.GetString(ms.ToArray());

        // Should contain indirect object markers
        Assert.Contains("obj", pdfText);
        Assert.Contains("endobj", pdfText);

        // Should have indirect references (N 0 R format)
        Assert.Matches(@"\d+ 0 R", pdfText);
    }

    [Fact]
    public void NewPage_IncrementsPageNumber()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        Assert.Equal(0, document.PageNumber);

        document.NewPage();
        Assert.Equal(1, document.PageNumber);

        document.NewPage();
        Assert.Equal(2, document.PageNumber);

        document.Close();
    }

    [Fact]
    public void Close_WithoutOpen_SetsClosedState()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        // Close without ever calling Open — should not throw
        document.Close();

        // Subsequent operations should fail with "closed" message
        Assert.Throws<InvalidOperationException>(() => document.Open());
    }

    [Fact]
    public void Close_IsIdempotent()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.Close();
        document.Close(); // Should not throw
    }

    [Fact]
    public void NewPage_AfterClose_Throws()
    {
        using var ms = new MemoryStream();
        var document = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(document, ms);

        document.Open();
        document.NewPage();
        document.Close();

        Assert.Throws<InvalidOperationException>(() => document.NewPage());
    }
}
