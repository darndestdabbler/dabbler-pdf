// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Pdf;

namespace DabblerPdf.Text;

/// <summary>
/// Provides text wrapping, measurement, and multi-line rendering utilities.
/// Works with any <see cref="BaseFont"/> and renders via <see cref="PdfContentByte"/>.
/// </summary>
public static class TextLayout
{
    /// <summary>Text alignment options for multi-line rendering.</summary>
    public enum Alignment { Left, Right, Center }

    /// <summary>
    /// Breaks text into lines that fit within the given width.
    /// Handles explicit line breaks (\n) and word-wrapping at word boundaries.
    /// </summary>
    /// <returns>A list of lines, each fitting within <paramref name="maxWidth"/> points.</returns>
    public static List<string> WrapText(string text, BaseFont font, double fontSize, double maxWidth)
    {
        var lines = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            lines.Add("");
            return lines;
        }

        // Split on explicit line breaks to get paragraphs
        var paragraphs = text.Split('\n');
        foreach (var para in paragraphs)
        {
            if (string.IsNullOrEmpty(para))
            {
                lines.Add("");
                continue;
            }

            var words = para.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add("");
                continue;
            }

            var currentLine = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                var testLine = currentLine + " " + words[i];
                var width = font.GetWidthPoint(testLine, fontSize);
                if (width <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    lines.Add(currentLine);
                    currentLine = words[i];
                }
            }
            lines.Add(currentLine);
        }

        return lines;
    }

    /// <summary>
    /// Measures the total height (in points) that wrapped text would occupy.
    /// Useful for CanGrow / CanShrink calculations.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font used for measurement.</param>
    /// <param name="fontSize">Font size in points.</param>
    /// <param name="maxWidth">Maximum width for wrapping in points.</param>
    /// <param name="leading">Line spacing in points. If 0, defaults to fontSize * 1.2.</param>
    /// <returns>Total height in points including all wrapped lines.</returns>
    public static double MeasureHeight(string text, BaseFont font, double fontSize,
        double maxWidth, double leading = 0)
    {
        if (leading == 0) leading = fontSize * 1.2;
        int lineCount = CountLines(text, font, fontSize, maxWidth);
        return lineCount * leading;
    }

    /// <summary>
    /// Counts the number of wrapped lines.
    /// </summary>
    public static int CountLines(string text, BaseFont font, double fontSize, double maxWidth)
    {
        return WrapText(text, font, fontSize, maxWidth).Count;
    }

    /// <summary>
    /// Draws wrapped text onto a PdfContentByte at the specified position.
    /// Text flows downward from (x, y) in PDF coordinates (bottom-left origin).
    /// </summary>
    /// <param name="cb">The content byte to draw on.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="fontSize">Font size in points.</param>
    /// <param name="x">Left edge X position in points.</param>
    /// <param name="y">Top of first line Y position in points (PDF coords — y decreases downward).</param>
    /// <param name="maxWidth">Maximum width for wrapping in points.</param>
    /// <param name="alignment">Text alignment within the maxWidth column.</param>
    /// <param name="leading">Line spacing in points. If 0, defaults to fontSize * 1.2.</param>
    /// <returns>The Y position after the last line (in PDF coordinates).</returns>
    public static double DrawText(PdfContentByte cb, string text, BaseFont font, double fontSize,
        double x, double y, double maxWidth,
        Alignment alignment = Alignment.Left, double leading = 0)
    {
        if (leading == 0) leading = fontSize * 1.2;
        var lines = WrapText(text, font, fontSize, maxWidth);
        if (lines.Count == 0) return y;

        cb.BeginText();
        cb.SetFontAndSize(font, fontSize);

        foreach (var line in lines)
        {
            double lineX = alignment switch
            {
                Alignment.Right => x + maxWidth - font.GetWidthPoint(line, fontSize),
                Alignment.Center => x + (maxWidth - font.GetWidthPoint(line, fontSize)) / 2,
                _ => x
            };

            cb.SetTextMatrix(lineX, y);
            cb.ShowText(line);
            y -= leading;
        }

        cb.EndText();

        return y;
    }
}
