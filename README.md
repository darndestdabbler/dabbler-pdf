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

The Base 14 fonts use `WinAnsiEncoding` (Windows-1252): ASCII plus Western European letters,
the euro sign, dashes and typographic quotes. A character outside Windows-1252 — Polish `ł`,
any Cyrillic letter, and so on — is written as `?` and measured by `GetWidthPoint` as `?`, so
measurement matches what is drawn. A TrueType font loaded with `WinAnsiEncoding` has the same
Windows-1252 limit and is measured the same way.

### Unicode text

To draw text in other scripts, load a TrueType font with `BaseFont.IdentityH`:

```csharp
var font = BaseFont.CreateFont("fonts/LiberationSans-Regular.ttf", BaseFont.IdentityH, true);

var cb = writer.DirectContent;
cb.BeginText();
cb.SetFontAndSize(font, 12);
cb.SetTextMatrix(72, 720);
cb.ShowText("Zażółć gęślą jaźń — Привет — Καλημέρα");
cb.EndText();
```

Every character the font has a glyph for is drawn: Polish and other Latin-extended letters,
Cyrillic, Greek, and CJK with a font that covers it. Each character is measured by its own
glyph, and copying, searching and text extraction give the text back. `CanEncode(c)` says
whether the font has a glyph for `c`; a character it lacks is drawn and measured as the font's
`.notdef` glyph (usually an empty box). One `.ttf` can be loaded with both encodings, and each
is its own font in the document.

Limits:

- No shaping, ligatures or right-to-left layout, so Arabic, Hebrew, Indic and Thai text does
  not draw correctly.
- No characters beyond U+FFFF (emoji, rarer CJK): each draws as `.notdef`.
- No fallback to another font for a missing glyph.
- TrueType (`.ttf`) fonts only, not OpenType CFF (`.otf`); horizontal writing only.
- The whole font file is embedded in each document that uses it; subsetting it to the glyphs
  used is planned for Session 17.
- The 14 standard fonts cannot use `IdentityH`: `CreateFont` throws `ArgumentException`.

## Samples

The [`DabblerPdf.Samples`](DabblerPdf.Samples/) project includes 11 working examples:

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
| S11 | Orders-style landscape report — multi-page, grouped, page footer template |

Run them with:

```bash
cd DabblerPdf.Samples
dotnet run
```

Output PDFs are written to `DabblerPdf.Samples/Output/`.

## Testing

DabblerPdf is covered by **614 automated tests** plus **11 runnable sample programs** that emit
real PDFs for visual review. Tests are organized in a four-tier hierarchy (strict unit →
integration → sample-based visual review → human review) and run on every build.

### Running the tests

```bash
# Full test suite (614 tests, ~0.3s)
dotnet test tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj

# Generate sample PDFs for visual review
dotnet run --project DabblerPdf.Samples
```

### What the tests cover

| Layer | Scope | Examples |
|---|---|---|
| **Unit** | Isolated logic — font metrics, coordinate translation, encoding, PDF date formatting | `BaseFont`, `CoordinateTranslator`, `PdfDate`, `BaseColor` |
| **Integration** | Full `Document` + `PdfWriter` + produced byte stream | `PdfWriterTests`, `DocumentMetadataTests`, `PdfInfoDictionaryTests`, `PageEventTests` |
| **Rendering-visibility** | Per-physical-page text extraction via [PdfPig](https://github.com/UglyToad/PdfPig) — proves content is actually visible to a reader, not just present in bytes | `MultipageDirectContentRenderingTests`, `RenderingVisibilityDisciplineTests`, `OrdersStyleSampleRenderingTests` |
| **Sample-based** | 11 runnable programs that emit PDFs to `DabblerPdf.Samples/Output/` for visual inspection | `S01_BasicText` through `S11_OrdersStyleReport` |

### Rendering-visibility discipline

Any test that asserts *"content X is visible in the PDF"* drives the assertion through a
rendering-aware parser (PdfPig) that reports per-physical-page text with bounding boxes —
not a raw byte grep of the content stream. A byte grep proves the text-showing operator is
present; it cannot detect that the text was overdrawn by a later fill, clipped, placed
off-page, or rendered in an unavailable font. The discipline and its motivating field incident
are captured in [`docs/MAINTAINING.md`](docs/MAINTAINING.md) → Testing.

Byte-level stream assertions are still used for *structural* properties (object IDs,
`/Info` dictionary keys, cross-reference-table validity) where a parser would add nothing.

### Content-stream compression

By default `PdfWriter` Flate-compresses a page or template content stream only when its
uncompressed length is over 128 bytes; one of 128 bytes or fewer is written as plain operators.
A small page therefore shows its text operators in the file's bytes, and the same page with one
more line does not. A byte-level text search of a generated PDF needs either a content stream
of 128 bytes or fewer or compression turned off:

```csharp
var writer = PdfWriter.GetInstance(document, stream);
writer.CompressContentStreams = false; // page and template content streams written uncompressed
```

`CompressContentStreams` (default `true`) affects only page and template content streams;
font programs and images are compressed as before.

### Test dependencies

| Dependency | License | Purpose | Ships in nupkg? |
|---|---|---|---|
| xUnit | Apache-2.0 | Test runner | No |
| PdfPig | Apache-2.0 | Per-page text extraction for rendering-visibility tests | No |

Test dependencies are not propagated to consumers — the shipped `DabblerPdf` package remains
dual-licensed under LGPL-2.1-only AND MPL-2.0.

## License

DabblerPdf is dual-licensed under the [LGPL-2.1](https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html) and [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/) — you may choose either license. This matches the licensing of [OpenPDF](https://github.com/LibrePDF/OpenPDF), from which this library is ported.

See [LICENSE](LICENSE) for full details.

## If this library stops being maintained

Vendor the source. Copy `src/DabblerPdf` into your own repository (or fork this one), reference it with a `<ProjectReference>`, and maintain it yourself under either license. Keep the [LICENSE](LICENSE) file and the license header on every source file, and keep the copyright notices intact. [`docs/MAINTAINING.md`](docs/MAINTAINING.md) explains how to build, test and release it, and what a redistributor owes under each license.

## Contributing

Issues and pull requests are welcome. Please open an issue first for significant changes so we can discuss the approach. [`docs/MAINTAINING.md`](docs/MAINTAINING.md) covers building, testing and releasing.
