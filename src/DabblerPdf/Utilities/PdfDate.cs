// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Globalization;
using System.Text;

namespace DabblerPdf.Utilities;

/// <summary>
/// Formats <see cref="DateTime"/> values as PDF date strings per
/// PDF 1.7 §7.9.4: <c>D:YYYYMMDDHHmmSSOHH'mm'</c>, where <c>O</c> is
/// <c>+</c>, <c>-</c>, or <c>Z</c>.
/// </summary>
public static class PdfDate
{
    /// <summary>
    /// Formats <paramref name="value"/> as a PDF date string.
    /// </summary>
    /// <param name="value">The date and time to format. Behaviour depends on
    /// <see cref="DateTime.Kind"/>:
    /// <list type="bullet">
    /// <item><see cref="DateTimeKind.Utc"/> — formatted with <c>Z</c> suffix.</item>
    /// <item><see cref="DateTimeKind.Local"/> — formatted with the local UTC offset.</item>
    /// <item><see cref="DateTimeKind.Unspecified"/> — treated as local time
    /// (matches <see cref="TimeZoneInfo.Local"/>'s offset for that instant).</item>
    /// </list>
    /// </param>
    /// <returns>A PDF date string suitable for use as the value of
    /// <c>/CreationDate</c> or <c>/ModDate</c> in the document information
    /// dictionary.</returns>
    public static string Format(DateTime value)
    {
        var sb = new StringBuilder(24);
        sb.Append("D:");
        sb.Append(value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));

        if (value.Kind == DateTimeKind.Utc)
        {
            sb.Append('Z');
            return sb.ToString();
        }

        TimeSpan offset = value.Kind == DateTimeKind.Local
            ? TimeZoneInfo.Local.GetUtcOffset(value)
            : TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(value, DateTimeKind.Local));

        if (offset == TimeSpan.Zero)
        {
            sb.Append('Z');
            return sb.ToString();
        }

        sb.Append(offset.Ticks >= 0 ? '+' : '-');
        var abs = offset.Duration();
        sb.Append(abs.Hours.ToString("D2", CultureInfo.InvariantCulture));
        sb.Append('\'');
        sb.Append(abs.Minutes.ToString("D2", CultureInfo.InvariantCulture));
        sb.Append('\'');
        return sb.ToString();
    }
}
