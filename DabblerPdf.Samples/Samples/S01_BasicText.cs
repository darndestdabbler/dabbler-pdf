using DabblerPdf.Document;
using DabblerPdf.Text;

namespace DabblerPdf.Samples.Samples;

public class S01_BasicText : SampleBase
{
    public override string Name => "Basic Text";
    public override string Description => "Font families, sizes, styles, and special characters";
    public override string FileName => "01-basic-text.pdf";

    protected override void GenerateCore(string outputDir)
    {
        var (doc, writer, ms) = CreateDocument();
        doc.NewPage();
        var cb = writer.DirectContent;

        // Fonts
        var helvetica = BaseFont.CreateFont(BaseFont.Helvetica, BaseFont.WinAnsiEncoding, false);
        var helveticaBold = BaseFont.CreateFont(BaseFont.HelveticaBold, BaseFont.WinAnsiEncoding, false);
        var helveticaItalic = BaseFont.CreateFont(BaseFont.HelveticaOblique, BaseFont.WinAnsiEncoding, false);
        var helveticaBoldItalic = BaseFont.CreateFont(BaseFont.HelveticaBoldOblique, BaseFont.WinAnsiEncoding, false);

        var times = BaseFont.CreateFont(BaseFont.TimesRoman, BaseFont.WinAnsiEncoding, false);
        var timesBold = BaseFont.CreateFont(BaseFont.TimesBold, BaseFont.WinAnsiEncoding, false);
        var timesItalic = BaseFont.CreateFont(BaseFont.TimesItalic, BaseFont.WinAnsiEncoding, false);
        var timesBoldItalic = BaseFont.CreateFont(BaseFont.TimesBoldItalic, BaseFont.WinAnsiEncoding, false);

        var courier = BaseFont.CreateFont(BaseFont.Courier, BaseFont.WinAnsiEncoding, false);
        var courierBold = BaseFont.CreateFont(BaseFont.CourierBold, BaseFont.WinAnsiEncoding, false);
        var courierItalic = BaseFont.CreateFont(BaseFont.CourierOblique, BaseFont.WinAnsiEncoding, false);
        var courierBoldItalic = BaseFont.CreateFont(BaseFont.CourierBoldOblique, BaseFont.WinAnsiEncoding, false);

        const string pangram = "The quick brown fox jumps over the lazy dog.";
        double y = Margin;

        // ── Title ──
        DrawText(cb, "S01: Basic Text \u2014 Type Specimen Sheet", helveticaBold, 18, Margin, y);
        y += 30;

        // ── Section 1: Font Families ──
        y = DrawSectionTitle(cb, "1. Font Families", helveticaBold, y);

        DrawLabel(cb, "Helvetica:", helvetica, Margin, y);
        y += 14;
        DrawText(cb, pangram, helvetica, 12, Margin, y);
        y += 18;

        DrawLabel(cb, "Times Roman:", helvetica, Margin, y);
        y += 14;
        DrawText(cb, pangram, times, 12, Margin, y);
        y += 18;

        DrawLabel(cb, "Courier:", helvetica, Margin, y);
        y += 14;
        DrawText(cb, pangram, courier, 12, Margin, y);
        y += 28;

        // ── Section 2: Font Styles (3x4 grid) ──
        y = DrawSectionTitle(cb, "2. Font Styles", helveticaBold, y);

        var families = new (string Name, BaseFont Regular, BaseFont Bold, BaseFont Italic, BaseFont BoldItalic)[]
        {
            ("Helvetica", helvetica, helveticaBold, helveticaItalic, helveticaBoldItalic),
            ("Times", times, timesBold, timesItalic, timesBoldItalic),
            ("Courier", courier, courierBold, courierItalic, courierBoldItalic),
        };

        foreach (var (familyName, regular, bold, italic, boldItalic) in families)
        {
            DrawLabel(cb, $"{familyName}:", helvetica, Margin, y);
            y += 14;

            DrawText(cb, $"Regular: {pangram}", regular, 10, Margin + 10, y);
            y += 14;
            DrawText(cb, $"Bold: {pangram}", bold, 10, Margin + 10, y);
            y += 14;
            DrawText(cb, $"Italic: {pangram}", italic, 10, Margin + 10, y);
            y += 14;
            DrawText(cb, $"Bold Italic: {pangram}", boldItalic, 10, Margin + 10, y);
            y += 20;
        }

        y += 8;

        // ── Section 3: Font Sizes ──
        y = DrawSectionTitle(cb, "3. Font Sizes", helveticaBold, y);

        double[] sizes = [8, 10, 12, 14, 18, 24, 36];
        foreach (var size in sizes)
        {
            DrawText(cb, $"{size}pt: The quick brown fox", helvetica, size, Margin, y);
            y += size + 6;
        }

        y += 8;

        // ── Section 4: Special Characters ──
        y = DrawSectionTitle(cb, "4. Special Characters (WinAnsi Encoding)", helveticaBold, y);

        DrawText(cb, "Ampersand: & | Angle brackets: < > | Quotes: \"double\" and 'single'", helvetica, 11, Margin, y);
        y += 16;
        DrawText(cb, "Em dash: \u2014 | Accented: \u00e9 \u00f1 \u00fc | Currency: \u20ac", helvetica, 11, Margin, y);
        y += 20;

        // Encoding limits note — demonstrates CanEncode
        DrawLabel(cb, "Encoding support (BaseFont.CanEncode):", helvetica, Margin, y);
        y += 14;

        var testChars = new (char Ch, string Name)[]
        {
            ('A', "A (ASCII)"),
            ('\u00e9', "\u00e9 (accented)"),
            ('\u20AC', "\u20ac (Euro)"),
            ('\u2014', "\u2014 (em dash)"),
        };

        double xPos = Margin;
        foreach (var (ch, name) in testChars)
        {
            bool supported = helvetica.CanEncode(ch);
            string label = $"{name}: {(supported ? "Yes" : "No")}";
            DrawText(cb, label, helvetica, 9, xPos, y);
            xPos += 120;
        }

        y += 16;
        DrawText(cb, "Note: WinAnsi (Cp1252) covers ASCII + Western European. CJK and emoji are not supported.",
            helvetica, 8, Margin, y, new BaseColor(128, 128, 128));

        SaveAndClose(doc, ms, OutputPath(outputDir));
    }
}
