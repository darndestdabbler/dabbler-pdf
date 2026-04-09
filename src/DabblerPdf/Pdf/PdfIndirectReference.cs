// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents an indirect reference to a PDF object: <c>N G R</c>
/// (e.g., <c>5 0 R</c>). Used for cross-referencing objects in the PDF file.
/// </summary>
public sealed class PdfIndirectReference : PdfObject
{
    /// <summary>The object number in the cross-reference table.</summary>
    public int Number { get; }

    /// <summary>The generation number (usually 0 for new documents).</summary>
    public int Generation { get; }

    public PdfIndirectReference(int number, int generation = 0) : base(INDIRECT)
    {
        Number = number;
        Generation = generation;
        Bytes = Encoding.ASCII.GetBytes($"{number} {generation} R");
    }

    public override string ToString() => $"{Number} {Generation} R";
}
