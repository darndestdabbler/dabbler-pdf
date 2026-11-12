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
    public enum Alignment
    {
        /// <summary>Left-aligned text (default).</summary>
        Left,

        /// <summary>Right-aligned text.</summary>
        Right,

        /// <summary>Center-aligned text.</summary>
        Center
    }

    /// <summary>
    /// Breaks text into lines that fit within the given width.
    /// Handles explicit line breaks (\n) and word-wrapping at runs of spaces.
    /// </summary>
    /// <remarks>
    /// Spaces are preserved. A paragraph (the text between explicit line breaks) that fits
    /// within <paramref name="maxWidth"/> is returned exactly as given, including leading,
    /// interior and trailing spaces. A paragraph that does not fit is broken only at a run of
    /// spaces: the run at which a line break is taken is dropped, so no line starts or ends
    /// with it, and every other run is kept at its full length. A word wider than
    /// <paramref name="maxWidth"/> is broken at character boundaries.
    /// </remarks>
    /// <param name="text">The text to wrap.</param>
    /// <param name="font">The font used for measurement.</param>
    /// <param name="fontSize">The font size in points.</param>
    /// <param name="maxWidth">The maximum width for each line in points.</param>
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
            if (para.Length == 0 || font.GetWidthPoint(para, fontSize) <= maxWidth)
            {
                lines.Add(para);
                continue;
            }

            WrapParagraph(para, font, fontSize, maxWidth, lines);
        }

        return lines;
    }

    /// <summary>
    /// Wraps one paragraph that does not fit, breaking only at runs of spaces.
    /// Completed lines, including the last, are added to <paramref name="lines"/>.
    /// </summary>
    private static void WrapParagraph(string para, BaseFont font, double fontSize, double maxWidth, List<string> lines)
    {
        var currentLine = "";
        bool hasWord = false;
        int i = 0;
        while (i < para.Length)
        {
            // Each piece is a run of spaces (possibly empty) followed by a word (possibly empty)
            int runStart = i;
            while (i < para.Length && para[i] == ' ') i++;
            int wordStart = i;
            while (i < para.Length && para[i] != ' ') i++;
            var run = para[runStart..wordStart];
            var word = para[wordStart..i];

            if (!hasWord)
            {
                // A paragraph of spaces only that does not fit yields an empty line
                if (word.Length == 0) break;
                // Leading spaces stay with the first word: no break is taken before it
                currentLine = PlaceWord(run + word, font, fontSize, maxWidth, lines);
                hasWord = true;
            }
            else if (font.GetWidthPoint(currentLine + run + word, fontSize) <= maxWidth)
            {
                currentLine += run + word;
            }
            else if (word.Length > 0)
            {
                // Break at this run and drop it
                lines.Add(currentLine);
                currentLine = PlaceWord(word, font, fontSize, maxWidth, lines);
            }
            // else: a trailing run that does not fit is where the break is taken, so it is dropped
        }
        lines.Add(currentLine);
    }

    /// <summary>
    /// Starts a line with <paramref name="word"/>, breaking it at character boundaries
    /// when it is wider than <paramref name="maxWidth"/>.
    /// </summary>
    private static string PlaceWord(string word, BaseFont font, double fontSize, double maxWidth, List<string> lines)
    {
        return font.GetWidthPoint(word, fontSize) > maxWidth
            ? BreakWord(word, font, fontSize, maxWidth, lines)
            : word;
    }

    /// <summary>
    /// Breaks a single word at character boundaries when it exceeds maxWidth.
    /// Completed lines are added to <paramref name="lines"/>; the remaining fragment is returned.
    /// </summary>
    private static string BreakWord(string word, BaseFont font, double fontSize, double maxWidth, List<string> lines)
    {
        var fragment = "";
        foreach (var c in word)
        {
            var test = fragment + c;
            if (font.GetWidthPoint(test, fontSize) > maxWidth && fragment.Length > 0)
            {
                lines.Add(fragment);
                fragment = c.ToString();
            }
            else
            {
                fragment = test;
            }
        }
        return fragment;
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
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font used for measurement.</param>
    /// <param name="fontSize">The font size in points.</param>
    /// <param name="maxWidth">The maximum width for wrapping in points.</param>
    /// <returns>The number of lines after wrapping.</returns>
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

        // Adjust y from top-of-text-area to first line baseline
        y -= font.GetFontDescriptor(BaseFont.ASCENT, fontSize);

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
