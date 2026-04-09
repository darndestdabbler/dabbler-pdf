using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S07_MultiPage : SampleBase
{
    public override string Name => "Multi-Page";
    public override string Description => "Pagination, headers/footers, page numbers";
    public override string FileName => "07-multi-page.pdf";

    private const int TotalRows = 50;
    private const double HeaderHeight = 36;
    private const double FooterHeight = 30;
    private const double RowHeight = 14;

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        // We need to know total pages upfront for "Page X of Y"
        // Calculate: how many rows fit per page?
        double contentTop = Margin + HeaderHeight;
        double contentBottom = PageHeight - Margin - FooterHeight;
        double availableHeight = contentBottom - contentTop;

        // Page 1 has extra report header (30pt)
        double reportHeaderHeight = 30;
        double firstPageRows = Math.Floor((availableHeight - reportHeaderHeight) / RowHeight);
        double otherPageRows = Math.Floor(availableHeight / RowHeight);

        int rowsRemaining = TotalRows;
        int totalPages = 1;
        rowsRemaining -= (int)firstPageRows;
        while (rowsRemaining > 0)
        {
            totalPages++;
            rowsRemaining -= (int)otherPageRows;
        }

        // Now generate pages
        int rowIndex = 0;
        for (int page = 1; page <= totalPages; page++)
        {
            doc.NewPage();
            var cb = writer.DirectContent;

            // ── Page Header ──
            DrawPageHeader(cb, boldFont, font, page);

            double y = Margin + HeaderHeight;

            // ── Report Header (page 1 only) ──
            if (page == 1)
            {
                DrawText(cb, "Multi-Page Document Sample", boldFont, 20, Margin, y + 20);
                y += reportHeaderHeight;
            }

            // ── Detail Rows ──
            double maxRows = page == 1 ? firstPageRows : otherPageRows;
            int rowsThisPage = 0;

            while (rowIndex < TotalRows && rowsThisPage < maxRows)
            {
                DrawText(cb, $"Row {rowIndex + 1}: This is detail row content for testing multi-page pagination and row flow across page boundaries.",
                    font, 9, Margin, y + 10);
                y += RowHeight;
                rowIndex++;
                rowsThisPage++;
            }

            // ── Report Footer (last page only) ──
            if (rowIndex >= TotalRows)
            {
                y += 10;
                cb.SetColorStroke(BaseColor.Black);
                cb.SetLineWidth(1);
                DrawHLine(cb, Margin, y, ContentWidth);
                y += 6;
                DrawText(cb, $"End of Report \u2014 {TotalRows} rows printed", boldFont, 10, Margin, y + 10);
            }

            // ── Page Footer ──
            DrawPageFooter(cb, font, page, totalPages);
        }

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }

    private static void DrawPageHeader(PdfContentByte cb, BaseFont boldFont, BaseFont font, int page)
    {
        double y = Margin;

        // Header background
        DrawRectFilled(cb, Margin, y, ContentWidth, HeaderHeight - 4, new BaseColor(240, 240, 240));

        // Left: title
        DrawText(cb, "DabblerPdf Multi-Page Sample", boldFont, 10, Margin + 4, y + 14);

        // Right: date
        string date = "March 15, 2025";
        double dateWidth = font.GetWidthPoint(date, 9);
        DrawText(cb, date, font, 9, Margin + ContentWidth - dateWidth - 4, y + 14);

        // Header underline
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(1);
        cb.SetLineDash([], 0);
        DrawHLine(cb, Margin, y + HeaderHeight - 4, ContentWidth);
    }

    private static void DrawPageFooter(PdfContentByte cb, BaseFont font, int page, int totalPages)
    {
        double y = PageHeight - Margin;

        // Footer line
        cb.SetColorStroke(BaseColor.Black);
        cb.SetLineWidth(0.5);
        cb.SetLineDash([], 0);
        DrawHLine(cb, Margin, y - FooterHeight + 4, ContentWidth);

        // Centered page number
        string pageText = $"Page {page} of {totalPages}";
        double textWidth = font.GetWidthPoint(pageText, 9);
        DrawText(cb, pageText, font, 9, Margin + (ContentWidth - textWidth) / 2, y - 10);
    }
}
