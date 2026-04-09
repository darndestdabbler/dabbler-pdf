// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Buffers.Binary;

namespace DabblerPdf.Image;

/// <summary>
/// Represents a JPEG image. The raw JPEG bytes are passed through directly
/// to the PDF (using DCTDecode filter) without re-encoding.
/// Parses JFIF/EXIF markers to extract width, height, color space, and bit depth.
/// </summary>
public sealed class Jpeg : Image
{
    // JPEG marker constants
    private const byte MarkerPrefix = 0xFF;
    private const byte SOI = 0xD8;  // Start of Image
    private const byte SOF0 = 0xC0; // Baseline DCT
    private const byte SOF1 = 0xC1; // Extended sequential DCT
    private const byte SOF2 = 0xC2; // Progressive DCT
    private const byte SOF3 = 0xC3; // Lossless
    private const byte SOS = 0xDA;  // Start of Scan (end of headers)

    public Jpeg(byte[] data)
    {
        RawData = data;
        ParseJpegHeaders(data);
        ScaledWidth = PixelWidth;
        ScaledHeight = PixelHeight;
    }

    private void ParseJpegHeaders(byte[] data)
    {
        if (data.Length < 2 || data[0] != MarkerPrefix || data[1] != SOI)
            throw new ArgumentException("Not a valid JPEG file (missing SOI marker).");

        int offset = 2;
        while (offset < data.Length - 1)
        {
            // Find next marker
            if (data[offset] != MarkerPrefix)
                throw new ArgumentException($"Expected JPEG marker at offset {offset}.");

            byte marker = data[offset + 1];

            // Skip padding bytes (0xFF followed by 0xFF)
            if (marker == MarkerPrefix)
            {
                offset++;
                continue;
            }

            // Skip standalone markers (no length field)
            if (marker == 0x00 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD9))
            {
                offset += 2;
                continue;
            }

            // Read segment length (big-endian, includes the 2 length bytes)
            if (offset + 3 >= data.Length)
                break;

            int segmentLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 2));
            if (segmentLength < 2)
                throw new ArgumentException($"Invalid JPEG segment length {segmentLength} at offset {offset}.");

            // Check for SOF markers (Start of Frame) — these contain image dimensions
            if (marker is SOF0 or SOF1 or SOF2 or SOF3
                or 0xC5 or 0xC6 or 0xC7  // SOF5-SOF7
                or 0xC9 or 0xCA or 0xCB   // SOF9-SOF11
                or 0xCD or 0xCE or 0xCF)  // SOF13-SOF15
            {
                if (offset + 9 >= data.Length)
                    throw new ArgumentException("JPEG SOF segment is truncated.");

                BitsPerComponent = data[offset + 4];
                PixelHeight = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 5));
                PixelWidth = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 7));
                ColorComponents = data[offset + 9];
                return;
            }

            // Stop at SOS — no more metadata after this
            if (marker == SOS)
                break;

            // Skip to next segment, validating bounds
            int nextOffset = offset + 2 + segmentLength;
            if (nextOffset > data.Length)
                throw new ArgumentException($"JPEG segment at offset {offset} extends beyond end of file.");
            offset = nextOffset;
        }

        throw new ArgumentException("Could not find JPEG SOF marker — unable to determine image dimensions.");
    }
}
