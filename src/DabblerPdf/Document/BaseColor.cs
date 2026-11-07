// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Document;

/// <summary>
/// Represents a color in the PDF color space. Supports RGB and grayscale.
/// Values are stored as integers (0–255) and as normalized doubles (0.0–1.0).
/// </summary>
public sealed class BaseColor : IEquatable<BaseColor>
{
    /// <summary>Red component as an integer (0–255).</summary>
    public int Red { get; }

    /// <summary>Green component as an integer (0–255).</summary>
    public int Green { get; }

    /// <summary>Blue component as an integer (0–255).</summary>
    public int Blue { get; }

    /// <summary>Red component normalized to 0.0–1.0.</summary>
    public double RedNormalized => Red / 255.0;

    /// <summary>Green component normalized to 0.0–1.0.</summary>
    public double GreenNormalized => Green / 255.0;

    /// <summary>Blue component normalized to 0.0–1.0.</summary>
    public double BlueNormalized => Blue / 255.0;

    /// <summary>
    /// Creates a color from integer RGB components (0–255). Values outside that range are clamped.
    /// </summary>
    /// <param name="red">Red component (0–255).</param>
    /// <param name="green">Green component (0–255).</param>
    /// <param name="blue">Blue component (0–255).</param>
    public BaseColor(int red, int green, int blue)
    {
        Red = Clamp(red);
        Green = Clamp(green);
        Blue = Clamp(blue);
    }

    /// <summary>
    /// Creates a color from normalized RGB components (0.0–1.0). Values outside that range are clamped.
    /// </summary>
    /// <param name="red">Red component (0.0–1.0).</param>
    /// <param name="green">Green component (0.0–1.0).</param>
    /// <param name="blue">Blue component (0.0–1.0).</param>
    public BaseColor(double red, double green, double blue)
        : this(
            (int)(SanitizeComponent(red) * 255 + 0.5),
            (int)(SanitizeComponent(green) * 255 + 0.5),
            (int)(SanitizeComponent(blue) * 255 + 0.5))
    {
    }

    private static double SanitizeComponent(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 0.0;

    /// <summary>Whether this is a grayscale color (R == G == B).</summary>
    public bool IsGray => Red == Green && Green == Blue;

    /// <summary>Black (0, 0, 0).</summary>
    public static readonly BaseColor Black = new(0, 0, 0);

    /// <summary>White (255, 255, 255).</summary>
    public static readonly BaseColor White = new(255, 255, 255);

    /// <summary>Red (255, 0, 0).</summary>
    public static readonly BaseColor Red_ = new(255, 0, 0);

    /// <summary>Green (0, 255, 0).</summary>
    public static readonly BaseColor Green_ = new(0, 255, 0);

    /// <summary>Blue (0, 0, 255).</summary>
    public static readonly BaseColor Blue_ = new(0, 0, 255);

    /// <summary>Light gray (192, 192, 192).</summary>
    public static readonly BaseColor LightGray = new(192, 192, 192);

    /// <summary>Dark gray (64, 64, 64).</summary>
    public static readonly BaseColor DarkGray = new(64, 64, 64);

    /// <inheritdoc/>
    public bool Equals(BaseColor? other) =>
        other is not null && Red == other.Red && Green == other.Green && Blue == other.Blue;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BaseColor other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Red, Green, Blue);

    /// <inheritdoc/>
    public override string ToString() => $"BaseColor({Red}, {Green}, {Blue})";

    /// <summary>Determines whether two colors have equal RGB values.</summary>
    /// <param name="left">The first color to compare.</param>
    /// <param name="right">The second color to compare.</param>
    public static bool operator ==(BaseColor? left, BaseColor? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two colors have different RGB values.</summary>
    /// <param name="left">The first color to compare.</param>
    /// <param name="right">The second color to compare.</param>
    public static bool operator !=(BaseColor? left, BaseColor? right) => !(left == right);

    private static int Clamp(int value) => Math.Clamp(value, 0, 255);
}
