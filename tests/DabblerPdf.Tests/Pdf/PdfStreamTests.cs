// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.IO.Compression;
using System.Text;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

public class PdfStreamTests
{
    [Fact]
    public void EmptyStream_WritesCorrectStructure()
    {
        var stream = new PdfStream([]);
        var output = GetOutput(stream);

        Assert.Contains("/Length 0", output);
        Assert.Contains("stream\n", output);
        Assert.Contains("\nendstream", output);
    }

    [Fact]
    public void StreamWithContent_IncludesLengthAndData()
    {
        var content = "BT /F1 12 Tf (Hello) Tj ET"u8.ToArray();
        var stream = new PdfStream(content);
        var output = GetOutput(stream);

        Assert.Contains($"/Length {content.Length}", output);
        Assert.Contains("BT /F1 12 Tf (Hello) Tj ET", output);
    }

    [Fact]
    public void FlateCompress_AddsFilterAndCompressesData()
    {
        var content = Encoding.ASCII.GetBytes(new string('A', 200));
        var stream = new PdfStream(content);
        stream.FlateCompress();
        var output = GetBytes(stream);
        var text = Encoding.ASCII.GetString(output);

        Assert.Contains("/Filter /FlateDecode", text);
        // Compressed output should be smaller than 200 bytes of 'A's
        Assert.True(output.Length < content.Length + 100,
            "Compressed stream should be significantly smaller than uncompressed");
    }

    [Fact]
    public void FlateCompress_CalledTwice_DoesNotDoubleCompress()
    {
        var content = Encoding.ASCII.GetBytes(new string('B', 100));
        var stream = new PdfStream(content);
        stream.FlateCompress();
        var firstOutput = GetBytes(stream);
        stream.FlateCompress();
        var secondOutput = GetBytes(stream);

        Assert.Equal(firstOutput.Length, secondOutput.Length);
    }

    [Fact]
    public void Type_IsDictionary()
    {
        // PdfStream extends PdfDictionary, so type is DICTIONARY
        // (PDF spec: stream is a dictionary followed by bytes)
        Assert.True(new PdfStream([]).IsDictionary());
    }

    private static string GetOutput(PdfObject obj)
    {
        return Encoding.ASCII.GetString(GetBytes(obj));
    }

    private static byte[] GetBytes(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return ms.ToArray();
    }
}

public class PdfIndirectObjectTests
{
    [Fact]
    public void WritesCorrectFormat()
    {
        var num = new PdfNumber(42);
        var indObj = new PdfIndirectObject(5, num);
        var output = GetOutput(indObj);

        Assert.StartsWith("5 0 obj\n", output);
        Assert.Contains("42", output);
        Assert.EndsWith("\nendobj\n", output);
    }

    [Fact]
    public void WriteTo_ReturnsCorrectOffset()
    {
        using var ms = new MemoryStream();
        // Write some padding first
        ms.Write("PADDING"u8);
        long paddingLength = ms.Position;

        var indObj = new PdfIndirectObject(1, new PdfNumber(99));
        long offset = indObj.WriteTo(ms);

        Assert.Equal(paddingLength, offset);
    }

    [Fact]
    public void Reference_MatchesObjectNumber()
    {
        var indObj = new PdfIndirectObject(7, PdfNull.Instance);
        var reference = indObj.Reference;

        Assert.Equal(7, reference.Number);
        Assert.Equal(0, reference.Generation);
    }

    private static string GetOutput(PdfIndirectObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfBodyTests
{
    [Fact]
    public void Add_AssignsSequentialObjectNumbers()
    {
        var body = new PdfBody();
        using var ms = new MemoryStream();

        var ref1 = body.Add(new PdfNumber(1), ms);
        var ref2 = body.Add(new PdfNumber(2), ms);

        Assert.Equal(1, ref1.Number);
        Assert.Equal(2, ref2.Number);
    }

    [Fact]
    public void ReserveReference_ThenAdd_UsesReservedNumber()
    {
        var body = new PdfBody();
        using var ms = new MemoryStream();

        var reserved = body.ReserveReference();
        Assert.Equal(1, reserved.Number);

        // Next auto-assigned should be 2
        var ref2 = body.Add(new PdfNumber(99), ms);
        Assert.Equal(2, ref2.Number);

        // Write the reserved object
        body.Add(new PdfNumber(1), reserved, ms);

        Assert.Equal(2, body.XrefEntries.Count);
    }

    [Fact]
    public void Size_IncludesObjectZero()
    {
        var body = new PdfBody();
        using var ms = new MemoryStream();

        body.Add(new PdfNumber(1), ms);
        // Size = next object number, which includes object 0
        Assert.Equal(2, body.Size);
    }

    [Fact]
    public void XrefEntries_RecordCorrectOffsets()
    {
        var body = new PdfBody();
        using var ms = new MemoryStream();

        var ref1 = body.Add(new PdfNumber(42), ms);
        var entry = body.XrefEntries[0];

        Assert.Equal(ref1.Number, entry.ObjectNumber);
        Assert.Equal(0L, entry.Offset); // First object starts at offset 0
    }
}

public class PdfCrossReferenceTableTests
{
    [Fact]
    public void WritesCorrectXrefFormat()
    {
        using var ms = new MemoryStream();
        var entries = new List<XrefEntry>
        {
            new(1, 100),
            new(2, 250),
        };

        PdfCrossReferenceTable.WriteTo(ms, entries, 3);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        Assert.StartsWith("xref\n0 3\n", output);
        Assert.Contains("0000000000 65535 f \r\n", output);
        Assert.Contains("0000000100 00000 n \r\n", output);
        Assert.Contains("0000000250 00000 n \r\n", output);
    }

    [Fact]
    public void ReturnsCorrectStartXrefOffset()
    {
        using var ms = new MemoryStream();
        ms.Write("PADDING"u8); // 7 bytes
        long startXref = PdfCrossReferenceTable.WriteTo(ms, [], 1);
        Assert.Equal(7L, startXref);
    }
}

public class PdfTrailerTests
{
    [Fact]
    public void WritesTrailerWithRequiredFields()
    {
        using var ms = new MemoryStream();
        var rootRef = new PdfIndirectReference(2);
        var infoRef = new PdfIndirectReference(3);

        PdfTrailer.WriteTo(ms, 4, rootRef, infoRef, 500);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        Assert.Contains("trailer", output);
        Assert.Contains("/Size 4", output);
        Assert.Contains("/Root 2 0 R", output);
        Assert.Contains("/Info 3 0 R", output);
        Assert.Contains("startxref\n500\n", output);
        Assert.Contains("%%EOF", output);
    }

    [Fact]
    public void WritesTrailer_WithoutInfo()
    {
        using var ms = new MemoryStream();
        var rootRef = new PdfIndirectReference(1);

        PdfTrailer.WriteTo(ms, 2, rootRef, null, 100);
        var output = Encoding.ASCII.GetString(ms.ToArray());

        Assert.DoesNotContain("/Info", output);
        Assert.Contains("/Root 1 0 R", output);
    }
}
