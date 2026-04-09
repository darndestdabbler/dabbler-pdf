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
    public int Red { get; }
    public int Green { get; }
    public int Blue { get; }

    /// <summary>Red component normalized to 0.0–1.0.</summary>
    public double RedNormalized => Red / 255.0;

    /// <summary>Green component normalized to 0.0–1.0.</summary>
    public double GreenNormalized => Green / 255.0;

    /// <summary>Blue component normalized to 0.0–1.0.</summary>
    public double BlueNormalized => Blue / 255.0;

    public BaseColor(int red, int green, int blue)
    {
        Red = Clamp(red);
        Green = Clamp(green);
        Blue = Clamp(blue);
    }

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

    // Common colors
    public static readonly BaseColor Black = new(0, 0, 0);
    public static readonly BaseColor White = new(255, 255, 255);
    public static readonly BaseColor Red_ = new(255, 0, 0);
    public static readonly BaseColor Green_ = new(0, 255, 0);
    public static readonly BaseColor Blue_ = new(0, 0, 255);
    public static readonly BaseColor LightGray = new(192, 192, 192);
    public static readonly BaseColor DarkGray = new(64, 64, 64);

    public bool Equals(BaseColor? other) =>
        other is not null && Red == other.Red && Green == other.Green && Blue == other.Blue;

    public override bool Equals(object? obj) => obj is BaseColor other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Red, Green, Blue);
    public override string ToString() => $"BaseColor({Red}, {Green}, {Blue})";

    public static bool operator ==(BaseColor? left, BaseColor? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(BaseColor? left, BaseColor? right) => !(left == right);

    private static int Clamp(int value) => Math.Clamp(value, 0, 255);
}
