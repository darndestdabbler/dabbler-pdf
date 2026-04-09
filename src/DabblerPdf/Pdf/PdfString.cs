// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents a PDF string object. Supports both literal <c>(text)</c> and
/// hex-encoded <c>&lt;hex&gt;</c> serialization.
/// </summary>
public sealed class PdfString : PdfObject
{
    private readonly string _value;
    private readonly byte[] _originalBytes;

    /// <summary>Whether this string should be serialized as hex.</summary>
    public bool HexWriting { get; set; }

    public PdfString(string value) : base(STRING)
    {
        _value = value;
        _originalBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(value);
    }

    public PdfString(byte[] bytes) : base(STRING)
    {
        _originalBytes = bytes;
        _value = Encoding.GetEncoding("iso-8859-1").GetString(bytes);
    }

    public string Value => _value;

    public byte[] OriginalBytes => (byte[])_originalBytes.Clone();

    public override void WriteTo(Stream outputStream)
    {
        if (HexWriting)
        {
            WriteHex(outputStream);
        }
        else
        {
            WriteLiteral(outputStream);
        }
    }

    private void WriteLiteral(Stream outputStream)
    {
        outputStream.WriteByte((byte)'(');
        foreach (byte b in _originalBytes)
        {
            switch (b)
            {
                case (byte)'\\':
                case (byte)'(':
                case (byte)')':
                    outputStream.WriteByte((byte)'\\');
                    outputStream.WriteByte(b);
                    break;
                case (byte)'\r':
                    outputStream.WriteByte((byte)'\\');
                    outputStream.WriteByte((byte)'r');
                    break;
                case (byte)'\n':
                    outputStream.WriteByte((byte)'\\');
                    outputStream.WriteByte((byte)'n');
                    break;
                default:
                    outputStream.WriteByte(b);
                    break;
            }
        }
        outputStream.WriteByte((byte)')');
    }

    private static readonly byte[] HexChars = "0123456789ABCDEF"u8.ToArray();

    private void WriteHex(Stream outputStream)
    {
        outputStream.WriteByte((byte)'<');
        foreach (byte b in _originalBytes)
        {
            outputStream.WriteByte(HexChars[b >> 4]);
            outputStream.WriteByte(HexChars[b & 0x0F]);
        }
        outputStream.WriteByte((byte)'>');
    }

    public override string ToString() => _value;
}
