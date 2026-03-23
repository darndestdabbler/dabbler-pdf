// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace OpenPdf.Document;

/// <summary>
/// Standard page size constants in PDF points (1/72 inch).
/// </summary>
public static class PageSize
{
    /// <summary>US Letter: 8.5 x 11 inches (612 x 792 points).</summary>
    public static readonly Rectangle Letter = new(612, 792);

    /// <summary>US Legal: 8.5 x 14 inches (612 x 1008 points).</summary>
    public static readonly Rectangle Legal = new(612, 1008);

    /// <summary>ISO A4: 210 x 297 mm (595.28 x 841.89 points).</summary>
    public static readonly Rectangle A4 = new(595.28, 841.89);

    /// <summary>ISO A3: 297 x 420 mm (841.89 x 1190.55 points).</summary>
    public static readonly Rectangle A3 = new(841.89, 1190.55);

    /// <summary>ISO A5: 148 x 210 mm (419.53 x 595.28 points).</summary>
    public static readonly Rectangle A5 = new(419.53, 595.28);

    /// <summary>US Tabloid: 11 x 17 inches (792 x 1224 points).</summary>
    public static readonly Rectangle Tabloid = new(792, 1224);

    /// <summary>US Executive: 7.25 x 10.5 inches (522 x 756 points).</summary>
    public static readonly Rectangle Executive = new(522, 756);
}
