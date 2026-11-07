// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Pdf;
using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Text;

public class Type1FontTests
{
    public class ConstructionTests
    {
        [Theory]
        [InlineData(BaseFont.Helvetica)]
        [InlineData(BaseFont.TimesRoman)]
        [InlineData(BaseFont.Courier)]
        [InlineData(BaseFont.Symbol)]
        [InlineData(BaseFont.ZapfDingbats)]
        public void Constructor_StandardFont_SetsPostscriptName(string fontName)
        {
            var font = new Type1Font(fontName);
            Assert.Equal(fontName, font.PostscriptName);
        }

        [Fact]
        public void Constructor_UnknownFont_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Type1Font("FakeFont"));
        }
    }

    public class WidthTests
    {
        [Theory]
        [InlineData(BaseFont.Helvetica, 'A', 667)]
        [InlineData(BaseFont.Helvetica, ' ', 278)]
        [InlineData(BaseFont.TimesRoman, 'A', 722)]
        [InlineData(BaseFont.TimesRoman, 'e', 444)]
        [InlineData(BaseFont.Courier, 'A', 600)]
        [InlineData(BaseFont.Courier, 'z', 600)]
        public void GetCharWidth_KnownCharacters_ReturnsCorrectWidth(string fontName, char c, int expectedWidth)
        {
            var font = new Type1Font(fontName);
            Assert.Equal(expectedWidth, font.GetCharWidth(c));
        }

        [Fact]
        public void GetCharWidth_NegativeIndex_ReturnsZero()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            Assert.Equal(0, font.GetCharWidth(-1));
        }

        [Fact]
        public void GetCharWidth_IndexBeyondArray_ReturnsZero()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            Assert.Equal(0, font.GetCharWidth(300));
        }

        [Fact]
        public void GetCharWidth_ControlChar_ReturnsZero()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            // Control characters (0-31) should have zero width
            Assert.Equal(0, font.GetCharWidth(0));
            Assert.Equal(0, font.GetCharWidth(10)); // newline
        }
    }

    public class DescriptorTests
    {
        [Fact]
        public void GetFontDescriptor_Ascent_ScalesToFontSize()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            // Helvetica ascent = 718, at 12pt: 718 * 12 / 1000 = 8.616
            double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
            Assert.Equal(8.616, ascent, 3);
        }

        [Fact]
        public void GetFontDescriptor_Descent_IsNegative()
        {
            var font = new Type1Font(BaseFont.TimesRoman);
            double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
            Assert.True(descent < 0);
        }

        [Fact]
        public void GetFontDescriptor_ItalicAngle_NonItalicIsZero()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            double angle = font.GetFontDescriptor(BaseFont.ITALICANGLE, 1000.0);
            Assert.Equal(0.0, angle, 6);
        }

        [Fact]
        public void GetFontDescriptor_ItalicAngle_ObliqueIsNegative()
        {
            var font = new Type1Font(BaseFont.HelveticaOblique);
            double angle = font.GetFontDescriptor(BaseFont.ITALICANGLE, 1000.0);
            Assert.True(angle < 0);
        }

        [Fact]
        public void GetFontDescriptor_UnknownKey_ReturnsZero()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            Assert.Equal(0.0, font.GetFontDescriptor(999, 12.0));
        }
    }

    public class FontDictionaryTests
    {
        [Fact]
        public void CreateFontDictionary_Helvetica_HasCorrectEntries()
        {
            var font = new Type1Font(BaseFont.Helvetica);
            var dict = font.CreateFontDictionary();

            using var ms = new MemoryStream();
            dict.WriteTo(ms);
            string output = System.Text.Encoding.ASCII.GetString(ms.ToArray());

            Assert.Contains("/Type /Font", output);
            Assert.Contains("/Subtype /Type1", output);
            Assert.Contains("/BaseFont /Helvetica", output);
            Assert.Contains("/Encoding /WinAnsiEncoding", output);
        }

        [Fact]
        public void CreateFontDictionary_Symbol_OmitsWinAnsiEncoding()
        {
            var font = new Type1Font(BaseFont.Symbol);
            var dict = font.CreateFontDictionary();

            using var ms = new MemoryStream();
            dict.WriteTo(ms);
            string output = System.Text.Encoding.ASCII.GetString(ms.ToArray());

            Assert.Contains("/BaseFont /Symbol", output);
            Assert.DoesNotContain("WinAnsiEncoding", output);
        }

        [Fact]
        public void CreateFontDictionary_ZapfDingbats_OmitsWinAnsiEncoding()
        {
            var font = new Type1Font(BaseFont.ZapfDingbats);
            var dict = font.CreateFontDictionary();

            using var ms = new MemoryStream();
            dict.WriteTo(ms);
            string output = System.Text.Encoding.ASCII.GetString(ms.ToArray());

            Assert.Contains("/BaseFont /ZapfDingbats", output);
            Assert.DoesNotContain("WinAnsiEncoding", output);
        }

        [Theory]
        [InlineData(BaseFont.TimesRoman)]
        [InlineData(BaseFont.Courier)]
        [InlineData(BaseFont.HelveticaBold)]
        public void CreateFontDictionary_StandardLatin_HasWinAnsiEncoding(string fontName)
        {
            var font = new Type1Font(fontName);
            var dict = font.CreateFontDictionary();

            using var ms = new MemoryStream();
            dict.WriteTo(ms);
            string output = System.Text.Encoding.ASCII.GetString(ms.ToArray());

            Assert.Contains("/Encoding /WinAnsiEncoding", output);
        }
    }
}
