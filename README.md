# DabblerPdf

A .NET port of [OpenPDF](https://github.com/LibrePDF/OpenPDF) (Java) providing text measurement and coordinate-based PDF generation. Targets .NET 10 with zero external dependencies.

## Purpose

This is a **scoped port** — not the entire OpenPDF library. It provides two capabilities for the Dabbler.Report reporting engine:

1. **Text measurement** — font metrics for layout composition
2. **PDF generation** — coordinate-based rendering of pre-positioned elements (text, lines, rectangles, images)

## Build & Test

```bash
dotnet build DabblerPdf.slnx
dotnet test tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj
```

## Consuming via Submodule

```bash
# Add as a git submodule
git submodule add https://github.com/darndestdabbler/dabbler-pdf.git lib/dabbler-pdf
```

Then add a project reference:

```xml
<ProjectReference Include="../lib/dabbler-pdf/src/DabblerPdf/DabblerPdf.csproj" />
```

## Quick Start

### Text Measurement

```csharp
using DabblerPdf.Text;

var font = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);
double width = font.GetWidthPoint("Hello World", 12.0);
double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
double lineHeight = ascent - descent;
```

### PDF Generation

```csharp
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;

using var stream = File.Create("report.pdf");
var document = new Document(PageSize.Letter);
var writer = PdfWriter.GetInstance(document, stream);
document.Open();
document.NewPage();

var cb = writer.DirectContent;

// Text
var font = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);
cb.BeginText();
cb.SetFontAndSize(font, 12);
cb.SetTextMatrix(72, 720);
cb.ShowText("Hello World");
cb.EndText();

// Rectangle with fill and stroke
cb.SetColorFill(new BaseColor(230, 230, 230));
cb.SetColorStroke(BaseColor.Black);
cb.Rectangle(72, 680, 468, 30);
cb.FillStroke();

// Line
cb.SetLineWidth(0.5);
cb.MoveTo(72, 675);
cb.LineTo(540, 675);
cb.Stroke();

document.Close();
```

### Images

```csharp
using DabblerPdf.Image;

// From file (JPEG or PNG)
var image = Image.GetInstance("logo.png");
image.ScaleToFit(144, 72);
image.SetAbsolutePosition(72, 600);
cb.AddImage(image);

// From byte array
var image = Image.GetInstance(jpegBytes);
image.SetAbsolutePosition(72, 500);
cb.AddImage(image);
```

### TrueType Fonts

```csharp
var font = BaseFont.CreateFont(@"C:\Windows\Fonts\arial.ttf", BaseFont.WinAnsiEncoding, true);
cb.SetFontAndSize(font, 14);
```

### Coordinate Translation

Converts from top-left-down inches (report layout) to bottom-left-up points (PDF):

```csharp
using DabblerPdf.Utilities;

var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(
    x: 1.0, y: 1.0, elementHeight: 0.25, pageHeightInches: 11.0);
// pdfX = 72.0, pdfY = 702.0

double points = CoordinateTranslator.InchesToPoints(1.0);  // 72.0
double inches = CoordinateTranslator.PointsToInches(72.0); // 1.0
```

## Built-in Fonts

Helvetica, Helvetica-Bold, Helvetica-Oblique, Helvetica-BoldOblique, Times-Roman, Times-Bold, Times-Italic, Times-BoldItalic, Courier, Courier-Bold, Courier-Oblique, Courier-BoldOblique, Symbol, ZapfDingbats

## License

LGPL-2.1-only AND MPL-2.0 (matching OpenPDF's dual license).

Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
