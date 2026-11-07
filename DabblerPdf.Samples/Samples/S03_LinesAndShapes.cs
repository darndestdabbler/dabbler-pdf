using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S03_LinesAndShapes : SampleBase
{
    public override string Name => "Lines and Shapes";
    public override string Description => "Lines, rectangles, borders, dash patterns";
    public override string FileName => "03-lines-and-shapes.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        double y = Margin;

        // ── Title ──
        DrawText(cb, "S03: Lines and Shapes", boldFont, 18, Margin, y);
        y += 30;

        // ── Section 1: Horizontal lines at different weights ──
        y = DrawSectionTitle(cb, "1. Line Weights", boldFont, y);

        double[] weights = [0.25, 0.5, 1, 2, 4];
        foreach (var w in weights)
        {
            DrawLabel(cb, $"{w}pt:", font, Margin, y);
            cb.SetColorStroke(BaseColor.Black);
            cb.SetLineWidth(w);
            cb.SetLineDash([], 0); // solid
            DrawHLine(cb, Margin + 50, y + 4, ContentWidth - 50);
            y += 16;
        }
        y += 8;

        // ── Section 2: Line styles ──
        y = DrawSectionTitle(cb, "2. Line Styles (1pt)", boldFont, y);

        cb.SetLineWidth(1);
        cb.SetColorStroke(BaseColor.Black);

        // Solid
        DrawLabel(cb, "Solid:", font, Margin, y);
        cb.SetLineDash([], 0);
        DrawHLine(cb, Margin + 50, y + 4, ContentWidth - 50);
        y += 16;

        // Dashed
        DrawLabel(cb, "Dashed:", font, Margin, y);
        cb.SetLineDash([6, 3], 0);
        DrawHLine(cb, Margin + 50, y + 4, ContentWidth - 50);
        y += 16;

        // Dotted
        DrawLabel(cb, "Dotted:", font, Margin, y);
        cb.SetLineDash([1, 3], 0);
        DrawHLine(cb, Margin + 50, y + 4, ContentWidth - 50);
        y += 24;

        // Reset dash
        cb.SetLineDash([], 0);

        // ── Section 3: Line colors ──
        y = DrawSectionTitle(cb, "3. Line Colors (1pt solid)", boldFont, y);

        var lineColors = new (string Label, BaseColor Color)[]
        {
            ("Black", BaseColor.Black),
            ("Red (#FF0000)", new BaseColor(255, 0, 0)),
            ("Green (#00FF00)", new BaseColor(0, 255, 0)),
            ("Blue (#0000FF)", new BaseColor(0, 0, 255)),
            ("Gray (#999999)", new BaseColor(153, 153, 153)),
        };

        cb.SetLineWidth(1);
        foreach (var (label, color) in lineColors)
        {
            DrawLabel(cb, label, font, Margin, y);
            cb.SetColorStroke(color);
            cb.SetLineDash([], 0);
            DrawHLine(cb, Margin + 120, y + 4, ContentWidth - 120);
            y += 16;
        }
        cb.SetColorStroke(BaseColor.Black);
        y += 8;

        // ── Section 4: Rectangles ──
        y = DrawSectionTitle(cb, "4. Rectangles", boldFont, y);

        double rectW = 72, rectH = 50;
        double gap = 10;
        double rx = Margin;

        // Border only
        DrawLabel(cb, "Border only", font, rx, y);
        y += 14;
        double rectY = y;
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1);
        cb.SetLineDash([], 0);
        DrawRectOutline(cb, rx, rectY, rectW, rectH);
        rx += rectW + gap;

        // Fill only
        DrawLabel(cb, "Fill only", font, rx, rectY - 14);
        DrawRectFilled(cb, rx, rectY, rectW, rectH, new BaseColor(100, 149, 237));
        rx += rectW + gap;

        // Border + fill
        DrawLabel(cb, "Border+Fill", font, rx, rectY - 14);
        DrawRectFilledAndStroked(cb, rx, rectY, rectW, rectH, new BaseColor(255, 200, 200), BaseColor.Black);
        rx += rectW + gap;

        // Dashed border
        DrawLabel(cb, "Dashed", font, rx, rectY - 14);
        cb.SetLineDash([4, 2], 0);
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1);
        DrawRectOutline(cb, rx, rectY, rectW, rectH);
        rx += rectW + gap;

        // Dotted border
        DrawLabel(cb, "Dotted", font, rx, rectY - 14);
        cb.SetLineDash([1, 3], 0);
        DrawRectOutline(cb, rx, rectY, rectW, rectH);
        rx += rectW + gap;

        // Rounded rectangle
        DrawLabel(cb, "Rounded", font, rx, rectY - 14);
        cb.SetLineDash([], 0);
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1);
        cb.RoundRectangle(rx, PdfY(rectY + rectH), rectW, rectH, 8);
        cb.Stroke();

        cb.SetLineDash([], 0);
        y = rectY + rectH + 20;

        // ── Section 5: Nested rectangles ──
        y = DrawSectionTitle(cb, "5. Nested Rectangles", boldFont, y);

        double outerW = 300, outerH = 100;
        double innerW = 150, innerH = 50;
        double outerX = Margin + (ContentWidth - outerW) / 2;

        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(2);
        DrawRectOutline(cb, outerX, y, outerW, outerH);

        double innerX = outerX + (outerW - innerW) / 2;
        double innerY = y + (outerH - innerH) / 2;
        cb.SetLineWidth(1);
        cb.SetColorStroke(new BaseColor(0, 0, 255));
        DrawRectOutline(cb, innerX, innerY, innerW, innerH);

        DrawText(cb, "Centered inner rectangle", font, 9, innerX + 20, innerY + 20);

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
