// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.IO.Compression;

namespace DabblerPdf.Tests.Image;

/// <summary>
/// Helper methods for creating minimal test images (JPEG and PNG) in memory.
/// </summary>
internal static class ImageTestHelpers
{
    /// <summary>
    /// Creates a minimal valid JPEG byte array with the given dimensions.
    /// This is a synthetic JPEG with valid markers but minimal scan data.
    /// </summary>
    public static byte[] CreateMinimalJpeg(int width, int height, int components = 3)
    {
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        // SOI
        bw.Write((byte)0xFF);
        bw.Write((byte)0xD8);

        // APP0 (JFIF header)
        bw.Write((byte)0xFF);
        bw.Write((byte)0xE0);
        WriteUInt16BE(bw, 16); // Length
        bw.Write("JFIF\0"u8);
        bw.Write((byte)1); // Version major
        bw.Write((byte)1); // Version minor
        bw.Write((byte)0); // Pixel aspect ratio
        WriteUInt16BE(bw, 1); // X density
        WriteUInt16BE(bw, 1); // Y density
        bw.Write((byte)0); // Thumbnail width
        bw.Write((byte)0); // Thumbnail height

        // SOF0 (Start of Frame - Baseline)
        bw.Write((byte)0xFF);
        bw.Write((byte)0xC0);
        WriteUInt16BE(bw, (ushort)(8 + components * 3)); // Length
        bw.Write((byte)8); // Bits per component
        WriteUInt16BE(bw, (ushort)height);
        WriteUInt16BE(bw, (ushort)width);
        bw.Write((byte)components); // Number of components
        for (int i = 0; i < components; i++)
        {
            bw.Write((byte)(i + 1)); // Component ID
            bw.Write((byte)0x11);     // Sampling factors (1x1)
            bw.Write((byte)0);        // Quantization table ID
        }

        // DHT (minimal Huffman table - DC)
        bw.Write((byte)0xFF);
        bw.Write((byte)0xC4);
        WriteUInt16BE(bw, 31); // Length
        bw.Write((byte)0x00); // DC table 0
        // 16 bytes of code counts (1 code of length 1)
        bw.Write((byte)1);
        for (int i = 0; i < 15; i++) bw.Write((byte)0);
        // Values
        for (int i = 0; i < 12; i++) bw.Write((byte)i);

        // DHT (minimal Huffman table - AC)
        bw.Write((byte)0xFF);
        bw.Write((byte)0xC4);
        WriteUInt16BE(bw, 31); // Length
        bw.Write((byte)0x10); // AC table 0
        bw.Write((byte)1);
        for (int i = 0; i < 15; i++) bw.Write((byte)0);
        for (int i = 0; i < 12; i++) bw.Write((byte)i);

        // DQT (quantization table)
        bw.Write((byte)0xFF);
        bw.Write((byte)0xDB);
        WriteUInt16BE(bw, 67); // Length
        bw.Write((byte)0); // Table ID 0, 8-bit precision
        for (int i = 0; i < 64; i++) bw.Write((byte)1); // All ones

        // SOS (Start of Scan)
        bw.Write((byte)0xFF);
        bw.Write((byte)0xDA);
        WriteUInt16BE(bw, (ushort)(6 + components * 2)); // Length
        bw.Write((byte)components);
        for (int i = 0; i < components; i++)
        {
            bw.Write((byte)(i + 1)); // Component selector
            bw.Write((byte)0x00);     // DC/AC table selectors
        }
        bw.Write((byte)0); // Start of spectral
        bw.Write((byte)63); // End of spectral
        bw.Write((byte)0); // Successive approx

        // Minimal scan data (just enough for valid structure)
        bw.Write((byte)0x00);

        // EOI
        bw.Write((byte)0xFF);
        bw.Write((byte)0xD9);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal valid PNG byte array with the given dimensions and a solid color.
    /// </summary>
    public static byte[] CreateMinimalPng(int width, int height, byte r = 255, byte g = 0, byte b = 0)
    {
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        // PNG signature
        bw.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        var ihdr = new byte[13];
        WriteUInt32BEToArray(ihdr, 0, (uint)width);
        WriteUInt32BEToArray(ihdr, 4, (uint)height);
        ihdr[8] = 8;  // Bit depth
        ihdr[9] = 2;  // Color type: RGB
        ihdr[10] = 0; // Compression
        ihdr[11] = 0; // Filter
        ihdr[12] = 0; // Interlace
        WritePngChunk(bw, "IHDR", ihdr);

        // IDAT chunk - build raw pixel data with PNG filter bytes
        var rawPixels = new byte[height * (1 + width * 3)]; // filter byte + RGB per row
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (1 + width * 3);
            rawPixels[rowStart] = 0; // Filter type: None
            for (int x = 0; x < width; x++)
            {
                int offset = rowStart + 1 + x * 3;
                rawPixels[offset] = r;
                rawPixels[offset + 1] = g;
                rawPixels[offset + 2] = b;
            }
        }

        // Compress with zlib (2-byte header + deflate + 4-byte checksum)
        var compressedIdat = ZlibCompress(rawPixels);
        WritePngChunk(bw, "IDAT", compressedIdat);

        // IEND chunk
        WritePngChunk(bw, "IEND", []);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal RGBA PNG (with alpha channel).
    /// </summary>
    public static byte[] CreateMinimalRgbaPng(int width, int height,
        byte r = 255, byte g = 0, byte b = 0, byte a = 128)
    {
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        // PNG signature
        bw.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        var ihdr = new byte[13];
        WriteUInt32BEToArray(ihdr, 0, (uint)width);
        WriteUInt32BEToArray(ihdr, 4, (uint)height);
        ihdr[8] = 8;  // Bit depth
        ihdr[9] = 6;  // Color type: RGBA
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WritePngChunk(bw, "IHDR", ihdr);

        // IDAT chunk
        var rawPixels = new byte[height * (1 + width * 4)];
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (1 + width * 4);
            rawPixels[rowStart] = 0; // Filter: None
            for (int x = 0; x < width; x++)
            {
                int offset = rowStart + 1 + x * 4;
                rawPixels[offset] = r;
                rawPixels[offset + 1] = g;
                rawPixels[offset + 2] = b;
                rawPixels[offset + 3] = a;
            }
        }

        var compressedIdat = ZlibCompress(rawPixels);
        WritePngChunk(bw, "IDAT", compressedIdat);

        // IEND
        WritePngChunk(bw, "IEND", []);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal grayscale PNG.
    /// </summary>
    public static byte[] CreateMinimalGrayscalePng(int width, int height, byte gray = 128)
    {
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        bw.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var ihdr = new byte[13];
        WriteUInt32BEToArray(ihdr, 0, (uint)width);
        WriteUInt32BEToArray(ihdr, 4, (uint)height);
        ihdr[8] = 8;  // Bit depth
        ihdr[9] = 0;  // Color type: Grayscale
        WritePngChunk(bw, "IHDR", ihdr);

        var rawPixels = new byte[height * (1 + width)];
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (1 + width);
            rawPixels[rowStart] = 0; // Filter: None
            for (int x = 0; x < width; x++)
            {
                rawPixels[rowStart + 1 + x] = gray;
            }
        }

        var compressedIdat = ZlibCompress(rawPixels);
        WritePngChunk(bw, "IDAT", compressedIdat);

        WritePngChunk(bw, "IEND", []);

        return ms.ToArray();
    }

    private static void WritePngChunk(BinaryWriter bw, string type, byte[] data)
    {
        WriteUInt32BE(bw, (uint)data.Length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        bw.Write(typeBytes);
        bw.Write(data);
        // CRC32 over type + data
        uint crc = Crc32(typeBytes, data);
        WriteUInt32BE(bw, crc);
    }

    private static byte[] ZlibCompress(byte[] data)
    {
        using var output = new MemoryStream();
        // Zlib header: CMF=0x78 (deflate, window 32k), FLG=0x01 (no dict, check bits)
        output.WriteByte(0x78);
        output.WriteByte(0x01);

        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }

        // Adler-32 checksum
        uint adler = Adler32(data);
        output.WriteByte((byte)(adler >> 24));
        output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));
        output.WriteByte((byte)adler);

        return output.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (byte d in data)
        {
            a = (a + d) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    private static void WriteUInt16BE(BinaryWriter bw, ushort value)
    {
        bw.Write((byte)(value >> 8));
        bw.Write((byte)value);
    }

    private static void WriteUInt32BE(BinaryWriter bw, uint value)
    {
        bw.Write((byte)(value >> 24));
        bw.Write((byte)(value >> 16));
        bw.Write((byte)(value >> 8));
        bw.Write((byte)value);
    }

    private static void WriteUInt32BEToArray(byte[] arr, int offset, uint value)
    {
        arr[offset] = (byte)(value >> 24);
        arr[offset + 1] = (byte)(value >> 16);
        arr[offset + 2] = (byte)(value >> 8);
        arr[offset + 3] = (byte)value;
    }

    // Simple CRC32 (PNG uses CRC-32/ISO-HDLC)
    private static readonly uint[] CrcTable = InitCrcTable();

    private static uint[] InitCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in type)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        foreach (byte b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }
}
