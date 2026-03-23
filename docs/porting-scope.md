# OpenPDF Porting Scope

**Project:** openpdf-dotnet
**Source:** [OpenPDF](https://github.com/LibrePDF/OpenPDF) (Java, LGPL-2.1 / MPL-2.0)
**Target:** .NET 10, C# 13
**Date:** 2026-03-23

---

## Purpose

Port a **scoped subset** of OpenPDF from Java to idiomatic C#. The goal is NOT
to port the entire library — only the parts needed by the Dabbler.Report
reporting engine in the `access-to-blazor` project.

The reporting engine needs two capabilities from this library:
1. **Text measurement** — measure text width and line height for a given font/size/style
2. **PDF generation** — render pre-positioned elements (text, lines, rectangles, images)
   at exact coordinates on PDF pages

---

## What to Port

### Tier 1 — Core (Port First)

These are the foundational classes everything else depends on.

| OpenPDF Java Class | C# Target Namespace | Purpose |
|--------------------|---------------------|---------|
| `com.lowagie.text.Document` | `OpenPdf.Document` | PDF document lifecycle (create, open, add pages, close) |
| `com.lowagie.text.Rectangle` | `OpenPdf.Document` | Page size, margins, borders, colors |
| `com.lowagie.text.PageSize` | `OpenPdf.Document` | Standard page size constants (Letter, A4, Legal, etc.) |
| `com.lowagie.text.pdf.PdfWriter` | `OpenPdf.Pdf` | PDF stream writer — creates the binary PDF structure |
| `com.lowagie.text.pdf.PdfDocument` | `OpenPdf.Pdf` | Internal document model that PdfWriter serializes |
| `com.lowagie.text.pdf.PdfPage` | `OpenPdf.Pdf` | Page representation in the PDF object tree |
| `com.lowagie.text.pdf.PdfStream` | `OpenPdf.Pdf` | Compressed content stream (page drawing commands) |
| `com.lowagie.text.pdf.PdfObject` / `PdfDictionary` / `PdfArray` / `PdfName` / `PdfNumber` / `PdfString` | `OpenPdf.Pdf` | PDF object model (the typed nodes of the PDF file structure) |
| `com.lowagie.text.pdf.PdfIndirectReference` / `PdfIndirectObject` | `OpenPdf.Pdf` | Cross-reference table entries |
| `com.lowagie.text.pdf.PdfBody` | `OpenPdf.Pdf` | Manages the sequence of PDF objects in the output stream |
| `com.lowagie.text.pdf.PdfCrossReference` / `PdfTrailer` | `OpenPdf.Pdf` | PDF trailer and xref table |

### Tier 2 — Rendering (Port Second)

The coordinate-based rendering API. This is what the report exporter calls.

| OpenPDF Java Class | C# Target Namespace | Purpose |
|--------------------|---------------------|---------|
| `com.lowagie.text.pdf.PdfContentByte` | `OpenPdf.Pdf` | **The main rendering API.** Draw text, lines, rectangles, images at absolute (x,y) coordinates. Manages graphics state (color, line width, font). |
| `com.lowagie.text.pdf.PdfGState` | `OpenPdf.Pdf` | Graphics state (opacity, blend mode) |
| `com.lowagie.text.pdf.BaseColor` | `OpenPdf.Document` | Color representation (RGB, CMYK) |

### Tier 3 — Fonts & Text Measurement (Port Third)

This is the **critical capability** the reporting engine's layout compositor needs.

| OpenPDF Java Class | C# Target Namespace | Purpose |
|--------------------|---------------------|---------|
| `com.lowagie.text.pdf.BaseFont` | `OpenPdf.Text` | **Font loading, metric caching, text measurement.** `getWidthPoint(text, fontSize)` is the key method. Handles the 14 built-in PDF fonts and TrueType embedding. |
| `com.lowagie.text.Font` | `OpenPdf.Text` | High-level font descriptor (family, size, style, color) |
| `com.lowagie.text.pdf.TrueTypeFont` | `OpenPdf.Text` | TrueType font file parser (.ttf/.ttc) — reads glyph widths, kerning tables, OS/2 metrics |
| `com.lowagie.text.pdf.FontDetails` | `OpenPdf.Text` | Per-font embedding data (subset, widths array for PDF) |
| `com.lowagie.text.pdf.DocumentFont` | `OpenPdf.Text` | Font extracted from existing PDF (not needed for generation — DEFER if complex) |
| `com.lowagie.text.pdf.PdfEncodings` | `OpenPdf.Text` | Character encoding tables (WinAnsi, Identity-H for Unicode) |

### Tier 4 — Images (Port Fourth)

| OpenPDF Java Class | C# Target Namespace | Purpose |
|--------------------|---------------------|---------|
| `com.lowagie.text.Image` | `OpenPdf.Image` | Image loading (file, byte array, URL), scaling, positioning |
| `com.lowagie.text.pdf.PdfImage` | `OpenPdf.Image` | Image embedding in PDF (JPEG pass-through, PNG deflate) |
| `com.lowagie.text.Jpeg` | `OpenPdf.Image` | JPEG-specific handling (pass raw bytes to PDF — no re-encoding) |
| `com.lowagie.text.png.PngImage` | `OpenPdf.Image` | PNG decoding for PDF embedding |

---

## What NOT to Port

These are explicitly out of scope. Do not port them even if they appear as
dependencies — find alternative approaches or stub them out.

| Category | Classes | Why Skip |
|----------|---------|----------|
| **High-level flow layout** | `Paragraph`, `Phrase`, `Chunk`, `Chapter`, `Section`, `List`, `ListItem` | Dabbler.Report has its own layout engine (`BandLayoutEngine`). We don't need OpenPDF to compute layout. |
| **Table abstraction** | `PdfPTable`, `PdfPCell`, `PdfPRow` | Report bands handle tabular layout via positioned elements. No table abstraction needed for v1. |
| **Form fields** | `PdfAcroForm`, `PdfFormField`, `TextField`, `RadioCheckField` | Not used in report output. |
| **Digital signatures** | `PdfSignature`, `PdfPKCS7`, `MakeSignature` | Not needed. |
| **PDF/A compliance** | `PdfAWriter`, `PdfAConformanceImp` | Deferred to v2 if needed. |
| **Encryption** | `PdfEncryption`, `PdfEncryptor` | Not needed for report export. |
| **Annotations** | `PdfAnnotation`, `PdfAction`, `PdfDestination`, `PdfOutline` | Bookmarks/links deferred to v2. |
| **Barcode** | `Barcode*` classes | Not used in Northwind reports. |
| **HTML/RTF parsing** | `HtmlParser`, `RtfParser` and related | Not relevant. |
| **PDF reading/editing** | `PdfReader`, `PdfStamper`, `PdfCopy` | We only generate PDFs, not read/edit them. |
| **XML/XFDF** | `XfdfReader`, `FdfReader`, `XmpMetadata` | Not needed. |
| **Legacy encodings** | CJK font support, `CJKFont`, `CMapAwareDocumentFont` | Not needed for Northwind reports. Add later if required. |

---

## Porting Guidelines

### Idiomatic C# — Not Line-by-Line Translation

The goal is a C# library that feels native to .NET developers, not a mechanical
Java-to-C# translation. Key transformations:

| Java Pattern | C# Equivalent |
|-------------|---------------|
| `public class Foo { private int bar; public int getBar() { return bar; } public void setBar(int bar) { this.bar = bar; } }` | `public class Foo { public int Bar { get; set; } }` |
| `throws IOException` | No checked exceptions in C# — use `/// <exception>` XML doc instead |
| `ArrayList<String>` | `List<string>` |
| `HashMap<String, Object>` | `Dictionary<string, object>` |
| `byte[]` with manual offset/length | `Span<byte>` or `ReadOnlySpan<byte>` where appropriate |
| `OutputStream` | `Stream` |
| `synchronized` | `lock` or avoid if single-threaded |
| `null` returns for errors | Return `null` with nullable annotations, or throw appropriate exceptions |
| `static final` constants | `const` or `static readonly` |
| `implements Interface` | `: IInterface` |
| `package com.lowagie.text.pdf` | `namespace OpenPdf.Pdf` |
| `@Override` | `override` keyword |
| Java `float` everywhere | Use `double` for coordinates and measurements (matches Dabbler.Report's `LayoutElement`) |

### Namespace Structure

```
OpenPdf/
├── Document/          → Document, Rectangle, PageSize, BaseColor
├── Text/              → Font, BaseFont, TrueTypeFont, FontDetails, PdfEncodings
├── Pdf/               → PdfWriter, PdfContentByte, PdfDocument, PdfPage, PdfObject tree
├── Image/             → Image, PdfImage, Jpeg, PngImage
└── Utilities/         → CoordinateTranslator, internal helpers
```

### Coordinate System

OpenPDF uses PDF's native coordinate system: **origin at bottom-left, Y-axis up,
units in points (1/72 inch)**. Keep this unchanged — the Dabbler.Report `PdfExporter`
handles the conversion from its own top-left-down inches system.

Provide a utility class for callers:

```csharp
namespace OpenPdf.Utilities;

public static class CoordinateTranslator
{
    public static (double PdfX, double PdfY) FromTopLeftInches(
        double x, double y, double elementHeight, double pageHeightInches)
    {
        return (x * 72.0, (pageHeightInches - y - elementHeight) * 72.0);
    }

    public static double InchesToPoints(double inches) => inches * 72.0;
    public static double PointsToInches(double points) => points / 72.0;
}
```

### License Headers

Every ported source file must include:

```csharp
// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0
```

### Testing Strategy

For each ported class, write tests that verify:

1. **Known outputs** — Generate a PDF with known content, verify the output bytes
   contain expected PDF operators (e.g., `BT ... ET` for text, `re` for rectangles)
2. **Text measurement accuracy** — Compare `BaseFont.GetWidthPoint()` results against
   known values from the Java OpenPDF (document expected values in test comments)
3. **Font loading** — Load built-in fonts (Helvetica, Times-Roman, Courier) and verify
   metric tables
4. **Round-trip coordinates** — Verify that CoordinateTranslator produces correct
   PDF coordinates for known Dabbler layout positions
5. **Image embedding** — Verify JPEG pass-through and PNG embedding produce valid PDF

### Dependencies

The ported library should have **zero external NuGet dependencies**. All PDF
generation logic is self-contained (this is one of the key advantages of porting
OpenPDF rather than depending on PdfSharp).

The only .NET dependencies should be:
- `System.IO` (streams)
- `System.IO.Compression` (deflate for PDF streams)
- `System.Text` (encoding)
- `System.Drawing.Primitives` (for `Color` struct if needed, or define our own)

---

## API Surface for Dabbler.Report

The consuming code in `access-to-blazor` needs these capabilities:

### Text Measurement (consumed by BandLayoutEngine)

```csharp
// Load a font and measure text
var font = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);
double widthInPoints = font.GetWidthPoint("Hello World", 12.0);
double ascentInPoints = font.GetFontDescriptor(BaseFont.ASCENT, 12.0);
double descentInPoints = font.GetFontDescriptor(BaseFont.DESCENT, 12.0);
double lineHeightInPoints = ascentInPoints - descentInPoints;
```

### PDF Generation (consumed by PdfExporter)

```csharp
// Create a PDF document
using var stream = File.Create("report.pdf");
var document = new Document(PageSize.Letter);
var writer = PdfWriter.GetInstance(document, stream);
document.Open();

// Get the direct content layer for coordinate rendering
var cb = writer.DirectContent;

// Draw text at absolute position
var font = BaseFont.CreateFont("Helvetica", BaseFont.WinAnsiEncoding, false);
cb.BeginText();
cb.SetFontAndSize(font, 12);
cb.SetTextMatrix(72, 720);  // 1 inch from left, 10 inches from bottom
cb.ShowText("Hello World");
cb.EndText();

// Draw a rectangle
cb.SetColorStroke(new BaseColor(0, 0, 0));
cb.SetColorFill(new BaseColor(230, 230, 230));
cb.Rectangle(72, 700, 468, 20);
cb.FillStroke();

// Draw a line
cb.MoveTo(72, 695);
cb.LineTo(540, 695);
cb.Stroke();

// Embed an image
var image = Image.GetInstance("logo.png");
image.ScaleToFit(100, 50);
image.SetAbsolutePosition(72, 650);
cb.AddImage(image);

document.Close();
```

### Minimum Viable API

The port is "done" when these test scenarios pass:
1. Create a multi-page PDF with text at known coordinates
2. Measure text width and it matches OpenPDF Java output within 0.1pt
3. Draw rectangles with fill and stroke colors
4. Draw lines with configurable width and color
5. Embed a JPEG image at a known position
6. Embed a PNG image at a known position
7. Use Helvetica, Times-Roman, and Courier (built-in fonts)
8. Load and embed a TrueType font from a .ttf file
9. The output PDF opens correctly in Adobe Acrobat, Chrome, and Edge

---

## Phased Implementation Plan

### Phase A — PDF Object Model + Empty Document
Port the PDF object tree (`PdfObject`, `PdfDictionary`, `PdfArray`, etc.),
`PdfWriter`, `PdfBody`, cross-reference table, and trailer. Goal: generate a
valid empty PDF that opens in a viewer.

### Phase B — Content Stream + Basic Drawing
Port `PdfContentByte` (lines, rectangles, fill, stroke, color). Goal: generate
a PDF with colored rectangles and lines.

### Phase C — Built-in Fonts + Text
Port `BaseFont` for the 14 built-in PDF fonts. Port text operators in
`PdfContentByte` (`BeginText`, `SetFontAndSize`, `ShowText`, etc.). Goal:
generate a PDF with positioned text and measure text width.

### Phase D — TrueType Fonts
Port `TrueTypeFont` for loading .ttf files, reading glyph widths, and
embedding font subsets. Goal: generate a PDF with embedded TrueType text.

### Phase E — Images
Port `Image`, `Jpeg`, `PngImage`, and `PdfImage`. Goal: embed JPEG and PNG
images in a PDF.

### Phase F — Polish & Integration
Verify against the Dabbler.Report `PdfExporter` API needs. Add
`CoordinateTranslator`. Ensure all "Minimum Viable API" scenarios pass.
Package for consumption via submodule + project reference.
