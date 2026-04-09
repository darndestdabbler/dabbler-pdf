using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S09_InvoiceSample : SampleBase
{
    public override string Name => "Invoice Sample";
    public override string Description => "Realistic invoice document";
    public override string FileName => "09-invoice.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);
        var italicFont = BaseFont.CreateFont(BaseFont.HelveticaOblique, BaseFont.WinAnsiEncoding, false);

        double y = Margin;

        // ── Company Header ──
        DrawText(cb, "Acme Corporation", boldFont, 24, Margin, y + 3);
        DrawText(cb, "123 Business Avenue, Suite 400", font, 9, Margin, y + 28);
        DrawText(cb, "Portland, OR 97201 | (503) 555-0142", font, 9, Margin, y + 40);

        // Logo in top-right
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "sample-logo.png");
        try
        {
            var logo = Image.Image.GetInstance(logoPath);
            logo.ScaleToFit(100, 50);
            double logoX = Margin + ContentWidth - logo.ScaledWidth;
            logo.SetAbsolutePosition(logoX, PdfY(y + 50));
            cb.AddImage(logo);
        }
        catch { /* skip if logo not available */ }

        y += 50;

        // Horizontal rule
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1.5);
        cb.SetLineDash([], 0);
        DrawHLine(cb, Margin, y, ContentWidth);
        y += 12;

        // ── Invoice Metadata (right-aligned) ──
        double metaX = Margin + ContentWidth - 160;
        DrawText(cb, "Invoice #: 10248", boldFont, 10, metaX, y + 10);
        DrawText(cb, "Date: March 15, 2025", font, 10, metaX, y + 24);
        DrawText(cb, "Due: April 14, 2025", font, 10, metaX, y + 38);

        // ── Bill To / Ship To ──
        DrawText(cb, "Bill To:", boldFont, 10, Margin, y + 10);
        DrawText(cb, "Jane Smith", font, 10, Margin, y + 24);
        DrawText(cb, "456 Customer Lane", font, 10, Margin, y + 36);
        DrawText(cb, "Seattle, WA 98101", font, 10, Margin, y + 48);

        double shipX = Margin + 200;
        DrawText(cb, "Ship To:", boldFont, 10, shipX, y + 10);
        DrawText(cb, "Jane Smith", font, 10, shipX, y + 24);
        DrawText(cb, "789 Delivery Road", font, 10, shipX, y + 36);
        DrawText(cb, "Seattle, WA 98101", font, 10, shipX, y + 48);

        y += 68;

        // ── Line Items Table ──
        var columns = new (string Header, double Width, int Align)[]
        {
            ("Item", 80, 0),
            ("Description", 168, 0),
            ("Qty", 40, 1),
            ("Unit Price", 80, 1),
            ("Ext. Price", 80, 1),
        };

        var items = new (string Item, string Desc, int Qty, double Price)[]
        {
            ("WDG-001", "Widget Pro - Standard", 5, 29.99),
            ("WDG-002", "Widget Pro - Deluxe", 2, 49.99),
            ("CBL-003", "USB-C Cable 6ft", 10, 9.99),
            ("BAT-004", "Battery Pack 10000mAh", 3, 24.95),
            ("SCR-005", "Screen Guard Ultra", 8, 12.49),
            ("HUB-006", "USB Hub 4-Port", 1, 34.99),
            ("STY-007", "Stylus Pen Premium", 4, 7.99),
            ("MNT-008", "Adjustable Desk Mount", 2, 59.95),
            ("CAB-009", "Ethernet Cable 10ft", 5, 6.99),
            ("PWR-010", "Wall Charger 65W", 3, 19.99),
        };

        double tableWidth = columns.Sum(c => c.Width);
        double tableX = Margin + (ContentWidth - tableWidth) / 2;
        double rowHeight = 18;
        var headerBg = new BaseColor(224, 224, 224);
        var altBg = new BaseColor(248, 248, 248);
        double cellPad = 4;

        // Header row
        DrawRectFilled(cb, tableX, y, tableWidth, rowHeight, headerBg);
        double cx = tableX;
        foreach (var (header, width, _) in columns)
        {
            DrawText(cb, header, boldFont, 9, cx + cellPad, y + 5);
            cx += width;
        }

        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1.5);
        DrawHLine(cb, tableX, y + rowHeight, tableWidth);
        y += rowHeight;

        // Data rows
        double subtotal = 0;
        for (int i = 0; i < items.Length; i++)
        {
            var (item, desc, qty, price) = items[i];
            double ext = qty * price;
            subtotal += ext;

            if (i % 2 == 1)
                DrawRectFilled(cb, tableX, y, tableWidth, rowHeight, altBg);

            string[] cells = [item, desc, qty.ToString(), $"${price:F2}", $"${ext:F2}"];

            cx = tableX;
            for (int col = 0; col < columns.Length; col++)
            {
                var (_, width, align) = columns[col];
                double tw = font.GetWidthPoint(cells[col], 9);
                double tx = align == 1 ? cx + width - tw - cellPad : cx + cellPad;
                DrawText(cb, cells[col], font, 9, tx, y + 5);
                cx += width;
            }

            cb.SetColorStroke(new BaseColor(220, 220, 220));
            cb.SetLineWidth(0.25);
            DrawHLine(cb, tableX, y + rowHeight, tableWidth);
            y += rowHeight;
        }

        // Table border
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(0.5);
        DrawRectOutline(cb, tableX, y - items.Length * rowHeight - rowHeight, tableWidth,
            (items.Length + 1) * rowHeight);

        y += 8;

        // ── Subtotals Section ──
        double totalsX = tableX + tableWidth - 180;
        double totalsW = 180;

        cb.SetLineWidth(0.5);
        cb.SetColorStroke(BaseColor.Black);

        // Subtotal
        DrawText(cb, "Subtotal:", font, 10, totalsX, y + 12);
        string subtotalStr = $"${subtotal:F2}";
        double stw = font.GetWidthPoint(subtotalStr, 10);
        DrawText(cb, subtotalStr, font, 10, totalsX + totalsW - stw, y + 12);
        y += 18;

        // Tax
        double tax = subtotal * 0.07;
        DrawText(cb, "Tax (7%):", font, 10, totalsX, y + 12);
        string taxStr = $"${tax:F2}";
        double txw = font.GetWidthPoint(taxStr, 10);
        DrawText(cb, taxStr, font, 10, totalsX + totalsW - txw, y + 12);
        y += 18;

        // Total line
        DrawHLine(cb, totalsX, y, totalsW);
        y += 4;
        double total = subtotal + tax;
        DrawText(cb, "Total:", boldFont, 12, totalsX, y + 14);
        string totalStr = $"${total:F2}";
        double tow = boldFont.GetWidthPoint(totalStr, 12);
        DrawText(cb, totalStr, boldFont, 12, totalsX + totalsW - tow, y + 14);
        y += 30;

        // ── Footer ──
        DrawHLine(cb, Margin, y, ContentWidth);
        y += 10;
        string footer = "Thank you for your business!";
        double fw = italicFont.GetWidthPoint(footer, 11);
        DrawText(cb, footer, italicFont, 11, Margin + (ContentWidth - fw) / 2, y + 12);

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
