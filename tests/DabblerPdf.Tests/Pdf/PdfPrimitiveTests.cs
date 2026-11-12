// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;
using DabblerPdf.Pdf;
using Xunit;

namespace DabblerPdf.Tests.Pdf;

/// <summary>
/// Dedicated tests for PDF primitive types: PdfBoolean, PdfNull, PdfNumber,
/// PdfName, PdfString, PdfLiteral. Supplements PdfObjectTests with deeper
/// coverage of serialization correctness, edge cases, and the fixes from
/// Session 2 V&amp;V code review.
/// </summary>

#region PdfBoolean

public class PdfBooleanPrimitiveTests
{
    [Fact]
    public void True_ValueIsTrue()
    {
        Assert.True(PdfBoolean.True.Value);
    }

    [Fact]
    public void False_ValueIsFalse()
    {
        Assert.False(PdfBoolean.False.Value);
    }

    [Fact]
    public void True_ToString_ReturnsTrue()
    {
        Assert.Equal("true", PdfBoolean.True.ToString());
    }

    [Fact]
    public void False_ToString_ReturnsFalse()
    {
        Assert.Equal("false", PdfBoolean.False.ToString());
    }

    [Fact]
    public void True_ToBytes_MatchesWriteTo()
    {
        var bytes = PdfBoolean.True.ToBytes();
        Assert.Equal("true", Encoding.ASCII.GetString(bytes));
    }

    [Fact]
    public void Singleton_Identity()
    {
        // The static True/False fields should always be the same reference
        Assert.Same(PdfBoolean.True, PdfBoolean.True);
        Assert.Same(PdfBoolean.False, PdfBoolean.False);
        Assert.NotSame(PdfBoolean.True, PdfBoolean.False);
    }
}

#endregion

#region PdfNull

public class PdfNullPrimitiveTests
{
    [Fact]
    public void ToString_ReturnsNull()
    {
        Assert.Equal("null", PdfNull.Instance.ToString());
    }

    [Fact]
    public void Singleton_Identity()
    {
        Assert.Same(PdfNull.Instance, PdfNull.Instance);
    }

    [Fact]
    public void ToBytes_MatchesWriteTo()
    {
        var bytes = PdfNull.Instance.ToBytes();
        Assert.Equal("null", Encoding.ASCII.GetString(bytes));
    }
}

#endregion

#region PdfNumber

public class PdfNumberPrimitiveTests
{
    [Fact]
    public void StringConstructor_ParsesInteger()
    {
        var num = new PdfNumber("42");
        Assert.Equal(42, num.IntValue);
        Assert.Equal("42", GetPdfText(num));
    }

    [Fact]
    public void StringConstructor_ParsesDouble()
    {
        var num = new PdfNumber("3.14");
        Assert.Equal(3.14, num.DoubleValue);
        Assert.Equal("3.14", GetPdfText(num));
    }

    [Fact]
    public void StringConstructor_NormalizesScientificNotation()
    {
        // Even if the input string uses scientific notation, the stored bytes
        // must use fixed-point (PDF spec forbids scientific notation)
        var num = new PdfNumber("1.23E+04");
        Assert.Equal(12300.0, num.DoubleValue);
        var text = GetPdfText(num);
        Assert.DoesNotContain("E", text);
        Assert.Equal("12300", text);
    }

    [Fact]
    public void NegativeInteger_WritesCorrectly()
    {
        var num = new PdfNumber(-5);
        Assert.Equal("-5", GetPdfText(num));
    }

    [Fact]
    public void NegativeDouble_WritesCorrectly()
    {
        var num = new PdfNumber(-0.75);
        Assert.Equal("-0.75", GetPdfText(num));
    }

    [Fact]
    public void Zero_WritesAsInteger()
    {
        var num = new PdfNumber(0.0);
        Assert.Equal("0", GetPdfText(num));
    }

    [Fact]
    public void LargeNumber_NoScientificNotation()
    {
        var num = new PdfNumber(1000000.0);
        var text = GetPdfText(num);
        Assert.DoesNotContain("E", text);
        Assert.Equal("1000000", text);
    }

    [Fact]
    public void SmallFraction_NoScientificNotation()
    {
        var num = new PdfNumber(0.000123);
        var text = GetPdfText(num);
        Assert.DoesNotContain("E", text);
        Assert.Contains("0.000123", text);
    }

    [Fact]
    public void NaN_WritesZero()
    {
        var num = new PdfNumber(double.NaN);
        Assert.Equal("0", GetPdfText(num));
    }

    [Fact]
    public void PositiveInfinity_WritesZero()
    {
        var num = new PdfNumber(double.PositiveInfinity);
        Assert.Equal("0", GetPdfText(num));
    }

    [Fact]
    public void NegativeInfinity_WritesZero()
    {
        var num = new PdfNumber(double.NegativeInfinity);
        Assert.Equal("0", GetPdfText(num));
    }

    [Fact]
    public void ToString_MatchesWriteTo()
    {
        var num = new PdfNumber(72.5);
        Assert.Equal(GetPdfText(num), num.ToString());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

#endregion

#region PdfName

public class PdfNamePrimitiveTests
{
    [Fact]
    public void HashChar_IsEscaped()
    {
        var name = new PdfName("A#B");
        Assert.Equal("/A#23B", GetPdfText(name));
    }

    [Fact]
    public void Percent_IsEscaped()
    {
        var name = new PdfName("100%");
        Assert.Equal("/100#25", GetPdfText(name));
    }

    [Fact]
    public void ParenthesesBrackets_AreEscaped()
    {
        var name = new PdfName("(test)");
        Assert.Equal("/#28test#29", GetPdfText(name));
    }

    [Fact]
    public void NonAscii_IsUtf8HexEncoded()
    {
        // 'ü' = U+00FC, UTF-8 = 0xC3 0xBC
        var name = new PdfName("ü");
        Assert.Equal("/#C3#BC", GetPdfText(name));
    }

    [Fact]
    public void Inequality_Works()
    {
        var a = new PdfName("Type");
        var b = new PdfName("Page");
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_OrdersAlphabetically()
    {
        var a = new PdfName("Alpha");
        var b = new PdfName("Beta");
        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
        Assert.Equal(0, a.CompareTo(new PdfName("Alpha")));
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquals()
    {
        var a = new PdfName("Type");
        var b = new PdfName("Type");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NullEquality_Operators()
    {
        PdfName? nullName = null;
        var name = new PdfName("Type");
        Assert.True(nullName == null);
        Assert.False(nullName == name);
        Assert.True(name != nullName);
    }

    [Fact]
    public void Value_ReturnsNameWithoutSlash()
    {
        var name = new PdfName("Font");
        Assert.Equal("Font", name.Value);
    }

    [Fact]
    public void ToString_IncludesSlash()
    {
        var name = new PdfName("Font");
        Assert.Equal("/Font", name.ToString());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

#endregion

#region PdfString

public class PdfStringPrimitiveTests
{
    [Fact]
    public void CarriageReturn_IsEscaped()
    {
        var str = new PdfString("a\rb");
        Assert.Equal("(a\\rb)", GetPdfText(str));
    }

    [Fact]
    public void LineFeed_IsEscaped()
    {
        var str = new PdfString("a\nb");
        Assert.Equal("(a\\nb)", GetPdfText(str));
    }

    [Fact]
    public void ByteConstructor_RoundTrips()
    {
        byte[] bytes = [72, 101, 108, 108, 111]; // "Hello"
        var str = new PdfString(bytes);
        Assert.Equal("Hello", str.Value);
        Assert.Equal("(Hello)", GetPdfText(str));
    }

    [Fact]
    public void OriginalBytes_ReturnsCopy()
    {
        var str = new PdfString("ABC");
        var bytes1 = str.OriginalBytes;
        bytes1[0] = 0; // Mutate the returned copy

        var bytes2 = str.OriginalBytes;
        Assert.Equal((byte)'A', bytes2[0]); // Original is not affected
    }

    [Fact]
    public void EmptyString_WritesEmptyParens()
    {
        var str = new PdfString("");
        Assert.Equal("()", GetPdfText(str));
    }

    [Fact]
    public void EmptyString_HexWritesEmptyAngleBrackets()
    {
        var str = new PdfString("") { HexWriting = true };
        Assert.Equal("<>", GetPdfText(str));
    }

    [Fact]
    public void HexWriting_AllByteValues()
    {
        // Test that hex output uses uppercase hex digits
        var str = new PdfString(new byte[] { 0x00, 0x0F, 0xFF }) { HexWriting = true };
        Assert.Equal("<000FFF>", GetPdfText(str));
    }

    [Fact]
    public void Value_ReturnsOriginalString()
    {
        var str = new PdfString("test");
        Assert.Equal("test", str.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var str = new PdfString("test");
        Assert.Equal("test", str.ToString());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

#endregion

#region PdfLiteral

public class PdfLiteralPrimitiveTests
{
    [Fact]
    public void ByteConstructor_WritesRawBytes()
    {
        var bytes = Encoding.ASCII.GetBytes("BT /F1 12 Tf ET");
        var lit = new PdfLiteral(bytes);
        Assert.Equal("BT /F1 12 Tf ET", GetPdfText(lit));
    }

    [Fact]
    public void EmptyLiteral_WritesNothing()
    {
        var lit = new PdfLiteral("");
        Assert.Equal("", GetPdfText(lit));
    }

    [Fact]
    public void ToString_MatchesContent()
    {
        var lit = new PdfLiteral("1 0 0 rg");
        Assert.Equal("1 0 0 rg", lit.ToString());
    }

    [Fact]
    public void Type_IsZero()
    {
        // PdfLiteral uses type 0 (not a standard PDF type)
        var lit = new PdfLiteral("test");
        Assert.False(lit.IsBoolean());
        Assert.False(lit.IsNumber());
        Assert.False(lit.IsString());
        Assert.False(lit.IsName());
        Assert.False(lit.IsArray());
        Assert.False(lit.IsDictionary());
        Assert.False(lit.IsNull());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

#endregion

#region PdfArray (additional)

public class PdfArrayPrimitiveTests
{
    [Fact]
    public void Add_Null_ThrowsArgumentNullException()
    {
        var arr = new PdfArray();
        Assert.Throws<ArgumentNullException>(() => arr.Add(null!));
    }

    [Fact]
    public void Insert_Null_ThrowsArgumentNullException()
    {
        var arr = new PdfArray();
        arr.Add(new PdfNumber(1));
        Assert.Throws<ArgumentNullException>(() => arr.Insert(0, null!));
    }

    [Fact]
    public void Indexer_SetNull_ThrowsArgumentNullException()
    {
        var arr = new PdfArray();
        arr.Add(new PdfNumber(1));
        Assert.Throws<ArgumentNullException>(() => arr[0] = null!);
    }

    [Fact]
    public void Insert_AtPosition_ShiftsElements()
    {
        var arr = new PdfArray();
        arr.Add(new PdfNumber(1));
        arr.Add(new PdfNumber(3));
        arr.Insert(1, new PdfNumber(2));
        Assert.Equal("[1 2 3]", GetPdfText(arr));
    }

    [Fact]
    public void RemoveAt_RemovesElement()
    {
        var arr = new PdfArray();
        arr.Add(new PdfNumber(1));
        arr.Add(new PdfNumber(2));
        arr.Add(new PdfNumber(3));
        arr.RemoveAt(1);
        Assert.Equal("[1 3]", GetPdfText(arr));
        Assert.Equal(2, arr.Count);
    }

    [Fact]
    public void Contains_FindsElement()
    {
        var num = new PdfNumber(42);
        var arr = new PdfArray(num);
        Assert.True(arr.Contains(num));
        Assert.False(arr.Contains(new PdfNumber(99)));
    }

    [Fact]
    public void SingleElementConstructor_Works()
    {
        var num = new PdfNumber(5);
        var arr = new PdfArray(num);
        Assert.Equal(1, arr.Count);
        Assert.Equal("[5]", GetPdfText(arr));
    }

    [Fact]
    public void CollectionConstructor_Works()
    {
        var elements = new PdfObject[] { new PdfNumber(1), new PdfNumber(2), new PdfNumber(3) };
        var arr = new PdfArray(elements);
        Assert.Equal(3, arr.Count);
        Assert.Equal("[1 2 3]", GetPdfText(arr));
    }

    [Fact]
    public void ToString_MatchesWriteTo()
    {
        var arr = new PdfArray(1, 2, 3);
        Assert.Equal(GetPdfText(arr), arr.ToString());
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

#endregion

#region PdfDictionary (additional)

public class PdfDictionaryPrimitiveTests
{
    [Fact]
    public void TypedConstructor_SetsTypeEntry()
    {
        var dict = new PdfDictionary(PdfName.Catalog);
        Assert.Equal(PdfName.Catalog, dict.GetAsName(PdfName.Type));
    }

    [Fact]
    public void Remove_RemovesEntry()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Page);
        dict.Remove(PdfName.Type);
        Assert.Equal(0, dict.Count);
        Assert.Null(dict.Get(PdfName.Type));
    }

    [Fact]
    public void Contains_ChecksKey()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Page);
        Assert.True(dict.Contains(PdfName.Type));
        Assert.False(dict.Contains(PdfName.Font));
    }

    [Fact]
    public void PutPdfNull_RemovesEntry()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Page);
        dict.Put(PdfName.Type, PdfNull.Instance);
        Assert.Equal(0, dict.Count);
    }

    [Fact]
    public void GetAsDictionary_ReturnsNestedDict()
    {
        var inner = new PdfDictionary();
        inner.Put(PdfName.Font, PdfName.Type1);
        var outer = new PdfDictionary();
        outer.Put(PdfName.Resources, inner);
        Assert.Same(inner, outer.GetAsDictionary(PdfName.Resources));
    }

    [Fact]
    public void GetAsArray_ReturnsArray()
    {
        var arr = new PdfArray(0, 0, 612, 792);
        var dict = new PdfDictionary();
        dict.Put(PdfName.MediaBox, arr);
        Assert.Same(arr, dict.GetAsArray(PdfName.MediaBox));
    }

    [Fact]
    public void GetAsBoolean_ReturnsBoolean()
    {
        var dict = new PdfDictionary();
        dict.Put(new PdfName("NeedAppearances"), PdfBoolean.True);
        Assert.Same(PdfBoolean.True, dict.GetAsBoolean(new PdfName("NeedAppearances")));
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        var dict = new PdfDictionary();
        dict.Put(PdfName.Type, PdfName.Page);
        dict.Put(PdfName.Count, new PdfNumber(5));
        Assert.Equal(2, dict.Keys.Count);
    }

    private static string GetPdfText(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.WriteTo(ms);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}

#endregion
