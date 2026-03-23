// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace OpenPdf.Pdf;

/// <summary>
/// Represents a PDF boolean value (<c>true</c> or <c>false</c>).
/// Use the static <see cref="True"/> and <see cref="False"/> instances.
/// </summary>
public sealed class PdfBoolean : PdfObject
{
    private static readonly byte[] TrueBytes = "true"u8.ToArray();
    private static readonly byte[] FalseBytes = "false"u8.ToArray();

    public static readonly PdfBoolean True = new(true);
    public static readonly PdfBoolean False = new(false);

    public bool Value { get; }

    public PdfBoolean(bool value) : base(BOOLEAN)
    {
        Value = value;
        Bytes = value ? TrueBytes : FalseBytes;
    }

    public override string ToString() => Value ? "true" : "false";
}
