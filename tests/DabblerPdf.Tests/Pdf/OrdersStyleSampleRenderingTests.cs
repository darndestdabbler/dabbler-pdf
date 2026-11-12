// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.IO.Compression;
using System.Text.RegularExpressions;
using DabblerPdf.Samples.Samples;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

/// <summary>
/// Rendering-visibility regression coverage for the Orders-style landscape
/// sample (<see cref="S11_OrdersStyleReport"/>), which mirrors the
/// <c>dabbler-platform</c> Orders By Retail Business report's multi-page
/// DirectContent pattern.
///
/// <para>
/// The sample exists because PdfPig-based text-presence assertions in
/// <c>MultipageDirectContentRenderingTests</c> proved insufficient to catch
/// a field report where the title was text-extractable but
/// human-invisible in Adobe Reader. This class tightens the discipline
/// further: bounding-box assertions for title position, title-color-stream
/// inspection, "no stacked composer page 2 on physical page 1" assertions,
/// and exactly-one footer-per-page assertions.
/// </para>
///
/// <para>
/// The tests drive the exact code path the runnable sample executes — the
/// sample is instantiated and <c>Generate</c> is called against a
/// <see cref="Path.GetTempPath"/> directory, then the produced bytes are
/// handed to PdfPig. Duplicating the band-drawing logic here would let the
/// test and the sample drift apart and defeat the purpose of the sample.
/// </para>
/// </summary>
public class OrdersStyleSampleRenderingTests
{
    private static byte[] GenerateSamplePdf()
    {
        var tempDir = Path.Combine(Path.GetTempPath(),
            "DabblerPdfTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            new S11_OrdersStyleReport().Generate(tempDir);
            return File.ReadAllBytes(Path.Combine(tempDir, "11-orders-style-report.pdf"));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void Sample_ProducesAtLeastThreePhysicalPages()
    {
        // Deliberate: the sample is sized (48 synthetic rows across 4 groups)
        // to span multiple pages so the NewPage boundary is exercised more
        // than once — a guard against fixes that work for page 1→2 but
        // regress on page 2→3.
        var pdfBytes = GenerateSamplePdf();

        using var pdf = PdfDocument.Open(pdfBytes);
        Assert.True(pdf.NumberOfPages >= 3,
            $"Expected at least 3 physical pages, got {pdf.NumberOfPages}.");
    }

    [Fact]
    public void Page1_ShowsTitle_AsHelveticaBold_InTopTwentyPercentOfPage()
    {
        // Primary assertion: the title literal is a contiguous word on page 1
        // whose bounding-box top Y lies in the top 20% of the page. "Top" in
        // PdfPig coordinates is the high-Y end (PDF origin is bottom-left),
        // so the threshold compares letter.BoundingBox.Top against
        // 0.80 * page.Height.
        var pdfBytes = GenerateSamplePdf();

        using var pdf = PdfDocument.Open(pdfBytes);
        var page1 = pdf.GetPage(1);

        var words = page1.GetWords().ToList();
        var titleWords = ExtractTitleLetters(page1);
        Assert.True(titleWords.Count > 0,
            $"Expected title '{S11_OrdersStyleReport.TitleText}' on page 1 — " +
            $"found none. Page text was:\n{page1.Text}");

        double topThreshold = 0.80 * page1.Height;
        foreach (var letter in titleWords)
        {
            Assert.True(letter.BoundingBox.Top >= topThreshold,
                $"Title letter '{letter.Value}' at top={letter.BoundingBox.Top} is " +
                $"below the top-20% threshold ({topThreshold}). Page height={page1.Height}.");
        }

        // Font assertion: the title is rendered in a Helvetica-Bold subset.
        // PdfPig surfaces the font name from the /BaseFont entry — Helvetica-Bold
        // is the literal the Base14 emitter uses.
        foreach (var letter in titleWords)
        {
            Assert.Contains("Helvetica-Bold", letter.FontName,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Page1_FooterSaysPageOneOfN_NeverPageTwoOfAnything()
    {
        // Regression guard for the stacking bug: if composer-page 2 had
        // re-rendered into physical page 1's content stream, the "Page 2 of N"
        // footer would appear on page 1 alongside "Page 1 of N".
        var pdfBytes = GenerateSamplePdf();

        using var pdf = PdfDocument.Open(pdfBytes);
        var page1Text = pdf.GetPage(1).Text;

        Assert.Contains("Page 1 of", page1Text);
        Assert.DoesNotMatch(new Regex(@"Page\s+2\s+of"), page1Text);
    }

    [Fact]
    public void EveryPage_HasExactlyOnePageXOfNFooter()
    {
        var pdfBytes = GenerateSamplePdf();

        using var pdf = PdfDocument.Open(pdfBytes);
        var pattern = new Regex(@"Page\s+(\d+)\s+of\s+(\d+)");

        for (int i = 1; i <= pdf.NumberOfPages; i++)
        {
            var text = pdf.GetPage(i).Text;
            var matches = pattern.Matches(text);
            var match = Assert.Single(matches);
            Assert.Equal(i.ToString(), match.Groups[1].Value);
            Assert.Equal(pdf.NumberOfPages.ToString(), match.Groups[2].Value);
        }
    }

    [Fact]
    public void TitleFillColor_Immediately_Precedes_TitleTextOperator_In_ContentStream()
    {
        // Structural color-state assertion: locate the title's Tj in the page-1
        // content stream, walk backward to the most-recent non-stroking color
        // operator ("rg" for RGB), and assert the normalized triple matches
        // the intended dark-navy (#1A1A2E). This guards the "visible but in a
        // transparent/background-matching color" failure mode that bounding-box
        // assertions alone cannot detect.
        var pdfBytes = GenerateSamplePdf();

        var streams = ExtractInflatedContentStreams(pdfBytes);
        string? page1Stream = streams.FirstOrDefault(
            s => s.Contains(S11_OrdersStyleReport.TitleText));
        Assert.NotNull(page1Stream);

        int titleIdx = page1Stream!.IndexOf(S11_OrdersStyleReport.TitleText,
            StringComparison.Ordinal);
        Assert.True(titleIdx >= 0);

        // Walk backward from the title literal to the most recent "rg" line.
        // The PdfContentByte emitter formats color operators as
        // "<r> <g> <b> rg\n" with newline terminators — latest occurrence wins.
        var rgMatches = Regex.Matches(
            page1Stream.Substring(0, titleIdx),
            @"(\S+)\s+(\S+)\s+(\S+)\s+rg");
        Assert.True(rgMatches.Count > 0,
            "Expected an 'rg' non-stroking color operator before the title's Tj, but found none.");

        var lastRg = rgMatches[^1];
        double r = double.Parse(lastRg.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        double g = double.Parse(lastRg.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        double b = double.Parse(lastRg.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);

        // Expected normalized triple for #1A1A2E = (26/255, 26/255, 46/255).
        const double tol = 0.005; // three-decimal-place tolerance on the emitted format.
        Assert.InRange(r, 26 / 255.0 - tol, 26 / 255.0 + tol);
        Assert.InRange(g, 26 / 255.0 - tol, 26 / 255.0 + tol);
        Assert.InRange(b, 46 / 255.0 - tol, 46 / 255.0 + tol);
    }

    [Fact]
    public void Page1_AlsoRenders_ColumnHeaders_AndGroupHeader_AlongsideTitle()
    {
        // Sanity check that the report pattern is complete on page 1 — the
        // title is not the only band there, and the pageHeader/column-header
        // band is what historically triggered the graphics-state-leak
        // failure mode, so we want it present on page 1 before we start
        // judging whether title color was overdrawn.
        var pdfBytes = GenerateSamplePdf();

        using var pdf = PdfDocument.Open(pdfBytes);
        var page1Text = pdf.GetPage(1).Text;

        Assert.Contains(S11_OrdersStyleReport.TitleText, page1Text);
        Assert.Contains("Order #",     page1Text);
        Assert.Contains("Order Date",  page1Text);
        Assert.Contains("Status",      page1Text);
        Assert.Contains("Total",       page1Text);
        // At least one synthetic customer group must have started on page 1.
        Assert.Contains("Acme Coffee", page1Text);
    }

    /// <summary>
    /// Returns the contiguous letters on the given page that spell the
    /// sample's title. Used for bounding-box and font-name assertions on the
    /// title specifically, as opposed to the raw page text.
    /// </summary>
    private static List<Letter> ExtractTitleLetters(Page page)
    {
        var titleWord = S11_OrdersStyleReport.TitleText.Split(' ')[0]; // "Sample"
        var result = new List<Letter>();
        var letters = page.Letters;
        for (int i = 0; i < letters.Count; i++)
        {
            // Match the first word of the title; return every letter on the
            // same baseline that sits before the line wraps.
            if (!string.Equals(letters[i].Value, titleWord[0].ToString(), StringComparison.Ordinal))
                continue;
            var candidateBuilder = new System.Text.StringBuilder();
            int j = i;
            while (j < letters.Count && candidateBuilder.Length < S11_OrdersStyleReport.TitleText.Length)
            {
                candidateBuilder.Append(letters[j].Value);
                j++;
            }
            if (candidateBuilder.ToString().StartsWith(S11_OrdersStyleReport.TitleText, StringComparison.Ordinal))
            {
                for (int k = i; k < i + S11_OrdersStyleReport.TitleText.Length && k < letters.Count; k++)
                {
                    if (!string.IsNullOrWhiteSpace(letters[k].Value))
                        result.Add(letters[k]);
                }
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Inflates every FlateDecoded content stream in the PDF and returns the
    /// decompressed text — same helper pattern as
    /// <c>RenderingVisibilityDisciplineTests</c>. Kept local so the two
    /// rendering-test classes stay independent.
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
                result.Add(System.Text.Encoding.Latin1.GetString(bytes));
            }

            cursor = streamEnd + "endstream".Length;
        }
        return result;
    }
}
