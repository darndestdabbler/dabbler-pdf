// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// Writes the PDF trailer: the trailer dictionary, <c>startxref</c> pointer,
/// and the <c>%%EOF</c> marker.
/// </summary>
public static class PdfTrailer
{
    /// <summary>
    /// Writes the trailer section to the output stream.
    /// </summary>
    /// <param name="outputStream">The PDF output stream.</param>
    /// <param name="size">Total number of objects (including object 0).</param>
    /// <param name="rootRef">Indirect reference to the catalog (/Root).</param>
    /// <param name="infoRef">Indirect reference to the info dictionary (/Info), or null.</param>
    /// <param name="startXref">Byte offset of the xref section.</param>
    public static void WriteTo(
        Stream outputStream,
        int size,
        PdfIndirectReference rootRef,
        PdfIndirectReference? infoRef,
        long startXref)
    {
        var trailer = new PdfDictionary();
        trailer.Put(PdfName.Size, new PdfNumber(size));
        trailer.Put(PdfName.Root, rootRef);
        if (infoRef != null)
            trailer.Put(PdfName.Info, infoRef);

        outputStream.Write("trailer\n"u8);
        trailer.WriteTo(outputStream);
        outputStream.Write(Encoding.ASCII.GetBytes($"\nstartxref\n{startXref}\n%%EOF\n"));
    }
}
