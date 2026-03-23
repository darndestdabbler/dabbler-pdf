// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using OpenPdf.Pdf;

namespace OpenPdf.Document;

/// <summary>
/// Represents a PDF document. Manages the document lifecycle: create, open,
/// add pages, and close. Works with <see cref="PdfWriter"/> to produce output.
/// </summary>
public class Document : IDisposable
{
    private PdfWriter? _writer;
    private bool _open;
    private bool _closed;
    private int _pageNumber;

    /// <summary>The page size for new pages.</summary>
    public Rectangle PageSize { get; set; }

    /// <summary>The current page number (1-based).</summary>
    public int PageNumber => _pageNumber;

    /// <summary>Whether the document is currently open.</summary>
    public bool IsOpen => _open;

    public Document() : this(OpenPdf.Document.PageSize.Letter) { }

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
        _pageNumber++;
        _writer!.AddPage(PageSize);
    }

    /// <summary>
    /// Closes the document and finalizes the PDF output.
    /// </summary>
    public void Close()
    {
        if (_closed)
            return;

        // If no pages were added, add one empty page (PDF requires at least one page)
        if (_open && _pageNumber == 0)
        {
            NewPage();
        }

        if (_open)
        {
            _writer!.Close();
            _open = false;
            _closed = true;
        }
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    private void EnsureOpen()
    {
        if (!_open)
            throw new InvalidOperationException("Document is not open. Call Open() first.");
        if (_closed)
            throw new InvalidOperationException("Document has been closed.");
    }
}
