# Verification Round 1

ISSUES FOUND

1.  **(Minor)** Potential loss of precision when formatting `PdfNumber`.
    *   **Issue** â†’ The `FormatDouble` method in `PdfNumber` uses the format string `"0.######"`, which truncates double-precision floating-point numbers to a maximum of 6 decimal places. A C# `double` has approximately 15-17 decimal digits of precision. While this correctly prevents scientific notation (a critical fix), it may cause an unacceptable loss of precision in applications requiring high accuracy (e.g., CAD, scientific graphics).
    *   **Location** â†’ `src/DabblerPdf/Pdf/PdfNumber.cs`, line 56
    *   **Fix** â†’ Use a format string that preserves more of the `double`'s native precision. Increasing the number of placeholders is a simple, low-risk improvement.
        ```csharp
        // Before
        string s = value.ToString("0.######", CultureInfo.InvariantCulture);

        // After
        string s = value.ToString("0.##############", CultureInfo.InvariantCulture); // ~14 decimal places
        ```

---

# Verification Round 2

ISSUES FOUND

The applied fix improves precision but does not fully resolve the underlying issue for all use cases. A related problem remains in the `string` constructor, which can still cause unnecessary precision loss.

### Issue: `string` constructor truncates precision

*   **Location**: `public PdfNumber(string content)`
*   **Description**: This constructor parses the input `string` into a `double` and then immediately re-serializes it using `FormatDouble`. If the original string contained more precision than the format string (`0.##############`), that precision is permanently lost. The constructor should preserve the original string representation.

    ```csharp
    // Example of precision loss:
    var original = "0.12345678901234567";
    var num = new PdfNumber(original);
    
    // Expected: "0.12345678901234567"
    // Actual:   "0.12345678901235"
    Console.WriteLine(num.ToString()); 
    ```

*   **Fix**: Modify the `string` constructor to store the original `content` string in `Bytes`. Also, update `ToString()` to return the stored byte representation as a string, ensuring consistency across all constructors.

    ```csharp
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
            _value = double.Parse(content, NumberStyles.Float, CultureInfo.InvariantCulture);
            Bytes = Encoding.ASCII.GetBytes(content);
        }
    
        public int IntValue => (int)_value;
        public double DoubleValue => _value;
    
        public override string ToString() => Encoding.ASCII.GetString(Bytes);
    
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
    ```
