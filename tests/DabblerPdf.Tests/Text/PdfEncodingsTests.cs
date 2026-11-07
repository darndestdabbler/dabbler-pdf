// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Text;

public class PdfEncodingsTests
{
    public class WinAnsiToCharTests
    {
        [Theory]
        [InlineData(32, ' ')]
        [InlineData(65, 'A')]
        [InlineData(97, 'a')]
        [InlineData(48, '0')]
        [InlineData(127, '\u007F')]
        public void AsciiRange_MapsToIdentity(int byteValue, char expected)
        {
            Assert.Equal(expected, PdfEncodings.WinAnsiToChar(byteValue));
        }

        [Theory]
        [InlineData(160, '\u00A0')] // non-breaking space
        [InlineData(169, '\u00A9')] // copyright
        [InlineData(191, '\u00BF')] // inverted question mark
        [InlineData(224, '\u00E0')] // a grave
        [InlineData(255, '\u00FF')] // y diaeresis
        public void LatinSupplement_MapsToIdentity(int byteValue, char expected)
        {
            Assert.Equal(expected, PdfEncodings.WinAnsiToChar(byteValue));
        }

        [Theory]
        [InlineData(128, '\u20AC')] // Euro sign
        [InlineData(130, '\u201A')] // single low-9 quotation mark
        [InlineData(131, '\u0192')] // f with hook
        [InlineData(132, '\u201E')] // double low-9 quotation mark
        [InlineData(133, '\u2026')] // horizontal ellipsis
        [InlineData(134, '\u2020')] // dagger
        [InlineData(145, '\u2018')] // left single quotation mark
        [InlineData(146, '\u2019')] // right single quotation mark
        [InlineData(147, '\u201C')] // left double quotation mark
        [InlineData(148, '\u201D')] // right double quotation mark
        [InlineData(149, '\u2022')] // bullet
        [InlineData(150, '\u2013')] // en dash
        [InlineData(151, '\u2014')] // em dash
        [InlineData(153, '\u2122')] // trade mark sign
        public void SpecialRange_MapsToCorrectUnicode(int byteValue, char expected)
        {
            Assert.Equal(expected, PdfEncodings.WinAnsiToChar(byteValue));
        }

        [Theory]
        [InlineData(129)]
        [InlineData(141)]
        [InlineData(143)]
        [InlineData(144)]
        [InlineData(157)]
        public void UndefinedBytes_MapToReplacementChar(int byteValue)
        {
            Assert.Equal('\uFFFD', PdfEncodings.WinAnsiToChar(byteValue));
        }

        [Fact]
        public void NegativeValue_ReturnsNull()
        {
            Assert.Equal('\0', PdfEncodings.WinAnsiToChar(-1));
        }

        [Fact]
        public void ValueAbove255_ReturnsNull()
        {
            Assert.Equal('\0', PdfEncodings.WinAnsiToChar(256));
        }
    }

    public class CharToWinAnsiTests
    {
        [Theory]
        [InlineData('A', 65)]
        [InlineData(' ', 32)]
        [InlineData('0', 48)]
        [InlineData('\t', 9)]
        public void AsciiRange_ReturnsSameValue(char c, int expected)
        {
            Assert.Equal(expected, PdfEncodings.CharToWinAnsi(c));
        }

        [Theory]
        [InlineData('\u00A0', 160)] // non-breaking space
        [InlineData('\u00A9', 169)] // copyright
        [InlineData('\u00FF', 255)] // y diaeresis
        public void LatinSupplement_ReturnsSameValue(char c, int expected)
        {
            Assert.Equal(expected, PdfEncodings.CharToWinAnsi(c));
        }

        [Theory]
        [InlineData('\u20AC', 128)] // Euro sign
        [InlineData('\u201C', 147)] // left double quotation mark
        [InlineData('\u201D', 148)] // right double quotation mark
        [InlineData('\u2013', 150)] // en dash
        [InlineData('\u2014', 151)] // em dash
        [InlineData('\u2122', 153)] // trade mark sign
        public void SpecialUnicode_MapsToCorrectByte(char c, int expected)
        {
            Assert.Equal(expected, PdfEncodings.CharToWinAnsi(c));
        }

        [Fact]
        public void UnmappableCharacter_ReturnsMinusOne()
        {
            // CJK character — not in WinAnsi
            Assert.Equal(-1, PdfEncodings.CharToWinAnsi('\u4E00'));
        }

        [Fact]
        public void UnicodeInGap_ReturnsMinusOne()
        {
            // Unicode chars that map to undefined WinAnsi positions should return -1
            // e.g. U+0081 which would be byte 129 (undefined in WinAnsi)
            Assert.Equal(-1, PdfEncodings.CharToWinAnsi('\u0081'));
        }
    }

    public class RoundTripTests
    {
        [Fact]
        public void AllDefinedWinAnsiBytes_RoundTrip()
        {
            // Every defined WinAnsi byte (not \uFFFD) should round-trip
            for (int b = 0; b < 256; b++)
            {
                char c = PdfEncodings.WinAnsiToChar(b);
                if (c == '\uFFFD' || c == '\0')
                    continue;

                int result = PdfEncodings.CharToWinAnsi(c);
                Assert.True(result == b,
                    $"Round-trip failed for byte {b}: WinAnsiToChar={c} (U+{(int)c:X4}), CharToWinAnsi={result}");
            }
        }
    }
}
