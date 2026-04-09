// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents the PDF document catalog (<c>/Type /Catalog</c>).
/// The root of the PDF document structure, pointing to the pages tree.
/// </summary>
public sealed class PdfCatalog : PdfDictionary
{
    /// <summary>Creates a document catalog pointing to the given pages tree.</summary>
    /// <param name="pagesRef">An indirect reference to the root <see cref="PdfPages"/> node.</param>
    public PdfCatalog(PdfIndirectReference pagesRef) : base(PdfName.Catalog)
    {
        Put(PdfName.Pages, pagesRef);
    }
}
