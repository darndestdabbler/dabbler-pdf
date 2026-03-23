// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using OpenPdf.Document;

namespace OpenPdf.Pdf;

/// <summary>
/// Orchestrates writing a complete PDF file to an output stream.
/// Manages the PDF header, body (indirect objects), cross-reference table, and trailer.
/// </summary>
public sealed class PdfWriter
{
    private static readonly byte[] PdfHeader = "%PDF-1.4\n%\xe2\xe3\xcf\xd3\n"u8.ToArray();

    private readonly Stream _outputStream;
    private readonly PdfBody _body = new();
    private readonly PdfPages _pages = new();
    private readonly PdfIndirectReference _pagesRef;
    private readonly PdfIndirectReference _catalogRef;
    private PdfIndirectReference? _infoRef;
    private bool _headerWritten;

    /// <summary>The document this writer is associated with.</summary>
    public Document.Document? Document { get; internal set; }

    private PdfWriter(Stream outputStream)
    {
        _outputStream = outputStream;
        _pagesRef = _body.ReserveReference();
        _catalogRef = _body.ReserveReference();
    }

    /// <summary>
    /// Creates a PdfWriter that writes to the given stream, associated with the document.
    /// This is the main factory method, matching OpenPDF's <c>PdfWriter.getInstance()</c>.
    /// </summary>
    public static PdfWriter GetInstance(Document.Document document, Stream outputStream)
    {
        var writer = new PdfWriter(outputStream);
        document.SetWriter(writer);
        return writer;
    }

    /// <summary>
    /// Writes the PDF header. Called when the document is opened.
    /// </summary>
    internal void Open()
    {
        if (_headerWritten)
            return;
        _outputStream.Write(PdfHeader);
        _headerWritten = true;
    }

    /// <summary>
    /// Adds a page to the PDF output.
    /// </summary>
    internal void AddPage(Rectangle pageSize)
    {
        // Create an empty content stream for the page
        var contentStream = new PdfStream(Array.Empty<byte>());
        var contentRef = _body.Add(contentStream, _outputStream);

        // Create the page dictionary
        var page = new PdfPage(pageSize, _pagesRef);
        page.SetContents(contentRef);
        var pageRef = _body.Add(page, _outputStream);
        _pages.AddPage(pageRef);
    }

    /// <summary>
    /// Finalizes the PDF: writes the pages tree, catalog, info dictionary,
    /// cross-reference table, and trailer.
    /// </summary>
    internal void Close()
    {
        // Write info dictionary
        var info = new PdfDictionary();
        info.Put(PdfName.Producer, new PdfString("OpenPdf.NET"));
        _infoRef = _body.Add(info, _outputStream);

        // Finalize and write pages tree
        _pages.Complete();
        _body.Add(_pages, _pagesRef, _outputStream);

        // Write catalog
        var catalog = new PdfCatalog(_pagesRef);
        _body.Add(catalog, _catalogRef, _outputStream);

        // Write cross-reference table
        long startXref = PdfCrossReferenceTable.WriteTo(
            _outputStream, _body.XrefEntries, _body.Size);

        // Write trailer
        PdfTrailer.WriteTo(_outputStream, _body.Size, _catalogRef, _infoRef, startXref);

        _outputStream.Flush();
    }
}
