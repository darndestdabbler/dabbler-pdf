// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Image;
using Xunit;

namespace DabblerPdf.Tests.Image;

/// <summary>
/// Dedicated JPEG parsing tests: header parsing, marker navigation,
/// color space detection, edge cases, and error handling.
/// </summary>
public class JpegTests
{
    // ─── SOF Marker Variations ────────────────────────────────────

    [Fact]
    public void Jpeg_SOF0_Baseline_ParsesDimensions()
    {
        // SOF0 (0xC0) = Baseline DCT — the most common JPEG type
        var data = ImageTestHelpers.CreateMinimalJpeg(640, 480);
        var jpeg = new Jpeg(data);

        Assert.Equal(640, jpeg.PixelWidth);
        Assert.Equal(480, jpeg.PixelHeight);
        Assert.Equal(8, jpeg.BitsPerComponent);
        Assert.Equal(3, jpeg.ColorComponents);
    }

    [Fact]
    public void Jpeg_SOF2_Progressive_ParsesDimensions()
    {
        // Create a JPEG with SOF2 (progressive DCT) marker instead of SOF0
        var data = CreateJpegWithSofType(0xC2, 320, 240, 3);
        var jpeg = new Jpeg(data);

        Assert.Equal(320, jpeg.PixelWidth);
        Assert.Equal(240, jpeg.PixelHeight);
        Assert.Equal(3, jpeg.ColorComponents);
    }

    [Fact]
    public void Jpeg_SOF1_ExtendedSequential_ParsesDimensions()
    {
        var data = CreateJpegWithSofType(0xC1, 100, 200, 3);
        var jpeg = new Jpeg(data);

        Assert.Equal(100, jpeg.PixelWidth);
        Assert.Equal(200, jpeg.PixelHeight);
    }

    // ─── Color Space Detection ────────────────────────────────────

    [Fact]
    public void Jpeg_Grayscale_HasOneComponent()
    {
        var data = ImageTestHelpers.CreateMinimalJpeg(50, 50, components: 1);
        var jpeg = new Jpeg(data);

        Assert.Equal(1, jpeg.ColorComponents);
    }

    [Fact]
    public void Jpeg_Rgb_HasThreeComponents()
    {
        var data = ImageTestHelpers.CreateMinimalJpeg(50, 50, components: 3);
        var jpeg = new Jpeg(data);

        Assert.Equal(3, jpeg.ColorComponents);
    }

    [Fact]
    public void Jpeg_Cmyk_HasFourComponents()
    {
        var data = ImageTestHelpers.CreateMinimalJpeg(50, 50, components: 4);
        var jpeg = new Jpeg(data);

        Assert.Equal(4, jpeg.ColorComponents);
    }

    // ─── Marker Navigation ────────────────────────────────────────

    [Fact]
    public void Jpeg_SkipsAppMarkers_BeforeSOF()
    {
        // Create JPEG with APP1 (EXIF) marker before SOF0
        var data = CreateJpegWithExtraMarkerBeforeSof(0xE1, 50, 300, 150);
        var jpeg = new Jpeg(data);

        Assert.Equal(300, jpeg.PixelWidth);
        Assert.Equal(150, jpeg.PixelHeight);
    }

    [Fact]
    public void Jpeg_SkipsCommentMarker()
    {
        // COM marker (0xFE) with comment data before SOF
        var data = CreateJpegWithExtraMarkerBeforeSof(0xFE, 20, 256, 128);
        var jpeg = new Jpeg(data);

        Assert.Equal(256, jpeg.PixelWidth);
        Assert.Equal(128, jpeg.PixelHeight);
    }

    [Fact]
    public void Jpeg_HandlesPaddingBytes()
    {
        // Multiple 0xFF bytes before a marker (padding)
        var data = CreateJpegWithPadding(200, 100);
        var jpeg = new Jpeg(data);

        Assert.Equal(200, jpeg.PixelWidth);
        Assert.Equal(100, jpeg.PixelHeight);
    }

    // ─── Scaled Dimensions ────────────────────────────────────────

    [Fact]
    public void Jpeg_ScaledDimensions_DefaultToPixels()
    {
        var data = ImageTestHelpers.CreateMinimalJpeg(800, 600);
        var jpeg = new Jpeg(data);

        Assert.Equal(800.0, jpeg.ScaledWidth);
        Assert.Equal(600.0, jpeg.ScaledHeight);
    }

    [Fact]
    public void Jpeg_RawData_IsPreserved()
    {
        var data = ImageTestHelpers.CreateMinimalJpeg(10, 10);
        var jpeg = new Jpeg(data);

        Assert.Same(data, jpeg.RawData);
    }

    // ─── Error Handling ───────────────────────────────────────────

    [Fact]
    public void Jpeg_EmptyData_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Jpeg([]));
    }

    [Fact]
    public void Jpeg_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Jpeg([0xFF]));
    }

    [Fact]
    public void Jpeg_InvalidSOI_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Jpeg([0xFF, 0x00, 0x00]));
    }

    [Fact]
    public void Jpeg_NoSOFMarker_Throws()
    {
        // SOI + SOS (no SOF in between) — can't determine dimensions
        byte[] data = [0xFF, 0xD8, 0xFF, 0xDA, 0x00, 0x02];
        Assert.Throws<ArgumentException>(() => new Jpeg(data));
    }

    [Fact]
    public void Jpeg_TruncatedSOF_Throws()
    {
        // SOI + SOF0 with truncated data
        byte[] data = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x08, 0x08, 0x00];
        Assert.Throws<ArgumentException>(() => new Jpeg(data));
    }

    [Fact]
    public void Jpeg_InvalidSegmentLength_Throws()
    {
        // SOI + APP0 marker with segment length 0 (less than minimum of 2)
        byte[] data = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00];
        Assert.Throws<ArgumentException>(() => new Jpeg(data));
    }

    [Fact]
    public void Jpeg_SegmentLengthOne_Throws()
    {
        // SOI + APP0 marker with segment length 1 (less than minimum of 2)
        byte[] data = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x01];
        Assert.Throws<ArgumentException>(() => new Jpeg(data));
    }

    // ─── Edge Cases ───────────────────────────────────────────────

    [Fact]
    public void Jpeg_SmallestValidDimensions()
    {
        var data = ImageTestHelpers.CreateMinimalJpeg(1, 1);
        var jpeg = new Jpeg(data);

        Assert.Equal(1, jpeg.PixelWidth);
        Assert.Equal(1, jpeg.PixelHeight);
    }

    [Fact]
    public void Jpeg_LargeDimensions()
    {
        // 16-bit max for JPEG dimensions is 65535
        var data = ImageTestHelpers.CreateMinimalJpeg(4096, 4096);
        var jpeg = new Jpeg(data);

        Assert.Equal(4096, jpeg.PixelWidth);
        Assert.Equal(4096, jpeg.PixelHeight);
    }

    // ─── Helper Methods ───────────────────────────────────────────

    /// <summary>
    /// Creates a JPEG with a specific SOF marker type (SOF0=0xC0, SOF1=0xC1, SOF2=0xC2, etc.)
    /// </summary>
    private static byte[] CreateJpegWithSofType(byte sofMarker, int width, int height, int components)
    {
        using var ms = new MemoryStream();

        // SOI
        ms.Write([0xFF, 0xD8]);

        // SOF with specified type
        ms.Write([0xFF, sofMarker]);
        WriteUInt16BE(ms, (ushort)(8 + components * 3)); // Length
        ms.WriteByte(8); // Bits per component
        WriteUInt16BE(ms, (ushort)height);
        WriteUInt16BE(ms, (ushort)width);
        ms.WriteByte((byte)components);
        for (int i = 0; i < components; i++)
        {
            ms.WriteByte((byte)(i + 1)); // Component ID
            ms.WriteByte(0x11);           // Sampling factors
            ms.WriteByte(0);              // Quantization table ID
        }

        // EOI
        ms.Write([0xFF, 0xD9]);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a JPEG with an extra marker segment before SOF0.
    /// </summary>
    private static byte[] CreateJpegWithExtraMarkerBeforeSof(byte markerType, int payloadSize, int width, int height)
    {
        using var ms = new MemoryStream();

        // SOI
        ms.Write([0xFF, 0xD8]);

        // Extra marker with payload
        ms.Write([0xFF, markerType]);
        WriteUInt16BE(ms, (ushort)(payloadSize + 2)); // Length includes 2 length bytes
        for (int i = 0; i < payloadSize; i++)
            ms.WriteByte((byte)(i & 0xFF));

        // SOF0
        int components = 3;
        ms.Write([0xFF, 0xC0]);
        WriteUInt16BE(ms, (ushort)(8 + components * 3));
        ms.WriteByte(8);
        WriteUInt16BE(ms, (ushort)height);
        WriteUInt16BE(ms, (ushort)width);
        ms.WriteByte((byte)components);
        for (int i = 0; i < components; i++)
        {
            ms.WriteByte((byte)(i + 1));
            ms.WriteByte(0x11);
            ms.WriteByte(0);
        }

        // EOI
        ms.Write([0xFF, 0xD9]);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a JPEG with padding bytes (0xFF 0xFF) before the SOF marker.
    /// </summary>
    private static byte[] CreateJpegWithPadding(int width, int height)
    {
        using var ms = new MemoryStream();

        // SOI
        ms.Write([0xFF, 0xD8]);

        // Padding: multiple 0xFF bytes
        ms.Write([0xFF, 0xFF, 0xFF]);

        // SOF0
        int components = 3;
        ms.Write([0xFF, 0xC0]);
        WriteUInt16BE(ms, (ushort)(8 + components * 3));
        ms.WriteByte(8);
        WriteUInt16BE(ms, (ushort)height);
        WriteUInt16BE(ms, (ushort)width);
        ms.WriteByte((byte)components);
        for (int i = 0; i < components; i++)
        {
            ms.WriteByte((byte)(i + 1));
            ms.WriteByte(0x11);
            ms.WriteByte(0);
        }

        // EOI
        ms.Write([0xFF, 0xD9]);

        return ms.ToArray();
    }

    private static void WriteUInt16BE(MemoryStream ms, ushort value)
    {
        ms.WriteByte((byte)(value >> 8));
        ms.WriteByte((byte)value);
    }
}
