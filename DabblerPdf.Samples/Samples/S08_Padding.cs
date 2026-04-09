using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S08_Padding : SampleBase
{
    public override string Name => "Padding";
    public override string Description => "Cell padding: uniform, asymmetric, nested";
    public override string FileName => "08-padding.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        double y = Margin;

        DrawText(cb, "S08: Padding", boldFont, 18, Margin, y);
        y += 30;

        const string sampleText = "Sample text content";
        double boxWidth = 200;
        double boxHeight = 40;

        // ── 1. No Padding ──
        y = DrawSectionTitle(cb, "1. No Padding", boldFont, y);
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1);
        cb.SetLineDash([], 0);
        DrawRectOutline(cb, Margin, y, boxWidth, boxHeight);
        DrawText(cb, sampleText, font, 10, Margin, y + 14);
        y += boxHeight + 20;

        // ── 2. Uniform Padding (0.1" = 7.2pt on all sides) ──
        y = DrawSectionTitle(cb, "2. Uniform Padding (0.1\" all sides)", boldFont, y);
        double pad = 7.2;
        DrawRectOutline(cb, Margin, y, boxWidth, boxHeight);
        DrawText(cb, sampleText, font, 10, Margin + pad, y + 14 + pad);
        // Draw inner padding boundary as dashed line
        cb.SaveState();
        cb.SetColorStroke(new BaseColor(200, 200, 200));
        cb.SetLineDash([2, 2], 0);
        cb.SetLineWidth(0.5);
        DrawRectOutline(cb, Margin + pad, y + pad, boxWidth - 2 * pad, boxHeight - 2 * pad);
        cb.RestoreState();
        y += boxHeight + 20;

        // ── 3. Asymmetric Padding ──
        y = DrawSectionTitle(cb, "3. Asymmetric Padding (T=0.05\", B=0.15\", L=0.2\", R=0.05\")", boldFont, y);
        double padT = 3.6, padB = 10.8, padL = 14.4, padR = 3.6;
        double asymHeight = 50;
        DrawRectOutline(cb, Margin, y, boxWidth, asymHeight);
        DrawText(cb, sampleText, font, 10, Margin + padL, y + 14 + padT);
        // Inner boundary
        cb.SaveState();
        cb.SetColorStroke(new BaseColor(200, 200, 200));
        cb.SetLineDash([2, 2], 0);
        cb.SetLineWidth(0.5);
        DrawRectOutline(cb, Margin + padL, y + padT, boxWidth - padL - padR, asymHeight - padT - padB);
        cb.RestoreState();

        // Label each padding value
        var labelFont = font;
        DrawLabel(cb, "T=0.05\"", labelFont, Margin + boxWidth + 10, y + 4, 7);
        DrawLabel(cb, "B=0.15\"", labelFont, Margin + boxWidth + 10, y + asymHeight - 10, 7);
        DrawLabel(cb, "L=0.2\"", labelFont, Margin + 2, y + asymHeight + 4, 7);
        DrawLabel(cb, "R=0.05\"", labelFont, Margin + boxWidth - 30, y + asymHeight + 4, 7);
        y += asymHeight + 24;

        // ── 4. Padding with Wrapping ──
        y = DrawSectionTitle(cb, "4. Padding with Wrapping", boldFont, y);
        string longText = "This is a longer text that should wrap within the padded area. The padding reduces the available text width, causing more wrapping than without padding.";
        double wrapPad = 10;
        double wrapWidth = 250;
        double innerWidth = wrapWidth - 2 * wrapPad;
        var lines = WrapText(longText, font, 10, innerWidth);
        double wrapHeight = lines.Count * 14 + 2 * wrapPad;

        DrawRectOutline(cb, Margin, y, wrapWidth, wrapHeight);
        double textY = y + wrapPad + 12;
        foreach (var line in lines)
        {
            DrawText(cb, line, font, 10, Margin + wrapPad, textY);
            textY += 14;
        }
        y += wrapHeight + 20;

        // ── 5. Side-by-Side Comparison ──
        y = DrawSectionTitle(cb, "5. Side-by-Side Comparison", boldFont, y);
        y = DrawLabel(cb, "Same text, same box size. Left: 0\" padding. Right: 0.15\" padding (10.8pt).", font, Margin, y);

        string compText = "This text demonstrates how padding affects the available wrapping width inside a fixed-size element.";
        double compWidth = 200;
        double compPad = 10.8;

        // Left: no padding
        var linesNoPad = WrapText(compText, font, 9, compWidth);
        double compHeight = Math.Max(linesNoPad.Count * 12 + 8, 60);
        DrawRectOutline(cb, Margin, y, compWidth, compHeight);
        double ty = y + 12;
        foreach (var line in linesNoPad)
        {
            DrawText(cb, line, font, 9, Margin + 2, ty);
            ty += 12;
        }
        DrawLabel(cb, "No padding", font, Margin, y + compHeight + 4);

        // Right: with padding
        double rx = Margin + compWidth + 20;
        var linesWithPad = WrapText(compText, font, 9, compWidth - 2 * compPad);
        double compHeightR = Math.Max(linesWithPad.Count * 12 + 2 * compPad, 60);
        DrawRectOutline(cb, rx, y, compWidth, compHeightR);
        ty = y + compPad + 12;
        foreach (var line in linesWithPad)
        {
            DrawText(cb, line, font, 9, rx + compPad, ty);
            ty += 12;
        }
        DrawLabel(cb, "0.15\" padding", font, rx, y + compHeightR + 4);

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
