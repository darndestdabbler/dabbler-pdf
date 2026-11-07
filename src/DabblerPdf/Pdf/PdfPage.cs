// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Document;

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents a single PDF page dictionary (<c>/Type /Page</c>).
/// Contains the media box, parent reference, resources, and optional content stream.
/// </summary>
public sealed class PdfPage : PdfDictionary
{
    /// <summary>Creates a page dictionary with the specified size and parent reference.</summary>
    /// <param name="pageSize">The page dimensions.</param>
    /// <param name="parentRef">An indirect reference to the parent <see cref="PdfPages"/> node.</param>
    public PdfPage(Rectangle pageSize, PdfIndirectReference parentRef)
        : base(PdfName.Page)
    {
        Put(PdfName.MediaBox, new PdfArray(
            pageSize.Left, pageSize.Bottom, pageSize.Right, pageSize.Top));
        Put(PdfName.Parent, parentRef);
        Put(PdfName.Resources, new PdfDictionary());
    }

    /// <summary>Sets the content stream reference for this page.</summary>
    /// <param name="contentsRef">An indirect reference to the content stream.</param>
    public void SetContents(PdfIndirectReference contentsRef)
    {
        Put(PdfName.Contents, contentsRef);
    }
}
