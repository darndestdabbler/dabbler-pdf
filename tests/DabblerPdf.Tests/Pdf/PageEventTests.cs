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

public class PageEventTests
{
    private static (PdfWriter Writer, MemoryStream Stream, DabblerPdf.Document.Document Doc) CreateWriter()
    {
        var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document();
        var writer = PdfWriter.GetInstance(doc, ms);
        return (writer, ms, doc);
    }

    /// <summary>
    /// Records all page event callbacks in order for verification.
    /// </summary>
    private class RecordingPageEvent : IPdfPageEvent
    {
        public List<string> Events { get; } = new();
        public List<int> PageNumbers { get; } = new();

        public void OnOpenDocument(PdfWriter writer, DabblerPdf.Document.Document document)
        {
            Events.Add("OpenDocument");
        }

        public void OnStartPage(PdfWriter writer, DabblerPdf.Document.Document document)
        {
            Events.Add($"StartPage({document.PageNumber})");
            PageNumbers.Add(document.PageNumber);
        }

        public void OnEndPage(PdfWriter writer, DabblerPdf.Document.Document document)
        {
            Events.Add($"EndPage({document.PageNumber})");
        }

        public void OnCloseDocument(PdfWriter writer, DabblerPdf.Document.Document document)
        {
            Events.Add("CloseDocument");
        }
    }

    public class CallbackOrderingTests
    {
        [Fact]
        public void SinglePage_CorrectCallbackOrder()
        {
            var (writer, _, doc) = CreateWriter();
            var recorder = new RecordingPageEvent();
            writer.PageEvent = recorder;

            doc.Open();
            doc.NewPage();
            doc.Close();

            Assert.Equal(new[]
            {
                "OpenDocument",
                "StartPage(1)",
                "EndPage(1)",
                "CloseDocument"
            }, recorder.Events);
        }

        [Fact]
        public void TwoPages_CorrectCallbackOrder()
        {
            var (writer, _, doc) = CreateWriter();
            var recorder = new RecordingPageEvent();
            writer.PageEvent = recorder;

            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            Assert.Equal(new[]
            {
                "OpenDocument",
                "StartPage(1)",
                "EndPage(1)",
                "StartPage(2)",
                "EndPage(2)",
                "CloseDocument"
            }, recorder.Events);
        }

        [Fact]
        public void ThreePages_CorrectCallbackOrder()
        {
            var (writer, _, doc) = CreateWriter();
            var recorder = new RecordingPageEvent();
            writer.PageEvent = recorder;

            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            Assert.Equal(new[]
            {
                "OpenDocument",
                "StartPage(1)",
                "EndPage(1)",
                "StartPage(2)",
                "EndPage(2)",
                "StartPage(3)",
                "EndPage(3)",
                "CloseDocument"
            }, recorder.Events);
        }
    }

    public class PageNumberTests
    {
        [Fact]
        public void PageNumber_CorrectInStartPageCallbacks()
        {
            var (writer, _, doc) = CreateWriter();
            var recorder = new RecordingPageEvent();
            writer.PageEvent = recorder;

            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            Assert.Equal(new[] { 1, 2, 3 }, recorder.PageNumbers);
        }
    }

    public class DirectContentDrawingTests
    {
        /// <summary>
        /// Page event that draws a header on each page via DirectContent.
        /// </summary>
        private class HeaderDrawingEvent : IPdfPageEvent
        {
            public void OnStartPage(PdfWriter writer, DabblerPdf.Document.Document document)
            {
                var cb = writer.DirectContent;
                var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
                cb.BeginText();
                cb.SetFontAndSize(font, 10);
                cb.SetTextMatrix(72, 750);
                cb.ShowText($"Header - Page {document.PageNumber}");
                cb.EndText();
            }
        }

        [Fact]
        public void OnStartPage_DrawingAppearsInOutput()
        {
            var (writer, ms, doc) = CreateWriter();
            writer.PageEvent = new HeaderDrawingEvent();

            doc.Open();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("Header - Page 1", output);
        }

        /// <summary>
        /// Page event that draws a footer on each page via DirectContent.
        /// </summary>
        private class FooterDrawingEvent : IPdfPageEvent
        {
            public void OnEndPage(PdfWriter writer, DabblerPdf.Document.Document document)
            {
                var cb = writer.DirectContent;
                var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
                cb.BeginText();
                cb.SetFontAndSize(font, 9);
                cb.SetTextMatrix(72, 30);
                cb.ShowText($"Footer - Page {document.PageNumber}");
                cb.EndText();
            }
        }

        [Fact]
        public void OnEndPage_DrawingAppearsInOutput()
        {
            var (writer, ms, doc) = CreateWriter();
            writer.PageEvent = new FooterDrawingEvent();

            doc.Open();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Contains("Footer - Page 1", output);
        }

        [Fact]
        public void MultiPage_HeaderAndFooterOnEachPage()
        {
            var (writer, ms, doc) = CreateWriter();
            var recorder = new RecordingPageEvent();
            writer.PageEvent = recorder;

            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            // Verify both pages got Start and End events
            Assert.Contains("StartPage(1)", recorder.Events);
            Assert.Contains("EndPage(1)", recorder.Events);
            Assert.Contains("StartPage(2)", recorder.Events);
            Assert.Contains("EndPage(2)", recorder.Events);
        }
    }

    public class NullPageEventTests
    {
        [Fact]
        public void NullPageEvent_DoesNotBreakAnything()
        {
            var (writer, ms, doc) = CreateWriter();
            // PageEvent is null by default

            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            var output = Encoding.ASCII.GetString(ms.ToArray());
            Assert.StartsWith("%PDF-1.4", output);
            Assert.Contains("/Count 2", output);
            Assert.Contains("%%EOF", output);
        }
    }

    public class DefaultInterfaceMethodTests
    {
        /// <summary>
        /// Implements only OnEndPage — other methods use default (no-op) implementations.
        /// </summary>
        private class FooterOnlyEvent : IPdfPageEvent
        {
            public int EndPageCount { get; private set; }

            public void OnEndPage(PdfWriter writer, DabblerPdf.Document.Document document)
            {
                EndPageCount++;
            }
        }

        [Fact]
        public void PartialImplementation_OnlyOverriddenMethodsCalled()
        {
            var (writer, ms, doc) = CreateWriter();
            var footer = new FooterOnlyEvent();
            writer.PageEvent = footer;

            doc.Open();
            doc.NewPage();
            doc.NewPage();
            doc.Close();

            Assert.Equal(2, footer.EndPageCount);
        }
    }
}
