// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Document;

namespace DabblerPdf.Pdf;

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
    private readonly Dictionary<PdfName, PdfIndirectReference> _pageXObjects = new();
    // Document-level font cache: font PostScript name → indirect reference
    private readonly Dictionary<string, PdfIndirectReference> _fontObjects = new();
    // Document-level image cache: Image instance → indirect reference
    private readonly Dictionary<Image.Image, PdfIndirectReference> _imageObjects = new();
    private int _imageCounter;
    // Template tracking
    private readonly List<PdfTemplate> _templates = [];
    private int _templateCounter;

    /// <summary>The document this writer is associated with.</summary>
    public Document.Document? Document { get; internal set; }

    /// <summary>
    /// The direct content layer for the current page. Use this to draw text,
    /// lines, rectangles, and images at absolute coordinates.
    /// </summary>
    public PdfContentByte DirectContent { get; }

    /// <summary>
    /// Gets or sets the page event handler. Set before calling <see cref="Document.Document.Open"/>.
    /// </summary>
    public IPdfPageEvent? PageEvent { get; set; }

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
        PageEvent?.OnOpenDocument(this, Document!);
    }

    /// <summary>
    /// Ends the current page: fires OnEndPage, flushes content, and resets state.
    /// On the first call, simply marks the first page as started (no flush needed).
    /// Called by <see cref="Document.Document.NewPage"/> before the page number advances.
    /// </summary>
    internal void EndCurrentPage(Rectangle pageSize)
    {
        if (!_firstPage)
        {
            PageEvent?.OnEndPage(this, Document!);
            FlushCurrentPage(Document!.PreviousPageSize ?? pageSize);

            // Reset DirectContent for the new page
            DirectContent.Reset();
            _pageFonts.Clear();
            _pageXObjects.Clear();
        }
        else
        {
            // First page: don't reset — content may have been written to DirectContent
            // before the first NewPage() call (common for single-page documents).
            _firstPage = false;
        }
    }

    /// <summary>
    /// Starts the new page: fires OnStartPage after the page number has been incremented.
    /// Called by <see cref="Document.Document.NewPage"/> after advancing the page number.
    /// </summary>
    internal void StartNewPage()
    {
        PageEvent?.OnStartPage(this, Document!);
    }

    /// <summary>
    /// Writes any PDF object as an indirect object and returns its reference.
    /// Used for font descriptors, font streams, and other supporting objects.
    /// </summary>
    internal PdfIndirectReference AddIndirectObject(PdfObject obj)
    {
        return _body.Add(obj, _outputStream);
    }

    /// <summary>
    /// Checks if a font with the given PostScript name is already registered in this writer.
    /// Returns the cached reference if so, or null if the font needs to be created.
    /// </summary>
    internal PdfIndirectReference? GetCachedFontRef(string postscriptName)
    {
        return _fontObjects.GetValueOrDefault(postscriptName);
    }

    /// <summary>
    /// Writes a font dictionary as an indirect object and returns its reference.
    /// Caches by PostScript name to avoid writing duplicates.
    /// </summary>
    internal PdfIndirectReference AddFontObject(PdfDictionary fontDict)
    {
        var baseFontName = fontDict.GetAsName(PdfName.BaseFont)
            ?? throw new ArgumentException(
                "Font dictionary must contain a /BaseFont entry.", nameof(fontDict));
        string key = baseFontName.Value;

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
    /// Adds an image XObject as an indirect object and registers it for the current page.
    /// Returns the resource name (e.g., /Im1) used to reference the image in content streams.
    /// </summary>
    internal PdfName AddPageImage(Image.Image image)
    {
        if (!_imageObjects.TryGetValue(image, out var imageRef))
        {
            // Create the PDF image XObject (handles soft mask if needed)
            var pdfImage = Image.PdfImage.Create(image, this);
            imageRef = _body.Add(pdfImage, _outputStream);
            _imageObjects[image] = imageRef;
        }

        // Check if this image is already on the current page
        foreach (var (name, existingRef) in _pageXObjects)
        {
            if (existingRef.Number == imageRef.Number)
                return name;
        }

        _imageCounter++;
        var resourceName = new PdfName($"Im{_imageCounter}");
        _pageXObjects[resourceName] = imageRef;
        return resourceName;
    }

    /// <summary>
    /// Creates a new PdfTemplate (Form XObject) that can be placed on pages
    /// and filled in later.
    /// </summary>
    public PdfTemplate CreateTemplate(double width, double height)
    {
        var reference = _body.ReserveReference();
        var template = new PdfTemplate(this, width, height, reference);
        _templates.Add(template);
        return template;
    }

    /// <summary>
    /// Registers a template as an XObject resource for the current page.
    /// Returns the resource name (e.g., /Tpl1) used in the content stream.
    /// </summary>
    internal PdfName AddPageTemplate(PdfTemplate template)
    {
        // Check if already registered on this page
        foreach (var (name, existingRef) in _pageXObjects)
        {
            if (existingRef.Number == template.Reference.Number)
                return name;
        }

        _templateCounter++;
        var resourceName = new PdfName($"Tpl{_templateCounter}");
        _pageXObjects[resourceName] = template.Reference;
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
            PageEvent?.OnEndPage(this, Document!);
            FlushCurrentPage(Document!.PageSize);
        }

        PageEvent?.OnCloseDocument(this, Document!);

        // Serialize templates (Form XObjects) — content may have been written after placement
        foreach (var template in _templates)
        {
            var formXObject = template.ToFormXObject();
            _body.Add(formXObject, template.Reference, _outputStream);
        }

        // Write info dictionary
        var info = new PdfDictionary();
        info.Put(PdfName.Producer, new PdfString("DabblerPdf.NET"));
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

        // Build resource dictionary
        if (_pageFonts.Count > 0 || _pageXObjects.Count > 0)
        {
            var resources = page.GetAsDictionary(PdfName.Resources) ?? new PdfDictionary();

            if (_pageFonts.Count > 0)
            {
                var fontDict = new PdfDictionary();
                foreach (var (name, fontRef) in _pageFonts)
                {
                    fontDict.Put(name, fontRef);
                }
                resources.Put(PdfName.Font, fontDict);
            }

            if (_pageXObjects.Count > 0)
            {
                var xObjectDict = new PdfDictionary();
                foreach (var (name, xRef) in _pageXObjects)
                {
                    xObjectDict.Put(name, xRef);
                }
                resources.Put(PdfName.XObject, xObjectDict);
            }

            page.Put(PdfName.Resources, resources);
        }

        var pageRef = _body.Add(page, _outputStream);
        _pages.AddPage(pageRef);
    }
}
