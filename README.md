# DabblerPdf

[![License: LGPL-2.1/MPL-2.0](https://img.shields.io/badge/License-LGPL--2.1%20%2F%20MPL--2.0-blue.svg)](LICENSE)

A lightweight .NET library for generating PDF documents. Ported from
[OpenPDF](https://github.com/LibrePDF/OpenPDF), DabblerPdf provides a clean, code-first API
for creating pages with text, images, lines, rectangles, and styled content — no heavy
dependencies, no design surface, just code-to-PDF.

## Features

- **Text rendering** — Helvetica, Times New Roman, and Courier font families with regular, bold, italic, and bold-italic styles (PDF Base 14 fonts, no embedding required)
- **TrueType font support** — load `.ttf` files for custom font rendering
- **Text wrapping** — word-level and character-level line breaking with left, right, and center alignment
- **Text measurement** — font metrics (width, ascent, descent) for layout composition
- **Images** — JPEG and PNG support with Stretch, ScaleToFit, and ActualSize scaling modes
- **Lines and rectangles** — horizontal/vertical lines, filled/stroked rectangles, solid/dashed/dotted borders
- **Colors** — full hex color support (#RRGGBB) and named color constants
- **Padding** — uniform and per-side (top, bottom, left, right)
- **Multi-page documents** — page headers, footers, and "Page X of Y" numbering with backfill
- **Document metadata** — set Title, Author, Subject, Keywords, Creator, and CreationDate in the PDF `/Info` dictionary
- **Coordinate translation** — convert between top-left-down inches (report layout) and bottom-left-up points (PDF)

## Installation

### NuGet Package

```bash
dotnet add package DabblerPdf
```

### Git Submodule

```bash
git submodule add https://github.com/darndestdabbler/dabbler-pdf.git lib/dabbler-pdf
```

Then add a project reference:

```xml
<ProjectReference Include="../lib/dabbler-pdf/src/DabblerPdf/DabblerPdf.csproj" />
```

## Quick Start

### Create a Simple PDF

```csharp
using DabblerPdf.Document;
using DabblerPdf.Pdf;
using DabblerPdf.Text;

using var stream = File.Create("hello.pdf");
var document = new Document(PageSize.Letter);
var writer = PdfWriter.GetInstance(document, stream);
document.Open();
document.NewPage();

var cb = writer.DirectContent;
var font = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);

cb.BeginText();
cb.SetFontAndSize(font, 24);
cb.SetTextMatrix(72, 700);
cb.ShowText("Hello, PDF!");
cb.EndText();

document.Close();
```

### Text with Styling

```csharp
var regular = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);
var bold = BaseFont.CreateFont("Helvetica-Bold", BaseFont.WinAnsiEncoding, false);

cb.BeginText();

// Title in bold
cb.SetFontAndSize(bold, 18);
cb.SetTextMatrix(72, 720);
cb.ShowText("Quarterly Report");

// Body text
cb.SetFontAndSize(regular, 12);
cb.SetTextMatrix(72, 700);
cb.ShowText("Revenue increased 15% year-over-year.");

cb.EndText();
```

### Lines and Rectangles

```csharp
// Filled rectangle with border
cb.SetColorFill(new BaseColor(230, 230, 230));
cb.SetColorStroke(BaseColor.Black);
cb.Rectangle(72, 650, 468, 30);
cb.FillStroke();

// Horizontal rule
cb.SetLineWidth(0.5);
cb.MoveTo(72, 645);
cb.LineTo(540, 645);
cb.Stroke();
```

### Images

```csharp
using DabblerPdf.Image;

var logo = Image.GetInstance("logo.png");
logo.ScaleToFit(144, 72);
logo.SetAbsolutePosition(72, 600);
cb.AddImage(logo);
```

### Text Measurement

```csharp
var font = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);
double width = font.GetWidthPoint("Hello World", 12.0);
double ascent = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
double descent = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
```

### Text Wrapping

```csharp
using DabblerPdf.Text;

var lines = TextLayout.WrapText("Long paragraph text...", font, 12.0, maxWidth: 468);
```

### Coordinate Translation

Convert from top-left-down inches (common in report layouts) to PDF's bottom-left-up points:

```csharp
using DabblerPdf.Utilities;

var (pdfX, pdfY) = CoordinateTranslator.FromTopLeftInches(
    x: 1.0, y: 1.0, elementHeight: 0.25, pageHeightInches: 11.0);
// pdfX = 72.0, pdfY = 702.0
```

### Document Metadata

Stage values for the PDF document information dictionary (`/Info`).
Adobe Reader, browsers, and indexers see these fields:

```csharp
var document = new Document(PageSize.Letter);
PdfWriter.GetInstance(document, stream);

document.AddTitle("Quarterly Report");
document.AddAuthor("AcmeCoffeeDistributors");
document.AddSubject("Q1 2026 results");
document.AddKeywords("orders, report, sales");
document.AddCreator("AcmeCoffeeOnline");
// CreationDate is auto-stamped at Close().
// Use document.SuppressCreationDate() to opt out.

document.Open();
// ... draw content ...
document.Close();
```

## API Overview

| Namespace | Key Types | Purpose |
|-----------|-----------|---------|
| `DabblerPdf.Document` | `Document`, `PageSize`, `Rectangle`, `BaseColor` | Document lifecycle, page sizes, colors |
| `DabblerPdf.Pdf` | `PdfWriter`, `PdfContentByte`, `PdfTemplate`, `IPdfPageEvent` | PDF writing, drawing API, page events |
| `DabblerPdf.Text` | `BaseFont`, `TextLayout` | Font metrics, text measurement, word wrapping |
| `DabblerPdf.Image` | `Image`, `Jpeg` | JPEG and PNG image embedding |
| `DabblerPdf.Utilities` | `CoordinateTranslator`, `PdfDate` | Inch/point coordinate conversion, PDF 1.7 §7.9.4 date formatting |

All public types and methods include XML documentation comments visible in IntelliSense.

## Font Support

DabblerPdf includes the 14 standard PDF fonts (Base 14). These are built into every PDF viewer — no font embedding required:

| Family | Regular | Bold | Italic | Bold Italic |
|--------|---------|------|--------|-------------|
| Helvetica | Helvetica | Helvetica-Bold | Helvetica-Oblique | Helvetica-BoldOblique |
| Times | Times-Roman | Times-Bold | Times-Italic | Times-BoldItalic |
| Courier | Courier | Courier-Bold | Courier-Oblique | Courier-BoldOblique |
| Symbol | Symbol | — | — | — |
| ZapfDingbats | ZapfDingbats | — | — | — |

TrueType fonts (`.ttf`) can also be loaded from file paths.

## Samples

The [`DabblerPdf.Samples`](DabblerPdf.Samples/) project includes 10 working examples:

| Sample | Description |
|--------|-------------|
| S01 | Basic text rendering |
| S02 | Text wrapping and alignment |
| S03 | Lines and shapes |
| S04 | Colors |
| S05 | Images |
| S06 | Table layout |
| S07 | Multi-page documents |
| S08 | Padding |
| S09 | Invoice (full-featured example) |
| S10 | Stress test |

Run them with:

```bash
cd DabblerPdf.Samples
dotnet run
```

Output PDFs are written to `DabblerPdf.Samples/Output/`.

## License

DabblerPdf is dual-licensed under the [LGPL-2.1](https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html) and [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/) — you may choose either license. This matches the licensing of [OpenPDF](https://github.com/LibrePDF/OpenPDF), from which this library is ported.

See [LICENSE](LICENSE) for full details.

## Contributing

Issues and pull requests are welcome. Please open an issue first for significant changes so we can discuss the approach.
