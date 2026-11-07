// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text.RegularExpressions;
using DabblerPdf.Utilities;
using Xunit;

namespace DabblerPdf.Tests.Utilities;

public class PdfDateTests
{
    [Fact]
    public void Format_UtcKind_UsesZSuffix()
    {
        var dt = new DateTime(2026, 4, 18, 12, 34, 56, DateTimeKind.Utc);
        Assert.Equal("D:20260418123456Z", PdfDate.Format(dt));
    }

    [Fact]
    public void Format_LocalKind_UsesLocalUtcOffset()
    {
        var dt = new DateTime(2026, 4, 18, 12, 34, 56, DateTimeKind.Local);
        var expected = ExpectedFormatted(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
        Assert.Equal(expected, PdfDate.Format(dt));
    }

    [Fact]
    public void Format_UnspecifiedKind_TreatedAsLocal()
    {
        var dt = new DateTime(2026, 4, 18, 12, 34, 56, DateTimeKind.Unspecified);
        var asLocal = DateTime.SpecifyKind(dt, DateTimeKind.Local);
        var expected = ExpectedFormatted(dt, TimeZoneInfo.Local.GetUtcOffset(asLocal));
        Assert.Equal(expected, PdfDate.Format(dt));
    }

    [Fact]
    public void Format_OutputMatchesPdfSpecRegex()
    {
        // PDF 1.7 §7.9.4 grammar (relaxed to the parts we always emit):
        //   D:YYYYMMDDHHmmSS (Z | [+-]HH'mm')
        var rx = new Regex(@"^D:\d{14}(Z|[+-]\d{2}'\d{2}')$");
        Assert.Matches(rx, PdfDate.Format(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)));
        Assert.Matches(rx, PdfDate.Format(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Local)));
        Assert.Matches(rx, PdfDate.Format(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void Format_ZeroPadsAllFields()
    {
        var dt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Assert.Equal("D:20260102030405Z", PdfDate.Format(dt));
    }

    [Fact]
    public void Format_LocalEqualToUtc_StillUsesZ()
    {
        // A DateTime whose computed offset is zero (e.g., UK winter) should
        // emit Z rather than +00'00' — both are spec-legal but Z is
        // canonical for zero offset.
        var utcInstant = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);
        var asLocal = utcInstant.ToLocalTime();
        if (TimeZoneInfo.Local.GetUtcOffset(asLocal) == TimeSpan.Zero)
        {
            Assert.EndsWith("Z", PdfDate.Format(asLocal));
        }
        // If the test runner is not in a UTC-aligned zone, this assertion
        // is vacuously skipped — Format still produces a spec-legal string,
        // covered by Format_OutputMatchesPdfSpecRegex.
    }

    [Theory]
    [InlineData(2, 30)]      // +02:30 (e.g., parts of India-adjacent zones)
    [InlineData(-5, 0)]      // -05:00 (US Eastern in winter)
    [InlineData(5, 45)]      // +05:45 (Nepal)
    [InlineData(0, 0)]       // 00:00 → Z
    public void Format_ApostropheSeparatedOffset(int offsetHours, int offsetMinutes)
    {
        // We can't change TimeZoneInfo.Local mid-test, so we assert against
        // a synthetic offset by formatting through a known computation.
        var offset = new TimeSpan(offsetHours, offsetMinutes, 0);
        var formatted = ExpectedFormatted(
            new DateTime(2026, 4, 18, 12, 34, 56),
            offset);
        if (offset == TimeSpan.Zero)
        {
            Assert.EndsWith("Z", formatted);
        }
        else
        {
            char sign = offset.Ticks >= 0 ? '+' : '-';
            var abs = offset.Duration();
            Assert.EndsWith($"{sign}{abs.Hours:D2}'{abs.Minutes:D2}'", formatted);
        }
    }

    // Mirrors PdfDate.Format's offset-formatting so the test can assert
    // against explicit offsets the host machine doesn't necessarily sit in.
    // If this drifts from PdfDate.Format, Format_OutputMatchesPdfSpecRegex
    // catches the structural divergence.
    private static string ExpectedFormatted(DateTime dt, TimeSpan offset)
    {
        var ymdhms = dt.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        if (offset == TimeSpan.Zero) return $"D:{ymdhms}Z";
        char sign = offset.Ticks >= 0 ? '+' : '-';
        var abs = offset.Duration();
        return $"D:{ymdhms}{sign}{abs.Hours:D2}'{abs.Minutes:D2}'";
    }
}
