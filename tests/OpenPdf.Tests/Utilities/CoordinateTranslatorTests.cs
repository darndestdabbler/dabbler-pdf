// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using OpenPdf.Utilities;
using Xunit;

namespace OpenPdf.Tests.Utilities;

public class CoordinateTranslatorTests
{
    [Fact]
    public void InchesToPoints_ConvertsCorrectly()
    {
        Assert.Equal(72.0, CoordinateTranslator.InchesToPoints(1.0));
        Assert.Equal(36.0, CoordinateTranslator.InchesToPoints(0.5));
        Assert.Equal(0.0, CoordinateTranslator.InchesToPoints(0.0));
        Assert.Equal(792.0, CoordinateTranslator.InchesToPoints(11.0));
    }

    [Fact]
    public void PointsToInches_ConvertsCorrectly()
    {
        Assert.Equal(1.0, CoordinateTranslator.PointsToInches(72.0));
        Assert.Equal(0.5, CoordinateTranslator.PointsToInches(36.0));
        Assert.Equal(0.0, CoordinateTranslator.PointsToInches(0.0));
        Assert.Equal(11.0, CoordinateTranslator.PointsToInches(792.0));
    }

    [Fact]
    public void InchesToPoints_And_PointsToInches_AreInverses()
    {
        double inches = 3.75;
        double points = CoordinateTranslator.InchesToPoints(inches);
        double roundTrip = CoordinateTranslator.PointsToInches(points);
        Assert.Equal(inches, roundTrip, precision: 10);
    }

    [Fact]
    public void FromTopLeftInches_TopLeftCorner_ReturnsBottomLeftOrigin()
    {
        // Element at top-left of an 11-inch page, 0.5 inch tall
        var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(0, 0, 0.5, 11.0);
        Assert.Equal(0.0, pdfX);
        // Bottom of element: page bottom is 0, so top-left (0,0) with 0.5" height
        // => PDF Y = (11.0 - 0 - 0.5) * 72 = 10.5 * 72 = 756
        Assert.Equal(756.0, pdfY);
    }

    [Fact]
    public void FromTopLeftInches_OneInchMargin_ReturnsExpectedCoordinates()
    {
        // Element at (1", 1") on Letter page (8.5 x 11), element is 0.25" tall
        var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(1.0, 1.0, 0.25, 11.0);
        Assert.Equal(72.0, pdfX);
        // PDF Y = (11.0 - 1.0 - 0.25) * 72 = 9.75 * 72 = 702
        Assert.Equal(702.0, pdfY);
    }

    [Fact]
    public void FromTopLeftInches_BottomOfPage_ReturnsNearZero()
    {
        // Element at bottom of 11-inch page, 0.5" tall, y = 10.5"
        var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(1.0, 10.5, 0.5, 11.0);
        Assert.Equal(72.0, pdfX);
        // PDF Y = (11.0 - 10.5 - 0.5) * 72 = 0
        Assert.Equal(0.0, pdfY);
    }

    [Fact]
    public void FromTopLeftInches_ZeroHeightElement_YIsDistanceFromBottom()
    {
        // Zero-height element (e.g., a line) at y=2" on 11" page
        var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(0, 2.0, 0, 11.0);
        Assert.Equal(0.0, pdfX);
        // PDF Y = (11.0 - 2.0 - 0) * 72 = 648
        Assert.Equal(648.0, pdfY);
    }

    [Fact]
    public void FromTopLeftInches_A4Page_WorksCorrectly()
    {
        // A4 page height is 297mm = 11.693 inches (approx)
        double a4HeightInches = 297.0 / 25.4;
        var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(1.0, 1.0, 0.5, a4HeightInches);
        Assert.Equal(72.0, pdfX);
        double expectedY = (a4HeightInches - 1.0 - 0.5) * 72.0;
        Assert.Equal(expectedY, pdfY, precision: 6);
    }
}
