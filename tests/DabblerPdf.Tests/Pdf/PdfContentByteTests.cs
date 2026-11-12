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

namespace DabblerPdf.Tests.Pdf;

public class PdfContentByteTests
{
    private static PdfContentByte CreateContentByte()
    {
        // Create a writer with a dummy stream to get a PdfContentByte
        var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document();
        var writer = PdfWriter.GetInstance(doc, ms);
        return writer.DirectContent;
    }

    private static string GetContent(PdfContentByte cb)
    {
        return Encoding.ASCII.GetString(cb.ToByteArray());
    }

    public class PathConstructionTests
    {
        [Fact]
        public void MoveTo_EmitsCorrectOperator()
        {
            var cb = CreateContentByte();
            cb.MoveTo(72, 720);
            Assert.Equal("72 720 m\n", GetContent(cb));
        }

        [Fact]
        public void LineTo_EmitsCorrectOperator()
        {
            var cb = CreateContentByte();
            cb.LineTo(540, 36);
            Assert.Equal("540 36 l\n", GetContent(cb));
        }

        [Fact]
        public void Rectangle_EmitsCorrectOperator()
        {
            var cb = CreateContentByte();
            cb.Rectangle(72, 700, 468, 20);
            Assert.Equal("72 700 468 20 re\n", GetContent(cb));
        }

        [Fact]
        public void CurveTo_EmitsCorrectOperator()
        {
            var cb = CreateContentByte();
            cb.CurveTo(100, 200, 300, 400, 500, 600);
            Assert.Equal("100 200 300 400 500 600 c\n", GetContent(cb));
        }

        [Fact]
        public void ClosePath_EmitsH()
        {
            var cb = CreateContentByte();
            cb.ClosePath();
            Assert.Equal("h\n", GetContent(cb));
        }

        [Fact]
        public void DecimalCoordinates_FormattedCorrectly()
        {
            var cb = CreateContentByte();
            cb.MoveTo(72.5, 720.123456);
            Assert.Equal("72.5 720.123456 m\n", GetContent(cb));
        }
    }

    public class PathPaintingTests
    {
        [Fact]
        public void Stroke_EmitsS()
        {
            var cb = CreateContentByte();
            cb.Stroke();
            Assert.Equal("S\n", GetContent(cb));
        }

        [Fact]
        public void Fill_EmitsF()
        {
            var cb = CreateContentByte();
            cb.Fill();
            Assert.Equal("f\n", GetContent(cb));
        }

        [Fact]
        public void FillStroke_EmitsB()
        {
            var cb = CreateContentByte();
            cb.FillStroke();
            Assert.Equal("B\n", GetContent(cb));
        }

        [Fact]
        public void EoFill_EmitsFStar()
        {
            var cb = CreateContentByte();
            cb.EoFill();
            Assert.Equal("f*\n", GetContent(cb));
        }

        [Fact]
        public void ClosePathStroke_EmitsLowercaseS()
        {
            var cb = CreateContentByte();
            cb.ClosePathStroke();
            Assert.Equal("s\n", GetContent(cb));
        }

        [Fact]
        public void NewPath_EmitsN()
        {
            var cb = CreateContentByte();
            cb.NewPath();
            Assert.Equal("n\n", GetContent(cb));
        }
    }

    public class ColorTests
    {
        [Fact]
        public void SetColorStroke_RGB_EmitsRG()
        {
            var cb = CreateContentByte();
            cb.SetColorStroke(new BaseColor(255, 0, 0));
            Assert.Equal("1 0 0 RG\n", GetContent(cb));
        }

        [Fact]
        public void SetColorFill_RGB_EmitsRg()
        {
            var cb = CreateContentByte();
            cb.SetColorFill(new BaseColor(0, 128, 255));
            var content = GetContent(cb);
            Assert.Contains("rg\n", content);
        }

        [Fact]
        public void SetColorStroke_Gray_EmitsG()
        {
            var cb = CreateContentByte();
            cb.SetColorStroke(new BaseColor(128, 128, 128));
            var content = GetContent(cb);
            Assert.Contains(" G\n", content);
            Assert.DoesNotContain("RG", content);
        }

        [Fact]
        public void SetColorFill_Gray_EmitsLowercaseG()
        {
            var cb = CreateContentByte();
            cb.SetColorFill(new BaseColor(0, 0, 0));
            Assert.Equal("0 g\n", GetContent(cb));
        }

        [Fact]
        public void SetColorStroke_SameColorTwice_EmitsOnce()
        {
            var cb = CreateContentByte();
            var red = new BaseColor(255, 0, 0);
            cb.SetColorStroke(red);
            cb.SetColorStroke(red);
            Assert.Equal("1 0 0 RG\n", GetContent(cb));
        }
    }

    public class GraphicsStateTests
    {
        [Fact]
        public void SaveRestore_EmitsQAndBigQ()
        {
            var cb = CreateContentByte();
            cb.SaveState();
            cb.RestoreState();
            Assert.Equal("q\nQ\n", GetContent(cb));
        }

        [Fact]
        public void RestoreState_WithoutSave_Throws()
        {
            var cb = CreateContentByte();
            Assert.Throws<InvalidOperationException>(() => cb.RestoreState());
        }

        [Fact]
        public void SetLineWidth_EmitsW()
        {
            var cb = CreateContentByte();
            cb.SetLineWidth(2.5);
            Assert.Equal("2.5 w\n", GetContent(cb));
        }

        [Fact]
        public void SetLineDash_EmitsPattern()
        {
            var cb = CreateContentByte();
            cb.SetLineDash([3, 2], 0);
            Assert.Equal("[3 2] 0 d\n", GetContent(cb));
        }

        [Fact]
        public void ConcatCtm_EmitsMatrix()
        {
            var cb = CreateContentByte();
            cb.ConcatCtm(1, 0, 0, 1, 72, 720);
            Assert.Equal("1 0 0 1 72 720 cm\n", GetContent(cb));
        }
    }

    public class TextTests
    {
        [Fact]
        public void BeginEndText_EmitsBtEt()
        {
            var cb = CreateContentByte();
            cb.BeginText();
            cb.EndText();
            Assert.Equal("BT\nET\n", GetContent(cb));
        }

        [Fact]
        public void SetTextMatrix_Position_EmitsTm()
        {
            var cb = CreateContentByte();
            cb.SetTextMatrix(72, 720);
            Assert.Equal("1 0 0 1 72 720 Tm\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_EmitsTj()
        {
            var cb = CreateContentByte();
            cb.ShowText("Hello");
            Assert.Equal("(Hello) Tj\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_EscapesSpecialChars()
        {
            var cb = CreateContentByte();
            cb.ShowText("a(b)c\\d");
            Assert.Equal("(a\\(b\\)c\\\\d) Tj\n", GetContent(cb));
        }

        [Fact]
        public void SetFontAndSize_EmitsTf()
        {
            var cb = CreateContentByte();
            cb.SetFontAndSize(new PdfName("Helvetica"), 12);
            Assert.Equal("/Helvetica 12 Tf\n", GetContent(cb));
        }

        private static readonly string FontPath =
            Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");

        [Fact]
        public void ShowText_IdentityHFont_EmitsHexGlyphIds()
        {
            var cb = CreateContentByte();
            cb.SetFontAndSize(BaseFont.CreateFont(FontPath, BaseFont.IdentityH, true), 12);
            cb.ShowText("Ah");
            Assert.EndsWith(" 12 Tf\n<0024004B> Tj\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_AfterRestoreState_UsesTheFontSetBeforeSaveState()
        {
            var cb = CreateContentByte();
            cb.SetFontAndSize(BaseFont.CreateFont(FontPath, BaseFont.IdentityH, true), 12);
            cb.SaveState();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false), 12);
            cb.ShowText("Ah");
            cb.RestoreState();
            cb.ShowText("Ah");
            Assert.EndsWith("(Ah) Tj\nQ\n<0024004B> Tj\n", GetContent(cb));
        }
    }

    public class ResetTests
    {
        [Fact]
        public void Reset_ClearsBuffer()
        {
            var cb = CreateContentByte();
            cb.MoveTo(0, 0);
            cb.Reset();
            Assert.Empty(cb.ToByteArray());
        }

        [Fact]
        public void Reset_AllowsColorReemission()
        {
            var cb = CreateContentByte();
            var red = new BaseColor(255, 0, 0);
            cb.SetColorStroke(red);
            cb.Reset();
            cb.SetColorStroke(red);
            // After reset, color should emit again
            Assert.Equal("1 0 0 RG\n", GetContent(cb));
        }
    }

    public class ControlCharEscapingTests
    {
        [Fact]
        public void ShowText_EscapesNewline()
        {
            var cb = CreateContentByte();
            cb.ShowText("line1\nline2");
            Assert.Equal("(line1\\nline2) Tj\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_EscapesCarriageReturn()
        {
            var cb = CreateContentByte();
            cb.ShowText("a\rb");
            Assert.Equal("(a\\rb) Tj\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_EscapesTab()
        {
            var cb = CreateContentByte();
            cb.ShowText("a\tb");
            Assert.Equal("(a\\tb) Tj\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_EscapesBackspace()
        {
            var cb = CreateContentByte();
            cb.ShowText("a\bb");
            Assert.Equal("(a\\bb) Tj\n", GetContent(cb));
        }

        [Fact]
        public void ShowText_EscapesFormFeed()
        {
            var cb = CreateContentByte();
            cb.ShowText("a\fb");
            Assert.Equal("(a\\fb) Tj\n", GetContent(cb));
        }
    }

    public class ClippingTests
    {
        [Fact]
        public void Clip_EmitsW()
        {
            var cb = CreateContentByte();
            cb.Clip();
            Assert.Equal("W\n", GetContent(cb));
        }

        [Fact]
        public void EoClip_EmitsWStar()
        {
            var cb = CreateContentByte();
            cb.EoClip();
            Assert.Equal("W*\n", GetContent(cb));
        }
    }

    public class AdditionalTextTests
    {
        [Fact]
        public void SetCharacterSpacing_EmitsTc()
        {
            var cb = CreateContentByte();
            cb.SetCharacterSpacing(0.5);
            Assert.Equal("0.5 Tc\n", GetContent(cb));
        }

        [Fact]
        public void SetWordSpacing_EmitsTw()
        {
            var cb = CreateContentByte();
            cb.SetWordSpacing(2);
            Assert.Equal("2 Tw\n", GetContent(cb));
        }

        [Fact]
        public void SetLeading_EmitsTL()
        {
            var cb = CreateContentByte();
            cb.SetLeading(14);
            Assert.Equal("14 TL\n", GetContent(cb));
        }

        [Fact]
        public void NewlineShowText_EmitsTStar()
        {
            var cb = CreateContentByte();
            cb.NewlineShowText();
            Assert.Equal("T*\n", GetContent(cb));
        }

        [Fact]
        public void SetTextMatrix_FullParams_EmitsTm()
        {
            var cb = CreateContentByte();
            cb.SetTextMatrix(2, 0, 0, 2, 72, 720);
            Assert.Equal("2 0 0 2 72 720 Tm\n", GetContent(cb));
        }
    }

    public class AdditionalGraphicsStateTests
    {
        [Fact]
        public void SetLineCap_EmitsJ()
        {
            var cb = CreateContentByte();
            cb.SetLineCap(1);
            Assert.Equal("1 J\n", GetContent(cb));
        }

        [Fact]
        public void SetLineJoin_EmitsLowercaseJ()
        {
            var cb = CreateContentByte();
            cb.SetLineJoin(2);
            Assert.Equal("2 j\n", GetContent(cb));
        }

        [Fact]
        public void SetMiterLimit_EmitsM()
        {
            var cb = CreateContentByte();
            cb.SetMiterLimit(10);
            Assert.Equal("10 M\n", GetContent(cb));
        }

        [Fact]
        public void EoFillStroke_EmitsBStar()
        {
            var cb = CreateContentByte();
            cb.EoFillStroke();
            Assert.Equal("B*\n", GetContent(cb));
        }

        [Fact]
        public void ClosePathFillStroke_EmitsLowercaseB()
        {
            var cb = CreateContentByte();
            cb.ClosePathFillStroke();
            Assert.Equal("b\n", GetContent(cb));
        }

        [Fact]
        public void SaveRestore_ResetsColorCache()
        {
            var cb = CreateContentByte();
            var red = new BaseColor(255, 0, 0);
            cb.SetColorStroke(red);
            cb.SaveState();
            cb.RestoreState();
            // After restore, color should be emitted again since cache is cleared
            cb.SetColorStroke(red);
            var content = GetContent(cb);
            // Should contain RG twice
            var count = content.Split("1 0 0 RG").Length - 1;
            Assert.Equal(2, count);
        }

        [Fact]
        public void NestedSaveRestore_TracksDepthCorrectly()
        {
            var cb = CreateContentByte();
            cb.SaveState();
            cb.SaveState();
            cb.RestoreState();
            cb.RestoreState();
            Assert.Equal("q\nq\nQ\nQ\n", GetContent(cb));
        }

        [Fact]
        public void DoubleRestore_Throws()
        {
            var cb = CreateContentByte();
            cb.SaveState();
            cb.RestoreState();
            Assert.Throws<InvalidOperationException>(() => cb.RestoreState());
        }
    }

    public class RoundRectangleTests
    {
        [Fact]
        public void RoundRectangle_EmitsExpectedOperators()
        {
            var cb = CreateContentByte();
            cb.RoundRectangle(100, 200, 200, 100, 10);
            var content = GetContent(cb);

            // Should contain: moveto, lineto, curveto, closepath
            Assert.Contains(" m\n", content);
            Assert.Contains(" l\n", content);
            Assert.Contains(" c\n", content);
            Assert.Contains("h\n", content);
        }

        [Fact]
        public void RoundRectangle_HasFourCurves()
        {
            var cb = CreateContentByte();
            cb.RoundRectangle(0, 0, 100, 80, 10);
            var content = GetContent(cb);

            // Count curveto operators (4 corners = 4 curves)
            var curveCount = content.Split(" c\n").Length - 1;
            Assert.Equal(4, curveCount);
        }

        [Fact]
        public void RoundRectangle_HasFourLines()
        {
            var cb = CreateContentByte();
            cb.RoundRectangle(0, 0, 100, 80, 10);
            var content = GetContent(cb);

            // Count lineto operators (4 sides)
            var lineCount = content.Split(" l\n").Length - 1;
            Assert.Equal(4, lineCount);
        }

        [Fact]
        public void RoundRectangle_RadiusClamped_ToHalfSmallDimension()
        {
            var cb = CreateContentByte();
            // Width=40, height=20, radius=30 → clamped to 10 (half of height)
            cb.RoundRectangle(0, 0, 40, 20, 30);
            var content = GetContent(cb);

            // Should still produce valid path (4 curves, closepath)
            var curveCount = content.Split(" c\n").Length - 1;
            Assert.Equal(4, curveCount);
            Assert.Contains("h\n", content);
        }

        [Fact]
        public void RoundRectangle_ZeroRadius_DegeneratesToRectangle()
        {
            var cb = CreateContentByte();
            cb.RoundRectangle(10, 20, 100, 50, 0);
            var content = GetContent(cb);

            // With radius=0, the Bézier control offset is 0, so curves degenerate
            // to points. The path should still contain moveto, lineto, curveto, closepath.
            Assert.Contains(" m\n", content);
            Assert.Contains(" l\n", content);
            Assert.Contains("h\n", content);
        }

        [Fact]
        public void RoundRectangle_StartPoint_IsCorrect()
        {
            var cb = CreateContentByte();
            cb.RoundRectangle(50, 100, 200, 80, 15);
            var content = GetContent(cb);

            // First operator should be moveto at (x + radius, y) = (65, 100)
            Assert.StartsWith("65 100 m\n", content);
        }

        [Fact]
        public void RoundRectangle_NegativeRadius_ClampedToZero()
        {
            var cb = CreateContentByte();
            cb.RoundRectangle(0, 0, 100, 100, -5);
            var content = GetContent(cb);

            // Should not throw; produces a valid path
            Assert.Contains(" m\n", content);
            Assert.Contains("h\n", content);
        }
    }

    public class FormatDoubleTests
    {
        [Theory]
        [InlineData(0.0, "0")]
        [InlineData(1.0, "1")]
        [InlineData(72.0, "72")]
        [InlineData(-1.0, "-1")]
        [InlineData(0.5, "0.5")]
        [InlineData(72.5, "72.5")]
        [InlineData(0.123456, "0.123456")]
        [InlineData(0.1234567, "0.123457")] // rounds to 6 places
        public void FormatDouble_ProducesExpectedOutput(double input, string expected)
        {
            Assert.Equal(expected, PdfContentByte.FormatDouble(input));
        }
    }
}
