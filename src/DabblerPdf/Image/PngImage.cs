// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Buffers.Binary;
using System.IO.Compression;

namespace DabblerPdf.Image;

/// <summary>
/// Parses a PNG image for embedding in a PDF. Reads the IHDR for dimensions
/// and color type, concatenates and decompresses IDAT chunks, reverses
/// per-row PNG filters, and separates alpha if present.
/// </summary>
public sealed class PngImage : Image
{
    // PNG color type flags
    private const byte ColorTypeGrayscale = 0;
    private const byte ColorTypeRgb = 2;
    private const byte ColorTypePalette = 3;
    private const byte ColorTypeGrayscaleAlpha = 4;
    private const byte ColorTypeRgba = 6;

    private byte _colorType;
    private byte _bitDepth;

    // Palette data (for color type 3)
    private byte[]? _palette;
    private byte[]? _paletteAlpha; // tRNS chunk for palette transparency

    /// <summary>
    /// Decompressed, unfiltered pixel data (no alpha) ready for PDF embedding.
    /// </summary>
    public byte[] DecompressedPixels { get; private set; } = [];

    /// <summary>
    /// Separated alpha channel bytes (one byte per pixel), or null if no alpha.
    /// </summary>
    public byte[]? AlphaChannel { get; private set; }

    private PngImage()
    {
    }

    /// <summary>
    /// Parses a PNG image from raw bytes.
    /// </summary>
    public static PngImage GetImage(byte[] data)
    {
        var png = new PngImage { RawData = data };
        png.Parse(data);
        png.ScaledWidth = png.PixelWidth;
        png.ScaledHeight = png.PixelHeight;
        return png;
    }

    private void Parse(byte[] data)
    {
        // Validate PNG signature
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(signature))
            throw new ArgumentException("Not a valid PNG file (bad signature).");

        int offset = 8;
        using var idatStream = new MemoryStream();
        bool ihdrFound = false;

        while (offset + 8 <= data.Length)
        {
            int chunkLength = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
            string chunkType = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);
            int chunkDataStart = offset + 8;

            if (chunkDataStart + chunkLength + 4 > data.Length) // +4 for CRC
                throw new ArgumentException($"PNG chunk '{chunkType}' is truncated.");

            switch (chunkType)
            {
                case "IHDR":
                    ParseIhdr(data.AsSpan(chunkDataStart, chunkLength));
                    ihdrFound = true;
                    break;

                case "PLTE":
                    _palette = data.AsSpan(chunkDataStart, chunkLength).ToArray();
                    break;

                case "tRNS":
                    _paletteAlpha = data.AsSpan(chunkDataStart, chunkLength).ToArray();
                    break;

                case "IDAT":
                    idatStream.Write(data, chunkDataStart, chunkLength);
                    break;

                case "IEND":
                    goto doneChunks;
            }

            // Skip: chunk length (4) + type (4) + data + CRC (4)
            offset = chunkDataStart + chunkLength + 4;
        }

    doneChunks:
        if (!ihdrFound)
            throw new ArgumentException("PNG missing IHDR chunk.");

        // Decompress concatenated IDAT data (zlib-compressed)
        var compressedData = idatStream.ToArray();
        var rawPixels = DecompressZlib(compressedData);

        // Reverse PNG per-row filters and extract pixel/alpha data
        ProcessPixelData(rawPixels);
    }

    private void ParseIhdr(ReadOnlySpan<byte> ihdr)
    {
        if (ihdr.Length < 13)
            throw new ArgumentException("PNG IHDR chunk too short.");

        PixelWidth = (int)BinaryPrimitives.ReadUInt32BigEndian(ihdr);
        PixelHeight = (int)BinaryPrimitives.ReadUInt32BigEndian(ihdr[4..]);
        _bitDepth = ihdr[8];
        _colorType = ihdr[9];

        BitsPerComponent = _bitDepth;

        // Set color components based on color type (before alpha separation)
        ColorComponents = _colorType switch
        {
            ColorTypeGrayscale => 1,
            ColorTypeRgb => 3,
            ColorTypePalette => 3, // Expanded from palette to RGB
            ColorTypeGrayscaleAlpha => 1, // Alpha separated
            ColorTypeRgba => 3, // Alpha separated
            _ => throw new NotSupportedException($"PNG color type {_colorType} is not supported.")
        };
    }

    private static byte[] DecompressZlib(byte[] compressedData)
    {
        // PNG IDAT uses zlib format: 2-byte header + deflate data + 4-byte checksum
        // Skip the 2-byte zlib header
        if (compressedData.Length < 2)
            throw new ArgumentException("PNG IDAT data is too short.");

        using var input = new MemoryStream(compressedData, 2, compressedData.Length - 2);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private void ProcessPixelData(byte[] rawPixels)
    {
        int bytesPerPixel = GetBytesPerPixel(); // For filter distance (min 1 per PNG spec)
        int rowBytes = (PixelWidth * GetBitsPerPixel() + 7) / 8; // Actual data bytes per row
        int stride = 1 + rowBytes; // 1 byte filter type + pixel data

        if (rawPixels.Length < PixelHeight * stride)
            throw new ArgumentException(
                $"PNG pixel data too short: expected {PixelHeight * stride}, got {rawPixels.Length}.");

        // Reverse PNG filters
        var unfiltered = Unfilter(rawPixels, bytesPerPixel, stride);

        // Handle palette expansion and alpha separation
        switch (_colorType)
        {
            case ColorTypeGrayscale:
                HandleGrayscale(unfiltered, bytesPerPixel);
                break;

            case ColorTypeRgb:
                HandleRgb(unfiltered, bytesPerPixel);
                break;

            case ColorTypePalette:
                HandlePalette(unfiltered);
                break;

            case ColorTypeGrayscaleAlpha:
                HandleGrayscaleAlpha(unfiltered);
                break;

            case ColorTypeRgba:
                HandleRgba(unfiltered);
                break;
        }
    }

    private int GetBitsPerPixel() => _colorType switch
    {
        ColorTypeGrayscale => _bitDepth,
        ColorTypeRgb => _bitDepth * 3,
        ColorTypePalette => _bitDepth,
        ColorTypeGrayscaleAlpha => _bitDepth * 2,
        ColorTypeRgba => _bitDepth * 4,
        _ => throw new NotSupportedException()
    };

    private int GetBytesPerPixel() => Math.Max(1, GetBitsPerPixel() / 8);

    private byte[] Unfilter(byte[] rawPixels, int bytesPerPixel, int stride)
    {
        // Output: rows of pixel data without the filter byte
        int rowBytes = stride - 1;
        var result = new byte[PixelHeight * rowBytes];
        var prevRow = new byte[rowBytes]; // initialized to zeros (PNG spec: row above first row is all zeros)

        for (int y = 0; y < PixelHeight; y++)
        {
            int srcOffset = y * stride;
            int dstOffset = y * rowBytes;
            byte filterType = rawPixels[srcOffset];
            var currentRow = result.AsSpan(dstOffset, rowBytes);
            var srcRow = rawPixels.AsSpan(srcOffset + 1, rowBytes);

            switch (filterType)
            {
                case 0: // None
                    srcRow.CopyTo(currentRow);
                    break;

                case 1: // Sub
                    for (int x = 0; x < rowBytes; x++)
                    {
                        byte left = x >= bytesPerPixel ? currentRow[x - bytesPerPixel] : (byte)0;
                        currentRow[x] = (byte)(srcRow[x] + left);
                    }
                    break;

                case 2: // Up
                    for (int x = 0; x < rowBytes; x++)
                    {
                        currentRow[x] = (byte)(srcRow[x] + prevRow[x]);
                    }
                    break;

                case 3: // Average
                    for (int x = 0; x < rowBytes; x++)
                    {
                        byte left = x >= bytesPerPixel ? currentRow[x - bytesPerPixel] : (byte)0;
                        currentRow[x] = (byte)(srcRow[x] + ((left + prevRow[x]) >> 1));
                    }
                    break;

                case 4: // Paeth
                    for (int x = 0; x < rowBytes; x++)
                    {
                        byte left = x >= bytesPerPixel ? currentRow[x - bytesPerPixel] : (byte)0;
                        byte up = prevRow[x];
                        byte upperLeft = x >= bytesPerPixel ? prevRow[x - bytesPerPixel] : (byte)0;
                        currentRow[x] = (byte)(srcRow[x] + PaethPredictor(left, up, upperLeft));
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown PNG filter type: {filterType}");
            }

            currentRow.CopyTo(prevRow);
        }

        return result;
    }

    private static byte PaethPredictor(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);

        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }

    private void HandleGrayscale(byte[] unfiltered, int bytesPerPixel)
    {
        // For 8-bit grayscale, pixels are already in the right format
        if (_bitDepth == 8)
        {
            DecompressedPixels = unfiltered;
        }
        else if (_bitDepth == 16)
        {
            // Downsample 16-bit to 8-bit (take high byte)
            BitsPerComponent = 8;
            var result = new byte[PixelWidth * PixelHeight];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = unfiltered[i * 2];
            }
            DecompressedPixels = result;
        }
        else
        {
            // Sub-byte depths (1, 2, 4 bit) — expand to 8-bit
            BitsPerComponent = 8;
            DecompressedPixels = ExpandSubByte(unfiltered, _bitDepth);
        }
    }

    private void HandleRgb(byte[] unfiltered, int bytesPerPixel)
    {
        if (_bitDepth == 8)
        {
            DecompressedPixels = unfiltered;
        }
        else if (_bitDepth == 16)
        {
            // Downsample 16-bit RGB to 8-bit (take high bytes)
            BitsPerComponent = 8;
            int pixelCount = PixelWidth * PixelHeight;
            var result = new byte[pixelCount * 3];
            for (int i = 0; i < pixelCount; i++)
            {
                result[i * 3] = unfiltered[i * 6];
                result[i * 3 + 1] = unfiltered[i * 6 + 2];
                result[i * 3 + 2] = unfiltered[i * 6 + 4];
            }
            DecompressedPixels = result;
        }
    }

    private void HandlePalette(byte[] unfiltered)
    {
        if (_palette == null)
            throw new ArgumentException("PNG palette color type requires a PLTE chunk.");

        BitsPerComponent = 8;
        int pixelCount = PixelWidth * PixelHeight;

        // Expand palette indices to RGB
        byte[] indices;
        if (_bitDepth < 8)
        {
            indices = ExpandSubByte(unfiltered, _bitDepth);
        }
        else
        {
            indices = unfiltered;
        }

        var rgb = new byte[pixelCount * 3];
        byte[]? alpha = null;
        bool hasAlpha = _paletteAlpha != null && _paletteAlpha.Length > 0;
        if (hasAlpha)
            alpha = new byte[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            int idx = indices[i];
            int paletteOffset = idx * 3;
            if (paletteOffset + 2 < _palette.Length)
            {
                rgb[i * 3] = _palette[paletteOffset];
                rgb[i * 3 + 1] = _palette[paletteOffset + 1];
                rgb[i * 3 + 2] = _palette[paletteOffset + 2];
            }

            if (alpha != null)
            {
                alpha[i] = idx < _paletteAlpha!.Length ? _paletteAlpha[idx] : (byte)255;
            }
        }

        DecompressedPixels = rgb;

        // Only set AlphaChannel if there are actually non-opaque values
        if (alpha != null && alpha.Any(a => a != 255))
        {
            AlphaChannel = alpha;
        }
    }

    private void HandleGrayscaleAlpha(byte[] unfiltered)
    {
        int pixelCount = PixelWidth * PixelHeight;
        int srcBpp = _bitDepth == 16 ? 4 : 2;

        var gray = new byte[pixelCount];
        var alpha = new byte[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            if (_bitDepth == 16)
            {
                gray[i] = unfiltered[i * srcBpp];
                alpha[i] = unfiltered[i * srcBpp + 2];
            }
            else
            {
                gray[i] = unfiltered[i * srcBpp];
                alpha[i] = unfiltered[i * srcBpp + 1];
            }
        }

        BitsPerComponent = 8;
        DecompressedPixels = gray;
        AlphaChannel = alpha;
    }

    private void HandleRgba(byte[] unfiltered)
    {
        int pixelCount = PixelWidth * PixelHeight;
        int srcBpp = _bitDepth == 16 ? 8 : 4;

        var rgb = new byte[pixelCount * 3];
        var alpha = new byte[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            if (_bitDepth == 16)
            {
                rgb[i * 3] = unfiltered[i * srcBpp];
                rgb[i * 3 + 1] = unfiltered[i * srcBpp + 2];
                rgb[i * 3 + 2] = unfiltered[i * srcBpp + 4];
                alpha[i] = unfiltered[i * srcBpp + 6];
            }
            else
            {
                rgb[i * 3] = unfiltered[i * srcBpp];
                rgb[i * 3 + 1] = unfiltered[i * srcBpp + 1];
                rgb[i * 3 + 2] = unfiltered[i * srcBpp + 2];
                alpha[i] = unfiltered[i * srcBpp + 3];
            }
        }

        BitsPerComponent = 8;
        DecompressedPixels = rgb;
        AlphaChannel = alpha;
    }

    /// <summary>
    /// Expands sub-byte pixel values (1, 2, or 4 bits per pixel) to 8-bit values.
    /// </summary>
    private byte[] ExpandSubByte(byte[] data, int bitDepth)
    {
        int pixelCount = PixelWidth * PixelHeight;
        var result = new byte[pixelCount];
        int pixelsPerByte = 8 / bitDepth;
        int mask = (1 << bitDepth) - 1;
        int maxVal = mask;
        int rowBytes = (PixelWidth * bitDepth + 7) / 8;

        int outIdx = 0;
        for (int y = 0; y < PixelHeight; y++)
        {
            int rowStart = y * rowBytes;
            for (int x = 0; x < PixelWidth; x++)
            {
                int byteIdx = rowStart + (x * bitDepth) / 8;
                int bitOffset = 8 - bitDepth - ((x * bitDepth) % 8);
                int val = (data[byteIdx] >> bitOffset) & mask;
                // Scale to 0-255 range
                result[outIdx++] = (byte)(val * 255 / maxVal);
            }
        }

        return result;
    }
}
