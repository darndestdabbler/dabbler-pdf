using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S02_TextWrapping : SampleBase
{
    public override string Name => "Text Wrapping";
    public override string Description => "Word wrap, CanGrow, long text";
    public override string FileName => "02-text-wrapping.pdf";

    private const string SampleParagraph =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore " +
        "et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut " +
        "aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse " +
        "cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in " +
        "culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus " +
        "error sit voluptatem accusantium doloremque laudantium.";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        double y = Margin;

        // ── Title ──
        DrawText(cb, "S02: Text Wrapping", boldFont, 18, Margin, y);
        y += 30;

        // ── Section 1: Controlled wrapping at different widths ──
        y = DrawSectionTitle(cb, "1. Controlled Wrapping Comparison (TextLayout API)", boldFont, y);

        double[] widths = [108, 216, 468]; // 1.5", 3.0", 6.5" in points
        string[] labels = ["1.5\" wide", "3.0\" wide", "6.5\" wide"];

        for (int i = 0; i < widths.Length; i++)
        {
            y = DrawLabel(cb, labels[i], font, Margin, y);
            double startY = y;
            double leading = 9 * 1.2;

            // Use TextLayout.DrawText (PDF coordinates: PdfY converts top-down to bottom-up)
            double pdfEndY = TextLayout.DrawText(cb, SampleParagraph, font, 9,
                Margin + 4, PdfY(y + 4), widths[i] - 8, leading: leading);

            // Convert the returned PDF Y back to top-down Y
            double endY = PageHeight - pdfEndY;

            // Draw border around the text block
            cb.SetColorStroke(BaseColor.Black);
            cb.SetLineWidth(0.5);
            DrawRectOutline(cb, Margin, startY - 2, widths[i], endY - startY + 6);

            y = endY + 14;
        }

        y += 8;

        // ── Section 2: Single very long word ──
        y = DrawSectionTitle(cb, "2. Single Long Word (1\" wide box)", boldFont, y);
        double longWordY = y;
        double pdfY2 = TextLayout.DrawText(cb, "Supercalifragilisticexpialidocious", font, 10,
            Margin + 4, PdfY(y + 4), 68);
        double longWordEndY = PageHeight - pdfY2;
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(0.5);
        DrawRectOutline(cb, Margin, longWordY - 2, 72, longWordEndY - longWordY + 6);
        y = longWordEndY + 14;

        // ── Section 3: Hard line breaks ──
        y = DrawSectionTitle(cb, "3. Hard Line Breaks", boldFont, y);
        y = DrawLabel(cb, "Text with \\n and \\n\\n:", font, Margin, y);
        string hardBreakText = "Line one\nLine two\nLine three\n\nAfter blank line\nFinal line";
        double pdfY3 = TextLayout.DrawText(cb, hardBreakText, font, 10,
            Margin, PdfY(y), ContentWidth, leading: 12);
        y = PageHeight - pdfY3 + 14;

        // ── Section 4: Alignment demonstration ──
        y = DrawSectionTitle(cb, "4. Alignment: Left / Center / Right", boldFont, y);

        string alignText = "This text demonstrates alignment modes provided by the TextLayout API.";
        double alignWidth = 300;
        var alignments = new[] { TextLayout.Alignment.Left, TextLayout.Alignment.Center, TextLayout.Alignment.Right };
        string[] alignLabels = ["Left aligned", "Center aligned", "Right aligned"];

        for (int i = 0; i < alignments.Length; i++)
        {
            y = DrawLabel(cb, alignLabels[i], font, Margin, y);
            double blockStart = y;

            double pdfEnd = TextLayout.DrawText(cb, alignText, font, 10,
                Margin + 4, PdfY(y + 4), alignWidth - 8, alignments[i], 12);
            double blockEnd = PageHeight - pdfEnd;

            cb.SetColorStroke(new BaseColor(150, 150, 150));
            cb.SetLineWidth(0.5);
            DrawRectOutline(cb, Margin, blockStart - 2, alignWidth, blockEnd - blockStart + 6);
            y = blockEnd + 14;
        }

        // ── Section 5: CanGrow / MeasureHeight demonstration ──
        y = DrawSectionTitle(cb, "5. CanGrow via MeasureHeight", boldFont, y);
        y = DrawLabel(cb, "Both boxes declared at 0.25\" height (18pt). Left: short text. Right: long text (CanGrow).", font, Margin, y);

        double boxHeight = 18;
        double boxWidth = 200;

        // Short text box
        DrawRectOutline(cb, Margin, y, boxWidth, boxHeight);
        DrawText(cb, "Hello", font, 10, Margin + 4, y + 4);

        // Long text box (CanGrow via MeasureHeight)
        double rightX = Margin + boxWidth + 20;
        string longText = "This text is much longer and should cause the CanGrow element to expand beyond its declared 0.25 inch height to fit all content.";
        double measuredHeight = TextLayout.MeasureHeight(longText, font, 10, boxWidth - 8, 12);
        double actualHeight = Math.Max(boxHeight, measuredHeight + 4);
        DrawRectOutline(cb, rightX, y, boxWidth, actualHeight);
        TextLayout.DrawText(cb, longText, font, 10,
            rightX + 4, PdfY(y + 4), boxWidth - 8, leading: 12);

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
