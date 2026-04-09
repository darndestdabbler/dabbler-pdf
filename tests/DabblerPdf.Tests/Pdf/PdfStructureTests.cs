// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

public class PdfPageTests
{
    [Fact]
    public void Constructor_SetsMediaBox()
    {
        var pageSize = new Rectangle(0, 0, 612, 792); // Letter
        var parentRef = new PdfIndirectReference(1);
        var page = new PdfPage(pageSize, parentRef);
        var output = GetPdfText(page);

        Assert.Contains("/Type /Page", output);
        Assert.Contains("/MediaBox [0 0 612 792]", output);
    }

    [Fact]
    public void Constructor_SetsParentReference()
    {
        var pageSize = new Rectangle(595, 842); // A4
        var parentRef = new PdfIndirectReference(5);
        var page = new PdfPage(pageSize, parentRef);
        var output = GetPdfText(page);

        Assert.Contains("/Parent 5 0 R", output);
    }

    [Fact]
    public void Constructor_IncludesEmptyResources()
    {
        var pageSize = new Rectangle(612, 792);
        var parentRef = new PdfIndirectReference(1);
        var page = new PdfPage(pageSize, parentRef);
        var output = GetPdfText(page);

        Assert.Contains("/Resources <<>>", output);
    }

    [Fact]
    public void SetContents_AddsContentsReference()
    {
        var pageSize = new Rectangle(612, 792);
        var parentRef = new PdfIndirectReference(1);
        var page = new PdfPage(pageSize, parentRef);

        var contentsRef = new PdfIndirectReference(10);
        page.SetContents(contentsRef);
        var output = GetPdfText(page);

        Assert.Contains("/Contents 10 0 R", output);
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfPagesTests
{
    [Fact]
    public void Constructor_SetsTypePages()
    {
        var pages = new PdfPages();
        var output = GetPdfText(pages);

        Assert.Contains("/Type /Pages", output);
    }

    [Fact]
    public void PageCount_ReflectsAddedPages()
    {
        var pages = new PdfPages();
        Assert.Equal(0, pages.PageCount);

        pages.AddPage(new PdfIndirectReference(1));
        Assert.Equal(1, pages.PageCount);

        pages.AddPage(new PdfIndirectReference(2));
        Assert.Equal(2, pages.PageCount);
    }

    [Fact]
    public void Complete_WritesKidsAndCount()
    {
        var pages = new PdfPages();
        pages.AddPage(new PdfIndirectReference(3));
        pages.AddPage(new PdfIndirectReference(7));
        pages.Complete();

        var output = GetPdfText(pages);

        Assert.Contains("/Kids [3 0 R 7 0 R]", output);
        Assert.Contains("/Count 2", output);
    }

    [Fact]
    public void Complete_IsIdempotent()
    {
        var pages = new PdfPages();
        pages.AddPage(new PdfIndirectReference(1));
        pages.Complete();
        pages.Complete(); // second call should not throw

        var output = GetPdfText(pages);
        Assert.Contains("/Count 1", output);
    }

    [Fact]
    public void AddPage_AfterComplete_Throws()
    {
        var pages = new PdfPages();
        pages.AddPage(new PdfIndirectReference(1));
        pages.Complete();

        Assert.Throws<InvalidOperationException>(() =>
            pages.AddPage(new PdfIndirectReference(2)));
    }

    [Fact]
    public void Complete_WithNoPages_WritesEmptyKids()
    {
        var pages = new PdfPages();
        pages.Complete();
        var output = GetPdfText(pages);

        Assert.Contains("/Kids []", output);
        Assert.Contains("/Count 0", output);
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfCatalogTests
{
    [Fact]
    public void Constructor_SetsTypeCatalog()
    {
        var pagesRef = new PdfIndirectReference(1);
        var catalog = new PdfCatalog(pagesRef);
        var output = GetPdfText(catalog);

        Assert.Contains("/Type /Catalog", output);
    }

    [Fact]
    public void Constructor_SetsPagesReference()
    {
        var pagesRef = new PdfIndirectReference(4);
        var catalog = new PdfCatalog(pagesRef);
        var output = GetPdfText(catalog);

        Assert.Contains("/Pages 4 0 R", output);
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfCrossReferenceTableAdditionalTests
{
    [Fact]
    public void XrefEntry_IsExactly20Bytes()
    {
        using var ms = new MemoryStream();
        var entries = new List<XrefEntry> { new(1, 12345) };

        PdfCrossReferenceTable.WriteTo(ms, entries, 2);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        // Extract lines after the header
        var lines = output.Split('\n');
        // lines[0] = "xref", lines[1] = "0 2", lines[2..] = entries (with \r\n inside)
        // Each entry is 20 bytes: 10-digit offset + space + 5-digit gen + space + flag + space + \r\n
        var bodyText = output[(output.IndexOf("0 2\n") + "0 2\n".Length)..];
        var entryBytes = Encoding.ASCII.GetBytes(bodyText);

        // 2 entries (object 0 free + object 1 in-use), each 20 bytes = 40 bytes
        Assert.Equal(40, entryBytes.Length);
    }

    [Fact]
    public void GapFill_WritesFreePlaceholders()
    {
        using var ms = new MemoryStream();
        // Object 1 is missing — should be filled with a free entry
        var entries = new List<XrefEntry> { new(2, 500) };

        PdfCrossReferenceTable.WriteTo(ms, entries, 3);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        // Should have: object 0 (free list head), object 1 (free placeholder), object 2 (in-use)
        Assert.Contains("0000000000 65535 f \n", output); // object 0
        Assert.Contains("0000000000 00000 f \n", output); // object 1 gap-fill
        Assert.Contains("0000000500 00000 n \n", output); // object 2
    }

    [Fact]
    public void EntriesAreSortedByObjectNumber()
    {
        using var ms = new MemoryStream();
        // Pass entries out of order
        var entries = new List<XrefEntry>
        {
            new(3, 800),
            new(1, 100),
            new(2, 400),
        };

        PdfCrossReferenceTable.WriteTo(ms, entries, 4);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        int pos1 = output.IndexOf("0000000100");
        int pos2 = output.IndexOf("0000000400");
        int pos3 = output.IndexOf("0000000800");

        Assert.True(pos1 < pos2, "Object 1 should appear before object 2");
        Assert.True(pos2 < pos3, "Object 2 should appear before object 3");
    }
}

public class PdfBodyAdditionalTests
{
    [Fact]
    public void InitialState_HasNoEntries()
    {
        var body = new PdfBody();

        Assert.Empty(body.XrefEntries);
        Assert.Equal(1, body.Size); // Only object 0 (next number is 1)
    }

    [Fact]
    public void Add_WithReservedRef_RecordsOffset()
    {
        var body = new PdfBody();
        using var ms = new MemoryStream();

        var reserved = body.ReserveReference();
        ms.Write("PADDING"u8); // 7 bytes offset

        body.Add(new PdfNumber(42), reserved, ms);

        Assert.Single(body.XrefEntries);
        Assert.Equal(reserved.Number, body.XrefEntries[0].ObjectNumber);
        Assert.Equal(7L, body.XrefEntries[0].Offset);
    }

    [Fact]
    public void MultipleObjects_TrackSequentialOffsets()
    {
        var body = new PdfBody();
        using var ms = new MemoryStream();

        body.Add(new PdfNumber(1), ms);
        long secondOffset = ms.Position;
        body.Add(new PdfNumber(2), ms);

        Assert.Equal(2, body.XrefEntries.Count);
        Assert.Equal(0L, body.XrefEntries[0].Offset);
        Assert.Equal(secondOffset, body.XrefEntries[1].Offset);
    }
}

public class PdfTrailerAdditionalTests
{
    [Fact]
    public void WritesStartxrefValue()
    {
        using var ms = new MemoryStream();
        var rootRef = new PdfIndirectReference(1);

        PdfTrailer.WriteTo(ms, 2, rootRef, null, 99999);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        Assert.Contains("startxref\n99999\n", output);
    }

    [Fact]
    public void EndsWithEofMarker()
    {
        using var ms = new MemoryStream();
        var rootRef = new PdfIndirectReference(1);

        PdfTrailer.WriteTo(ms, 1, rootRef, null, 0);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        Assert.EndsWith("%%EOF\n", output);
    }
}
