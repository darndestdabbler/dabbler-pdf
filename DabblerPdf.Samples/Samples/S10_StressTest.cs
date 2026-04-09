using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S10_StressTest : SampleBase
{
    public override string Name => "Stress Test";
    public override string Description => "Large document, many pages, performance";
    public override string FileName => "10-stress-test.pdf";

    private const int TotalRows = 1000;
    private const double HeaderHeight = 30;
    private const double FooterHeight = 24;
    private const double RowHeight = 13;

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        // Fixed-seed RNG for reproducibility
        var rng = new Random(42);

        string[] firstNames = ["Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Hank", "Iris", "Jack",
            "Karen", "Leo", "Mona", "Nick", "Olive", "Paul", "Quinn", "Rita", "Sam", "Tina"];
        string[] lastNames = ["Smith", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor", "Anderson", "Thomas"];

        // Calculate rows per page
        double contentTop = Margin + HeaderHeight;
        double contentBottom = PageHeight - Margin - FooterHeight;
        int rowsPerPage = (int)((contentBottom - contentTop) / RowHeight);
        int totalPages = (int)Math.Ceiling((double)TotalRows / rowsPerPage);

        // Column positions
        double colRow = Margin;
        double colName = Margin + 45;
        double colAmount = Margin + ContentWidth - 120;
        double colRunning = Margin + ContentWidth - 50;

        int rowIndex = 0;
        double runningTotal = 0;

        for (int page = 1; page <= totalPages; page++)
        {
            doc.NewPage();
            var cb = writer.DirectContent;

            // ── Page Header ──
            DrawRectFilled(cb, Margin, Margin, ContentWidth, HeaderHeight - 4, new BaseColor(230, 230, 230));
            DrawText(cb, "DabblerPdf Stress Test \u2014 1,000 Rows", boldFont, 9, Margin + 4, Margin + 12);
            string pageInfo = $"Page {page} of {totalPages}";
            double piw = font.GetWidthPoint(pageInfo, 8);
            DrawText(cb, pageInfo, font, 8, Margin + ContentWidth - piw - 4, Margin + 12);

            // Column headers
            double hy = Margin + HeaderHeight;
            cb.SetColorStroke(BaseColor.Black);
            cb.SetLineWidth(0.5);
            cb.SetLineDash([], 0);
            DrawHLine(cb, Margin, hy, ContentWidth);

            DrawText(cb, "#", boldFont, 8, colRow, hy + 10);
            DrawText(cb, "Name", boldFont, 8, colName, hy + 10);
            DrawText(cb, "Amount", boldFont, 8, colAmount, hy + 10);
            DrawText(cb, "Running", boldFont, 8, colRunning, hy + 10);

            double y = hy + RowHeight + 2;

            // ── Data Rows ──
            var altBg = new BaseColor(245, 245, 245);
            int rowsThisPage = Math.Min(rowsPerPage, TotalRows - rowIndex);

            for (int r = 0; r < rowsThisPage; r++)
            {
                if (r % 2 == 1)
                    DrawRectFilled(cb, Margin, y - 2, ContentWidth, RowHeight, altBg);

                string name = $"{firstNames[rng.Next(firstNames.Length)]} {lastNames[rng.Next(lastNames.Length)]}";
                double amount = Math.Round(rng.NextDouble() * 999.99 + 0.01, 2);
                runningTotal += amount;

                DrawText(cb, (rowIndex + 1).ToString(), font, 7, colRow, y + 9);
                DrawText(cb, name, font, 7, colName, y + 9);

                string amountStr = $"${amount:F2}";
                double aw = font.GetWidthPoint(amountStr, 7);
                DrawText(cb, amountStr, font, 7, colAmount + 60 - aw, y + 9);

                string runStr = $"${runningTotal:F2}";
                double rw = font.GetWidthPoint(runStr, 7);
                DrawText(cb, runStr, font, 7, colRunning + 50 - rw, y + 9);

                y += RowHeight;
                rowIndex++;
            }

            // ── Page Footer ──
            double footerY = PageHeight - Margin - FooterHeight;
            cb.SetColorStroke(new BaseColor(180, 180, 180));
            cb.SetLineWidth(0.5);
            DrawHLine(cb, Margin, footerY, ContentWidth);
            string footerText = $"Page {page} of {totalPages} | Rows {rowIndex - rowsThisPage + 1}\u2013{rowIndex} of {TotalRows}";
            double ftw = font.GetWidthPoint(footerText, 7);
            DrawText(cb, footerText, font, 7, Margin + (ContentWidth - ftw) / 2, footerY + 10);
        }

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
