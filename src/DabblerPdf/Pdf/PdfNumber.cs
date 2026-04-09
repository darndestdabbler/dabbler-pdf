// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Globalization;
using System.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents a PDF numeric value (integer or real number).
/// </summary>
public sealed class PdfNumber : PdfObject
{
    private double _value;

    public PdfNumber(int value) : base(NUMBER)
    {
        _value = value;
        Bytes = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
    }

    public PdfNumber(double value) : base(NUMBER)
    {
        _value = value;
        Bytes = Encoding.ASCII.GetBytes(FormatDouble(value));
    }

    public PdfNumber(string content) : base(NUMBER)
    {
        _value = double.Parse(content, CultureInfo.InvariantCulture);
        Bytes = Encoding.ASCII.GetBytes(FormatDouble(_value));
    }

    public int IntValue => (int)_value;
    public double DoubleValue => _value;

    public override string ToString() => FormatDouble(_value);

    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return "0"; // NaN and Infinity are not valid in PDF; emit zero as safe fallback
        }

        // If the value is a whole number, format as integer
        if (value == Math.Floor(value))
        {
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        }

        // PDF spec forbids scientific notation — use fixed-point and trim trailing zeros
        string s = value.ToString("0.##############", CultureInfo.InvariantCulture);
        return s;
    }
}
