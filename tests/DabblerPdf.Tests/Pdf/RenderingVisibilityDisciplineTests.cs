// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.IO.Compression;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;
using UglyToad.PdfPig;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

/// <summary>
/// Demonstrates the rendering-visibility test discipline captured in
/// <c>docs/MAINTAINING.md</c> → Testing →
/// "Bytes prove wiring; parsers prove rendering".
///
/// <para>
/// The sibling class <see cref="MultipageDirectContentRenderingTests"/> covers
/// the stacked-overdraw failure mode (two composer pages in one physical page's
/// content stream). This class covers a <i>different</i> visibility failure
/// mode — text placed off-page — to prove the discipline catches more than
/// just the bug that motivated the set.
/// </para>
///
/// <para>
/// A byte-level grep of the inflated content stream will happily report the
/// marker as present, because the text-showing operator <c>Tj</c> is genuinely
/// there. PdfPig, which models per-physical-page visible geometry, exposes the
/// marker's position and lets the test assert it lies outside the page's
/// MediaBox. That gap between "bytes say yes" and "rendering says off-page"
/// is exactly the reason the discipline exists: bytes prove wiring, parsers
/// prove rendering.
/// </para>
/// </summary>
public class RenderingVisibilityDisciplineTests
{
    private const string OffPageMarker = "OFF_PAGE_MARKER";

    private static byte[] BuildPdfWithOffPageText()
    {
        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var cb = writer.DirectContent;
        cb.BeginText();
        cb.SetFontAndSize(font, 14);
        // Letter height is 792pt. Y = 900 places the baseline 108pt above the
        // top edge of the page — the text is never reachable by a reader.
        cb.SetTextMatrix(72, 900);
        cb.ShowText(OffPageMarker);
        cb.EndText();

        doc.Close();
        return ms.ToArray();
    }

    [Fact]
    public void OffPageText_IsReportedByPdfPig_OutsideTheMediaBox()
    {
        // This is the primary discipline assertion: the rendering-aware
        // parser surfaces the off-page position, which a byte grep cannot.
        var pdfBytes = BuildPdfWithOffPageText();

        using var pdf = PdfDocument.Open(pdfBytes);
        Assert.Equal(1, pdf.NumberOfPages);

        var page = pdf.GetPage(1);
        var letters = page.Letters
            .Where(l => !string.IsNullOrWhiteSpace(l.Value))
            .ToList();

        Assert.NotEmpty(letters);

        // Every glyph's bounding box must sit entirely above the page's top
        // edge. This is the "visible at the unexpected position" assertion
        // from the spec — stated as a strict inequality on a stable PdfPig
        // API (BoundingBox.Bottom and Page.Height).
        foreach (var letter in letters)
        {
            Assert.True(
                letter.BoundingBox.Bottom > page.Height,
                $"Expected glyph '{letter.Value}' bottom={letter.BoundingBox.Bottom} " +
                $"to lie above page top={page.Height}, but it did not.");
        }
    }

    [Fact]
    public void OffPageText_ContradictsNaiveByteGrep_Discipline_Illustration()
    {
        // Demonstrates the exact failure mode the discipline exists to prevent:
        // a byte-level substring search of the inflated content stream would
        // pass, but the text is not actually visible to any reader. The test
        // keeps both assertions in one place so a future maintainer sees why
        // byte greps are insufficient for visibility claims.
        var pdfBytes = BuildPdfWithOffPageText();

        var inflatedContentStreams = ExtractInflatedContentStreams(pdfBytes);
        Assert.Contains(inflatedContentStreams, s => s.Contains(OffPageMarker));

        using var pdf = PdfDocument.Open(pdfBytes);
        var page = pdf.GetPage(1);
        Assert.All(
            page.Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)),
            letter => Assert.True(letter.BoundingBox.Bottom > page.Height));
    }

    /// <summary>
    /// Inflates every FlateDecode'd content stream in the PDF and returns the
    /// decompressed text. Small helper; intentionally tolerant of minor
    /// structural variation so it stays useful across PdfWriter revisions.
    /// </summary>
    private static List<string> ExtractInflatedContentStreams(byte[] pdfBytes)
    {
        var result = new List<string>();
        var raw = System.Text.Encoding.Latin1.GetString(pdfBytes);
        int cursor = 0;
        while (true)
        {
            int streamStart = raw.IndexOf("stream\n", cursor, StringComparison.Ordinal);
            if (streamStart < 0)
                streamStart = raw.IndexOf("stream\r\n", cursor, StringComparison.Ordinal);
            if (streamStart < 0) break;
            int dataStart = raw.IndexOf('\n', streamStart) + 1;
            int streamEnd = raw.IndexOf("\nendstream", dataStart, StringComparison.Ordinal);
            if (streamEnd < 0) break;

            int length = streamEnd - dataStart;
            var bytes = new byte[length];
            Array.Copy(pdfBytes, dataStart, bytes, 0, length);

            try
            {
                using var ms = new MemoryStream(bytes);
                using var deflate = new ZLibStream(ms, CompressionMode.Decompress);
                using var reader = new StreamReader(deflate, System.Text.Encoding.Latin1);
                result.Add(reader.ReadToEnd());
            }
            catch
            {
                // Not Flate-compressed — add the raw text so the same grep works.
                result.Add(System.Text.Encoding.Latin1.GetString(bytes));
            }

            cursor = streamEnd + "endstream".Length;
        }
        return result;
    }
}
