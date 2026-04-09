// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents the PDF pages tree node (<c>/Type /Pages</c>).
/// Collects child page references and writes the final /Kids array and /Count.
/// </summary>
public sealed class PdfPages : PdfDictionary
{
    private readonly PdfArray _kids = new();

    public PdfPages() : base(PdfName.Pages)
    {
    }

    /// <summary>Adds a page reference to the /Kids array.</summary>
    public void AddPage(PdfIndirectReference pageRef)
    {
        _kids.Add(pageRef);
    }

    /// <summary>The number of pages added.</summary>
    public int PageCount => _kids.Count;

    /// <summary>
    /// Finalizes the /Kids and /Count entries before writing.
    /// Call this after all pages have been added.
    /// </summary>
    public void Complete()
    {
        Put(PdfName.Kids, _kids);
        Put(PdfName.Count, new PdfNumber(_kids.Count));
    }
}
