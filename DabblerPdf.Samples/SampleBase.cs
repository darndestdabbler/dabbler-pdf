using System.Diagnostics;
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;

namespace DabblerPdf.Samples;

public abstract class SampleBase
{
    // Letter page dimensions in points
    protected const double PageWidth = 612;
    protected const double PageHeight = 792;
    protected const double Margin = 72; // 1 inch
    protected const double ContentWidth = 468; // PageWidth - 2 * Margin

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string FileName { get; }

    public void Generate(string outputDir)
    {
        var sw = Stopwatch.StartNew();
        GenerateCore(outputDir);
        sw.Stop();
        PrintResult(outputDir, sw);
    }

    protected abstract void GenerateCore(string outputDir);

    protected string OutputPath(string outputDir) =>
        Path.Combine(outputDir, FileName);

    private void PrintResult(string outputDir, Stopwatch sw)
    {
        var path = OutputPath(outputDir);
        var size = new FileInfo(path).Length;
        Console.WriteLine($"  \u2713 {Name} \u2014 {size / 1024.0:F1} KB, {sw.ElapsedMilliseconds} ms \u2192 {FileName}");
    }

    // ─── PDF Creation Helpers ──────────────────────────────────────

    protected static (Document.Document doc, PdfWriter writer, MemoryStream ms) CreateDocument()
    {
        var ms = new MemoryStream();
        var doc = new Document.Document(PageSize.Letter);
        var writer = PdfWriter.GetInstance(doc, ms);
        doc.Open();
        return (doc, writer, ms);
    }

    protected static void SaveAndClose(Document.Document doc, MemoryStream ms, string path)
    {
        doc.Close();
        File.WriteAllBytes(path, ms.ToArray());
    }

    // ─── Coordinate Helpers ────────────────────────────────────────
    // We work in "top-down" points: x from left, y from top of page.
    // These convert to PDF coordinates (bottom-left origin).

    /// <summary>Convert top-down Y to PDF Y coordinate.</summary>
    protected static double PdfY(double topDownY) => PageHeight - topDownY;

    // ─── Text Drawing Helpers ──────────────────────────────────────

    /// <summary>Draw a single line of text at (x, y) in top-down coords.</summary>
    protected static void DrawText(PdfContentByte cb, string text, BaseFont font, double size, double x, double y)
    {
        cb.BeginText();
        cb.SetFontAndSize(font, size);
        cb.SetTextMatrix(x, PdfY(y));
        cb.ShowText(text);
        cb.EndText();
    }

    /// <summary>Draw text with a specific fill color.</summary>
    protected static void DrawText(PdfContentByte cb, string text, BaseFont font, double size, double x, double y, BaseColor color)
    {
        cb.SaveState();
        cb.BeginText();
        cb.SetFontAndSize(font, size);
        cb.SetColorFill(color);
        cb.SetTextMatrix(x, PdfY(y));
        cb.ShowText(text);
        cb.EndText();
        cb.RestoreState();
    }

    /// <summary>
    /// Word-wrap text within maxWidth and draw it. Returns the Y position after the last line.
    /// </summary>
    protected static double DrawWrappedText(PdfContentByte cb, string text, BaseFont font, double size,
        double x, double y, double maxWidth, double leading = 0)
    {
        if (leading == 0) leading = size * 1.2;
        var lines = WrapText(text, font, size, maxWidth);
        foreach (var line in lines)
        {
            DrawText(cb, line, font, size, x, y);
            y += leading;
        }
        return y;
    }

    /// <summary>Break text into lines that fit within maxWidth.</summary>
    protected static List<string> WrapText(string text, BaseFont font, double size, double maxWidth)
    {
        var lines = new List<string>();

        // Handle explicit line breaks first
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
                var width = font.GetWidthPoint(testLine, size);
                if (width <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    lines.Add(currentLine);
                    currentLine = words[i];

                    // If a single word exceeds maxWidth, just add it as its own line
                }
            }
            lines.Add(currentLine);
        }
        return lines;
    }

    /// <summary>Measure the number of wrapped lines for text in the given width.</summary>
    protected static int CountWrappedLines(string text, BaseFont font, double size, double maxWidth)
    {
        return WrapText(text, font, size, maxWidth).Count;
    }

    // ─── Shape Drawing Helpers ─────────────────────────────────────

    /// <summary>Draw a horizontal line at (x, y) with given width, in top-down coords.</summary>
    protected static void DrawHLine(PdfContentByte cb, double x, double y, double width)
    {
        cb.MoveTo(x, PdfY(y));
        cb.LineTo(x + width, PdfY(y));
        cb.Stroke();
    }

    /// <summary>Draw a rectangle outline at (x, y) in top-down coords.</summary>
    protected static void DrawRectOutline(PdfContentByte cb, double x, double y, double width, double height)
    {
        cb.Rectangle(x, PdfY(y + height), width, height);
        cb.Stroke();
    }

    /// <summary>Draw a filled rectangle at (x, y) in top-down coords.</summary>
    protected static void DrawRectFilled(PdfContentByte cb, double x, double y, double width, double height, BaseColor fillColor)
    {
        cb.SaveState();
        cb.SetColorFill(fillColor);
        cb.Rectangle(x, PdfY(y + height), width, height);
        cb.Fill();
        cb.RestoreState();
    }

    /// <summary>Draw a rectangle with both fill and stroke in top-down coords.</summary>
    protected static void DrawRectFilledAndStroked(PdfContentByte cb, double x, double y, double width, double height,
        BaseColor fillColor, BaseColor strokeColor, double lineWidth = 0.5)
    {
        cb.SaveState();
        cb.SetColorFill(fillColor);
        cb.SetColorStroke(strokeColor);
        cb.SetLineWidth(lineWidth);
        cb.Rectangle(x, PdfY(y + height), width, height);
        cb.FillStroke();
        cb.RestoreState();
    }

    // ─── Section Helpers ───────────────────────────────────────────

    /// <summary>Draw a section title and return the Y position below it.</summary>
    protected static double DrawSectionTitle(PdfContentByte cb, string title, BaseFont boldFont, double y, double size = 14)
    {
        DrawText(cb, title, boldFont, size, Margin, y);
        return y + size + 8;
    }

    /// <summary>Draw a label at the given position.</summary>
    protected static double DrawLabel(PdfContentByte cb, string label, BaseFont font, double x, double y, double size = 9)
    {
        DrawText(cb, label, font, size, x, y, new BaseColor(100, 100, 100));
        return y + size + 4;
    }
}
