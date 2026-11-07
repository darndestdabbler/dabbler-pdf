// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

/// <summary>
/// Landscape multi-page report that mirrors the Orders By Retail Business
/// report in <c>dabbler-platform</c>: a reportHeader band with a prominent
/// title rendered on physical page 1 before any <c>NewPage()</c> call, a
/// pageHeader band with a gray-filled column-header row that repeats on
/// every page, grouped detail rows with light-blue group backgrounds, a
/// page footer with a "Page N of M" template, and a report footer on the
/// last page. The purpose is to give the 1.1.1 DirectContent multi-page
/// fix a realistic in-library reproduction so the title-on-page-1 claim
/// can be verified visually before 1.1.1 ships.
/// </summary>
public class S11_OrdersStyleReport : SampleBase
{
    public override string Name => "Orders-Style Report";
    public override string Description => "Landscape multi-page report mirroring the Orders By Retail Business pattern";
    public override string FileName => "11-orders-style-report.pdf";

    // Landscape 11x8.5 inches (PDF points).
    private const double PageW = 792;
    private const double PageH = 612;

    // Margins match the real RDF: 0.75" L/R, 0.5" T/B.
    private const double MarginL = 54;
    private const double MarginR = 54;
    private const double MarginT = 36;
    private const double MarginB = 36;
    private const double ContentW = PageW - MarginL - MarginR; // 684

    // Sample-specific literal. Deliberately not the real report's title so
    // the PdfPig assertions can pin to this string without overlapping the
    // production report's language.
    public const string TitleText = "Sample Orders by Region";
    private const string DateStampText = "Wednesday, April 22, 2026";

    // Band colors pulled from the real orders-by-customer.rdf.json. The
    // assertion in the companion test inspects the rg operator immediately
    // preceding the title's Tj and expects these normalized values (to three
    // decimal places), so the raw RGB triples are intentionally fixed.
    private static readonly BaseColor NavyTitle       = new(26, 26, 46);       // #1A1A2E
    private static readonly BaseColor GroupHeaderBg   = new(214, 228, 240);    // #D6E4F0
    private static readonly BaseColor ColumnHeaderBg  = new(240, 240, 240);    // #F0F0F0
    private static readonly BaseColor DarkText        = new(51, 51, 51);       // #333333
    private static readonly BaseColor MediumText      = new(102, 102, 102);    // #666666
    private static readonly BaseColor FooterText      = new(153, 153, 153);    // #999999
    private static readonly BaseColor ThickRule       = new(51, 51, 51);       // #333333
    private static readonly BaseColor ThinRule        = new(204, 204, 204);    // #CCCCCC
    private static readonly BaseColor HeaderRule      = new(153, 153, 153);    // #999999

    // Band heights (points).
    private const double ReportHeaderH = 72;  // 1.0" — title + date + rule
    private const double PageHeaderH = 22;    // column-header row
    private const double DetailRowH = 18;
    private const double GroupHeaderH = 22;
    private const double GroupFooterH = 22;
    private const double PageFooterH = 28;
    private const double ReportFooterH = 36;

    // Table columns (x measured from MarginL).
    private static readonly (string Header, double X, double Width, bool RightAlign)[] Columns =
    {
        ("Order #",    0,     72,  false),
        ("Order Date", 72,    130, false),
        ("Ship Date",  202,   130, false),
        ("Status",     332,   115, false),
        ("Items",      447,   94,  true),
        ("Total",      541,   143, true),
    };

    /// <summary>Convert top-down Y (from top of page) to PDF Y (from bottom).</summary>
    private static double PdfYLs(double topDownY) => PageH - topDownY;

    // Local text-drawing helpers — SampleBase's helpers assume portrait
    // Letter (PageHeight = 792), so they would mis-position text on this
    // 612-tall landscape page.
    private static new void DrawText(PdfContentByte cb, string text, BaseFont font, double size,
        double x, double topDownY, BaseColor color)
    {
        cb.SaveState();
        cb.BeginText();
        cb.SetFontAndSize(font, size);
        cb.SetColorFill(color);
        double baseline = PdfYLs(topDownY) - font.GetFontDescriptor(BaseFont.ASCENT, size);
        cb.SetTextMatrix(x, baseline);
        cb.ShowText(text);
        cb.EndText();
        cb.RestoreState();
    }

    private static void DrawFilledRect(PdfContentByte cb, double x, double topDownY,
        double width, double height, BaseColor fill)
    {
        cb.SaveState();
        cb.SetColorFill(fill);
        cb.Rectangle(x, PdfYLs(topDownY + height), width, height);
        cb.Fill();
        cb.RestoreState();
    }

    private static void DrawHRule(PdfContentByte cb, double x, double topDownY, double width,
        BaseColor stroke, double lineWidth = 0.5)
    {
        cb.SaveState();
        cb.SetColorStroke(stroke);
        cb.SetLineWidth(lineWidth);
        cb.SetLineDash(Array.Empty<double>(), 0);
        cb.MoveTo(x, PdfYLs(topDownY));
        cb.LineTo(x + width, PdfYLs(topDownY));
        cb.Stroke();
        cb.RestoreState();
    }

    private static double MeasureText(BaseFont font, string text, double size) =>
        font.GetWidthPoint(text, size);

    private readonly record struct OrderRow(int OrderId, DateTime OrderDate, DateTime ShipDate,
        string Status, int Items, double Total);

    private static List<(string Customer, List<OrderRow> Rows)> BuildSyntheticGroups()
    {
        var baseDate = new DateTime(2026, 4, 1);
        var statuses = new[] { "Shipped", "Pending", "Processing", "Delivered" };
        var groups = new (string Customer, int Count)[]
        {
            ("Acme Coffee - Seattle",    12),
            ("Bean & Brew Roasters",     13),
            ("Cascade Cafes Inc.",       11),
            ("Downtown Espresso Works",  12),
        };

        var result = new List<(string Customer, List<OrderRow> Rows)>();
        int orderId = 10_001;
        var rng = new Random(42);
        foreach (var (customer, count) in groups)
        {
            var rows = new List<OrderRow>(count);
            for (int i = 0; i < count; i++)
            {
                var orderDate = baseDate.AddDays(rng.Next(0, 21));
                var shipDate = orderDate.AddDays(rng.Next(1, 5));
                int items = rng.Next(1, 12);
                double total = Math.Round(items * (12.0 + rng.NextDouble() * 85.0), 2);
                rows.Add(new OrderRow(orderId++, orderDate, shipDate,
                    statuses[rng.Next(statuses.Length)], items, total));
            }
            result.Add((customer, rows));
        }
        return result;
    }

    /// <summary>
    /// Page event that paints the thin-rule + date + "Page N of &lt;template&gt;"
    /// footer on every physical page, using a PdfTemplate for the total page
    /// count so it is filled in after the last page is built.
    /// </summary>
    private sealed class PageFooterEvent : IPdfPageEvent
    {
        private readonly BaseFont _font;
        private readonly PdfTemplate _totalPagesTemplate;

        public PageFooterEvent(BaseFont font, PdfTemplate totalPagesTemplate)
        {
            _font = font;
            _totalPagesTemplate = totalPagesTemplate;
        }

        public void OnOpenDocument(PdfWriter writer, Document.Document document) { }
        public void OnStartPage(PdfWriter writer, Document.Document document) { }
        public void OnCloseDocument(PdfWriter writer, Document.Document document) { }

        public void OnEndPage(PdfWriter writer, Document.Document document)
        {
            var cb = writer.DirectContent;
            double footerTop = PageH - MarginB - PageFooterH;
            double ruleY = footerTop + 2;
            double textY = footerTop + 14;

            DrawHRule(cb, MarginL, ruleY, ContentW, ThinRule, 0.5);
            DrawText(cb, DateStampText, _font, 8, MarginL, textY, FooterText);

            string prefix = $"Page {document.PageNumber} of ";
            double prefixW = MeasureText(_font, prefix, 8);
            double groupW = prefixW + _totalPagesTemplate.Width;
            double startX = PageW - MarginR - groupW;

            DrawText(cb, prefix, _font, 8, startX, textY, FooterText);
            double baseline = PdfYLs(textY) - _font.GetFontDescriptor(BaseFont.ASCENT, 8);
            cb.AddTemplate(_totalPagesTemplate, startX + prefixW, baseline);
        }
    }

    protected override void GenerateCore(string outputDir)
    {
        using var ms = new MemoryStream();
        var doc = new Document.Document(new Rectangle(PageW, PageH));
        var writer = PdfWriter.GetInstance(doc, ms);

        // Stamp document metadata the same way the real PdfExporter does — the
        // Adobe Reader "Document Properties" dialog shows these, which was the
        // other half of the 1.1.1 user-visible surface.
        doc.AddTitle(TitleText);
        doc.AddAuthor("DabblerPdf Samples");
        doc.AddSubject("Orders-style multi-page sample for 1.1.1 verification");
        doc.AddCreator("DabblerPdf.Samples.S11_OrdersStyleReport");

        doc.Open();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);
        var italicFont = BaseFont.CreateFont(BaseFont.HelveticaOblique, BaseFont.WinAnsiEncoding, false);

        var totalPagesTemplate = writer.CreateTemplate(26, 10);
        writer.PageEvent = new PageFooterEvent(font, totalPagesTemplate);

        var groups = BuildSyntheticGroups();

        double contentBottomY = PageH - MarginB - PageFooterH;
        double y = MarginT;

        // --- reportHeader (page 1 only) ---
        // Drawn straight to DirectContent BEFORE any NewPage() call — this is
        // the exact composer-page-1 pattern that hit the 1.1.1 stacking bug.
        var cb = writer.DirectContent;

        double titleY = y + 10;
        DrawText(cb, TitleText, boldFont, 18, MarginL, titleY, NavyTitle);

        string dateStr = DateStampText;
        double dateW = MeasureText(italicFont, dateStr, 9);
        DrawText(cb, dateStr, italicFont, 9, PageW - MarginR - dateW, titleY + 20, MediumText);

        DrawHRule(cb, MarginL, y + ReportHeaderH - 6, ContentW, ThickRule, 1.0);
        y += ReportHeaderH;

        // --- pageHeader for page 1 ---
        DrawPageHeader(cb, boldFont, y);
        y += PageHeaderH;

        int grandCount = 0;
        double grandTotal = 0;

        foreach (var (customerName, rows) in groups)
        {
            if (y + GroupHeaderH + DetailRowH > contentBottomY)
            {
                doc.NewPage();
                y = MarginT;
                cb = writer.DirectContent;
                DrawPageHeader(cb, boldFont, y);
                y += PageHeaderH;
            }

            DrawGroupHeader(cb, boldFont, customerName, y);
            y += GroupHeaderH;

            double groupSum = 0;
            int groupCount = 0;
            foreach (var row in rows)
            {
                if (y + DetailRowH > contentBottomY)
                {
                    doc.NewPage();
                    y = MarginT;
                    cb = writer.DirectContent;
                    DrawPageHeader(cb, boldFont, y);
                    y += PageHeaderH;
                }
                DrawDetailRow(cb, font, row, y);
                y += DetailRowH;
                groupSum += row.Total;
                groupCount++;
            }

            if (y + GroupFooterH > contentBottomY)
            {
                doc.NewPage();
                y = MarginT;
                cb = writer.DirectContent;
                DrawPageHeader(cb, boldFont, y);
                y += PageHeaderH;
            }
            DrawGroupFooter(cb, font, italicFont, boldFont, customerName, groupCount, groupSum, y);
            y += GroupFooterH;

            grandCount += groupCount;
            grandTotal += groupSum;
        }

        if (y + ReportFooterH > contentBottomY)
        {
            doc.NewPage();
            y = MarginT;
            cb = writer.DirectContent;
            DrawPageHeader(cb, boldFont, y);
            y += PageHeaderH;
        }
        DrawReportFooter(cb, boldFont, grandCount, grandTotal, y);

        // Fill the total-pages template now: doc.PageNumber is the final
        // physical-page count, because the last page's content is already
        // committed and doc.Close will only flush it (no extra page).
        totalPagesTemplate.Content.BeginText();
        totalPagesTemplate.Content.SetFontAndSize(font, 8);
        totalPagesTemplate.Content.SetColorFill(FooterText);
        totalPagesTemplate.Content.SetTextMatrix(0, 0);
        totalPagesTemplate.Content.ShowText(doc.PageNumber.ToString());
        totalPagesTemplate.Content.EndText();

        doc.Close();
        File.WriteAllBytes(OutputPath(outputDir), ms.ToArray());
    }

    private static void DrawPageHeader(PdfContentByte cb, BaseFont boldFont, double topY)
    {
        DrawFilledRect(cb, MarginL, topY, ContentW, PageHeaderH, ColumnHeaderBg);
        foreach (var col in Columns)
        {
            double cellX = MarginL + col.X;
            string text = col.Header;
            double textW = MeasureText(boldFont, text, 8);
            double tx = col.RightAlign
                ? cellX + col.Width - textW - 4
                : cellX + 4;
            DrawText(cb, text, boldFont, 8, tx, topY + 6, DarkText);
        }
        DrawHRule(cb, MarginL, topY + PageHeaderH, ContentW, HeaderRule, 0.5);
    }

    private static void DrawGroupHeader(PdfContentByte cb, BaseFont boldFont, string customer, double topY)
    {
        DrawFilledRect(cb, MarginL, topY, ContentW, GroupHeaderH, GroupHeaderBg);
        DrawText(cb, customer, boldFont, 10, MarginL + 6, topY + 6, NavyTitle);
    }

    private static void DrawDetailRow(PdfContentByte cb, BaseFont font, OrderRow row, double topY)
    {
        string[] cells =
        {
            row.OrderId.ToString(),
            row.OrderDate.ToString("d"),
            row.ShipDate.ToString("d"),
            row.Status,
            row.Items.ToString(),
            $"${row.Total:F2}",
        };
        for (int i = 0; i < Columns.Length; i++)
        {
            double cellX = MarginL + Columns[i].X;
            double cellW = Columns[i].Width;
            string text = cells[i];
            double textW = MeasureText(font, text, 9);
            double tx = Columns[i].RightAlign
                ? cellX + cellW - textW - 4
                : cellX + 4;
            DrawText(cb, text, font, 9, tx, topY + 4, DarkText);
        }
    }

    private static void DrawGroupFooter(PdfContentByte cb, BaseFont font, BaseFont italicFont,
        BaseFont boldFont, string customer, int count, double sum, double topY)
    {
        // Thin subtotal rule above the footer line.
        double ruleX = MarginL + Columns[4].X;
        double ruleW = ContentW - Columns[4].X;
        DrawHRule(cb, ruleX, topY, ruleW, MediumText, 0.5);

        string label = $"Subtotal for {customer}";
        DrawText(cb, label, italicFont, 9, MarginL + 6, topY + 6, DarkText);

        string countStr = count.ToString("N0");
        double countW = MeasureText(font, countStr, 9);
        double countCellX = MarginL + Columns[4].X;
        double countCellW = Columns[4].Width;
        DrawText(cb, countStr, font, 9, countCellX + countCellW - countW - 4, topY + 6, MediumText);

        string totalStr = $"${sum:F2}";
        double totalW = MeasureText(boldFont, totalStr, 9);
        double totalCellX = MarginL + Columns[5].X;
        double totalCellW = Columns[5].Width;
        DrawText(cb, totalStr, boldFont, 9, totalCellX + totalCellW - totalW - 4, topY + 6, DarkText);
    }

    private static void DrawReportFooter(PdfContentByte cb, BaseFont boldFont, int count, double sum, double topY)
    {
        DrawHRule(cb, MarginL, topY, ContentW, ThickRule, 1.0);

        DrawText(cb, "Grand Total", boldFont, 12, MarginL, topY + 14, NavyTitle);

        string countStr = count.ToString("N0");
        double countW = MeasureText(boldFont, countStr, 10);
        double countCellX = MarginL + Columns[4].X;
        double countCellW = Columns[4].Width;
        DrawText(cb, countStr, boldFont, 10, countCellX + countCellW - countW - 4, topY + 14, DarkText);

        string totalStr = $"${sum:F2}";
        double totalW = MeasureText(boldFont, totalStr, 12);
        double totalCellX = MarginL + Columns[5].X;
        double totalCellW = Columns[5].Width;
        DrawText(cb, totalStr, boldFont, 12, totalCellX + totalCellW - totalW - 4, topY + 14, NavyTitle);
    }
}
