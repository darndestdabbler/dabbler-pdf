// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Pdf;

/// <summary>
/// Abstract base class for all PDF objects in the PDF object model.
/// Corresponds to the nine basic object types defined in the PDF specification.
/// </summary>
public abstract class PdfObject
{
    /// <summary>PDF object type constants matching the PDF specification.</summary>
    public const int BOOLEAN = 1;
    public const int NUMBER = 2;
    public const int STRING = 3;
    public const int NAME = 4;
    public const int ARRAY = 5;
    public const int DICTIONARY = 6;
    public const int STREAM = 7;
    public const int NULL = 8;
    public const int INDIRECT = 9;

    /// <summary>The type of this PDF object.</summary>
    public int Type { get; }

    /// <summary>
    /// Raw byte content of this object. Subclasses may store their serialized
    /// form here for simple types, or override <see cref="WriteTo"/> directly.
    /// </summary>
    protected byte[]? Bytes { get; set; }

    protected PdfObject(int type)
    {
        Type = type;
    }

    protected PdfObject(int type, byte[] bytes)
    {
        Type = type;
        Bytes = bytes;
    }

    /// <summary>
    /// Writes the PDF representation of this object to the given stream.
    /// </summary>
    public virtual void WriteTo(Stream outputStream)
    {
        if (Bytes != null)
        {
            outputStream.Write(Bytes);
        }
    }

    /// <summary>Returns the PDF representation as a byte array.</summary>
    public virtual byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        WriteTo(ms);
        return ms.ToArray();
    }

    public bool IsBoolean() => Type == BOOLEAN;
    public bool IsNumber() => Type == NUMBER;
    public bool IsString() => Type == STRING;
    public bool IsName() => Type == NAME;
    public bool IsArray() => Type == ARRAY;
    public bool IsDictionary() => Type == DICTIONARY;
    public bool IsStream() => Type == STREAM;
    public bool IsNull() => Type == NULL;
    public bool IsIndirect() => Type == INDIRECT;
}
