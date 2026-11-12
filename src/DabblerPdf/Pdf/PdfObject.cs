// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Abstract base class for all PDF objects in the PDF object model.
/// Corresponds to the nine basic object types defined in the PDF specification.
/// </summary>
public abstract class PdfObject
{
    /// <summary>PDF boolean type constant.</summary>
    public const int BOOLEAN = 1;

    /// <summary>PDF numeric type constant.</summary>
    public const int NUMBER = 2;

    /// <summary>PDF string type constant.</summary>
    public const int STRING = 3;

    /// <summary>PDF name type constant.</summary>
    public const int NAME = 4;

    /// <summary>PDF array type constant.</summary>
    public const int ARRAY = 5;

    /// <summary>PDF dictionary type constant.</summary>
    public const int DICTIONARY = 6;

    /// <summary>PDF stream type constant.</summary>
    public const int STREAM = 7;

    /// <summary>PDF null type constant.</summary>
    public const int NULL = 8;

    /// <summary>PDF indirect reference type constant.</summary>
    public const int INDIRECT = 9;

    /// <summary>The type of this PDF object.</summary>
    public int Type { get; }

    /// <summary>
    /// Raw byte content of this object. Subclasses may store their serialized
    /// form here for simple types, or override <see cref="WriteTo"/> directly.
    /// </summary>
    protected byte[]? Bytes { get; set; }

    /// <summary>Creates a PDF object of the specified type.</summary>
    /// <param name="type">One of the type constants (e.g., <see cref="BOOLEAN"/>, <see cref="NUMBER"/>).</param>
    protected PdfObject(int type)
    {
        Type = type;
    }

    /// <summary>Creates a PDF object of the specified type with initial byte content.</summary>
    /// <param name="type">One of the type constants.</param>
    /// <param name="bytes">The serialized PDF representation of this object.</param>
    protected PdfObject(int type, byte[] bytes)
    {
        Type = type;
        Bytes = bytes;
    }

    /// <summary>
    /// Writes the PDF representation of this object to the given stream.
    /// </summary>
    /// <param name="outputStream">The stream to write to.</param>
    public virtual void WriteTo(Stream outputStream)
    {
        if (Bytes != null)
        {
            outputStream.Write(Bytes);
        }
    }

    /// <summary>Returns the PDF representation as a byte array.</summary>
    /// <returns>A byte array containing the PDF representation.</returns>
    public virtual byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        WriteTo(ms);
        return ms.ToArray();
    }

    /// <summary>Returns <c>true</c> if this object is a PDF boolean.</summary>
    public bool IsBoolean() => Type == BOOLEAN;

    /// <summary>Returns <c>true</c> if this object is a PDF number.</summary>
    public bool IsNumber() => Type == NUMBER;

    /// <summary>Returns <c>true</c> if this object is a PDF string.</summary>
    public bool IsString() => Type == STRING;

    /// <summary>Returns <c>true</c> if this object is a PDF name.</summary>
    public bool IsName() => Type == NAME;

    /// <summary>Returns <c>true</c> if this object is a PDF array.</summary>
    public bool IsArray() => Type == ARRAY;

    /// <summary>Returns <c>true</c> if this object is a PDF dictionary.</summary>
    public bool IsDictionary() => Type == DICTIONARY;

    /// <summary>Returns <c>true</c> if this object is a PDF stream.</summary>
    public bool IsStream() => Type == STREAM;

    /// <summary>Returns <c>true</c> if this object is a PDF null.</summary>
    public bool IsNull() => Type == NULL;

    /// <summary>Returns <c>true</c> if this object is a PDF indirect reference.</summary>
    public bool IsIndirect() => Type == INDIRECT;
}
