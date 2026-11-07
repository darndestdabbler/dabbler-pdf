// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents a PDF array: an ordered collection of PDF objects.
/// Serialized as <c>[obj1 obj2 obj3]</c>.
/// </summary>
public class PdfArray : PdfObject
{
    private static readonly byte[] OpenBracket = "["u8.ToArray();
    private static readonly byte[] CloseBracket = "]"u8.ToArray();
    private static readonly byte[] Space = " "u8.ToArray();

    private readonly List<PdfObject> _elements = [];

    /// <summary>Creates an empty PDF array.</summary>
    public PdfArray() : base(ARRAY) { }

    /// <summary>Creates a PDF array containing a single element.</summary>
    /// <param name="element">The initial element.</param>
    public PdfArray(PdfObject element) : base(ARRAY)
    {
        _elements.Add(element);
    }

    /// <summary>Creates a PDF array from a sequence of PDF objects.</summary>
    /// <param name="elements">The elements to include.</param>
    public PdfArray(IEnumerable<PdfObject> elements) : base(ARRAY)
    {
        _elements.AddRange(elements);
    }

    /// <summary>Creates a PDF array of <see cref="PdfNumber"/> values from doubles.</summary>
    /// <param name="values">The numeric values.</param>
    public PdfArray(params double[] values) : base(ARRAY)
    {
        foreach (double v in values)
            _elements.Add(new PdfNumber(v));
    }

    /// <summary>The number of elements in this array.</summary>
    public int Count => _elements.Count;

    /// <summary>Gets or sets the element at the specified index.</summary>
    public PdfObject this[int index]
    {
        get => _elements[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _elements[index] = value;
        }
    }

    /// <summary>Appends an element to the end of the array.</summary>
    /// <param name="obj">The <see cref="PdfObject"/> to add.</param>
    public void Add(PdfObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        _elements.Add(obj);
    }

    /// <summary>Inserts an element at the specified index.</summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="obj">The <see cref="PdfObject"/> to insert.</param>
    public void Insert(int index, PdfObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        _elements.Insert(index, obj);
    }

    /// <summary>Removes the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index) => _elements.RemoveAt(index);

    /// <summary>Returns <c>true</c> if the array contains the given object.</summary>
    /// <param name="obj">The object to locate.</param>
    /// <returns><c>true</c> if found; otherwise, <c>false</c>.</returns>
    public bool Contains(PdfObject obj) => _elements.Contains(obj);

    /// <inheritdoc/>
    public override void WriteTo(Stream outputStream)
    {
        outputStream.Write(OpenBracket);
        for (int i = 0; i < _elements.Count; i++)
        {
            if (i > 0)
                outputStream.Write(Space);
            _elements[i].WriteTo(outputStream);
        }
        outputStream.Write(CloseBracket);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[{string.Join(" ", _elements)}]";
    }
}
