// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents the PDF <c>null</c> object. Use the <see cref="Instance"/> singleton.
/// </summary>
public sealed class PdfNull : PdfObject
{
    private static readonly byte[] NullBytes = "null"u8.ToArray();

    public static readonly PdfNull Instance = new();

    private PdfNull() : base(NULL, NullBytes)
    {
    }

    public override string ToString() => "null";
}
