// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Utilities;

/// <summary>
/// Converts between top-left-down inches (Dabbler.Report coordinate system)
/// and bottom-left-up points (PDF coordinate system).
/// </summary>
public static class CoordinateTranslator
{
    private const double PointsPerInch = 72.0;

    /// <summary>
    /// Converts a top-left-down position in inches to PDF coordinates (bottom-left-up, points).
    /// </summary>
    /// <param name="x">Horizontal position in inches from the left edge.</param>
    /// <param name="y">Vertical position in inches from the top edge.</param>
    /// <param name="elementHeight">Height of the element in inches.</param>
    /// <param name="pageHeightInches">Total page height in inches.</param>
    /// <returns>PDF coordinates in points (origin at bottom-left).</returns>
    public static (double PdfX, double PdfY) FromTopLeftInches(
        double x, double y, double elementHeight, double pageHeightInches)
    {
        return (x * PointsPerInch, (pageHeightInches - y - elementHeight) * PointsPerInch);
    }

    /// <summary>
    /// Converts inches to PDF points (1 inch = 72 points).
    /// </summary>
    public static double InchesToPoints(double inches) => inches * PointsPerInch;

    /// <summary>
    /// Converts PDF points to inches (72 points = 1 inch).
    /// </summary>
    public static double PointsToInches(double points) => points / PointsPerInch;
}
