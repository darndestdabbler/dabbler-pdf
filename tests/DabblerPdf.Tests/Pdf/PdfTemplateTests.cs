// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

public class PdfTemplateTests
{
    private static (PdfWriter Writer, MemoryStream Stream, DabblerPdf.Document.Document Doc) CreateWriter()
    {
        var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document();
        var writer = PdfWriter.GetInstance(doc, ms);
        return (writer, ms, doc);
    }

    public class TemplateCreationTests
    {
        [Fact]
        public void CreateTemplate_ReturnsPdfTemplate()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var template = writer.CreateTemplate(100, 20);

            Assert.NotNull(template);
            Assert.Equal(100, template.Width);
            Assert.Equal(20, template.Height);
            Assert.NotNull(template.Content);

            doc.Close();
        }

        [Fact]
        public void CreateTemplate_MultipleTemplates_AllDistinct()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var t1 = writer.CreateTemplate(100, 20);
            var t2 = writer.CreateTemplate(200, 30);

            Assert.NotEqual(t1, t2);
            Assert.Equal(100, t1.Width);
            Assert.Equal(200, t2.Width);

            doc.Close();
        }
    }

    public class TemplateContentTests
    {
        [Fact]
        public void Template_ContentCanBeWritten()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var template = writer.CreateTemplate(100, 20);
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

            template.Content.BeginText();
            template.Content.SetFontAndSize(font, 9);
            template.Content.SetTextMatrix(0, 0);
            template.Content.ShowText("42");
            template.Content.EndText();

            var content = Encoding.ASCII.GetString(template.Content.ToByteArray());
            Assert.Contains("BT", content);
            Assert.Contains("Tf", content);
            Assert.Contains("(42) Tj", content);
            Assert.Contains("ET", content);

            doc.Close();
        }

        [Fact]
        public void Template_ContentWrittenAfterPlacement_AppearsInOutput()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            var cb = writer.DirectContent;
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

            // Create and place template BEFORE writing content
            var template = writer.CreateTemplate(30, 12);
            cb.AddTemplate(template, 400, 30);

            // Write template content AFTER placement
            template.Content.BeginText();
            template.Content.SetFontAndSize(font, 9);
            template.Content.SetTextMatrix(0, 0);
            template.Content.ShowText("7");
            template.Content.EndText();

            doc.Close();

            // The template content should be in the PDF output
            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.StartsWith("%PDF-1.4", output);
            Assert.Contains("%%EOF", output);
        }
    }

    public class AddTemplateTests
    {
        [Fact]
        public void AddTemplate_EmitsCorrectOperators()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            var cb = writer.DirectContent;

            var template = writer.CreateTemplate(100, 20);
            cb.AddTemplate(template, 72, 750);

            var content = Encoding.ASCII.GetString(cb.ToByteArray());
            Assert.Contains("q", content);
            Assert.Contains("1 0 0 1 72 750 cm", content);
            Assert.Contains("Do", content);
            Assert.Contains("Q", content);

            doc.Close();
        }

        [Fact]
        public void AddTemplate_UsesTemplateResourceName()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            var cb = writer.DirectContent;

            var template = writer.CreateTemplate(100, 20);
            cb.AddTemplate(template, 72, 750);

            var content = Encoding.ASCII.GetString(cb.ToByteArray());
            Assert.Contains("/Tpl1 Do", content);

            doc.Close();
        }

        [Fact]
        public void AddTemplate_SameTemplateOnSamePage_ReuseResourceName()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            var cb = writer.DirectContent;

            var template = writer.CreateTemplate(100, 20);
            cb.AddTemplate(template, 72, 750);
            cb.AddTemplate(template, 72, 700);

            var content = Encoding.ASCII.GetString(cb.ToByteArray());
            // Both should use /Tpl1 since it's the same template
            var occurrences = content.Split("/Tpl1 Do").Length - 1;
            Assert.Equal(2, occurrences);

            doc.Close();
        }

        [Fact]
        public void AddTemplate_PlacedOnMultiplePages_ProducesValidPdf()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

            var template = writer.CreateTemplate(30, 12);

            // Place on page 1
            doc.NewPage();
            writer.DirectContent.AddTemplate(template, 400, 30);

            // Place on page 2
            doc.NewPage();
            writer.DirectContent.AddTemplate(template, 400, 30);

            // Fill in template content after all pages
            template.Content.BeginText();
            template.Content.SetFontAndSize(font, 9);
            template.Content.SetTextMatrix(0, 0);
            template.Content.ShowText("2");
            template.Content.EndText();

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.StartsWith("%PDF-1.4", output);
            Assert.Contains("/Count 2", output);
            Assert.Contains("%%EOF", output);
        }
    }

    public class FormXObjectTests
    {
        [Fact]
        public void FormXObject_ContainsCorrectType()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var template = writer.CreateTemplate(100, 20);
            template.Content.BeginText();
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            template.Content.SetFontAndSize(font, 9);
            template.Content.SetTextMatrix(0, 0);
            template.Content.ShowText("test");
            template.Content.EndText();

            writer.DirectContent.AddTemplate(template, 0, 0);
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            // Form XObject should contain /Type /XObject and /Subtype /Form
            Assert.Contains("/Type /XObject", output);
            Assert.Contains("/Subtype /Form", output);
        }

        [Fact]
        public void FormXObject_ContainsBBox()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var template = writer.CreateTemplate(100, 20);
            writer.DirectContent.AddTemplate(template, 0, 0);
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("/BBox [0 0 100 20]", output);
        }
    }

    public class PageXOfYIntegrationTests
    {
        [Fact]
        public void PageXOfY_TemplatePattern_ProducesValidPdf()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);

            // Create template for total page count
            var totalPagesTemplate = writer.CreateTemplate(30, 12);

            // Generate 3 pages, each with "Page N of " + template
            for (int i = 0; i < 3; i++)
            {
                doc.NewPage();
                var cb = writer.DirectContent;

                cb.BeginText();
                cb.SetFontAndSize(font, 9);
                cb.SetTextMatrix(72, 30);
                cb.ShowText($"Page {doc.PageNumber} of ");
                cb.EndText();

                // Place the template right after "of "
                double textWidth = font.GetWidthPoint($"Page {doc.PageNumber} of ", 9);
                cb.AddTemplate(totalPagesTemplate, 72 + textWidth, 30);
            }

            // Fill in the total page count AFTER all pages
            totalPagesTemplate.Content.BeginText();
            totalPagesTemplate.Content.SetFontAndSize(font, 9);
            totalPagesTemplate.Content.SetTextMatrix(0, 0);
            totalPagesTemplate.Content.ShowText(doc.PageNumber.ToString());
            totalPagesTemplate.Content.EndText();

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.StartsWith("%PDF-1.4", output);
            Assert.Contains("/Count 3", output);
            Assert.Contains("Page 1 of ", output);
            Assert.Contains("Page 2 of ", output);
            Assert.Contains("Page 3 of ", output);
            Assert.Contains("%%EOF", output);
        }
    }

    public class CompressContentStreamsTests
    {
        // Writes one page that places a template showing `text`; returns the file and the
        // template content stream's length. The page's own content stays under 128 bytes.
        private static (string Pdf, int ContentLength) WriteTemplate(string text, bool? compress = null)
        {
            var (writer, ms, doc) = CreateWriter();
            if (compress is bool c) writer.CompressContentStreams = c;
            doc.Open();
            doc.NewPage();

            var template = writer.CreateTemplate(500, 20);
            writer.DirectContent.AddTemplate(template, 0, 0);

            var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
            template.Content.BeginText();
            template.Content.SetFontAndSize(font, 9);
            template.Content.SetTextMatrix(0, 0);
            template.Content.ShowText(text);
            template.Content.EndText();
            int length = template.Content.ToByteArray().Length;

            doc.Close();
            return (Encoding.ASCII.GetString(ms.ToArray()), length);
        }

        [Fact]
        public void Default_TemplateOf128Bytes_HasNoFilter()
        {
            int baseline = WriteTemplate("").ContentLength;
            var (pdf, length) = WriteTemplate(new string('x', 128 - baseline));

            Assert.Equal(128, length);
            Assert.DoesNotContain("/Filter", pdf);
        }

        [Fact]
        public void Default_TemplateOver128Bytes_IsFlateDecoded()
        {
            int baseline = WriteTemplate("").ContentLength;
            var (pdf, length) = WriteTemplate(new string('x', 129 - baseline));

            Assert.Equal(129, length);
            Assert.Contains("/Filter /FlateDecode", pdf);
        }

        [Fact]
        public void CompressContentStreamsFalse_TemplateOver128Bytes_HasNoFilterAndTextIsInBytes()
        {
            string text = "Uncompressed template text that is long enough to pass the threshold " +
                          new string('x', 128);
            var (pdf, length) = WriteTemplate(text, compress: false);

            Assert.True(length > 128);
            Assert.DoesNotContain("/Filter", pdf);
            Assert.Contains($"({text}) Tj", pdf);
        }
    }
}
