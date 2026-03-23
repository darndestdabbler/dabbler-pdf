// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace OpenPdf.Pdf;

/// <summary>
/// Wraps a <see cref="PdfObject"/> for writing as an indirect object:
/// <c>N G obj\n...\nendobj\n</c>. Tracks its byte offset in the output
/// for cross-reference table generation.
/// </summary>
public sealed class PdfIndirectObject
{
    private static readonly byte[] ObjOpen = " obj\n"u8.ToArray();
    private static readonly byte[] ObjClose = "\nendobj\n"u8.ToArray();

    /// <summary>The object number.</summary>
    public int Number { get; }

    /// <summary>The generation number (usually 0).</summary>
    public int Generation { get; }

    /// <summary>The wrapped PDF object.</summary>
    public PdfObject Object { get; }

    public PdfIndirectObject(int number, PdfObject obj, int generation = 0)
    {
        Number = number;
        Generation = generation;
        Object = obj;
    }

    /// <summary>
    /// Writes the indirect object to the stream and returns the byte offset
    /// at which the object started (for the xref table).
    /// </summary>
    public long WriteTo(Stream outputStream)
    {
        long offset = outputStream.Position;
        var header = Encoding.ASCII.GetBytes($"{Number} {Generation}");
        outputStream.Write(header);
        outputStream.Write(ObjOpen);
        Object.WriteTo(outputStream);
        outputStream.Write(ObjClose);
        return offset;
    }

    /// <summary>
    /// Creates a <see cref="PdfIndirectReference"/> pointing to this object.
    /// </summary>
    public PdfIndirectReference Reference => new(Number, Generation);
}
