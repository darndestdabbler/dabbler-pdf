// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Document;

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
    public Rectangle(double width, double height)
        : this(0, 0, width, height)
    {
    }

    /// <summary>Creates a copy of an existing rectangle.</summary>
    public Rectangle(Rectangle other)
        : this(other.Left, other.Bottom, other.Right, other.Top)
    {
    }

    public double Width => Right - Left;
    public double Height => Top - Bottom;

    public override string ToString() => $"Rectangle({Left}, {Bottom}, {Right}, {Top})";
}
