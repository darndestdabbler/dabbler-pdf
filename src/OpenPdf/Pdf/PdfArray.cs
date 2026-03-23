// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Pdf;

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

    public PdfArray() : base(ARRAY) { }

    public PdfArray(PdfObject element) : base(ARRAY)
    {
        _elements.Add(element);
    }

    public PdfArray(IEnumerable<PdfObject> elements) : base(ARRAY)
    {
        _elements.AddRange(elements);
    }

    public PdfArray(params double[] values) : base(ARRAY)
    {
        foreach (double v in values)
            _elements.Add(new PdfNumber(v));
    }

    public int Count => _elements.Count;

    public PdfObject this[int index]
    {
        get => _elements[index];
        set => _elements[index] = value;
    }

    public void Add(PdfObject obj) => _elements.Add(obj);

    public void Insert(int index, PdfObject obj) => _elements.Insert(index, obj);

    public void RemoveAt(int index) => _elements.RemoveAt(index);

    public bool Contains(PdfObject obj) => _elements.Contains(obj);

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

    public override string ToString()
    {
        return $"[{string.Join(" ", _elements)}]";
    }
}
