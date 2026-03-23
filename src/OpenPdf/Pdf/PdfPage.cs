// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using OpenPdf.Document;

namespace OpenPdf.Pdf;

/// <summary>
/// Represents a single PDF page dictionary (<c>/Type /Page</c>).
/// Contains the media box, parent reference, resources, and optional content stream.
/// </summary>
public sealed class PdfPage : PdfDictionary
{
    public PdfPage(Rectangle pageSize, PdfIndirectReference parentRef)
        : base(PdfName.Page)
    {
        Put(PdfName.MediaBox, new PdfArray(
            pageSize.Left, pageSize.Bottom, pageSize.Right, pageSize.Top));
        Put(PdfName.Parent, parentRef);
        Put(PdfName.Resources, new PdfDictionary());
    }

    /// <summary>Sets the content stream reference for this page.</summary>
    public void SetContents(PdfIndirectReference contentsRef)
    {
        Put(PdfName.Contents, contentsRef);
    }
}
