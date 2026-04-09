// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Document;
using Xunit;

namespace DabblerPdf.Tests.Document;

public class BaseColorTests
{
    [Fact]
    public void IntConstructor_ClampsValues()
    {
        var color = new BaseColor(300, -10, 128);
        Assert.Equal(255, color.Red);
        Assert.Equal(0, color.Green);
        Assert.Equal(128, color.Blue);
    }

    [Fact]
    public void DoubleConstructor_ConvertsCorrectly()
    {
        var color = new BaseColor(1.0, 0.5, 0.0);
        Assert.Equal(255, color.Red);
        Assert.Equal(128, color.Green);
        Assert.Equal(0, color.Blue);
    }

    [Fact]
    public void NormalizedValues_ReturnCorrectFractions()
    {
        var color = new BaseColor(255, 0, 128);
        Assert.Equal(1.0, color.RedNormalized);
        Assert.Equal(0.0, color.GreenNormalized);
        Assert.Equal(128 / 255.0, color.BlueNormalized, 6);
    }

    [Fact]
    public void IsGray_TrueWhenRGBEqual()
    {
        Assert.True(new BaseColor(128, 128, 128).IsGray);
        Assert.True(BaseColor.Black.IsGray);
        Assert.True(BaseColor.White.IsGray);
    }

    [Fact]
    public void IsGray_FalseWhenRGBDiffer()
    {
        Assert.False(new BaseColor(128, 129, 128).IsGray);
        Assert.False(BaseColor.Red_.IsGray);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var a = new BaseColor(10, 20, 30);
        var b = new BaseColor(10, 20, 30);
        var c = new BaseColor(10, 20, 31);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.True(a == b);
        Assert.True(a != c);
    }

    [Fact]
    public void StaticConstants_HaveExpectedValues()
    {
        Assert.Equal(new BaseColor(0, 0, 0), BaseColor.Black);
        Assert.Equal(new BaseColor(255, 255, 255), BaseColor.White);
        Assert.Equal(new BaseColor(255, 0, 0), BaseColor.Red_);
        Assert.Equal(new BaseColor(0, 255, 0), BaseColor.Green_);
        Assert.Equal(new BaseColor(0, 0, 255), BaseColor.Blue_);
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquals()
    {
        var a = new BaseColor(10, 20, 30);
        var b = new BaseColor(10, 20, 30);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_WithNull_ReturnsFalse()
    {
        var color = new BaseColor(10, 20, 30);
        Assert.False(color.Equals(null));
        Assert.False(color == null);
        Assert.True(color != null);
    }

    [Fact]
    public void Equality_BothNull_ReturnsTrue()
    {
        BaseColor? a = null;
        BaseColor? b = null;
        Assert.True(a == b);
    }

    [Fact]
    public void ToString_IncludesComponents()
    {
        var color = new BaseColor(10, 20, 30);
        var str = color.ToString();
        Assert.Contains("10", str);
        Assert.Contains("20", str);
        Assert.Contains("30", str);
    }

    [Fact]
    public void LightGray_And_DarkGray_AreGray()
    {
        Assert.True(BaseColor.LightGray.IsGray);
        Assert.True(BaseColor.DarkGray.IsGray);
        Assert.Equal(192, BaseColor.LightGray.Red);
        Assert.Equal(64, BaseColor.DarkGray.Red);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void DoubleConstructor_NonFiniteValues_DoNotThrow(double badValue)
    {
        var color = new BaseColor(badValue, badValue, badValue);
        // Non-finite values should be sanitized to 0
        Assert.Equal(0, color.Red);
        Assert.Equal(0, color.Green);
        Assert.Equal(0, color.Blue);
    }

    [Fact]
    public void DoubleConstructor_OutOfRange_ClampedByBothLayers()
    {
        // Values > 1.0 are clamped to 1.0 by SanitizeComponent, then * 255 = 255
        var color = new BaseColor(2.0, -1.0, 0.5);
        Assert.Equal(255, color.Red);
        Assert.Equal(0, color.Green);
        Assert.Equal(128, color.Blue);
    }
}
