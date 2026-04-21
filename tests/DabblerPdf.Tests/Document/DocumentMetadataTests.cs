// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Document;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Document;

public class DocumentMetadataApiTests
{
    private static DabblerPdf.Document.Document NewDoc()
    {
        var doc = new DabblerPdf.Document.Document(PageSize.Letter);
        // Tests that exercise post-Close behaviour need a writer attached so
        // Close() does not throw on the missing writer; tests that only stage
        // metadata pre-open do not need one. Always attach for uniformity.
        var ms = new MemoryStream();
        PdfWriter.GetInstance(doc, ms);
        return doc;
    }

    [Fact]
    public void AddTitle_StagesValue()
    {
        var doc = NewDoc();
        Assert.True(doc.AddTitle("Quarterly Report"));
        Assert.Equal("Quarterly Report", doc.GetMetadata().Title);
    }

    [Fact]
    public void AddAuthor_AddSubject_AddKeywords_AddCreator_AllStage()
    {
        var doc = NewDoc();
        doc.AddAuthor("AcmeCoffeeDistributors");
        doc.AddSubject("Orders report");
        doc.AddKeywords("orders, report, sales");
        doc.AddCreator("AcmeCoffeeOnline");
        var m = doc.GetMetadata();
        Assert.Equal("AcmeCoffeeDistributors", m.Author);
        Assert.Equal("Orders report", m.Subject);
        Assert.Equal("orders, report, sales", m.Keywords);
        Assert.Equal("AcmeCoffeeOnline", m.Creator);
    }

    [Fact]
    public void AddTitle_NullValue_Throws()
    {
        var doc = NewDoc();
        Assert.Throws<ArgumentNullException>(() => doc.AddTitle(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("   \r\n  ")]
    public void AddTitle_EmptyOrWhitespace_NoOp_ReturnsFalse(string input)
    {
        var doc = NewDoc();
        doc.AddTitle("Real Title");
        Assert.False(doc.AddTitle(input));
        Assert.Equal("Real Title", doc.GetMetadata().Title);
    }

    [Fact]
    public void AddTitle_CalledTwice_LastWriteWins()
    {
        var doc = NewDoc();
        doc.AddTitle("First");
        doc.AddTitle("Second");
        Assert.Equal("Second", doc.GetMetadata().Title);
    }

    [Fact]
    public void AddCreationDate_NoArg_StagesUtcNow_AndClearsSuppression()
    {
        var doc = NewDoc();
        doc.SuppressCreationDate();
        Assert.True(doc.GetMetadata().CreationDateSuppressed);

        var before = DateTime.UtcNow.AddSeconds(-1);
        doc.AddCreationDate();
        var after = DateTime.UtcNow.AddSeconds(1);

        var m = doc.GetMetadata();
        Assert.NotNull(m.CreationDate);
        Assert.Equal(DateTimeKind.Utc, m.CreationDate!.Value.Kind);
        Assert.InRange(m.CreationDate!.Value, before, after);
        Assert.False(m.CreationDateSuppressed);
    }

    [Fact]
    public void AddCreationDate_Explicit_StagesValue()
    {
        var doc = NewDoc();
        var dt = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);
        doc.AddCreationDate(dt);
        Assert.Equal(dt, doc.GetMetadata().CreationDate);
    }

    [Fact]
    public void SuppressCreationDate_ClearsAnyStagedValue()
    {
        var doc = NewDoc();
        doc.AddCreationDate(new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc));
        doc.SuppressCreationDate();
        var m = doc.GetMetadata();
        Assert.Null(m.CreationDate);
        Assert.True(m.CreationDateSuppressed);
    }

    [Fact]
    public void AddTitle_AfterClose_Throws()
    {
        var doc = NewDoc();
        doc.Open();
        doc.Close();
        Assert.Throws<InvalidOperationException>(() => doc.AddTitle("X"));
    }

    [Fact]
    public void AddAuthor_AfterClose_Throws()
    {
        var doc = NewDoc();
        doc.Open();
        doc.Close();
        Assert.Throws<InvalidOperationException>(() => doc.AddAuthor("X"));
    }

    [Fact]
    public void AddCreationDate_AfterClose_Throws()
    {
        var doc = NewDoc();
        doc.Open();
        doc.Close();
        Assert.Throws<InvalidOperationException>(() => doc.AddCreationDate());
        Assert.Throws<InvalidOperationException>(() => doc.AddCreationDate(DateTime.UtcNow));
    }

    [Fact]
    public void SuppressCreationDate_AfterClose_Throws()
    {
        var doc = NewDoc();
        doc.Open();
        doc.Close();
        Assert.Throws<InvalidOperationException>(() => doc.SuppressCreationDate());
    }

    [Fact]
    public void GetMetadata_BeforeAnyCalls_ReturnsAllNullsAndUnsuppressed()
    {
        var doc = NewDoc();
        var m = doc.GetMetadata();
        Assert.Null(m.Title);
        Assert.Null(m.Author);
        Assert.Null(m.Subject);
        Assert.Null(m.Keywords);
        Assert.Null(m.Creator);
        Assert.Null(m.CreationDate);
        Assert.False(m.CreationDateSuppressed);
    }

    [Fact]
    public void AddMetadata_AllowedBeforeOpen()
    {
        // The spec stages metadata onto the Document before Open();
        // PdfWriter consumes it at Close(). Pre-Open staging must succeed.
        var doc = NewDoc();
        doc.AddTitle("Pre-open title");
        Assert.Equal("Pre-open title", doc.GetMetadata().Title);
    }

    [Fact]
    public void AddMetadata_AllowedAfterOpenBeforeClose()
    {
        var doc = NewDoc();
        doc.Open();
        doc.AddTitle("Mid-document title");
        Assert.Equal("Mid-document title", doc.GetMetadata().Title);
        doc.Close();
    }
}
