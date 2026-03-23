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
    private bool _firstPage = true;

    // Per-page resource tracking
    private readonly Dictionary<PdfName, PdfIndirectReference> _pageFonts = new();
    // Document-level font cache: font PostScript name → indirect reference
    private readonly Dictionary<string, PdfIndirectReference> _fontObjects = new();

    /// <summary>The document this writer is associated with.</summary>
    public Document.Document? Document { get; internal set; }

    /// <summary>
    /// The direct content layer for the current page. Use this to draw text,
    /// lines, rectangles, and images at absolute coordinates.
    /// </summary>
    public PdfContentByte DirectContent { get; }

    private PdfWriter(Stream outputStream)
    {
        _outputStream = outputStream;
        _pagesRef = _body.ReserveReference();
        _catalogRef = _body.ReserveReference();
        DirectContent = new PdfContentByte(this);
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
    /// Adds a page to the PDF output. If there is content in DirectContent,
    /// it is flushed as the page's content stream.
    /// </summary>
    internal void AddPage(Rectangle pageSize)
    {
        // On non-first pages, flush the previous page's content first
        if (!_firstPage)
        {
            FlushCurrentPage(Document!.PreviousPageSize ?? pageSize);
        }
        _firstPage = false;

        // Reset DirectContent for the new page
        DirectContent.Reset();
        _pageFonts.Clear();
    }

    /// <summary>
    /// Writes a font dictionary as an indirect object and returns its reference.
    /// Caches by PostScript name to avoid writing duplicates.
    /// </summary>
    internal PdfIndirectReference AddFontObject(PdfDictionary fontDict)
    {
        var baseFontName = fontDict.GetAsName(PdfName.BaseFont);
        string key = baseFontName?.Value ?? fontDict.GetHashCode().ToString();

        if (_fontObjects.TryGetValue(key, out var existing))
            return existing;

        var fontRef = _body.Add(fontDict, _outputStream);
        _fontObjects[key] = fontRef;
        return fontRef;
    }

    /// <summary>
    /// Registers a font for the current page's resources.
    /// Returns the resource name (e.g., /F1) used to reference the font in content streams.
    /// </summary>
    internal PdfName AddPageFont(PdfName baseFontName, PdfIndirectReference fontRef)
    {
        // Check if already registered
        foreach (var (name, existingRef) in _pageFonts)
        {
            if (existingRef.Number == fontRef.Number)
                return name;
        }

        var resourceName = new PdfName($"F{_pageFonts.Count + 1}");
        _pageFonts[resourceName] = fontRef;
        return resourceName;
    }

    /// <summary>
    /// Finalizes the PDF: writes the last page, pages tree, catalog, info dictionary,
    /// cross-reference table, and trailer.
    /// </summary>
    internal void Close()
    {
        // Flush the last page
        if (!_firstPage)
        {
            FlushCurrentPage(Document!.PageSize);
        }

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

    private void FlushCurrentPage(Rectangle pageSize)
    {
        // Create the content stream from DirectContent bytes
        var contentBytes = DirectContent.ToByteArray();
        var contentStream = new PdfStream(contentBytes);
        if (contentBytes.Length > 128)
            contentStream.FlateCompress();
        var contentRef = _body.Add(contentStream, _outputStream);

        // Build the page dictionary
        var page = new PdfPage(pageSize, _pagesRef);
        page.SetContents(contentRef);

        // Add font resources if any were used
        if (_pageFonts.Count > 0)
        {
            var fontDict = new PdfDictionary();
            foreach (var (name, fontRef) in _pageFonts)
            {
                fontDict.Put(name, fontRef);
            }
            var resources = page.GetAsDictionary(PdfName.Resources) ?? new PdfDictionary();
            resources.Put(PdfName.Font, fontDict);
            page.Put(PdfName.Resources, resources);
        }

        var pageRef = _body.Add(page, _outputStream);
        _pages.AddPage(pageRef);
    }
}
