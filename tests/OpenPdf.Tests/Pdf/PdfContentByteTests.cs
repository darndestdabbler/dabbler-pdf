// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using OpenPdf.Document;
using OpenPdf.Pdf;
using Xunit;

namespace OpenPdf.Tests.Pdf;

public class PdfContentByteTests
{
    private static PdfContentByte CreateContentByte()
    {
        // Create a writer with a dummy stream to get a PdfContentByte
        var ms = new MemoryStream();
        var doc = new OpenPdf.Document.Document();
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
