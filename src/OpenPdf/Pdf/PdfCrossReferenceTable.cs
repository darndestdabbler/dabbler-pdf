// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace OpenPdf.Pdf;

/// <summary>
/// Writes the PDF cross-reference (xref) table. Each entry is exactly 20 bytes:
/// <c>OOOOOOOOOO GGGGG n \r\n</c> (10-digit offset, 5-digit generation, in-use flag).
/// Object 0 is always the head of the free list.
/// </summary>
public static class PdfCrossReferenceTable
{
    /// <summary>
    /// Writes the xref table to the output stream and returns the byte offset
    /// where the xref section started (for the <c>startxref</c> pointer).
    /// </summary>
    public static long WriteTo(Stream outputStream, IReadOnlyList<XrefEntry> entries, int size)
    {
        long startXref = outputStream.Position;

        // xref header
        var header = Encoding.ASCII.GetBytes($"xref\n0 {size}\n");
        outputStream.Write(header);

        // Object 0: head of free list (always present)
        var freeEntry = Encoding.ASCII.GetBytes("0000000000 65535 f \r\n");
        outputStream.Write(freeEntry);

        // Sort entries by object number to write in order
        var sorted = entries.OrderBy(e => e.ObjectNumber).ToArray();
        int nextExpected = 1;

        foreach (var entry in sorted)
        {
            // Fill gaps with free entries if any object numbers were skipped
            while (nextExpected < entry.ObjectNumber)
            {
                outputStream.Write(Encoding.ASCII.GetBytes("0000000000 00000 f \r\n"));
                nextExpected++;
            }

            var line = string.Format("{0:D10} {1:D5} n \r\n", entry.Offset, 0);
            outputStream.Write(Encoding.ASCII.GetBytes(line));
            nextExpected++;
        }

        return startXref;
    }
}
