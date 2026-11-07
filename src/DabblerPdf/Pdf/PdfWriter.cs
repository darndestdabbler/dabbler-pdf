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
    // True when StartNewPage has been called without a matching EndCurrentPage flush.
    // Distinguishes "a page is open and awaiting content/flush" (e.g. right after the
    // last NewPage() in a session) from "the last page was already flushed into _pages
    // via EndCurrentPage" — important to avoid double-flushing an empty trailing page.
    private bool _pageStarted;

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
    /// <param name="document">The document to associate with this writer.</param>
    /// <param name="outputStream">The stream that will receive the PDF output.</param>
    /// <returns>A new <see cref="PdfWriter"/> instance.</returns>
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
    /// Called by <see cref="Document.Document.NewPage"/> before the page number advances.
    /// </summary>
    /// <remarks>
    /// The flush fires whenever a page is open (StartNewPage has been called since the
    /// last flush) OR whenever <see cref="PdfContentByte.HasContent"/> shows pending
    /// bytes. That second condition is the fix for the multi-page DirectContent
    /// pattern — a consumer that writes to DirectContent before the first NewPage()
    /// call expects that content to flush as physical page 1. Without the HasContent
    /// check, the first-page branch would skip the flush and the next page's content
    /// would accumulate in the same buffer, producing page stacking on physical page 1.
    /// When the flush handles the implicit first page, OnStartPage is fired too so the
    /// Start/End event pair is balanced.
    /// </remarks>
    internal void EndCurrentPage(Rectangle pageSize)
    {
        bool hasContent = DirectContent.HasContent;
        if (_pageStarted || hasContent)
        {
            if (!_pageStarted && hasContent)
            {
                // Implicit first page: content written before any explicit NewPage.
                // Fire OnStartPage so the event pair is balanced.
                PageEvent?.OnStartPage(this, Document!);
            }
            PageEvent?.OnEndPage(this, Document!);
            FlushCurrentPage(Document!.PreviousPageSize ?? pageSize);

            DirectContent.Reset();
            _pageFonts.Clear();
            _pageXObjects.Clear();
            _pageStarted = false;
        }
    }

    /// <summary>
    /// Starts the new page: fires OnStartPage after the page number has been incremented.
    /// Called by <see cref="Document.Document.NewPage"/> after advancing the page number.
    /// </summary>
    internal void StartNewPage()
    {
        PageEvent?.OnStartPage(this, Document!);
        _pageStarted = true;
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
    /// <param name="width">Width of the template bounding box in points.</param>
    /// <param name="height">Height of the template bounding box in points.</param>
    /// <returns>A new <see cref="PdfTemplate"/> instance.</returns>
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
        // Flush the pending page. After the EndCurrentPage fix, _firstPage is no
        // longer a reliable "has-pending-page" signal — EndCurrentPage can flush
        // and still leave StartNewPage queued on the next page. Use the open-page
        // tracker plus pending-content as the discriminator.
        bool hasContent = DirectContent.HasContent;
        if (_pageStarted)
        {
            PageEvent?.OnEndPage(this, Document!);
            FlushCurrentPage(Document!.PageSize);
            _pageStarted = false;
        }
        else if (hasContent)
        {
            // Pattern: content written without any NewPage call. Treat as an
            // implicit page 1 and fire both events to keep the pair balanced.
            PageEvent?.OnStartPage(this, Document!);
            PageEvent?.OnEndPage(this, Document!);
            FlushCurrentPage(Document!.PageSize);
        }

        // PDF requires ≥1 page. If nothing above flushed (Open → Close with no
        // content and no NewPage), emit an empty page 1.
        if (_pages.PageCount == 0)
        {
            PageEvent?.OnStartPage(this, Document!);
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

        var metadata = Document!.GetMetadata();
        if (metadata.Title is { Length: > 0 } title)
            info.Put(PdfName.Title, new PdfString(title));
        if (metadata.Author is { Length: > 0 } author)
            info.Put(PdfName.Author, new PdfString(author));
        if (metadata.Subject is { Length: > 0 } subject)
            info.Put(PdfName.Subject, new PdfString(subject));
        if (metadata.Keywords is { Length: > 0 } keywords)
            info.Put(PdfName.Keywords, new PdfString(keywords));
        if (metadata.Creator is { Length: > 0 } creator)
            info.Put(PdfName.Creator, new PdfString(creator));

        // CreationDate: explicit value wins; otherwise auto-stage UtcNow at close
        // time unless the caller opted out via SuppressCreationDate(). The auto-now
        // path here captures the instant a hair later than AddCreationDate() would —
        // intentional, sub-second difference, not a bug.
        DateTime? creationDate = metadata.CreationDate
            ?? (metadata.CreationDateSuppressed ? (DateTime?)null : DateTime.UtcNow);
        if (creationDate is DateTime cd)
            info.Put(PdfName.CreationDate, new PdfString(Utilities.PdfDate.Format(cd)));

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
