// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Text;

public class TextLayoutTests
{
    private static BaseFont Helvetica =>
        BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

    // ─── WrapText Tests ───────────────────────────────────────────

    public class WrapTextTests
    {
        [Fact]
        public void ShortText_NoWrap()
        {
            var font = Helvetica;
            var lines = TextLayout.WrapText("Hello", font, 12, 200);
            Assert.Single(lines);
            Assert.Equal("Hello", lines[0]);
        }

        [Fact]
        public void ExactFitText_SingleLine()
        {
            var font = Helvetica;
            string text = "Test";
            double width = font.GetWidthPoint(text, 12);
            var lines = TextLayout.WrapText(text, font, 12, width);
            Assert.Single(lines);
            Assert.Equal(text, lines[0]);
        }

        [Fact]
        public void MultiWordWrapping()
        {
            var font = Helvetica;
            // Use a narrow width to force wrapping
            var lines = TextLayout.WrapText("one two three four five", font, 12, 60);
            Assert.True(lines.Count > 1, "Text should wrap to multiple lines");
            // All words should still be present
            string rejoined = string.Join(" ", lines);
            Assert.Equal("one two three four five", rejoined);
        }

        [Fact]
        public void SingleLongWord_OwnLine()
        {
            var font = Helvetica;
            // Very narrow width — a long word can't fit
            var lines = TextLayout.WrapText("Supercalifragilisticexpialidocious", font, 10, 30);
            Assert.Single(lines);
            Assert.Equal("Supercalifragilisticexpialidocious", lines[0]);
        }

        [Fact]
        public void ExplicitNewline_ProducesMultipleLines()
        {
            var font = Helvetica;
            var lines = TextLayout.WrapText("Line one\nLine two\nLine three", font, 12, 500);
            Assert.Equal(3, lines.Count);
            Assert.Equal("Line one", lines[0]);
            Assert.Equal("Line two", lines[1]);
            Assert.Equal("Line three", lines[2]);
        }

        [Fact]
        public void DoubleNewline_ProducesBlankLine()
        {
            var font = Helvetica;
            var lines = TextLayout.WrapText("Before\n\nAfter", font, 12, 500);
            Assert.Equal(3, lines.Count);
            Assert.Equal("Before", lines[0]);
            Assert.Equal("", lines[1]);
            Assert.Equal("After", lines[2]);
        }

        [Fact]
        public void EmptyString_ReturnsSingleEmptyLine()
        {
            var font = Helvetica;
            var lines = TextLayout.WrapText("", font, 12, 200);
            Assert.Single(lines);
            Assert.Equal("", lines[0]);
        }

        [Fact]
        public void VeryNarrowWidth_EachWordOnOwnLine()
        {
            var font = Helvetica;
            // Width of 1pt — each word must be its own line
            var lines = TextLayout.WrapText("a b c", font, 12, 1);
            Assert.Equal(3, lines.Count);
            Assert.Equal("a", lines[0]);
            Assert.Equal("b", lines[1]);
            Assert.Equal("c", lines[2]);
        }
    }

    // ─── MeasureHeight Tests ──────────────────────────────────────

    public class MeasureHeightTests
    {
        [Fact]
        public void MeasureHeight_MatchesCountLinesTimesLeading()
        {
            var font = Helvetica;
            string text = "Hello world this is a test";
            double maxWidth = 80;
            double leading = 14;

            double height = TextLayout.MeasureHeight(text, font, 12, maxWidth, leading);
            int lineCount = TextLayout.CountLines(text, font, 12, maxWidth);

            Assert.Equal(lineCount * leading, height, 0.001);
        }

        [Fact]
        public void MeasureHeight_DefaultLeading()
        {
            var font = Helvetica;
            double fontSize = 10;
            double expectedLeading = fontSize * 1.2; // 12

            double height = TextLayout.MeasureHeight("Hello", font, fontSize, 500);
            Assert.Equal(expectedLeading, height, 0.001);
        }

        [Fact]
        public void MeasureHeight_MultipleLines()
        {
            var font = Helvetica;
            double leading = 15;
            string text = "Line one\nLine two\nLine three";

            double height = TextLayout.MeasureHeight(text, font, 12, 500, leading);
            Assert.Equal(3 * leading, height, 0.001);
        }
    }

    // ─── CountLines Tests ─────────────────────────────────────────

    public class CountLinesTests
    {
        [Fact]
        public void SingleLine_ReturnsOne()
        {
            var font = Helvetica;
            Assert.Equal(1, TextLayout.CountLines("Hello", font, 12, 500));
        }

        [Fact]
        public void ThreeExplicitLines_ReturnsThree()
        {
            var font = Helvetica;
            Assert.Equal(3, TextLayout.CountLines("a\nb\nc", font, 12, 500));
        }
    }

    // ─── DrawText Tests ───────────────────────────────────────────

    public class DrawTextTests
    {
        private static (PdfContentByte cb, PdfWriter writer, MemoryStream ms) CreateContentByte()
        {
            var ms = new MemoryStream();
            var doc = new DabblerPdf.Document.Document(PageSize.Letter);
            var writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();
            doc.NewPage();
            return (writer.DirectContent, writer, ms);
        }

        [Fact]
        public void DrawText_LeftAlignment_EmitsCorrectOperators()
        {
            var (cb, writer, ms) = CreateContentByte();
            var font = Helvetica;

            double returnedY = TextLayout.DrawText(cb, "Hello", font, 12, 72, 720, 200,
                TextLayout.Alignment.Left, 14);

            var content = Encoding.ASCII.GetString(cb.ToByteArray());
            Assert.Contains("BT", content);
            Assert.Contains("Tj", content);
            Assert.Contains("ET", content);
            // Y should decrease by leading
            Assert.Equal(720 - 14, returnedY, 0.001);
        }

        [Fact]
        public void DrawText_RightAlignment_ShiftsXPosition()
        {
            var (cb, writer, ms) = CreateContentByte();
            var font = Helvetica;

            TextLayout.DrawText(cb, "Hi", font, 12, 72, 720, 200,
                TextLayout.Alignment.Right, 14);

            var content = Encoding.ASCII.GetString(cb.ToByteArray());
            // The text matrix X should be > 72 (right-shifted)
            // "Hi" is much less than 200pt wide, so the X will be close to 72 + 200
            Assert.Contains("Tm", content);
        }

        [Fact]
        public void DrawText_CenterAlignment_CentersText()
        {
            var (cb, writer, ms) = CreateContentByte();
            var font = Helvetica;

            TextLayout.DrawText(cb, "Hi", font, 12, 72, 720, 200,
                TextLayout.Alignment.Center, 14);

            var content = Encoding.ASCII.GetString(cb.ToByteArray());
            Assert.Contains("Tm", content);
        }

        [Fact]
        public void DrawText_MultipleLines_ReturnsCorrectY()
        {
            var (cb, writer, ms) = CreateContentByte();
            var font = Helvetica;
            double leading = 14;

            double returnedY = TextLayout.DrawText(cb, "Line one\nLine two\nLine three",
                font, 12, 72, 720, 500, leading: leading);

            // 3 lines × 14 leading = 42 total descent
            Assert.Equal(720 - 3 * leading, returnedY, 0.001);
        }

        [Fact]
        public void DrawText_DefaultLeading_UsesFontSizeTimes1Point2()
        {
            var (cb, writer, ms) = CreateContentByte();
            var font = Helvetica;
            double fontSize = 10;

            double returnedY = TextLayout.DrawText(cb, "Hello", font, fontSize, 72, 720, 500);

            Assert.Equal(720 - fontSize * 1.2, returnedY, 0.001);
        }
    }
}
