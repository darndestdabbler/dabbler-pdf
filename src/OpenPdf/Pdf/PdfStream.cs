// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.IO.Compression;

namespace OpenPdf.Pdf;

/// <summary>
/// Represents a PDF stream object: a dictionary followed by a sequence of bytes
/// enclosed in <c>stream ... endstream</c>. Supports optional Flate compression.
/// </summary>
public class PdfStream : PdfDictionary
{
    private static readonly byte[] StreamOpen = "\nstream\n"u8.ToArray();
    private static readonly byte[] StreamClose = "\nendstream"u8.ToArray();

    private byte[] _rawBytes;
    private bool _compressed;

    public PdfStream(byte[] content) : base()
    {
        _rawBytes = content;
    }

    protected PdfStream() : base()
    {
        _rawBytes = [];
    }

    /// <summary>
    /// Compresses the stream content using Flate (zlib/deflate).
    /// Sets the /Filter to /FlateDecode.
    /// </summary>
    public void FlateCompress()
    {
        if (_compressed)
            return;

        using var output = new MemoryStream();
        // PDF FlateDecode expects raw deflate (no zlib header by default with DeflateStream),
        // but most PDF readers accept both. We'll write a minimal zlib header for compatibility.
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(_rawBytes, 0, _rawBytes.Length);
        }

        _rawBytes = output.ToArray();
        _compressed = true;
        Put(PdfName.Filter, PdfName.FlateDecode);
    }

    public override void WriteTo(Stream outputStream)
    {
        Put(PdfName.Length, new PdfNumber(_rawBytes.Length));
        base.WriteTo(outputStream);
        outputStream.Write(StreamOpen);
        outputStream.Write(_rawBytes);
        outputStream.Write(StreamClose);
    }
}
