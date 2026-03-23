// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using OpenPdf.Image;
using Xunit;

namespace OpenPdf.Tests.Image;

public class ImageTests
{
    // ─── JPEG Parsing ──────────────────────────────────────────────

    [Fact]
    public void Jpeg_ParsesDimensionsFromSOF0()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(320, 240);
        var image = new Jpeg(jpegBytes);

        Assert.Equal(320, image.PixelWidth);
        Assert.Equal(240, image.PixelHeight);
        Assert.Equal(8, image.BitsPerComponent);
        Assert.Equal(3, image.ColorComponents);
    }

    [Fact]
    public void Jpeg_DefaultScaledDimensionsMatchPixels()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(200, 100);
        var image = new Jpeg(jpegBytes);

        Assert.Equal(200.0, image.ScaledWidth);
        Assert.Equal(100.0, image.ScaledHeight);
    }

    [Fact]
    public void Jpeg_GrayscaleHasOneComponent()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(50, 50, components: 1);
        var image = new Jpeg(jpegBytes);

        Assert.Equal(1, image.ColorComponents);
    }

    [Fact]
    public void Jpeg_RawDataIsPreserved()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(10, 10);
        var image = new Jpeg(jpegBytes);

        Assert.Equal(jpegBytes, image.RawData);
    }

    [Fact]
    public void Jpeg_InvalidDataThrows()
    {
        Assert.Throws<ArgumentException>(() => new Jpeg([0x00, 0x00, 0x00]));
    }

    // ─── PNG Parsing ───────────────────────────────────────────────

    [Fact]
    public void Png_ParsesRgbDimensions()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalPng(100, 80);
        var image = PngImage.GetImage(pngBytes);

        Assert.Equal(100, image.PixelWidth);
        Assert.Equal(80, image.PixelHeight);
        Assert.Equal(8, image.BitsPerComponent);
        Assert.Equal(3, image.ColorComponents);
    }

    [Fact]
    public void Png_DecompressesRgbPixels()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalPng(2, 2, r: 255, g: 0, b: 0);
        var image = PngImage.GetImage(pngBytes);

        // 2x2 RGB = 12 bytes
        Assert.Equal(12, image.DecompressedPixels.Length);
        // First pixel should be red
        Assert.Equal(255, image.DecompressedPixels[0]); // R
        Assert.Equal(0, image.DecompressedPixels[1]);   // G
        Assert.Equal(0, image.DecompressedPixels[2]);   // B
        Assert.Null(image.AlphaChannel);
    }

    [Fact]
    public void Png_RgbaHasAlphaChannel()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalRgbaPng(3, 3, a: 128);
        var image = PngImage.GetImage(pngBytes);

        Assert.Equal(3, image.PixelWidth);
        Assert.Equal(3, image.PixelHeight);
        Assert.Equal(3, image.ColorComponents); // RGB (alpha separated)
        Assert.Equal(27, image.DecompressedPixels.Length); // 3x3x3
        Assert.NotNull(image.AlphaChannel);
        Assert.Equal(9, image.AlphaChannel!.Length); // 3x3
        Assert.All(image.AlphaChannel, a => Assert.Equal(128, a));
    }

    [Fact]
    public void Png_GrayscaleParsesCorrectly()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalGrayscalePng(4, 4, gray: 200);
        var image = PngImage.GetImage(pngBytes);

        Assert.Equal(4, image.PixelWidth);
        Assert.Equal(4, image.PixelHeight);
        Assert.Equal(1, image.ColorComponents);
        Assert.Equal(16, image.DecompressedPixels.Length); // 4x4x1
        Assert.All(image.DecompressedPixels, p => Assert.Equal(200, p));
    }

    [Fact]
    public void Png_InvalidSignatureThrows()
    {
        Assert.Throws<ArgumentException>(() => PngImage.GetImage([0x00, 0x00]));
    }

    // ─── Image.GetInstance (format detection) ──────────────────────

    [Fact]
    public void GetInstance_DetectsJpegFromBytes()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(10, 10);
        var image = OpenPdf.Image.Image.GetInstance(jpegBytes);

        Assert.IsType<Jpeg>(image);
    }

    [Fact]
    public void GetInstance_DetectsPngFromBytes()
    {
        var pngBytes = ImageTestHelpers.CreateMinimalPng(10, 10);
        var image = OpenPdf.Image.Image.GetInstance(pngBytes);

        Assert.IsType<PngImage>(image);
    }

    [Fact]
    public void GetInstance_UnrecognizedFormatThrows()
    {
        Assert.Throws<NotSupportedException>(() =>
            OpenPdf.Image.Image.GetInstance([0x00, 0x01, 0x02, 0x03]));
    }

    // ─── Scaling ───────────────────────────────────────────────────

    [Fact]
    public void ScaleToFit_PreservesAspectRatio()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(400, 200);
        var image = new Jpeg(jpegBytes);

        image.ScaleToFit(200, 200);

        // 400:200 = 2:1 aspect ratio. Fitting into 200x200, width is limiting.
        Assert.Equal(200.0, image.ScaledWidth, precision: 1);
        Assert.Equal(100.0, image.ScaledHeight, precision: 1);
    }

    [Fact]
    public void ScaleToFit_HeightLimiting()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(200, 400);
        var image = new Jpeg(jpegBytes);

        image.ScaleToFit(300, 200);

        // 200:400 = 1:2 aspect ratio. Fitting into 300x200, height is limiting.
        Assert.Equal(100.0, image.ScaledWidth, precision: 1);
        Assert.Equal(200.0, image.ScaledHeight, precision: 1);
    }

    [Fact]
    public void ScaleAbsolute_SetsExactDimensions()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(100, 100);
        var image = new Jpeg(jpegBytes);

        image.ScaleAbsolute(50, 75);

        Assert.Equal(50.0, image.ScaledWidth);
        Assert.Equal(75.0, image.ScaledHeight);
    }

    [Fact]
    public void ScalePercent_ScalesFromOriginalPixelDimensions()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(200, 100);
        var image = new Jpeg(jpegBytes);

        image.ScalePercent(50);

        Assert.Equal(100.0, image.ScaledWidth);
        Assert.Equal(50.0, image.ScaledHeight);
    }

    // ─── Positioning ───────────────────────────────────────────────

    [Fact]
    public void SetAbsolutePosition_SetsCoordinates()
    {
        var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(10, 10);
        var image = new Jpeg(jpegBytes);

        Assert.False(image.HasAbsolutePosition);

        image.SetAbsolutePosition(72.0, 650.0);

        Assert.True(image.HasAbsolutePosition);
        Assert.Equal(72.0, image.AbsoluteX);
        Assert.Equal(650.0, image.AbsoluteY);
    }
}
