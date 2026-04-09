// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Text;

public class BaseFontTests
{
    public class CreateFontTests
    {
        [Theory]
        [InlineData(BaseFont.Helvetica)]
        [InlineData(BaseFont.HelveticaBold)]
        [InlineData(BaseFont.HelveticaOblique)]
        [InlineData(BaseFont.HelveticaBoldOblique)]
        [InlineData(BaseFont.TimesRoman)]
        [InlineData(BaseFont.TimesBold)]
        [InlineData(BaseFont.TimesItalic)]
        [InlineData(BaseFont.TimesBoldItalic)]
        [InlineData(BaseFont.Courier)]
        [InlineData(BaseFont.CourierBold)]
        [InlineData(BaseFont.CourierOblique)]
        [InlineData(BaseFont.CourierBoldOblique)]
        [InlineData(BaseFont.Symbol)]
        [InlineData(BaseFont.ZapfDingbats)]
        public void CreateFont_AllStandardFonts_Succeed(string fontName)
        {
            var font = BaseFont.CreateFont(fontName, BaseFont.WinAnsiEncoding, false);
            Assert.NotNull(font);
            Assert.Equal(fontName, font.PostscriptName);
        }

        [Fact]
        public void CreateFont_UnknownFont_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                BaseFont.CreateFont("NonExistent", BaseFont.WinAnsiEncoding, false));
        }

        [Fact]
        public void CreateFont_CachesInstances()
        {
            var font1 = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            var font2 = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            Assert.Same(font1, font2);
        }
    }

    public class TextMeasurementTests
    {
        // Expected widths verified against OpenPDF Java:
        // BaseFont bf = BaseFont.createFont("Helvetica", "Cp1252", false);
        // bf.getWidthPoint("Hello World", 12f) → 60.012

        [Fact]
        public void GetWidthPoint_HelloWorld_Helvetica12()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            double width = font.GetWidthPoint("Hello World", 12.0);

            // "Hello World" in Helvetica at 12pt:
            // H(722) + e(556) + l(222) + l(222) + o(556) + space(278) +
            // W(944) + o(556) + r(333) + l(222) + d(556) = 5167
            // Width = 5167 * 12 / 1000 = 62.004
            Assert.Equal(62.004, width, 3);
        }

        [Fact]
        public void GetWidthPoint_Courier_AllCharsSameWidth()
        {
            var font = BaseFont.CreateFont(BaseFont.Courier, BaseFont.WinAnsiEncoding, false);
            double width = font.GetWidthPoint("AAAA", 10.0);
            // Courier: all chars are 600 units wide
            // 4 * 600 * 10 / 1000 = 24.0
            Assert.Equal(24.0, width, 3);
        }

        [Fact]
        public void GetWidthPoint_EmptyString_ReturnsZero()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            Assert.Equal(0.0, font.GetWidthPoint("", 12.0));
        }

        [Fact]
        public void GetWidthPoint_TimesRoman_KnownValue()
        {
            var font = BaseFont.CreateFont(BaseFont.TimesRoman, BaseFont.WinAnsiEncoding, false);
            double width = font.GetWidthPoint("Test", 12.0);
            // T(611) + e(444) + s(389) + t(278) = 1722
            // Width = 1722 * 12 / 1000 = 20.664
            Assert.Equal(20.664, width, 3);
        }

        [Fact]
        public void GetCharWidth_Space_Helvetica()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            Assert.Equal(278, font.GetCharWidth(32));
        }

        [Fact]
        public void GetCharWidth_OutOfRange_ReturnsZero()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            Assert.Equal(0, font.GetCharWidth(300));
            Assert.Equal(0, font.GetCharWidth(-1));
        }
    }

    public class FontDescriptorTests
    {
        [Fact]
        public void Helvetica_Ascent_At12pt()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
            // 718 * 12 / 1000 = 8.616
            Assert.Equal(8.616, ascent, 3);
        }

        [Fact]
        public void Helvetica_Descent_IsNegative()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
            Assert.True(descent < 0);
            // -207 * 12 / 1000 = -2.484
            Assert.Equal(-2.484, descent, 3);
        }

        [Fact]
        public void TimesRoman_CapHeight()
        {
            var font = BaseFont.CreateFont(BaseFont.TimesRoman, BaseFont.WinAnsiEncoding, false);
            double capHeight = font.GetFontDescriptor(BaseFont.CAPHEIGHT, 12.0);
            // 662 * 12 / 1000 = 7.944
            Assert.Equal(7.944, capHeight, 3);
        }

        [Fact]
        public void HelveticaOblique_ItalicAngle()
        {
            var font = BaseFont.CreateFont(BaseFont.HelveticaOblique, BaseFont.WinAnsiEncoding, false);
            double angle = font.GetFontDescriptor(BaseFont.ITALICANGLE, 1.0);
            // Italic angle is -12 degrees (unscaled, but we pass fontSize=1 so it's -12/1000)
            // Actually: raw = -12, result = -12 * 1.0 / 1000 = -0.012
            Assert.Equal(-0.012, angle, 6);
        }

        [Fact]
        public void LineHeight_CanBeComputed()
        {
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
            double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
            double lineHeight = ascent - descent;
            // (718 + 207) * 12 / 1000 = 11.1
            Assert.Equal(11.1, lineHeight, 3);
        }
    }
}
