// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Pdf;

/// <summary>
/// Manages the body of a PDF file — the sequence of indirect objects.
/// Assigns object numbers and tracks byte offsets for the cross-reference table.
/// </summary>
public sealed class PdfBody
{
    private int _nextObjectNumber = 1;
    private readonly List<XrefEntry> _xrefEntries = [];

    /// <summary>
    /// Gets the cross-reference entries recorded so far.
    /// </summary>
    public IReadOnlyList<XrefEntry> XrefEntries => _xrefEntries;

    /// <summary>
    /// Gets the total number of objects (including object 0).
    /// Used for the trailer /Size entry.
    /// </summary>
    public int Size => _nextObjectNumber;

    /// <summary>
    /// Reserves the next object number and returns an indirect reference to it.
    /// The object has not been written yet.
    /// </summary>
    public PdfIndirectReference ReserveReference()
    {
        return new PdfIndirectReference(_nextObjectNumber++);
    }

    /// <summary>
    /// Writes a PDF object to the output stream as an indirect object.
    /// Returns the indirect reference for the newly written object.
    /// </summary>
    public PdfIndirectReference Add(PdfObject obj, Stream outputStream)
    {
        int number = _nextObjectNumber++;
        var indirectObj = new PdfIndirectObject(number, obj);
        long offset = indirectObj.WriteTo(outputStream);
        _xrefEntries.Add(new XrefEntry(number, offset));
        return indirectObj.Reference;
    }

    /// <summary>
    /// Writes a PDF object with a pre-reserved object number.
    /// </summary>
    public void Add(PdfObject obj, PdfIndirectReference reference, Stream outputStream)
    {
        var indirectObj = new PdfIndirectObject(reference.Number, obj);
        long offset = indirectObj.WriteTo(outputStream);
        _xrefEntries.Add(new XrefEntry(reference.Number, offset));
    }
}

/// <summary>
/// An entry in the cross-reference table: object number and byte offset.
/// </summary>
public readonly record struct XrefEntry(int ObjectNumber, long Offset);
