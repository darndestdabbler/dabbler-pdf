// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using System.Text.RegularExpressions;
using DabblerPdf.Document;
using DabblerPdf.Image;
using DabblerPdf.Pdf;
using DabblerPdf.Tests.Image;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

public class PdfWriterTests
{
    private static (PdfWriter Writer, MemoryStream Stream, DabblerPdf.Document.Document Doc) CreateWriter()
    {
        var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document();
        var writer = PdfWriter.GetInstance(doc, ms);
        return (writer, ms, doc);
    }

    public class FactoryTests
    {
        [Fact]
        public void GetInstance_AssociatesWriterWithDocument()
        {
            var (writer, _, doc) = CreateWriter();
            Assert.NotNull(writer);
            Assert.NotNull(writer.DirectContent);
            Assert.Same(doc, writer.Document);
        }
    }

    public class PageLifecycleTests
    {
        [Fact]
        public void SinglePage_ProducesValidPdf()
        {
            var (_, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.StartsWith("%PDF-1.4", output);
            Assert.Contains("%%EOF", output);
        }

        [Fact]
        public void MultiplePages_ProducesMultiplePageObjects()
        {
            var (_, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            // Pages tree should have /Count 3
            Assert.Contains("/Count 3", output);
        }

        [Fact]
        public void NoExplicitPage_ProducesOneEmptyPage()
        {
            var (_, ms, doc) = CreateWriter();
            doc.Open();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("/Count 1", output);
            Assert.Contains("%%EOF", output);
        }

        [Fact]
        public void PdfHeader_ContainsVersionAndBinaryComment()
        {
            var (_, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            doc.Close();

            var bytes = ms.ToArray();
            // First 9 bytes should be %PDF-1.4\n
            Assert.Equal((byte)'%', bytes[0]);
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'D', bytes[2]);
            Assert.Equal((byte)'F', bytes[3]);

            // Second line should contain high-byte binary comment
            // to signal binary content to file transfer programs
            var headerStr = Encoding.ASCII.GetString(bytes, 0, Math.Min(30, bytes.Length));
            Assert.Contains("%PDF-1.4", headerStr);
        }
    }

    public class FontRegistrationTests
    {
        [Fact]
        public void AddFontObject_CachesByBaseFontName()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var fontDict1 = new PdfDictionary();
            fontDict1.Put(PdfName.BaseFont, new PdfName("Helvetica"));
            fontDict1.Put(PdfName.Subtype, PdfName.Type1);

            var fontDict2 = new PdfDictionary();
            fontDict2.Put(PdfName.BaseFont, new PdfName("Helvetica"));
            fontDict2.Put(PdfName.Subtype, PdfName.Type1);

            var ref1 = writer.AddFontObject(fontDict1);
            var ref2 = writer.AddFontObject(fontDict2);

            // Same base font name should return the same reference
            Assert.Equal(ref1.Number, ref2.Number);
        }

        [Fact]
        public void AddFontObject_DifferentFonts_GetDifferentReferences()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var fontDict1 = new PdfDictionary();
            fontDict1.Put(PdfName.BaseFont, new PdfName("Helvetica"));

            var fontDict2 = new PdfDictionary();
            fontDict2.Put(PdfName.BaseFont, new PdfName("Courier"));

            var ref1 = writer.AddFontObject(fontDict1);
            var ref2 = writer.AddFontObject(fontDict2);

            Assert.NotEqual(ref1.Number, ref2.Number);
        }

        [Fact]
        public void AddFontObject_MissingBaseFont_Throws()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var fontDict = new PdfDictionary();
            // No /BaseFont entry

            Assert.Throws<ArgumentException>(() => writer.AddFontObject(fontDict));
        }

        [Fact]
        public void GetCachedFontRef_ReturnsNullForUnknownFont()
        {
            var (writer, _, _) = CreateWriter();
            Assert.Null(writer.GetCachedFontRef("NonExistent"));
        }

        [Fact]
        public void AddPageFont_ReturnsDifferentNamesForDifferentFonts()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var fontDict1 = new PdfDictionary();
            fontDict1.Put(PdfName.BaseFont, new PdfName("Helvetica"));
            var ref1 = writer.AddFontObject(fontDict1);

            var fontDict2 = new PdfDictionary();
            fontDict2.Put(PdfName.BaseFont, new PdfName("Courier"));
            var ref2 = writer.AddFontObject(fontDict2);

            var name1 = writer.AddPageFont(new PdfName("Helvetica"), ref1);
            var name2 = writer.AddPageFont(new PdfName("Courier"), ref2);

            Assert.NotEqual(name1.Value, name2.Value);
            Assert.StartsWith("F", name1.Value);
            Assert.StartsWith("F", name2.Value);
        }

        [Fact]
        public void AddPageFont_SameFontTwice_ReturnsSameResourceName()
        {
            var (writer, _, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            var fontDict = new PdfDictionary();
            fontDict.Put(PdfName.BaseFont, new PdfName("Helvetica"));
            var fontRef = writer.AddFontObject(fontDict);

            var name1 = writer.AddPageFont(new PdfName("Helvetica"), fontRef);
            var name2 = writer.AddPageFont(new PdfName("Helvetica"), fontRef);

            Assert.Equal(name1.Value, name2.Value);
        }
    }

    public class ResourceDictionaryTests
    {
        [Fact]
        public void PageWithFont_HasFontInResources()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            // Register a font object and add it to the page
            var fontDict = new PdfDictionary();
            fontDict.Put(PdfName.BaseFont, new PdfName("Helvetica"));
            fontDict.Put(PdfName.Subtype, PdfName.Type1);
            var fontRef = writer.AddFontObject(fontDict);
            writer.AddPageFont(new PdfName("Helvetica"), fontRef);

            writer.DirectContent.SetFontAndSize(new PdfName("F1"), 12);
            writer.DirectContent.BeginText();
            writer.DirectContent.ShowText("Test");
            writer.DirectContent.EndText();

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("/Font", output);
        }
    }

    public class InfoDictionaryTests
    {
        [Fact]
        public void ClosedDocument_ContainsProducerInfo()
        {
            var (_, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("DabblerPdf.NET", output);
        }
    }

    public class ImageRegistrationTests
    {
        [Fact]
        public void AddPageImage_SameImageOnMultiplePages_WritesXObjectOnce()
        {
            var (writer, ms, doc) = CreateWriter();
            var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(10, 10);
            var image = DabblerPdf.Image.Image.GetInstance(jpegBytes);
            image.SetAbsolutePosition(0, 0);

            doc.Open();

            // Page 1: add the image
            doc.NewPage();
            writer.DirectContent.AddImage(image);

            // Page 2: add the same image instance
            doc.NewPage();
            writer.DirectContent.AddImage(image);

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());

            // The image XObject should only be written once (deduplication)
            var occurrences = Regex.Matches(output, "/Subtype /Image").Count;
            Assert.Equal(1, occurrences);
        }

        [Fact]
        public void AddPageImage_SameImageSamePage_ReturnsSameResourceName()
        {
            var (writer, ms, doc) = CreateWriter();
            var jpegBytes = ImageTestHelpers.CreateMinimalJpeg(10, 10);
            var image = DabblerPdf.Image.Image.GetInstance(jpegBytes);
            image.SetAbsolutePosition(0, 0);

            doc.Open();
            doc.NewPage();

            // Add the same image twice to the same page
            writer.DirectContent.AddImage(image);
            writer.DirectContent.AddImage(image);

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());

            // Only one /Subtype /Image should exist
            var imageObjCount = Regex.Matches(output, "/Subtype /Image").Count;
            Assert.Equal(1, imageObjCount);
        }
    }

    public class StructuralIntegrityTests
    {
        [Fact]
        public void ClosedDocument_HasXrefAndTrailer()
        {
            var (_, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("xref", output);
            Assert.Contains("trailer", output);
            Assert.Contains("startxref", output);
            Assert.Contains("/Root", output);
        }

        [Fact]
        public void ContentStreamAboveThreshold_IsCompressed()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            // Write enough content to exceed the 128-byte compression threshold
            for (int i = 0; i < 50; i++)
            {
                writer.DirectContent.MoveTo(i, i);
                writer.DirectContent.LineTo(i + 100, i + 100);
                writer.DirectContent.Stroke();
            }

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("/FlateDecode", output);
        }

        [Fact]
        public void SmallContentStream_IsNotCompressed()
        {
            var (writer, ms, doc) = CreateWriter();
            doc.Open();
            doc.NewPage();

            // Write minimal content (well under 128 bytes)
            writer.DirectContent.MoveTo(0, 0);
            writer.DirectContent.LineTo(100, 100);
            writer.DirectContent.Stroke();

            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.DoesNotContain("/FlateDecode", output);
        }
    }
}
