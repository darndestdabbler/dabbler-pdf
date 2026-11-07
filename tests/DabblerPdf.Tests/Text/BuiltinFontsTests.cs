// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Text;

public class BuiltinFontsTests
{
    public class IsStandardFontTests
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
        public void All14StandardFonts_AreRecognized(string fontName)
        {
            Assert.True(BuiltinFonts.IsStandardFont(fontName));
        }

        [Theory]
        [InlineData("Arial")]
        [InlineData("helvetica")] // case-sensitive
        [InlineData("")]
        public void NonStandardFonts_AreNotRecognized(string fontName)
        {
            Assert.False(BuiltinFonts.IsStandardFont(fontName));
        }
    }

    public class GetDescriptorTests
    {
        [Theory]
        [InlineData(BaseFont.Helvetica, 718, -207)]
        [InlineData(BaseFont.TimesRoman, 683, -217)]
        [InlineData(BaseFont.Courier, 629, -157)]
        public void Descriptor_HasCorrectAscentDescent(string fontName, double expectedAscent, double expectedDescent)
        {
            var desc = BuiltinFonts.GetDescriptor(fontName);
            Assert.Equal(expectedAscent, desc.Ascent);
            Assert.Equal(expectedDescent, desc.Descent);
        }

        [Fact]
        public void Descriptor_TimesItalic_HasNonZeroItalicAngle()
        {
            var desc = BuiltinFonts.GetDescriptor(BaseFont.TimesItalic);
            Assert.Equal(-15.5, desc.ItalicAngle);
        }

        [Fact]
        public void Descriptor_Helvetica_HasZeroItalicAngle()
        {
            var desc = BuiltinFonts.GetDescriptor(BaseFont.Helvetica);
            Assert.Equal(0, desc.ItalicAngle);
        }

        [Fact]
        public void Descriptor_UnknownFont_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuiltinFonts.GetDescriptor("FakeFont"));
        }
    }

    public class GetWidthsTests
    {
        [Fact]
        public void Helvetica_SpaceWidth_Is278()
        {
            var widths = BuiltinFonts.GetWidths(BaseFont.Helvetica);
            Assert.Equal(278, widths[32]);
        }

        [Fact]
        public void Courier_AllPrintable_Are600()
        {
            var widths = BuiltinFonts.GetWidths(BaseFont.Courier);
            for (int i = 32; i < 128; i++)
                Assert.Equal(600, widths[i]);
            for (int i = 160; i < 256; i++)
                Assert.Equal(600, widths[i]);
        }

        [Fact]
        public void Helvetica_ObliqueSharesWidths()
        {
            var regular = BuiltinFonts.GetWidths(BaseFont.Helvetica);
            var oblique = BuiltinFonts.GetWidths(BaseFont.HelveticaOblique);
            Assert.Same(regular, oblique);
        }

        [Fact]
        public void HelveticaBold_BoldObliqueSharesWidths()
        {
            var bold = BuiltinFonts.GetWidths(BaseFont.HelveticaBold);
            var boldOblique = BuiltinFonts.GetWidths(BaseFont.HelveticaBoldOblique);
            Assert.Same(bold, boldOblique);
        }

        [Fact]
        public void GetWidths_UnknownFont_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuiltinFonts.GetWidths("FakeFont"));
        }

        [Fact]
        public void WidthArrays_Have256Entries()
        {
            var widths = BuiltinFonts.GetWidths(BaseFont.Helvetica);
            Assert.Equal(256, widths.Length);
        }
    }

    public class ExtendedLatinCompletenessTests
    {
        // Characters 161-191 were previously missing from some fonts.
        // These tests verify the AFM-sourced width data is present.

        [Theory]
        [InlineData(BaseFont.Helvetica)]
        [InlineData(BaseFont.HelveticaBold)]
        [InlineData(BaseFont.TimesRoman)]
        [InlineData(BaseFont.TimesBold)]
        [InlineData(BaseFont.TimesItalic)]
        [InlineData(BaseFont.TimesBoldItalic)]
        public void ExtendedLatin_161To191_AllNonZero(string fontName)
        {
            var widths = BuiltinFonts.GetWidths(fontName);
            for (int i = 161; i <= 191; i++)
            {
                Assert.True(widths[i] > 0,
                    $"{fontName}: width[{i}] should be non-zero but was {widths[i]}");
            }
        }

        [Theory]
        [InlineData(BaseFont.TimesBold, 161, 333)]    // exclamdown
        [InlineData(BaseFont.TimesBold, 169, 747)]    // copyright
        [InlineData(BaseFont.TimesBold, 191, 500)]    // questiondown
        [InlineData(BaseFont.TimesItalic, 161, 389)]  // exclamdown
        [InlineData(BaseFont.TimesItalic, 172, 675)]  // logicalnot
        [InlineData(BaseFont.TimesBoldItalic, 170, 266)] // ordfeminine
        [InlineData(BaseFont.TimesBoldItalic, 181, 576)] // mu
        public void ExtendedLatin_SpotCheckValues(string fontName, int charCode, int expectedWidth)
        {
            var widths = BuiltinFonts.GetWidths(fontName);
            Assert.Equal(expectedWidth, widths[charCode]);
        }

        [Theory]
        [InlineData(BaseFont.Helvetica)]
        [InlineData(BaseFont.HelveticaBold)]
        [InlineData(BaseFont.TimesRoman)]
        [InlineData(BaseFont.TimesBold)]
        [InlineData(BaseFont.TimesItalic)]
        [InlineData(BaseFont.TimesBoldItalic)]
        [InlineData(BaseFont.Courier)]
        public void ExtendedLatin_192To255_AllNonZero(string fontName)
        {
            var widths = BuiltinFonts.GetWidths(fontName);
            for (int i = 192; i <= 255; i++)
            {
                Assert.True(widths[i] > 0,
                    $"{fontName}: width[{i}] should be non-zero but was {widths[i]}");
            }
        }
    }
}
