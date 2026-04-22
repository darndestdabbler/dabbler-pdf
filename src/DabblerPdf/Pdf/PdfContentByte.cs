// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Globalization;
using System.Text;
using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// The main rendering API for drawing on a PDF page. Provides methods for
/// path construction, stroking/filling, color, graphics state, and text.
/// Accumulates PDF content stream operators as bytes.
/// </summary>
public sealed class PdfContentByte
{
    private readonly MemoryStream _buffer = new();
    private readonly PdfWriter _writer;
    private readonly PdfTemplate? _template;

    // Graphics state stack depth tracking
    private int _stateDepth;

    // Current color state for optimization (avoid redundant operators)
    private BaseColor? _currentStrokeColor;
    private BaseColor? _currentFillColor;

    internal PdfContentByte(PdfWriter writer)
    {
        _writer = writer;
    }

    internal PdfContentByte(PdfWriter writer, PdfTemplate template)
    {
        _writer = writer;
        _template = template;
    }

    /// <summary>
    /// Gets the accumulated content stream bytes.
    /// </summary>
    internal byte[] ToByteArray() => _buffer.ToArray();

    /// <summary>
    /// True if any bytes have been written to the current page's buffer since the
    /// last <see cref="Reset"/>. Used by the writer's page-boundary logic to decide
    /// whether pending content must be flushed on a <c>NewPage()</c> transition.
    /// </summary>
    internal bool HasContent => _buffer.Length > 0;

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
    /// <param name="width">The line width in points.</param>
    public void SetLineWidth(double width)
    {
        Append(FormatDouble(width));
        Append(" w\n");
    }

    /// <summary>Sets the line cap style (PDF <c>J</c> operator).</summary>
    /// <param name="style">0 = butt, 1 = round, 2 = projecting square.</param>
    public void SetLineCap(int style)
    {
        Append(style.ToString(CultureInfo.InvariantCulture));
        Append(" J\n");
    }

    /// <summary>Sets the line join style (PDF <c>j</c> operator).</summary>
    /// <param name="style">0 = miter, 1 = round, 2 = bevel.</param>
    public void SetLineJoin(int style)
    {
        Append(style.ToString(CultureInfo.InvariantCulture));
        Append(" j\n");
    }

    /// <summary>Sets the line dash pattern (PDF <c>d</c> operator).</summary>
    /// <param name="pattern">Array of dash/gap lengths in points.</param>
    /// <param name="phase">The dash phase offset in points.</param>
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
    /// <param name="limit">The miter limit value.</param>
    public void SetMiterLimit(double limit)
    {
        Append(FormatDouble(limit));
        Append(" M\n");
    }

    // ─── Color ─────────────────────────────────────────────────────

    /// <summary>Sets the stroke color (PDF <c>RG</c> or <c>G</c> operator).</summary>
    /// <param name="color">The stroke color.</param>
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
    /// <param name="color">The fill color.</param>
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
    /// <param name="x">X coordinate in points.</param>
    /// <param name="y">Y coordinate in points.</param>
    public void MoveTo(double x, double y)
    {
        Append(FormatDouble(x));
        Append(" ");
        Append(FormatDouble(y));
        Append(" m\n");
    }

    /// <summary>Appends a straight line to the current path (PDF <c>l</c> operator).</summary>
    /// <param name="x">End-point X coordinate in points.</param>
    /// <param name="y">End-point Y coordinate in points.</param>
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
    /// <param name="x1">First control point X.</param>
    /// <param name="y1">First control point Y.</param>
    /// <param name="x2">Second control point X.</param>
    /// <param name="y2">Second control point Y.</param>
    /// <param name="x3">End point X.</param>
    /// <param name="y3">End point Y.</param>
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
    /// <param name="x">Lower-left X coordinate in points.</param>
    /// <param name="y">Lower-left Y coordinate in points.</param>
    /// <param name="width">Width in points.</param>
    /// <param name="height">Height in points.</param>
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

    /// <summary>
    /// Appends a rectangle with rounded corners to the current path.
    /// Uses cubic Bézier arcs at each corner.
    /// The path is appended but NOT painted — call <see cref="Stroke"/>,
    /// <see cref="Fill"/>, or <see cref="FillStroke"/> afterward.
    /// </summary>
    /// <param name="x">Lower-left X coordinate in points.</param>
    /// <param name="y">Lower-left Y coordinate in points.</param>
    /// <param name="width">Width in points.</param>
    /// <param name="height">Height in points.</param>
    /// <param name="radius">Corner radius in points. Clamped to half the smaller dimension.</param>
    public void RoundRectangle(double x, double y, double width, double height, double radius)
    {
        // Clamp radius to prevent invalid geometry
        radius = Math.Min(radius, Math.Min(width / 2, height / 2));
        if (radius < 0) radius = 0;

        // Bézier approximation for quarter circle: control point offset
        const double magic = 0.5523; // 4 * (sqrt(2) - 1) / 3

        double b = magic * radius;

        // Start at bottom-left, just above the corner arc
        MoveTo(x + radius, y);

        // Bottom edge → bottom-right corner arc
        LineTo(x + width - radius, y);
        CurveTo(x + width - radius + b, y,
                x + width, y + radius - b,
                x + width, y + radius);

        // Right edge → top-right corner arc
        LineTo(x + width, y + height - radius);
        CurveTo(x + width, y + height - radius + b,
                x + width - radius + b, y + height,
                x + width - radius, y + height);

        // Top edge → top-left corner arc
        LineTo(x + radius, y + height);
        CurveTo(x + radius - b, y + height,
                x, y + height - radius + b,
                x, y + height - radius);

        // Left edge → bottom-left corner arc
        LineTo(x, y + radius);
        CurveTo(x, y + radius - b,
                x + radius - b, y,
                x + radius, y);

        ClosePath();
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
    /// <param name="a">Scale X / rotate component.</param>
    /// <param name="b">Skew Y component.</param>
    /// <param name="c">Skew X component.</param>
    /// <param name="d">Scale Y / rotate component.</param>
    /// <param name="e">Translate X in points.</param>
    /// <param name="f">Translate Y in points.</param>
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
    /// <param name="a">Scale X / rotate component.</param>
    /// <param name="b">Skew Y component.</param>
    /// <param name="c">Skew X component.</param>
    /// <param name="d">Scale Y / rotate component.</param>
    /// <param name="x">Translate X in points.</param>
    /// <param name="y">Translate Y in points.</param>
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
    /// <param name="x">X position in points.</param>
    /// <param name="y">Y position in points.</param>
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

        // Route font registration to template or page resources
        var resourceName = _template != null
            ? _template.AddFont(new PdfName(font.PostscriptName), fontRef)
            : _writer.AddPageFont(new PdfName(font.PostscriptName), fontRef);

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
    /// <param name="text">The text to display.</param>
    public void ShowText(string text)
    {
        Append("(");
        AppendEscapedPdfString(text);
        Append(") Tj\n");
    }

    /// <summary>Sets the character spacing (PDF <c>Tc</c> operator).</summary>
    /// <param name="spacing">Extra spacing between characters in points.</param>
    public void SetCharacterSpacing(double spacing)
    {
        Append(FormatDouble(spacing));
        Append(" Tc\n");
    }

    /// <summary>Sets the word spacing (PDF <c>Tw</c> operator).</summary>
    /// <param name="spacing">Extra spacing between words in points.</param>
    public void SetWordSpacing(double spacing)
    {
        Append(FormatDouble(spacing));
        Append(" Tw\n");
    }

    /// <summary>Sets the text leading (PDF <c>TL</c> operator).</summary>
    /// <param name="leading">Line spacing in points.</param>
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

    // ─── Images ────────────────────────────────────────────────────

    /// <summary>
    /// Adds an image to the page at the image's absolute position and scaled dimensions.
    /// The image must have an absolute position set via <see cref="Image.Image.SetAbsolutePosition"/>.
    /// Emits: q → cm (transform) → /ImN Do → Q.
    /// </summary>
    public void AddImage(Image.Image image)
    {
        if (!image.HasAbsolutePosition)
            throw new InvalidOperationException(
                "Image must have an absolute position set before adding to a page.");

        AddImage(image, image.ScaledWidth, 0, 0, image.ScaledHeight,
            image.AbsoluteX, image.AbsoluteY);
    }

    /// <summary>
    /// Adds an image with an explicit transformation matrix.
    /// The matrix maps from image space (1×1 unit square) to page coordinates.
    /// Parameters a–f form the CTM: [a b c d e f].
    /// Typical usage: a = width, d = height, e = x, f = y (others 0).
    /// </summary>
    public void AddImage(Image.Image image, double a, double b, double c, double d, double e, double f)
    {
        // Register the image as a page XObject and get its resource name
        var resourceName = _writer.AddPageImage(image);

        // Save state, apply transformation, invoke image, restore state
        Append("q\n");
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
        Append("/");
        Append(resourceName.Value);
        Append(" Do\n");
        Append("Q\n");
    }

    // ─── Templates ──────────────────────────────────────────────────

    /// <summary>
    /// Places a PdfTemplate at the given position on the current page.
    /// The template content can be written at any time before document close.
    /// </summary>
    /// <param name="template">The template to place.</param>
    /// <param name="x">X position in points.</param>
    /// <param name="y">Y position in points.</param>
    public void AddTemplate(PdfTemplate template, double x, double y)
    {
        var resourceName = _writer.AddPageTemplate(template);

        Append("q\n");
        Append("1 0 0 1 ");
        Append(FormatDouble(x));
        Append(" ");
        Append(FormatDouble(y));
        Append(" cm\n");
        Append("/");
        Append(resourceName.Value);
        Append(" Do\n");
        Append("Q\n");
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
                case '\n':
                    Append("\\n");
                    break;
                case '\r':
                    Append("\\r");
                    break;
                case '\t':
                    Append("\\t");
                    break;
                case '\b':
                    Append("\\b");
                    break;
                case '\f':
                    Append("\\f");
                    break;
                default:
                    if (ch < 128)
                    {
                        _buffer.WriteByte((byte)ch);
                    }
                    else
                    {
                        // Convert Unicode to WinAnsi byte value, then emit as octal
                        int byteVal = Text.PdfEncodings.CharToWinAnsi(ch);
                        if (byteVal < 0) byteVal = 0x3F; // '?' for unmappable chars
                        Append($"\\{Convert.ToString(byteVal, 8).PadLeft(3, '0')}");
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
