// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents raw PDF content that is written directly to the output stream
/// without any escaping or transformation.
/// </summary>
public sealed class PdfLiteral : PdfObject
{
    /// <summary>Creates a literal from an ASCII text string.</summary>
    /// <param name="text">The raw PDF content.</param>
    public PdfLiteral(string text) : base(0, Encoding.ASCII.GetBytes(text))
    {
    }

    /// <summary>Creates a literal from raw bytes.</summary>
    /// <param name="bytes">The raw PDF content bytes.</param>
    public PdfLiteral(byte[] bytes) : base(0, bytes)
    {
    }

    /// <inheritdoc/>
    public override string ToString() => Bytes != null ? Encoding.ASCII.GetString(Bytes) : "";
}
