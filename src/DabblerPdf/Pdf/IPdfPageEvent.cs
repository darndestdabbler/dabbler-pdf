// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Callback interface for PDF document lifecycle events.
/// Implement this interface and assign to <see cref="PdfWriter.PageEvent"/>
/// to receive notifications when pages are created.
/// </summary>
public interface IPdfPageEvent
{
    /// <summary>Called once after the document is opened.</summary>
    void OnOpenDocument(PdfWriter writer, Document.Document document) { }

    /// <summary>
    /// Called at the start of each page, before any content is drawn.
    /// Use this to draw page headers, background elements, or watermarks.
    /// </summary>
    void OnStartPage(PdfWriter writer, Document.Document document) { }

    /// <summary>
    /// Called at the end of each page, just before the page content is finalized.
    /// Use this to draw page footers or page numbers.
    /// </summary>
    void OnEndPage(PdfWriter writer, Document.Document document) { }

    /// <summary>Called once after the document is closed.</summary>
    void OnCloseDocument(PdfWriter writer, Document.Document document) { }
}
