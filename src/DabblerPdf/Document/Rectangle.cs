// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Document;

/// <summary>
/// Represents a rectangle in PDF coordinate space (origin bottom-left, units in points).
/// Used for page sizes, media boxes, and bounding boxes.
/// </summary>
public class Rectangle
{
    /// <summary>Lower-left X coordinate.</summary>
    public double Left { get; }

    /// <summary>Lower-left Y coordinate.</summary>
    public double Bottom { get; }

    /// <summary>Upper-right X coordinate.</summary>
    public double Right { get; }

    /// <summary>Upper-right Y coordinate.</summary>
    public double Top { get; }

    /// <summary>
    /// Creates a rectangle from explicit coordinates.
    /// </summary>
    /// <param name="left">Lower-left X coordinate in points.</param>
    /// <param name="bottom">Lower-left Y coordinate in points.</param>
    /// <param name="right">Upper-right X coordinate in points.</param>
    /// <param name="top">Upper-right Y coordinate in points.</param>
    public Rectangle(double left, double bottom, double right, double top)
    {
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    /// <summary>
    /// Creates a rectangle with lower-left at origin (0, 0).
    /// Convenient for page sizes: <c>new Rectangle(width, height)</c>.
    /// </summary>
    /// <param name="width">Width in points.</param>
    /// <param name="height">Height in points.</param>
    public Rectangle(double width, double height)
        : this(0, 0, width, height)
    {
    }

    /// <summary>Creates a copy of an existing rectangle.</summary>
    /// <param name="other">The rectangle to copy.</param>
    public Rectangle(Rectangle other)
        : this(other.Left, other.Bottom, other.Right, other.Top)
    {
    }

    /// <summary>The width of the rectangle in points (1/72 inch).</summary>
    public double Width => Right - Left;

    /// <summary>The height of the rectangle in points (1/72 inch).</summary>
    public double Height => Top - Bottom;

    /// <inheritdoc/>
    public override string ToString() => $"Rectangle({Left}, {Bottom}, {Right}, {Top})";
}
