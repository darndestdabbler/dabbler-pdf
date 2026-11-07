// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

public class PdfBooleanTests
{
    [Fact]
    public void True_WritesCorrectPdf()
    {
        Assert.Equal("true", GetPdfText(PdfBoolean.True));
    }

    [Fact]
    public void False_WritesCorrectPdf()
    {
        Assert.Equal("false", GetPdfText(PdfBoolean.False));
    }

    [Fact]
    public void Type_IsBoolean()
    {
        Assert.True(PdfBoolean.True.IsBoolean());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfNumberTests
{
    [Fact]
    public void Integer_WritesCorrectPdf()
    {
        var num = new PdfNumber(42);
        Assert.Equal("42", GetPdfText(num));
        Assert.Equal(42, num.IntValue);
    }

    [Fact]
    public void Double_WritesCorrectPdf()
    {
        var num = new PdfNumber(3.14);
        Assert.Equal("3.14", GetPdfText(num));
        Assert.Equal(3.14, num.DoubleValue);
    }

    [Fact]
    public void WholeDouble_WritesAsInteger()
    {
        var num = new PdfNumber(72.0);
        Assert.Equal("72", GetPdfText(num));
    }

    [Fact]
    public void Type_IsNumber()
    {
        Assert.True(new PdfNumber(1).IsNumber());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfStringTests
{
    [Fact]
    public void Literal_WritesWithParentheses()
    {
        var str = new PdfString("Hello");
        Assert.Equal("(Hello)", GetPdfText(str));
    }

    [Fact]
    public void Literal_EscapesSpecialChars()
    {
        var str = new PdfString("a(b)c\\d");
        Assert.Equal("(a\\(b\\)c\\\\d)", GetPdfText(str));
    }

    [Fact]
    public void Hex_WritesWithAngleBrackets()
    {
        var str = new PdfString("Hi") { HexWriting = true };
        Assert.Equal("<4869>", GetPdfText(str));
    }

    [Fact]
    public void Type_IsString()
    {
        Assert.True(new PdfString("x").IsString());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfNameTests
{
    [Fact]
    public void WritesWithSlash()
    {
        var name = new PdfName("Type");
        Assert.Equal("/Type", GetPdfText(name));
    }

    [Fact]
    public void AcceptsLeadingSlash()
    {
        var name = new PdfName("/Type");
        Assert.Equal("/Type", GetPdfText(name));
        Assert.Equal("Type", name.Value);
    }

    [Fact]
    public void EscapesSpecialChars()
    {
        var name = new PdfName("A B");
        Assert.Equal("/A#20B", GetPdfText(name));
    }

    [Fact]
    public void Equality_Works()
    {
        var a = new PdfName("Type");
        var b = new PdfName("Type");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void StaticConstants_HaveCorrectValues()
    {
        Assert.Equal("/Type", GetPdfText(PdfName.Type));
        Assert.Equal("/Pages", GetPdfText(PdfName.Pages));
        Assert.Equal("/MediaBox", GetPdfText(PdfName.MediaBox));
    }

    [Fact]
    public void Type_IsName()
    {
        Assert.True(PdfName.Type.IsName());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfNullTests
{
    [Fact]
    public void WritesNull()
    {
        Assert.Equal("null", GetPdfText(PdfNull.Instance));
    }

    [Fact]
    public void Type_IsNull()
    {
        Assert.True(PdfNull.Instance.IsNull());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfArrayTests
{
    [Fact]
    public void Empty_WritesEmptyBrackets()
    {
        Assert.Equal("[]", GetPdfText(new PdfArray()));
    }

    [Fact]
    public void WithElements_WritesSpaceSeparated()
    {
        var arr = new PdfArray();
        arr.Add(new PdfNumber(0));
        arr.Add(new PdfNumber(0));
        arr.Add(new PdfNumber(612));
        arr.Add(new PdfNumber(792));
        Assert.Equal("[0 0 612 792]", GetPdfText(arr));
    }

    [Fact]
    public void FromDoubles_CreatesNumberArray()
    {
        var arr = new PdfArray(0, 0, 612, 792);
        Assert.Equal("[0 0 612 792]", GetPdfText(arr));
        Assert.Equal(4, arr.Count);
    }

    [Fact]
    public void Type_IsArray()
    {
        Assert.True(new PdfArray().IsArray());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfDictionaryTests
{
    [Fact]
    public void Empty_WritesEmptyDict()
    {
        Assert.Equal("<<>>", GetPdfText(new PdfDictionary()));
    }

    [Fact]
    public void WithEntries_WritesKeyValuePairs()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Catalog);
        var text = GetPdfText(dict);
        Assert.Contains("/Type", text);
        Assert.Contains("/Catalog", text);
    }

    [Fact]
    public void PutNull_RemovesEntry()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Catalog);
        Assert.Equal(1, dict.Count);
        dict.Put(PdfName.Type, null);
        Assert.Equal(0, dict.Count);
    }

    [Fact]
    public void TypeSafeAccessors_Work()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Page);
        dict.Put(PdfName.Count, new PdfNumber(5));

        Assert.Equal(PdfName.Page, dict.GetAsName(PdfName.Type));
        Assert.Equal(5, dict.GetAsNumber(PdfName.Count)?.IntValue);
        Assert.Null(dict.GetAsString(PdfName.Type)); // wrong type returns null
    }

    [Fact]
    public void Type_IsDictionary()
    {
        Assert.True(new PdfDictionary().IsDictionary());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfIndirectReferenceTests
{
    [Fact]
    public void WritesCorrectFormat()
    {
        var r = new PdfIndirectReference(5, 0);
        Assert.Equal("5 0 R", GetPdfText(r));
    }

    [Fact]
    public void DefaultGeneration_IsZero()
    {
        var r = new PdfIndirectReference(42);
        Assert.Equal("42 0 R", GetPdfText(r));
    }

    [Fact]
    public void Type_IsIndirect()
    {
        Assert.True(new PdfIndirectReference(1).IsIndirect());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

public class PdfLiteralTests
{
    [Fact]
    public void WritesRawContent()
    {
        var lit = new PdfLiteral("0 0 0 rg");
        Assert.Equal("0 0 0 rg", GetPdfText(lit));
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}
