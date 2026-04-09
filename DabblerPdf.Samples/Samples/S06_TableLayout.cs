using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S06_TableLayout : SampleBase
{
    public override string Name => "Table Layout";
    public override string Description => "Grid-like layout with borders and alignment";
    public override string FileName => "06-table-layout.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        double y = Margin;

        // ── Title ──
        DrawText(cb, "S06: Table Layout", boldFont, 18, Margin, y);
        y += 30;
        y = DrawSectionTitle(cb, "Simple 4-Column Product Table", boldFont, y);

        // Column definitions: name, width, alignment (0=left, 1=right, 2=center)
        var columns = new (string Header, double Width, int Align)[]
        {
            ("Product", 180, 0),
            ("Category", 108, 0),
            ("Price", 72, 1),
            ("In Stock", 72, 2),
        };

        var data = new string[][]
        {
            ["Widget Pro", "Hardware", "$29.99", "Yes"],
            ["Gadget Mini", "Electronics", "$149.50", "Yes"],
            ["Cable 6ft", "Accessories", "$9.99", "No"],
            ["Battery Pack", "Power", "$24.95", "Yes"],
            ["Screen Guard", "Accessories", "$12.49", "Yes"],
            ["USB Hub 4-Port", "Electronics", "$34.99", "No"],
            ["Stylus Pen", "Accessories", "$7.99", "Yes"],
            ["Desk Mount", "Hardware", "$59.95", "Yes"],
        };

        double rowHeight = 20;
        double tableX = Margin;
        double tableWidth = columns.Sum(c => c.Width);
        var headerBg = new BaseColor(224, 224, 224);
        var altBg = new BaseColor(245, 245, 245);
        double cellPadding = 4;

        // ── Header Row ──
        DrawRectFilled(cb, tableX, y, tableWidth, rowHeight, headerBg);

        double cx = tableX;
        foreach (var (header, width, _) in columns)
        {
            DrawText(cb, header, boldFont, 10, cx + cellPadding, y + 14);
            cx += width;
        }

        // Header underline (2pt)
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(2);
        cb.SetLineDash([], 0);
        DrawHLine(cb, tableX, y + rowHeight, tableWidth);

        y += rowHeight;

        // ── Data Rows ──
        for (int row = 0; row < data.Length; row++)
        {
            // Alternating background
            if (row % 2 == 1)
            {
                DrawRectFilled(cb, tableX, y, tableWidth, rowHeight, altBg);
            }

            cx = tableX;
            for (int col = 0; col < columns.Length; col++)
            {
                var (_, width, align) = columns[col];
                var text = data[row][col];
                double textWidth = font.GetWidthPoint(text, 10);

                double tx = align switch
                {
                    1 => cx + width - textWidth - cellPadding,  // right
                    2 => cx + (width - textWidth) / 2,          // center
                    _ => cx + cellPadding,                       // left
                };

                DrawText(cb, text, font, 10, tx, y + 14);
                cx += width;
            }

            // Grid line below row (0.5pt)
            cb.SetColorStroke(new BaseColor(200, 200, 200));
            cb.SetLineWidth(0.5);
            DrawHLine(cb, tableX, y + rowHeight, tableWidth);

            y += rowHeight;
        }

        // ── Vertical Grid Lines ──
        cb.SetColorStroke(new BaseColor(200, 200, 200));
        cb.SetLineWidth(0.5);
        double gridTop = Margin + 30 + 22; // below section title
        double gridBottom = y;

        cx = tableX;
        for (int i = 0; i <= columns.Length; i++)
        {
            cb.MoveTo(cx, PdfY(gridTop));
            cb.LineTo(cx, PdfY(gridBottom));
            cb.Stroke();
            if (i < columns.Length) cx += columns[i].Width;
        }

        // Outer border
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1);
        DrawRectOutline(cb, tableX, gridTop, tableWidth, gridBottom - gridTop);

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
