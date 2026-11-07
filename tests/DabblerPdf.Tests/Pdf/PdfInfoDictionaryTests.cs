// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using System.Text.RegularExpressions;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

/// <summary>
/// End-to-end tests that build a real PDF, parse the resulting bytes, and
/// inspect the document information dictionary (/Info).
/// </summary>
public class PdfInfoDictionaryTests
{
    private static (byte[] bytes, string text) Build(Action<DabblerPdf.Document.Document> stage)
    {
        using var ms = new MemoryStream();
        var doc = new DabblerPdf.Document.Document(PageSize.Letter);
        PdfWriter.GetInstance(doc, ms);
        stage(doc);
        doc.Open();
        doc.Close();
        var bytes = ms.ToArray();
        return (bytes, Encoding.GetEncoding("iso-8859-1").GetString(bytes));
    }

    /// <summary>
    /// Locate the /Info indirect-object body in the PDF text. Throws if the
    /// trailer doesn't reference an /Info object or the object can't be
    /// found. The dictionary returned is the raw text between &lt;&lt; and &gt;&gt;.
    /// </summary>
    private static string ExtractInfoDictionary(string pdfText)
    {
        var trailerInfo = Regex.Match(pdfText, @"/Info\s+(\d+)\s+\d+\s+R");
        Assert.True(trailerInfo.Success, "Trailer does not reference an /Info object");
        var objNum = trailerInfo.Groups[1].Value;
        var objMatch = Regex.Match(
            pdfText,
            objNum + @"\s+\d+\s+obj\s*<<(?<dict>[\s\S]*?)>>\s*endobj");
        Assert.True(objMatch.Success, $"/Info indirect object {objNum} not found");
        return objMatch.Groups["dict"].Value;
    }

    [Fact]
    public void NoStagedMetadata_DefaultsToProducerPlusCreationDate()
    {
        var (_, text) = Build(_ => { });
        var dict = ExtractInfoDictionary(text);
        Assert.Contains("/Producer", dict);
        Assert.Contains("DabblerPdf.NET", dict);
        Assert.Contains("/CreationDate", dict);
    }

    [Fact]
    public void SuppressCreationDate_ProducesOnlyProducer_RestoresOnePointZeroShape()
    {
        var (_, text) = Build(d => d.SuppressCreationDate());
        var dict = ExtractInfoDictionary(text);
        Assert.Contains("/Producer", dict);
        Assert.DoesNotContain("/CreationDate", dict);
        Assert.DoesNotContain("/Title", dict);
        Assert.DoesNotContain("/Author", dict);
        Assert.DoesNotContain("/Subject", dict);
        Assert.DoesNotContain("/Keywords", dict);
        Assert.DoesNotContain("/Creator", dict);
    }

    [Fact]
    public void StagedMetadata_AppearsInInfoDictionary()
    {
        var (_, text) = Build(d =>
        {
            d.AddTitle("Orders by Retail Business");
            d.AddAuthor("AcmeCoffeeDistributors");
            d.AddSubject("Orders report");
            d.AddKeywords("orders, report, sales");
            d.AddCreator("AcmeCoffeeOnline");
        });
        var dict = ExtractInfoDictionary(text);
        Assert.Contains("/Title (Orders by Retail Business)", dict);
        Assert.Contains("/Author (AcmeCoffeeDistributors)", dict);
        Assert.Contains("/Subject (Orders report)", dict);
        Assert.Contains("/Keywords (orders, report, sales)", dict);
        Assert.Contains("/Creator (AcmeCoffeeOnline)", dict);
        // Producer is preserved
        Assert.Contains("/Producer (DabblerPdf.NET)", dict);
    }

    [Fact]
    public void CreationDate_AutoNow_IsSpecCompliantPdfDateString()
    {
        var (_, text) = Build(_ => { });
        var dict = ExtractInfoDictionary(text);
        var rx = new Regex(@"/CreationDate\s*\(D:\d{14}(Z|[+-]\d{2}'\d{2}')\)");
        Assert.Matches(rx, dict);
    }

    [Fact]
    public void CreationDate_Explicit_RoundTripsThroughOutput()
    {
        var explicitDate = new DateTime(2026, 4, 18, 12, 34, 56, DateTimeKind.Utc);
        var (_, text) = Build(d => d.AddCreationDate(explicitDate));
        var dict = ExtractInfoDictionary(text);
        Assert.Contains("/CreationDate (D:20260418123456Z)", dict);
    }

    [Fact]
    public void StringWithParenthesis_IsPdfEscaped()
    {
        // PdfString already escapes \, (, ); confirm staged metadata
        // strings flow through that path.
        var (_, text) = Build(d => d.AddTitle("Q1 (2026) Recap"));
        var dict = ExtractInfoDictionary(text);
        Assert.Contains(@"/Title (Q1 \(2026\) Recap)", dict);
    }

    [Fact]
    public void EmptyStringNoOp_DoesNotEmitTitle()
    {
        var (_, text) = Build(d => d.AddTitle(""));
        var dict = ExtractInfoDictionary(text);
        Assert.DoesNotContain("/Title", dict);
    }

    [Fact]
    public void MetadataPlusSuppressCreationDate_OmitsOnlyCreationDate()
    {
        var (_, text) = Build(d =>
        {
            d.AddTitle("T");
            d.AddAuthor("A");
            d.SuppressCreationDate();
        });
        var dict = ExtractInfoDictionary(text);
        Assert.Contains("/Title (T)", dict);
        Assert.Contains("/Author (A)", dict);
        Assert.DoesNotContain("/CreationDate", dict);
    }
}
