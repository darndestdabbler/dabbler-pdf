// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Globalization;
using System.Text;
using OpenPdf.Document;
using OpenPdf.Text;

namespace OpenPdf.Pdf;

/// <summary>
/// The main rendering API for drawing on a PDF page. Provides methods for
/// path construction, stroking/filling, color, graphics state, and text.
/// Accumulates PDF content stream operators as bytes.
/// </summary>
public sealed class PdfContentByte
{
    private readonly MemoryStream _buffer = new();
    private readonly PdfWriter _writer;

    // Graphics state stack depth tracking
    private int _stateDepth;

    // Current color state for optimization (avoid redundant operators)
    private BaseColor? _currentStrokeColor;
    private BaseColor? _currentFillColor;

    internal PdfContentByte(PdfWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Gets the accumulated content stream bytes.
    /// </summary>
    internal byte[] ToByteArray() => _buffer.ToArray();

    /// <summary>
    /// Resets the content buffer for reuse on the next page.
    /// </summary>
    internal void Reset()
    {
        _buffer.SetLength(0);
        _stateDepth = 0;
        _currentStrokeColor = null;
        _currentFillColor = null;
    }

    // ─── Graphics State ────────────────────────────────────────────

    /// <summary>Saves the current graphics state (PDF <c>q</c> operator).</summary>
    public void SaveState()
    {
        Append("q\n");
        _stateDepth++;
    }

    /// <summary>Restores the previous graphics state (PDF <c>Q</c> operator).</summary>
    public void RestoreState()
    {
        if (_stateDepth <= 0)
            throw new InvalidOperationException("Graphics state stack underflow.");
        Append("Q\n");
        _stateDepth--;
        // After restore, cached colors are unknown
        _currentStrokeColor = null;
        _currentFillColor = null;
    }

    /// <summary>Sets the line width (PDF <c>w</c> operator).</summary>
    public void SetLineWidth(double width)
    {
        Append(FormatDouble(width));
        Append(" w\n");
    }

    /// <summary>Sets the line cap style (PDF <c>J</c> operator).</summary>
    public void SetLineCap(int style)
    {
        Append(style.ToString(CultureInfo.InvariantCulture));
        Append(" J\n");
    }

    /// <summary>Sets the line join style (PDF <c>j</c> operator).</summary>
    public void SetLineJoin(int style)
    {
        Append(style.ToString(CultureInfo.InvariantCulture));
        Append(" j\n");
    }

    /// <summary>Sets the line dash pattern (PDF <c>d</c> operator).</summary>
    public void SetLineDash(double[] pattern, double phase)
    {
        Append("[");
        for (int i = 0; i < pattern.Length; i++)
        {
            if (i > 0) Append(" ");
            Append(FormatDouble(pattern[i]));
        }
        Append("] ");
        Append(FormatDouble(phase));
        Append(" d\n");
    }

    /// <summary>Sets the miter limit (PDF <c>M</c> operator).</summary>
    public void SetMiterLimit(double limit)
    {
        Append(FormatDouble(limit));
        Append(" M\n");
    }

    // ─── Color ─────────────────────────────────────────────────────

    /// <summary>Sets the stroke color (PDF <c>RG</c> or <c>G</c> operator).</summary>
    public void SetColorStroke(BaseColor color)
    {
        if (color == _currentStrokeColor)
            return;
        _currentStrokeColor = color;

        if (color.IsGray)
        {
            Append(FormatDouble(color.RedNormalized));
            Append(" G\n");
        }
        else
        {
            Append(FormatDouble(color.RedNormalized));
            Append(" ");
            Append(FormatDouble(color.GreenNormalized));
            Append(" ");
            Append(FormatDouble(color.BlueNormalized));
            Append(" RG\n");
        }
    }

    /// <summary>Sets the fill color (PDF <c>rg</c> or <c>g</c> operator).</summary>
    public void SetColorFill(BaseColor color)
    {
        if (color == _currentFillColor)
            return;
        _currentFillColor = color;

        if (color.IsGray)
        {
            Append(FormatDouble(color.RedNormalized));
            Append(" g\n");
        }
        else
        {
            Append(FormatDouble(color.RedNormalized));
            Append(" ");
            Append(FormatDouble(color.GreenNormalized));
            Append(" ");
            Append(FormatDouble(color.BlueNormalized));
            Append(" rg\n");
        }
    }

    /// <summary>Resets the stroke color to black.</summary>
    public void ResetColorStroke()
    {
        SetColorStroke(BaseColor.Black);
    }

    /// <summary>Resets the fill color to black.</summary>
    public void ResetColorFill()
    {
        SetColorFill(BaseColor.Black);
    }

    // ─── Path Construction ─────────────────────────────────────────

    /// <summary>Begins a new subpath at the given point (PDF <c>m</c> operator).</summary>
    public void MoveTo(double x, double y)
    {
        Append(FormatDouble(x));
        Append(" ");
        Append(FormatDouble(y));
        Append(" m\n");
    }

    /// <summary>Appends a straight line to the current path (PDF <c>l</c> operator).</summary>
    public void LineTo(double x, double y)
    {
        Append(FormatDouble(x));
        Append(" ");
        Append(FormatDouble(y));
        Append(" l\n");
    }

    /// <summary>
    /// Appends a cubic Bézier curve to the current path (PDF <c>c</c> operator).
    /// </summary>
    public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        Append(FormatDouble(x1));
        Append(" ");
        Append(FormatDouble(y1));
        Append(" ");
        Append(FormatDouble(x2));
        Append(" ");
        Append(FormatDouble(y2));
        Append(" ");
        Append(FormatDouble(x3));
        Append(" ");
        Append(FormatDouble(y3));
        Append(" c\n");
    }

    /// <summary>
    /// Appends a rectangle to the current path (PDF <c>re</c> operator).
    /// </summary>
    public void Rectangle(double x, double y, double width, double height)
    {
        Append(FormatDouble(x));
        Append(" ");
        Append(FormatDouble(y));
        Append(" ");
        Append(FormatDouble(width));
        Append(" ");
        Append(FormatDouble(height));
        Append(" re\n");
    }

    /// <summary>Closes the current subpath (PDF <c>h</c> operator).</summary>
    public void ClosePath()
    {
        Append("h\n");
    }

    // ─── Path Painting ─────────────────────────────────────────────

    /// <summary>Strokes the current path (PDF <c>S</c> operator).</summary>
    public void Stroke()
    {
        Append("S\n");
    }

    /// <summary>Closes and strokes the current path (PDF <c>s</c> operator).</summary>
    public void ClosePathStroke()
    {
        Append("s\n");
    }

    /// <summary>Fills the current path using the non-zero winding rule (PDF <c>f</c> operator).</summary>
    public void Fill()
    {
        Append("f\n");
    }

    /// <summary>Fills the current path using the even-odd rule (PDF <c>f*</c> operator).</summary>
    public void EoFill()
    {
        Append("f*\n");
    }

    /// <summary>Fills and strokes the current path (PDF <c>B</c> operator).</summary>
    public void FillStroke()
    {
        Append("B\n");
    }

    /// <summary>Fills (even-odd) and strokes the current path (PDF <c>B*</c> operator).</summary>
    public void EoFillStroke()
    {
        Append("B*\n");
    }

    /// <summary>Closes, fills, and strokes (PDF <c>b</c> operator).</summary>
    public void ClosePathFillStroke()
    {
        Append("b\n");
    }

    /// <summary>Ends the path without filling or stroking (PDF <c>n</c> operator).</summary>
    public void NewPath()
    {
        Append("n\n");
    }

    // ─── Clipping ──────────────────────────────────────────────────

    /// <summary>Intersects the clipping path with the current path (non-zero winding, PDF <c>W</c>).</summary>
    public void Clip()
    {
        Append("W\n");
    }

    /// <summary>Intersects the clipping path with the current path (even-odd, PDF <c>W*</c>).</summary>
    public void EoClip()
    {
        Append("W*\n");
    }

    // ─── Coordinate Transform ──────────────────────────────────────

    /// <summary>
    /// Applies a transformation matrix (PDF <c>cm</c> operator).
    /// The matrix is [a b c d e f] as in the PDF spec.
    /// </summary>
    public void ConcatCtm(double a, double b, double c, double d, double e, double f)
    {
        Append(FormatDouble(a));
        Append(" ");
        Append(FormatDouble(b));
        Append(" ");
        Append(FormatDouble(c));
        Append(" ");
        Append(FormatDouble(d));
        Append(" ");
        Append(FormatDouble(e));
        Append(" ");
        Append(FormatDouble(f));
        Append(" cm\n");
    }

    // ─── Text (Phase C stubs — will be expanded) ───────────────────

    /// <summary>Begins a text object (PDF <c>BT</c> operator).</summary>
    public void BeginText()
    {
        Append("BT\n");
    }

    /// <summary>Ends a text object (PDF <c>ET</c> operator).</summary>
    public void EndText()
    {
        Append("ET\n");
    }

    /// <summary>
    /// Sets the text matrix (PDF <c>Tm</c> operator).
    /// For simple positioning, use <c>SetTextMatrix(x, y)</c>.
    /// </summary>
    public void SetTextMatrix(double a, double b, double c, double d, double x, double y)
    {
        Append(FormatDouble(a));
        Append(" ");
        Append(FormatDouble(b));
        Append(" ");
        Append(FormatDouble(c));
        Append(" ");
        Append(FormatDouble(d));
        Append(" ");
        Append(FormatDouble(x));
        Append(" ");
        Append(FormatDouble(y));
        Append(" Tm\n");
    }

    /// <summary>
    /// Sets the text position (identity matrix with translation).
    /// </summary>
    public void SetTextMatrix(double x, double y)
    {
        SetTextMatrix(1, 0, 0, 1, x, y);
    }

    /// <summary>
    /// Sets the font and size for subsequent text operations (PDF <c>Tf</c> operator).
    /// Automatically registers the font in the current page's resources.
    /// </summary>
    public void SetFontAndSize(BaseFont font, double size)
    {
        // Check if this font is already registered in the writer (avoid creating
        // duplicate font stream/descriptor indirect objects for TrueType fonts)
        var fontRef = _writer.GetCachedFontRef(font.PostscriptName);
        if (fontRef == null)
        {
            PdfDictionary fontDict;
            if (font is Type1Font type1Font)
            {
                fontDict = type1Font.CreateFontDictionary();
            }
            else if (font is TrueTypeFont ttFont)
            {
                fontDict = ttFont.CreateFontDictionary(_writer);
            }
            else
            {
                throw new NotSupportedException($"Font type {font.GetType().Name} is not yet supported for PDF rendering.");
            }

            fontRef = _writer.AddFontObject(fontDict);
        }

        var resourceName = _writer.AddPageFont(new PdfName(font.PostscriptName), fontRef);

        Append("/");
        Append(resourceName.Value);
        Append(" ");
        Append(FormatDouble(size));
        Append(" Tf\n");
    }

    /// <summary>
    /// Sets the font and size using a raw PDF resource name (PDF <c>Tf</c> operator).
    /// The font must already be registered in the page resources.
    /// </summary>
    public void SetFontAndSize(PdfName fontName, double size)
    {
        Append("/");
        Append(fontName.Value);
        Append(" ");
        Append(FormatDouble(size));
        Append(" Tf\n");
    }

    /// <summary>Shows a text string (PDF <c>Tj</c> operator).</summary>
    public void ShowText(string text)
    {
        Append("(");
        AppendEscapedPdfString(text);
        Append(") Tj\n");
    }

    /// <summary>Sets the character spacing (PDF <c>Tc</c> operator).</summary>
    public void SetCharacterSpacing(double spacing)
    {
        Append(FormatDouble(spacing));
        Append(" Tc\n");
    }

    /// <summary>Sets the word spacing (PDF <c>Tw</c> operator).</summary>
    public void SetWordSpacing(double spacing)
    {
        Append(FormatDouble(spacing));
        Append(" Tw\n");
    }

    /// <summary>Sets the text leading (PDF <c>TL</c> operator).</summary>
    public void SetLeading(double leading)
    {
        Append(FormatDouble(leading));
        Append(" TL\n");
    }

    /// <summary>Moves to the next line (PDF <c>T*</c> operator).</summary>
    public void NewlineShowText()
    {
        Append("T*\n");
    }

    // ─── Internal helpers ──────────────────────────────────────────

    private void Append(string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        _buffer.Write(bytes, 0, bytes.Length);
    }

    private void AppendEscapedPdfString(string text)
    {
        foreach (char ch in text)
        {
            switch (ch)
            {
                case '(':
                    Append("\\(");
                    break;
                case ')':
                    Append("\\)");
                    break;
                case '\\':
                    Append("\\\\");
                    break;
                default:
                    if (ch < 128)
                    {
                        _buffer.WriteByte((byte)ch);
                    }
                    else
                    {
                        // Encode as octal for non-ASCII
                        Append($"\\{(int)ch:o3}");
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Formats a double for PDF output. Uses up to 6 decimal places,
    /// stripping trailing zeros. Integer values emit no decimal point.
    /// </summary>
    internal static string FormatDouble(double value)
    {
        // Fast path for integers
        if (value == Math.Floor(value) && value >= int.MinValue && value <= int.MaxValue)
            return ((int)value).ToString(CultureInfo.InvariantCulture);

        // Use up to 6 decimal places, strip trailing zeros
        string result = value.ToString("0.######", CultureInfo.InvariantCulture);
        return result;
    }
}
