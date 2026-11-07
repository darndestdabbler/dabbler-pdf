// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.IO.Compression;

namespace DabblerPdf.Pdf;

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

    /// <summary>Creates a stream with the given byte content.</summary>
    /// <param name="content">The raw stream bytes (before any compression).</param>
    public PdfStream(byte[] content) : base()
    {
        _rawBytes = content;
    }

    /// <summary>Creates an empty stream. Subclasses should call <see cref="SetRawBytes"/> to set content.</summary>
    protected PdfStream() : base()
    {
        _rawBytes = [];
    }

    /// <summary>
    /// Sets the raw byte content of the stream. Used by subclasses (e.g., PdfImage)
    /// that build their content after construction.
    /// </summary>
    protected void SetRawBytes(byte[] bytes)
    {
        _rawBytes = bytes;
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
        // PDF FlateDecode requires zlib format (RFC 1950). ZLibStream produces the
        // correct header and ADLER32 checksum, unlike DeflateStream (raw RFC 1951).
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(_rawBytes, 0, _rawBytes.Length);
        }

        _rawBytes = output.ToArray();
        _compressed = true;
        Put(PdfName.Filter, PdfName.FlateDecode);
    }

    /// <inheritdoc/>
    public override void WriteTo(Stream outputStream)
    {
        Put(PdfName.Length, new PdfNumber(_rawBytes.Length));
        base.WriteTo(outputStream);
        outputStream.Write(StreamOpen);
        outputStream.Write(_rawBytes);
        outputStream.Write(StreamClose);
    }
}
