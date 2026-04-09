// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Image;

/// <summary>
/// Base class for images that can be embedded in a PDF document.
/// Supports loading from file paths or byte arrays, scaling, and absolute positioning.
/// </summary>
public abstract class Image
{
    /// <summary>Original pixel width of the image.</summary>
    public int PixelWidth { get; protected set; }

    /// <summary>Original pixel height of the image.</summary>
    public int PixelHeight { get; protected set; }

    /// <summary>Bits per color component (typically 8).</summary>
    public int BitsPerComponent { get; protected set; } = 8;

    /// <summary>Number of color components (1 = gray, 3 = RGB, 4 = CMYK).</summary>
    public int ColorComponents { get; protected set; } = 3;

    /// <summary>
    /// The scaled width in points. Defaults to pixel width (1 pixel = 1 point).
    /// </summary>
    public double ScaledWidth { get; protected set; }

    /// <summary>
    /// The scaled height in points. Defaults to pixel height (1 pixel = 1 point).
    /// </summary>
    public double ScaledHeight { get; protected set; }

    /// <summary>Absolute X position in points (from left edge).</summary>
    public double AbsoluteX { get; private set; }

    /// <summary>Absolute Y position in points (from bottom edge).</summary>
    public double AbsoluteY { get; private set; }

    /// <summary>Whether an absolute position has been set.</summary>
    public bool HasAbsolutePosition { get; private set; }

    /// <summary>The raw image data bytes.</summary>
    public byte[] RawData { get; protected set; } = [];

    /// <summary>
    /// Sets the absolute position where this image will be placed on the page.
    /// </summary>
    public void SetAbsolutePosition(double x, double y)
    {
        AbsoluteX = x;
        AbsoluteY = y;
        HasAbsolutePosition = true;
    }

    /// <summary>
    /// Scales the image to fit within the given dimensions while preserving
    /// the aspect ratio. The image will be scaled down (never up) to fit.
    /// </summary>
    public void ScaleToFit(double maxWidth, double maxHeight)
    {
        double widthRatio = maxWidth / ScaledWidth;
        double heightRatio = maxHeight / ScaledHeight;
        double ratio = Math.Min(widthRatio, heightRatio);
        if (ratio < 1.0)
        {
            ScaledWidth *= ratio;
            ScaledHeight *= ratio;
        }
    }

    /// <summary>
    /// Scales the image to exact dimensions (ignoring aspect ratio).
    /// </summary>
    public void ScaleAbsolute(double width, double height)
    {
        ScaledWidth = width;
        ScaledHeight = height;
    }

    /// <summary>
    /// Scales the image by a percentage (100 = original size).
    /// </summary>
    public void ScalePercent(double percent)
    {
        double factor = percent / 100.0;
        ScaledWidth = PixelWidth * factor;
        ScaledHeight = PixelHeight * factor;
    }

    /// <summary>
    /// Loads an image from a file path. Detects the format from the file extension.
    /// </summary>
    public static Image GetInstance(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var bytes = File.ReadAllBytes(path);

        return extension switch
        {
            ".jpg" or ".jpeg" => new Jpeg(bytes),
            ".png" => PngImage.GetImage(bytes),
            _ => throw new NotSupportedException($"Image format '{extension}' is not supported.")
        };
    }

    /// <summary>
    /// Loads an image from a byte array. Detects the format from the file signature.
    /// </summary>
    public static Image GetInstance(byte[] data)
    {
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
            return new Jpeg(data);

        if (data.Length >= 8
            && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
            && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return PngImage.GetImage(data);

        throw new NotSupportedException("Unrecognized image format.");
    }
}
