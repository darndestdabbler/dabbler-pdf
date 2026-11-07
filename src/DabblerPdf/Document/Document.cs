// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Pdf;

namespace DabblerPdf.Document;

/// <summary>
/// Represents a PDF document. Manages the document lifecycle: create, open,
/// add pages, and close. Works with <see cref="PdfWriter"/> to produce output.
/// </summary>
public partial class Document : IDisposable
{
    private PdfWriter? _writer;
    private bool _open;
    private bool _closed;
    private int _pageNumber;

    /// <summary>The page size for new pages.</summary>
    public Rectangle PageSize { get; set; }

    /// <summary>The page size of the most recently started page (before the current one).</summary>
    internal Rectangle? PreviousPageSize { get; private set; }

    /// <summary>The current page number (1-based).</summary>
    public int PageNumber => _pageNumber;

    /// <summary>Whether the document is currently open.</summary>
    public bool IsOpen => _open;

    /// <summary>
    /// Creates a new document with US Letter page size (8.5 x 11 inches).
    /// </summary>
    public Document() : this(DabblerPdf.Document.PageSize.Letter) { }

    /// <summary>
    /// Creates a new document with the specified page size.
    /// </summary>
    /// <param name="pageSize">The default page size for new pages.</param>
    public Document(Rectangle pageSize)
    {
        PageSize = pageSize;
    }

    /// <summary>
    /// Associates a writer with this document. Called by <see cref="PdfWriter.GetInstance"/>.
    /// </summary>
    internal void SetWriter(PdfWriter writer)
    {
        _writer = writer;
        writer.Document = this;
    }

    /// <summary>
    /// Opens the document for writing. Must be called before adding pages.
    /// </summary>
    public void Open()
    {
        if (_open)
            return;
        if (_closed)
            throw new InvalidOperationException("Document has been closed.");
        if (_writer == null)
            throw new InvalidOperationException("No PdfWriter associated. Call PdfWriter.GetInstance() first.");

        _open = true;
        _writer.Open();
    }

    /// <summary>
    /// Adds a new blank page with the current page size.
    /// </summary>
    public void NewPage()
    {
        EnsureOpen();
        // If content was written before any explicit NewPage call, this transition
        // flushes an implicit page 1. Advance the page counter so OnEndPage fires at
        // PageNumber == 1 for that flush instead of 0.
        if (_pageNumber == 0 && _writer!.DirectContent.HasContent)
        {
            _pageNumber = 1;
        }
        _writer!.EndCurrentPage(PageSize);
        _pageNumber++;
        _writer!.StartNewPage();
        PreviousPageSize = PageSize;
    }

    /// <summary>
    /// Closes the document and finalizes the PDF output.
    /// </summary>
    public void Close()
    {
        if (_closed)
            return;

        if (_open)
        {
            // PDF requires ≥1 page; the writer enforces that on Close() and fires
            // OnStartPage/OnEndPage for the implicit page. Advance PageNumber here
            // so those events see PageNumber == 1.
            if (_pageNumber == 0)
            {
                _pageNumber = 1;
            }

            _writer!.Close();
            _open = false;
        }

        _closed = true;
    }

    /// <summary>Closes the document if still open and releases resources.</summary>
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    private void EnsureOpen()
    {
        if (_closed)
            throw new InvalidOperationException("Document has been closed.");
        if (!_open)
            throw new InvalidOperationException("Document is not open. Call Open() first.");
    }

    private void EnsureNotClosed()
    {
        if (_closed)
            throw new InvalidOperationException("Document has been closed.");
    }
}
