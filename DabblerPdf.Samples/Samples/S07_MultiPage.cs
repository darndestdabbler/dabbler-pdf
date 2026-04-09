using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S07_MultiPage : SampleBase
{
    public override string Name => "Multi-Page";
    public override string Description => "Pagination, headers/footers via page events, page numbers with PdfTemplate";
    public override string FileName => "07-multi-page.pdf";

    private const int TotalRows = 50;
    private const double HeaderHeight = 36;
    private const double FooterHeight = 30;
    private const double RowHeight = 14;

    /// <summary>
    /// Page event handler that draws headers and footers on each page.
    /// Uses a PdfTemplate for "Page X of Y" so the total page count
    /// is filled in after all pages are generated.
    /// </summary>
    private class HeaderFooterPageEvent : IPdfPageEvent
    {
        private readonly BaseFont _boldFont;
        private readonly BaseFont _font;
        private readonly PdfTemplate _totalPagesTemplate;

        public HeaderFooterPageEvent(BaseFont boldFont, BaseFont font, PdfTemplate totalPagesTemplate)
        {
            _boldFont = boldFont;
            _font = font;
            _totalPagesTemplate = totalPagesTemplate;
        }

        public void OnStartPage(PdfWriter writer, Document.Document document)
        {
            var cb = writer.DirectContent;
            double y = Margin;

            // Header background
            DrawRectFilled(cb, Margin, y, ContentWidth, HeaderHeight - 4, new BaseColor(240, 240, 240));

            // Left: title
            DrawText(cb, "DabblerPdf Multi-Page Sample", _boldFont, 10, Margin + 4, y + 14);

            // Right: date
            string date = "March 15, 2025";
            double dateWidth = _font.GetWidthPoint(date, 9);
            DrawText(cb, date, _font, 9, Margin + ContentWidth - dateWidth - 4, y + 14);

            // Header underline
            cb.SetColorStroke(BaseColor.Black);
            cb.SetLineWidth(1);
            cb.SetLineDash([], 0);
            DrawHLine(cb, Margin, y + HeaderHeight - 4, ContentWidth);
        }

        public void OnEndPage(PdfWriter writer, Document.Document document)
        {
            var cb = writer.DirectContent;
            double y = PageHeight - Margin;

            // Footer line
            cb.SetColorStroke(BaseColor.Black);
            cb.SetLineWidth(0.5);
            cb.SetLineDash([], 0);
            DrawHLine(cb, Margin, y - FooterHeight + 4, ContentWidth);

            // "Page N of " text followed by the template for the total
            string pagePrefix = $"Page {document.PageNumber} of ";
            double prefixWidth = _font.GetWidthPoint(pagePrefix, 9);
            double totalWidth = prefixWidth + _totalPagesTemplate.Width;
            double startX = Margin + (ContentWidth - totalWidth) / 2;

            // Draw "Page N of "
            DrawText(cb, pagePrefix, _font, 9, startX, y - 10);

            // Place the template for the total page count
            cb.AddTemplate(_totalPagesTemplate, startX + prefixWidth, PdfY(y - 10));
        }
    }

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        // Create a template for the total page count — content filled in after all pages
        var totalPagesTemplate = writer.CreateTemplate(30, 12);

        // Calculate pagination layout
        double contentTop = Margin + HeaderHeight;
        double contentBottom = PageHeight - Margin - FooterHeight;
        double availableHeight = contentBottom - contentTop;

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

        // Set up the page event handler with the template
        var pageEvent = new HeaderFooterPageEvent(boldFont, font, totalPagesTemplate);
        writer.PageEvent = pageEvent;

        // Generate pages — headers and footers are handled by the page event
        int rowIndex = 0;
        for (int page = 1; page <= totalPages; page++)
        {
            doc.NewPage();
            var cb = writer.DirectContent;

            double y = Margin + HeaderHeight;

            // Report header (page 1 only)
            if (page == 1)
            {
                DrawText(cb, "Multi-Page Document Sample", boldFont, 20, Margin, y + 20);
                y += reportHeaderHeight;
            }

            // Detail rows
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

            // Report footer (last page only)
            if (rowIndex >= TotalRows)
            {
                y += 10;
                cb.SetColorStroke(BaseColor.Black);
                cb.SetLineWidth(1);
                DrawHLine(cb, Margin, y, ContentWidth);
                y += 6;
                DrawText(cb, $"End of Report \u2014 {TotalRows} rows printed", boldFont, 10, Margin, y + 10);
            }
        }

        // Fill in the total page count template AFTER all pages are generated
        totalPagesTemplate.Content.BeginText();
        totalPagesTemplate.Content.SetFontAndSize(font, 9);
        totalPagesTemplate.Content.SetTextMatrix(0, 0);
        totalPagesTemplate.Content.ShowText(doc.PageNumber.ToString());
        totalPagesTemplate.Content.EndText();

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
