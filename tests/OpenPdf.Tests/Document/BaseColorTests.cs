// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using OpenPdf.Document;
using Xunit;

namespace OpenPdf.Tests.Document;

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
}
