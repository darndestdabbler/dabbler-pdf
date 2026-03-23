// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace OpenPdf.Pdf;

/// <summary>
/// Represents raw PDF content that is written directly to the output stream
/// without any escaping or transformation.
/// </summary>
public sealed class PdfLiteral : PdfObject
{
    public PdfLiteral(string text) : base(0, Encoding.ASCII.GetBytes(text))
    {
    }

    public PdfLiteral(byte[] bytes) : base(0, bytes)
    {
    }

    public override string ToString() => Bytes != null ? Encoding.ASCII.GetString(Bytes) : "";
}
