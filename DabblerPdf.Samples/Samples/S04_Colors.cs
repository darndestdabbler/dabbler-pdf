using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S04_Colors : SampleBase
{
    public override string Name => "Colors";
    public override string Description => "Color palette, backgrounds, alternating rows";
    public override string FileName => "04-colors.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        double y = Margin;

        // ── Title ──
        DrawText(cb, "S04: Colors", boldFont, 18, Margin, y);
        y += 30;

        // ── Section 1: Color Swatches ──
        y = DrawSectionTitle(cb, "1. Color Swatches", boldFont, y);

        var swatches = new (string Label, BaseColor Color)[]
        {
            ("Red\n#FF0000", new BaseColor(255, 0, 0)),
            ("Green\n#00FF00", new BaseColor(0, 255, 0)),
            ("Blue\n#0000FF", new BaseColor(0, 0, 255)),
            ("Cyan\n#00FFFF", new BaseColor(0, 255, 255)),
            ("Magenta\n#FF00FF", new BaseColor(255, 0, 255)),
            ("Yellow\n#FFFF00", new BaseColor(255, 255, 0)),
        };

        double swW = 65, swH = 36;
        double sx = Margin;
        foreach (var (label, color) in swatches)
        {
            DrawRectFilledAndStroked(cb, sx, y, swW, swH, color, BaseColor.Black, 0.5);
            var parts = label.Split('\n');
            DrawLabel(cb, parts[0], font, sx + 2, y + swH + 4, 8);
            DrawLabel(cb, parts[1], font, sx + 2, y + swH + 14, 7);
            sx += swW + 8;
        }
        y += swH + 30;

        // Grayscale row
        var grays = new (string Label, BaseColor Color)[]
        {
            ("White\n#FFFFFF", BaseColor.White),
            ("Light\n#CCCCCC", new BaseColor(204, 204, 204)),
            ("Medium\n#999999", new BaseColor(153, 153, 153)),
            ("Dark\n#333333", new BaseColor(51, 51, 51)),
            ("Black\n#000000", BaseColor.Black),
        };

        sx = Margin;
        foreach (var (label, color) in grays)
        {
            DrawRectFilledAndStroked(cb, sx, y, swW, swH, color, BaseColor.Black, 0.5);
            var parts = label.Split('\n');
            DrawLabel(cb, parts[0], font, sx + 2, y + swH + 4, 8);
            DrawLabel(cb, parts[1], font, sx + 2, y + swH + 14, 7);
            sx += swW + 8;
        }
        y += swH + 30;

        // ── Section 2: Text Colors ──
        y = DrawSectionTitle(cb, "2. Text Colors", boldFont, y);

        var textColors = new (string Label, BaseColor Color)[]
        {
            ("Black text", BaseColor.Black),
            ("Red text", new BaseColor(255, 0, 0)),
            ("Blue text", new BaseColor(0, 0, 255)),
            ("Green text", new BaseColor(0, 128, 0)),
            ("Dark gray text", new BaseColor(100, 100, 100)),
        };

        foreach (var (label, color) in textColors)
        {
            DrawText(cb, label, font, 11, Margin, y, color);
            y += 16;
        }

        // White text on dark background
        DrawRectFilled(cb, Margin, y - 2, 200, 18, new BaseColor(51, 51, 51));
        DrawText(cb, "White text on #333333 background", font, 11, Margin + 4, y + 11, BaseColor.White);
        y += 26;

        // ── Section 3: Background Fills ──
        y = DrawSectionTitle(cb, "3. Background Fills", boldFont, y);

        var bgColors = new (string Label, BaseColor Bg)[]
        {
            ("Light yellow (#FFFFCC)", new BaseColor(255, 255, 204)),
            ("Light blue (#CCE5FF)", new BaseColor(204, 229, 255)),
            ("Light green (#CCFFCC)", new BaseColor(204, 255, 204)),
            ("Light red (#FFCCCC)", new BaseColor(255, 204, 204)),
        };

        foreach (var (label, bg) in bgColors)
        {
            DrawRectFilled(cb, Margin, y - 2, ContentWidth, 20, bg);
            DrawText(cb, $"Readable black text on {label}", font, 10, Margin + 4, y + 11);
            y += 24;
        }
        y += 8;

        // ── Section 4: Alternating Rows ──
        y = DrawSectionTitle(cb, "4. Alternating Row Simulation", boldFont, y);

        var white = BaseColor.White;
        var altGray = new BaseColor(240, 240, 240);

        for (int i = 0; i < 10; i++)
        {
            var rowBg = i % 2 == 0 ? white : altGray;
            DrawRectFilled(cb, Margin, y - 2, ContentWidth, 18, rowBg);
            DrawText(cb, $"Row {i + 1}: This is a sample data row with alternating background colors.", font, 10, Margin + 4, y + 11);
            y += 18;
        }

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
