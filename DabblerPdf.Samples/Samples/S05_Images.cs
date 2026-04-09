using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S05_Images : SampleBase
{
    public override string Name => "Images";
    public override string Description => "Embedded images, scale modes";
    public override string FileName => "05-images.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        var font = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var boldFont = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "sample-logo.png");

        double y = Margin;

        // ── Title ──
        DrawText(cb, "S05: Images", boldFont, 18, Margin, y);
        y += 30;

        // ── Section 1: Scale Modes (2" x 2" element) ──
        y = DrawSectionTitle(cb, "1. Scale Modes (2\" x 2\" element)", boldFont, y);

        double elemSize = 144; // 2 inches
        double gap = 20;

        string[] modeLabels = ["Stretch", "ScaleToFit", "ActualSize"];

        for (int i = 0; i < 3; i++)
        {
            double ex = Margin + i * (elemSize + gap);

            // Draw dashed border for the element boundary
            cb.SaveState();
            cb.SetColorStroke(new BaseColor(180, 180, 180));
            cb.SetLineWidth(0.5);
            cb.SetLineDash([4, 2], 0);
            DrawRectOutline(cb, ex, y, elemSize, elemSize);
            cb.RestoreState();

            // Draw the image
            var image = Image.Image.GetInstance(logoPath);

            switch (i)
            {
                case 0: // Stretch — fill the full element
                    image.ScaleAbsolute(elemSize, elemSize);
                    image.SetAbsolutePosition(ex, PdfY(y + elemSize));
                    cb.AddImage(image);
                    break;

                case 1: // ScaleToFit — maintain aspect ratio
                    image.ScaleToFit(elemSize, elemSize);
                    // Center within element
                    double offsetX = (elemSize - image.ScaledWidth) / 2;
                    double offsetY = (elemSize - image.ScaledHeight) / 2;
                    image.SetAbsolutePosition(ex + offsetX, PdfY(y + elemSize) + offsetY);
                    cb.AddImage(image);
                    break;

                case 2: // ActualSize — native pixel dimensions at top-left
                    image.SetAbsolutePosition(ex, PdfY(y) - image.ScaledHeight);
                    cb.AddImage(image);
                    break;
            }

            // Label below
            DrawLabel(cb, modeLabels[i], font, ex + 4, y + elemSize + 4);
        }

        y += elemSize + 24;

        // ── Section 2: Different element sizes with ScaleToFit ──
        y = DrawSectionTitle(cb, "2. Different Element Sizes (ScaleToFit)", boldFont, y);

        var elementSizes = new (double W, double H, string Label)[]
        {
            (72, 72, "1\" x 1\""),
            (144, 72, "2\" x 1\""),
            (72, 144, "1\" x 2\""),
        };

        double sx = Margin;
        double maxH = 0;
        foreach (var (w, h, label) in elementSizes)
        {
            // Dashed border
            cb.SaveState();
            cb.SetColorStroke(new BaseColor(180, 180, 180));
            cb.SetLineWidth(0.5);
            cb.SetLineDash([4, 2], 0);
            DrawRectOutline(cb, sx, y, w, h);
            cb.RestoreState();

            // Image scaled to fit
            var image = Image.Image.GetInstance(logoPath);
            image.ScaleToFit(w, h);
            double ox = (w - image.ScaledWidth) / 2;
            double oy = (h - image.ScaledHeight) / 2;
            image.SetAbsolutePosition(sx + ox, PdfY(y + h) + oy);
            cb.AddImage(image);

            DrawLabel(cb, label, font, sx, y + h + 4);
            maxH = Math.Max(maxH, h);
            sx += w + gap;
        }

        y += maxH + 24;

        // ── Section 3: Missing image placeholder ──
        y = DrawSectionTitle(cb, "3. Missing Image Handling", boldFont, y);

        try
        {
            var missing = Image.Image.GetInstance("nonexistent-file.png");
            missing.SetAbsolutePosition(Margin, PdfY(y + 50));
            cb.AddImage(missing);
        }
        catch (Exception ex)
        {
            // Draw a placeholder rectangle with error text
            DrawRectFilledAndStroked(cb, Margin, y, 144, 72,
                new BaseColor(240, 240, 240), new BaseColor(200, 200, 200));
            DrawText(cb, "[Missing Image]", font, 10, Margin + 20, y + 30);
            DrawText(cb, $"Error: {ex.GetType().Name}", font, 8, Margin + 4, y + 44,
                new BaseColor(180, 0, 0));
        }

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
