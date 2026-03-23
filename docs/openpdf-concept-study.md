# OpenPDF Concept Study — Task 1.3

**Project:** Dabbler.WpfControls / Dabbler.Report
**Date:** 2026-03-22
**Purpose:** Extract PDF rendering patterns from OpenPDF's architecture to inform the Dabbler.Report PDF export design, and evaluate .NET PDF libraries for implementation. No Java code is ported — only concepts, coordinate models, and rendering strategies.

**References:**
- Task 1.1 deliverable: `docs/northwind-report-inventory.md`
- Task 1.2 deliverable: `docs/jasperreports-concept-study.md`
- Project plan: `WPF_ReportDefinition_RenderingEngine_Plan.md` (Section 8.2)
- Existing scaffolding: `src/Dabbler.Report/Export/`

---

## 1. PDF Coordinate Model

### 1.1 OpenPDF's Coordinate System

OpenPDF (and the PDF specification itself) uses a coordinate system with:

- **Origin at bottom-left** of the page
- **Units in points** (1 point = 1/72 inch)
- **Y-axis increases upward** (opposite of most screen coordinate systems)

A standard US Letter page (8.5" × 11") measures 612 × 792 points.

```
(0, 792) ──────────── (612, 792)   ← Top of page
    │                      │
    │     Content Area     │
    │                      │
(0, 0) ────────────── (612, 0)     ← Bottom of page (PDF origin)
```

### 1.2 OpenPDF's Two Rendering Modes

OpenPDF provides two fundamentally different rendering approaches:

**High-Level Flow Mode (Document.add())**
- Content flows top-to-bottom automatically
- The library tracks a Y-cursor internally
- Page breaks are inserted automatically when content overflows
- Tables, paragraphs, and images are added sequentially
- Headers/footers are injected via page event callbacks

**Low-Level Coordinate Mode (PdfContentByte)**
- Direct positioning via absolute coordinates (x, y)
- Full control over every element's position
- No automatic page breaks — the caller manages pages manually
- Used for precise layouts, watermarks, overlays, and custom drawing

### 1.3 Dabbler Coordinate Model (Existing Scaffolding)

The existing scaffolding uses a different but equally valid coordinate system:

- **Origin at top-left** of the page (screen convention)
- **Units in inches** (matches Access report design surface)
- **Y-axis increases downward** (natural reading order)

This is defined in `LayoutElement`:
```csharp
public double X { get; set; }  // inches from page left edge
public double Y { get; set; }  // inches from page top edge
public double Width { get; set; }
public double Height { get; set; }
```

### 1.4 Coordinate Translation Algorithm

The `PdfExporter` must translate from Dabbler's layout coordinates to PDF coordinates. The conversion is straightforward:

```
pdfX = layoutX * 72.0
pdfY = (pageHeightInches - layoutY - elementHeightInches) * 72.0
pdfWidth = layoutWidth * 72.0
pdfHeight = layoutHeight * 72.0
```

Where:
- `72.0` converts inches to points
- The Y-axis is inverted: subtract from page height, then subtract element height to get the bottom-left corner (which PDF uses as the element origin)

**Recommendation:** Encapsulate this conversion in a dedicated `CoordinateTranslator` helper rather than scattering the arithmetic across rendering methods. This isolates the one place where Dabbler's top-left-down model meets PDF's bottom-left-up model.

```csharp
// Proposed helper (concept only)
internal static class CoordinateTranslator
{
    public static (double PdfX, double PdfY) ToPdfCoordinates(
        double layoutX, double layoutY, double elementHeight,
        double pageHeightInches)
    {
        return (
            layoutX * 72.0,
            (pageHeightInches - layoutY - elementHeight) * 72.0
        );
    }

    public static double InchesToPoints(double inches) => inches * 72.0;
    public static double PointsToInches(double points) => points / 72.0;
}
```

---

## 2. Font Embedding and Text Measurement

### 2.1 OpenPDF's Font Architecture

OpenPDF's font system is built around the `BaseFont` class:

- **14 built-in PDF fonts** (Helvetica, Times-Roman, Courier, etc.) — always available, no embedding needed
- **TrueType font embedding** — reads `.ttf`/`.ttc` files, embeds font programs in the PDF, generates width arrays for each glyph
- **Font metric caching** — once a `BaseFont` is created, its width tables are cached for reuse

**Text measurement:**
```java
// OpenPDF pattern
float width = baseFont.getWidthPoint("Hello World", 12f);
// Internally: getWidth(text) * 0.001f * fontSize
// Returns width in PDF points
```

The `getWidthPointKerned()` variant adjusts for kerning pairs, producing more accurate measurements at the cost of additional computation.

### 2.2 Font Measurement in the Band Layout Context

Font measurement is critical in two phases of report rendering:

**Phase 1 — Layout Composition (BandLayoutEngine):**
Text measurement determines whether content fits within an element's declared bounds. When text overflows, the engine must decide whether to:
- Truncate the text
- Wrap to multiple lines (increasing band height)
- Shrink the font to fit (not recommended for reports)

This is where the JasperReports `TextAdjust` concept (from Task 1.2) intersects with font metrics.

**Phase 2 — PDF Export (PdfExporter):**
The chosen .NET PDF library handles font embedding and glyph rendering. The exporter must:
1. Map Dabbler style font declarations (family, size, bold, italic) to library font objects
2. Embed referenced fonts in the output PDF
3. Render text at the correct position with correct metrics

### 2.3 Recommendations for Dabbler.Report

**Font metric service:** Create a `FontMetrics` abstraction that measures text width and line height for a given font/size/style combination. This service is consumed by `BandLayoutEngine` during layout and by `PdfExporter` during rendering.

```csharp
// Proposed abstraction (concept only)
internal interface IFontMetrics
{
    double MeasureWidth(string text, string fontFamily, double fontSizePoints,
                        bool isBold, bool isItalic);
    double GetLineHeight(string fontFamily, double fontSizePoints,
                         bool isBold, bool isItalic);
}
```

**WPF implementation for layout:** During layout composition (which runs on the WPF side), use WPF's `FormattedText` or `GlyphTypeface` for text measurement. These are available without rendering to screen.

**PDF implementation for export:** The PDF library provides its own font measurement. Use the library's metrics during PDF rendering to ensure pixel-perfect alignment (WPF and PDF font metrics can differ slightly for the same font).

**Font caching:** Cache font metric objects (keyed by family + size + style) to avoid repeated lookups during the detail band rendering loop — the hot path where the same fonts are measured thousands of times.

**Decision: two font metric implementations vs. one.**
Using WPF metrics for layout and PDF-library metrics for export introduces a risk of slight discrepancies (a line that fits during layout might overflow by a fraction of a point during export). Mitigation: add a 2% width safety margin during layout measurement, or accept the minor discrepancy since report text elements typically have generous width.

---

## 3. Table Rendering and Cell Layout

### 3.1 OpenPDF's PdfPTable Model

OpenPDF's `PdfPTable` provides a complete table rendering abstraction:

**Column width specification:**
- **Relative widths:** `float[] { 1f, 2f, 1f }` — columns sized by ratio (first and third columns are half the width of the second)
- **Absolute widths:** specified in points, sum determines table width
- **Percentage:** table width as a percentage of available page width

The library translates all width specifications to absolute point coordinates for rendering.

**Cell model (PdfPCell):**
- Individual cell padding (top, bottom, left, right)
- Cell borders (independently configurable per side)
- Horizontal/vertical alignment within the cell
- Colspan spanning
- Background color
- Minimum height

**Cell rendering pipeline:**
1. Calculate column x-coordinates from widths
2. For each row, calculate cell heights (max across the row)
3. Draw cell backgrounds
4. Draw cell borders
5. Render cell content within the padded area

### 3.2 How This Maps to Dabbler's Band Model

Dabbler.Report does **not** have a table abstraction — and it doesn't need one for v1. The band model already handles tabular layouts:

- **Column headers** → elements positioned in the `PageHeader` or `GroupHeader` band
- **Data cells** → elements positioned in the `Detail` band
- **Column alignment** → elements at consistent X positions across bands
- **Column widths** → implicit from element X + Width values

This is exactly how Access reports and JasperReports work: tabular appearance is achieved through element positioning within bands, not through a table abstraction.

### 3.3 When a Table Abstraction Would Be Needed

A table abstraction would be useful for:
- **Subreport rendering** — when a subreport within a band renders tabular data with variable row counts
- **Invoice line items** — already handled by the detail band in the current model
- **Complex nested tables** — not present in Northwind reports

**Recommendation for v1:** No dedicated table type. The band model with positioned elements handles all Northwind report patterns. If future reports require embedded tables (e.g., a summary table within a group footer), consider adding an `ElementType.Table` with inline column/row definitions, but defer this to v2.

### 3.4 What to Extract from PdfPTable for PDF Export

Even without a table abstraction, the PdfPTable rendering pipeline informs how the `PdfExporter` renders element borders and backgrounds:

**Border rendering pattern:**
```
For each element with a border style:
  1. Calculate the border rectangle in PDF coordinates
  2. Set stroke color and line width
  3. Draw the rectangle (or individual sides based on border specification)
  4. Clip content to the padded inner rectangle
```

**Background rendering pattern:**
```
For each element with a background color:
  1. Draw a filled rectangle at the element's position
  2. Render text content on top of the background
  3. Draw borders last (on top of everything)
```

**Rendering order matters:** Background → Content → Borders. This prevents borders from being clipped by content and ensures backgrounds don't cover adjacent elements.

---

## 4. Image Handling

### 4.1 OpenPDF's Image Model

OpenPDF's `Image` class supports:

**Image sources:**
- File paths (JPEG, PNG, GIF, BMP, TIFF, WMF)
- Byte arrays (for embedded/database images)
- URLs (for remote images)

**Scaling methods:**
- `scaleToFit(maxWidth, maxHeight)` — scales to fit within bounds, maintaining aspect ratio
- `scaleAbsolute(width, height)` — forces exact dimensions, may distort aspect ratio
- `scalePercent(percent)` — uniform scaling by percentage

**Positioning:**
- `setAbsolutePosition(x, y)` — places image at exact PDF coordinates
- Flow positioning — image is added to the document flow and wraps with text

### 4.2 Dabbler.Report Image Requirements

From the Northwind report inventory (Task 1.1):

| Report | Image Use | Source |
|--------|-----------|--------|
| rptEmployeeEmailList | Company logo | Static file (Northwind logo) |
| rptEmployeePhoneList | Company logo | Static file |
| rptProductCatalog | Product images | BLOB field (`Picture` column) |

Two image source patterns:
1. **Static images** — referenced by file path in the RDF, same image on every page (logos, letterheads)
2. **Data-bound images** — BLOB data from a database column, different per record (product photos)

### 4.3 Recommendations for Dabbler.Report

**Element definition (already scaffolded):**
```csharp
// ElementDefinition already has:
public string? ImageSource { get; set; }  // file path or field reference
```

**Extend for v1:**
- If `ImageSource` starts with `[` (e.g., `[Picture]`), treat as a field reference → resolve from the current data row as a byte array
- Otherwise, treat as a file path relative to the RDF file location

**Scaling strategy:**
Add `ImageScaleMode` to `ElementDefinition`:
```
ScaleToFit   — maintain aspect ratio within element bounds (default)
Stretch      — fill element bounds exactly (may distort)
ActualSize   — render at native resolution, clip if larger than bounds
```

**PDF export pattern:**
```
For each Image element:
  1. Resolve image source (file path or data row BLOB)
  2. Load image bytes
  3. Create PDF library image object
  4. Apply scaling based on ScaleMode and element bounds
  5. Position at translated PDF coordinates
  6. Add to page
```

**Image caching:** Static images (logos) should be loaded once and reused across pages. Data-bound images are per-record and not cached. The PDF library should handle deduplication within the PDF file (embedding the same image data once even if referenced on multiple pages).

---

## 5. Page Break Logic in PDF Context

### 5.1 OpenPDF's Page Break Handling

OpenPDF handles page breaks differently depending on the rendering mode:

**High-level mode (Document.add()):**
- Automatic page breaks when content exceeds page height
- Tables split automatically between pages
- `headerRows` property causes header rows to repeat on continuation pages
- `keepTogether` prevents a table from splitting across pages (forces a page break before the table if it won't fit)
- `splitRows` controls whether individual rows can be split across pages

**Low-level mode (PdfContentByte):**
- No automatic page breaks — the caller creates new pages explicitly
- Full manual control over what appears on each page

**Known issue:** OpenPDF's automatic table splitting can behave inconsistently when mixing `writeSelectedRows()` (coordinate-based) with `Document.add()` (flow-based). These are separate code paths with different splitting logic.

### 5.2 Where Page Breaks Happen in Dabbler.Report

Page breaks are **not the PdfExporter's responsibility** in the Dabbler architecture. The `BandLayoutEngine` (informed by Task 1.2's JasperReports algorithms) handles page composition:

```
BandLayoutEngine.ComposePages() → List<PageLayout>
                                    ↓
                              Each PageLayout contains
                              positioned LayoutElements
                              with absolute coordinates
                                    ↓
                          PdfExporter renders each page
                          independently — no page break
                          decisions needed
```

This is a critical architectural separation: the layout engine decides **what goes on each page**, and the PDF exporter simply **renders each page**. The exporter never needs to split content across pages.

### 5.3 What the PdfExporter Does Need

Even though page breaks are pre-computed, the exporter handles:

1. **PDF page creation** — create a new PDF page for each `PageLayout`
2. **Page size consistency** — all pages use the same dimensions from `ReportMetadata`
3. **Element rendering order** — render elements in correct z-order (backgrounds first, then content, then borders, then lines/rectangles)
4. **Page-level metadata** — inject PDF document properties (title, author) from `ReportMetadata`

### 5.4 The Two-Pass TotalPages Problem

The JasperReports concept study (Task 1.2) identified a two-pass rendering requirement: expressions like `{TotalPages}` cannot be resolved until all pages have been composed. The `BandLayoutEngine` handles this by:

1. **First pass:** Compose all pages, leaving `{TotalPages}` as a placeholder
2. **Second pass:** Set `PageLayout.TotalPages` on all pages, resolve `{TotalPages}` in element text

By the time `PdfExporter` receives the `List<PageLayout>`, all expressions including `{TotalPages}` are fully resolved. The exporter sees only final text content.

---

## 6. Document Lifecycle and Page Events

### 6.1 OpenPDF's Lifecycle Pattern

OpenPDF's document lifecycle follows a strict sequence:

```
Document doc = new Document(pageSize, margins...);
PdfWriter writer = PdfWriter.getInstance(doc, outputStream);
writer.setPageEvent(pageEventHandler);  // register callbacks
doc.open();                              // must open before writing
  doc.add(element1);                     // write content
  doc.add(element2);
  doc.newPage();                         // explicit page break
  doc.add(element3);
doc.close();                             // must close to finalize PDF
```

**Page events (PdfPageEvent interface):**
| Event | When | Common use |
|-------|------|------------|
| `onOpenDocument` | After `doc.open()` | Initialize shared resources |
| `onStartPage` | Before first content on a page | (Rarely used — fires even for empty pages) |
| `onEndPage` | After page content, before write | **Headers and footers** |
| `onCloseDocument` | After `doc.close()` | Cleanup |

**Critical constraint:** Do not call `Document.add()` inside page events — it causes recursion. Headers/footers in OpenPDF are rendered via `PdfContentByte` (direct coordinate drawing) inside `onEndPage`.

### 6.2 Why Dabbler Doesn't Need Page Events

In OpenPDF/iText, page events exist because headers and footers are injected after the fact — the content flow doesn't know about them. In Dabbler.Report, headers and footers are composed by the `BandLayoutEngine` as regular `LayoutElement` entries on each page. They're not special — they're just elements at known positions.

This means the `PdfExporter` has a simpler lifecycle:

```csharp
// Conceptual PdfExporter flow
public async Task ExportAsync(IReadOnlyList<PageLayout> pages, Stream output, ...)
{
    // 1. Create PDF document
    // 2. Set document metadata (title, author from ReportMetadata)
    // 3. For each PageLayout:
    //    a. Create new PDF page
    //    b. For each LayoutElement (sorted by rendering order):
    //       - Translate coordinates
    //       - Render element by type (text, line, rectangle, image)
    // 4. Close/finalize PDF document
    // 5. Flush to output stream
}
```

No event system, no callbacks, no special header/footer handling. The layout engine has already done the hard work.

---

## 7. .NET PDF Library Evaluation

### 7.1 Candidates

The project plan (Section 8.3) identified three candidates. The current landscape (March 2026):

| Library | License | API Style | Maturity |
|---------|---------|-----------|----------|
| **QuestPDF** | Community (MIT-like, revenue cap) | Fluent component DSL | Active, modern |
| **PdfSharp / MigraDoc** | MIT | Low-level coordinate (PdfSharp) + document model (MigraDoc) | Mature, stable |
| **iText 7 for .NET** | AGPL / Commercial | Object-oriented, comprehensive | Enterprise-grade |

### 7.2 QuestPDF

**Architecture:** Fluent C# API using a component-tree model. Documents are composed from nested containers, columns, rows, and content blocks. The library handles pagination automatically.

**Strengths:**
- Modern, readable C# API
- Automatic pagination and content splitting
- Built-in support for headers, footers, page numbers
- Good image handling (file, byte array, stream)
- Active community and maintenance

**Concerns:**
- **License:** "Community License" is free for companies with less than $1M annual gross revenue. Above that threshold, a commercial license is required. This is NOT a true MIT license.
- **API mismatch:** QuestPDF's strength is its automatic layout engine — but Dabbler.Report already has its own layout engine (`BandLayoutEngine`). Using QuestPDF for pre-positioned elements means fighting against its layout model rather than leveraging it.
- **Coordinate rendering:** QuestPDF is designed for flow-based composition, not coordinate-based rendering. Placing elements at exact (x, y) positions requires workarounds (`Canvas` API with `SkiaSharp`).

**Verdict:** Poor fit. QuestPDF duplicates Dabbler's layout engine, and its license has revenue restrictions. The Dabbler.Report engine needs a library that renders pre-positioned elements, not one that computes layout.

### 7.3 PdfSharp / MigraDoc

**Architecture:** Two-layer system:
- **PdfSharp** — low-level coordinate-based PDF rendering. Direct control over pages, graphics, text, images. Think of it as a PDF `Canvas`.
- **MigraDoc** — high-level document model (Document → Sections → Paragraphs/Tables). MigraDoc renders through PdfSharp internally.

**PdfSharp rendering pattern (relevant to Dabbler):**
```csharp
// PdfSharp coordinate rendering — conceptual
var document = new PdfDocument();
var page = document.AddPage();
page.Width = XUnit.FromInch(8.5);
page.Height = XUnit.FromInch(11.0);

var gfx = XGraphics.FromPdfPage(page);
var font = new XFont("Arial", 12, XFontStyleEx.Regular);

// Origin is TOP-LEFT (unlike raw PDF) — PdfSharp translates internally
gfx.DrawString("Hello", font, XBrushes.Black,
    new XPoint(72, 72));  // 1 inch from left, 1 inch from top

gfx.DrawRectangle(XPens.Black, XBrushes.LightGray,
    72, 200, 468, 20);  // filled rectangle

gfx.DrawImage(XImage.FromFile("logo.png"),
    72, 72, 100, 50);   // positioned image
```

**Critical advantage:** PdfSharp uses a **top-left origin with Y-axis pointing down** — the same convention as Dabbler's `LayoutElement`. This means coordinate translation is trivial (just convert inches to points).

**Strengths:**
- **MIT license** — truly open source, no revenue restrictions
- **Coordinate-based API** — exactly what the `PdfExporter` needs for rendering pre-positioned elements
- **Top-left origin** — matches Dabbler's coordinate system, eliminating the Y-axis inversion complexity
- **Font embedding** — supports TrueType font embedding
- **Image support** — JPEG, PNG, BMP, GIF, TIFF from files, streams, or byte arrays
- **Mature** — decades of production use, stable API
- **No layout engine** — PdfSharp doesn't try to lay out content. It draws what you tell it, where you tell it. This is the right abstraction for an exporter that receives pre-composed pages.

**Concerns:**
- No automatic text wrapping — the caller must break text into lines (but `BandLayoutEngine` handles this during composition)
- No automatic table rendering — not needed since bands handle tabular layout
- Font metric measurement available via `XTextFormatter` and `XFont` but less documented than other areas

**MigraDoc** is not needed — it provides document-level layout that duplicates `BandLayoutEngine`. Use PdfSharp directly.

**Verdict:** Excellent fit. PdfSharp is the right abstraction level — a coordinate-based PDF canvas that renders pre-positioned elements from the layout engine.

### 7.4 iText 7 for .NET

**Architecture:** Comprehensive PDF library with both high-level (Document/layout) and low-level (PdfCanvas) APIs.

**Strengths:**
- Most feature-complete PDF library in the .NET ecosystem
- Excellent font support, accessibility (PDF/UA), and PDF/A compliance
- Low-level `PdfCanvas` API for coordinate rendering

**Concerns:**
- **AGPL license** — requires either open-sourcing the entire application or purchasing a commercial license. Not compatible with a proprietary enterprise migration platform without commercial licensing.
- Overly complex for the Dabbler.Report use case — most features are unnecessary

**Verdict:** Rejected due to AGPL license. The enterprise migration platform is proprietary.

### 7.5 Recommendation: PdfSharp

**PdfSharp is the recommended PDF library for Dabbler.Report.**

Rationale:
1. **MIT license** — no restrictions, compatible with any downstream licensing
2. **Coordinate-based API** — matches the exporter's responsibility (render pre-positioned elements)
3. **Same coordinate convention** — top-left origin with Y-down, matching `LayoutElement`
4. **No layout engine** — doesn't compete with `BandLayoutEngine`
5. **Mature and stable** — production-proven, minimal risk
6. **Zero unnecessary complexity** — draws what you tell it, nothing more

NuGet package: `PdfSharp` (current version 6.x for .NET 6+)

---

## 8. PdfExporter Implementation Design

### 8.1 Coordinate Translation (Simplified)

Because PdfSharp uses the same top-left-down convention as `LayoutElement`, coordinate translation reduces to a unit conversion:

```csharp
// Inches to PdfSharp XUnit
XUnit x = XUnit.FromInch(element.X);
XUnit y = XUnit.FromInch(element.Y);
XUnit width = XUnit.FromInch(element.Width);
XUnit height = XUnit.FromInch(element.Height);
```

No Y-axis inversion needed. PdfSharp handles the PDF-internal coordinate transformation.

### 8.2 Element Rendering Pipeline

```
For each PageLayout in the composed report:
  1. Create PdfPage with dimensions from ReportMetadata
  2. Create XGraphics from the page
  3. For each LayoutElement (sorted: backgrounds → content → borders):
     a. Resolve style from StyleDefinition
     b. Convert coordinates (inches → XUnit)
     c. Switch on ElementType:
        - StaticText / TextField → DrawString with XFont, alignment, clipping
        - Line → DrawLine with XPen
        - Rectangle → DrawRectangle (filled and/or stroked)
        - Image → DrawImage with XImage, scaling
  4. Dispose XGraphics
```

### 8.3 Style Resolution

Map `StyleDefinition` properties to PdfSharp types:

| StyleDefinition | PdfSharp Type |
|----------------|---------------|
| `FontFamily` + `FontSize` + `IsBold` + `IsItalic` | `XFont` |
| `ForegroundColor` | `XBrush` (via `XSolidBrush`) |
| `BackgroundColor` | `XBrush` for fill rectangle |
| `Border` | `XPen` for rectangle stroke |
| `HorizontalAlignment` | `XStringFormat.Alignment` |
| `VerticalAlignment` | `XStringFormat.LineAlignment` |
| `Padding` | Inset the text drawing rectangle |

**Font caching:** Create an `XFont` cache keyed by `(family, size, bold, italic)`. Detail bands reuse the same fonts thousands of times — avoid creating new `XFont` objects per element.

**Brush/pen caching:** Similarly cache `XBrush` and `XPen` objects by color/width.

### 8.4 Text Rendering with Clipping and Wrapping

```csharp
// Conceptual text rendering
void RenderText(XGraphics gfx, LayoutElement element, StyleDefinition style)
{
    var font = GetOrCreateFont(style);
    var brush = GetOrCreateBrush(style.ForegroundColor);
    var format = CreateStringFormat(style);

    // Apply padding
    var rect = new XRect(
        XUnit.FromInch(element.X + (style.Padding ?? 0)),
        XUnit.FromInch(element.Y + (style.Padding ?? 0)),
        XUnit.FromInch(element.Width - 2 * (style.Padding ?? 0)),
        XUnit.FromInch(element.Height - 2 * (style.Padding ?? 0)));

    // XTextFormatter handles word wrapping within the rectangle
    var formatter = new XTextFormatter(gfx);
    formatter.Alignment = MapAlignment(style.HorizontalAlignment);
    formatter.DrawString(element.Text ?? "", font, brush, rect);
}
```

`XTextFormatter` handles word wrapping within a bounding rectangle — exactly what's needed for multi-line text elements.

### 8.5 Image Rendering

```csharp
// Conceptual image rendering
void RenderImage(XGraphics gfx, LayoutElement element, byte[]? imageData, string? filePath)
{
    XImage image;
    if (imageData != null)
        image = XImage.FromStream(() => new MemoryStream(imageData));
    else if (filePath != null)
        image = XImage.FromFile(filePath);
    else
        return; // No image source — skip

    var rect = new XRect(
        XUnit.FromInch(element.X),
        XUnit.FromInch(element.Y),
        XUnit.FromInch(element.Width),
        XUnit.FromInch(element.Height));

    // ScaleToFit: maintain aspect ratio within bounds
    gfx.DrawImage(image, rect);
}
```

### 8.6 Proposed PdfExporter Skeleton

```csharp
// Conceptual structure — not final implementation code
public class PdfExporter : IReportExporter
{
    // Cache for frequently-used font/brush/pen objects
    private readonly Dictionary<(string, double, bool, bool), XFont> _fontCache = new();
    private readonly Dictionary<string, XSolidBrush> _brushCache = new();

    public async Task ExportAsync(
        IReadOnlyList<PageLayout> pages,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        var document = new PdfDocument();
        document.Info.Title = /* from ReportMetadata */;

        foreach (var pageLayout in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = document.AddPage();
            page.Width = XUnit.FromInch(/* pageWidth */);
            page.Height = XUnit.FromInch(/* pageHeight */);

            using var gfx = XGraphics.FromPdfPage(page);

            // Render backgrounds first, then content, then borders
            foreach (var element in pageLayout.Elements)
            {
                RenderElement(gfx, element);
            }
        }

        document.Save(outputStream);
    }
}
```

---

## 9. Properties and Types to Add

Based on this analysis, the following additions to the existing scaffolding are recommended:

### 9.1 New Enum

| Enum | Values | Where | Purpose |
|------|--------|-------|---------|
| `ImageScaleMode` | `ScaleToFit`, `Stretch`, `ActualSize` | `Rdf/` | Controls image scaling within element bounds |

### 9.2 New Properties on Existing Types

| Type | Property | Data Type | Purpose |
|------|----------|-----------|---------|
| `ElementDefinition` | `ImageScaleMode` | `ImageScaleMode` | How to scale images within element bounds |
| `StyleDefinition` | `BorderTop`, `BorderBottom`, `BorderLeft`, `BorderRight` | `string?` | Individual border sides (replace single `Border`) |
| `StyleDefinition` | `PaddingTop`, `PaddingBottom`, `PaddingLeft`, `PaddingRight` | `double?` | Individual padding sides (replace single `Padding`) |
| `ReportMetadata` | `DefaultFontFamily` | `string?` | Fallback font when style doesn't specify |
| `ReportMetadata` | `DefaultFontSize` | `double?` | Fallback font size |

### 9.3 New Internal Types (Implementation Phase)

| Type | Location | Purpose |
|------|----------|---------|
| `CoordinateTranslator` | `Export/` | Inches-to-XUnit conversion helpers |
| `PdfStyleResolver` | `Export/` | Maps `StyleDefinition` → PdfSharp `XFont`/`XBrush`/`XPen` with caching |
| `PdfElementRenderer` | `Export/` | Renders individual `LayoutElement` types to `XGraphics` |

### 9.4 NuGet Dependency

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| `PdfSharp` | 6.x | `Dabbler.Report` | PDF rendering |

**Note:** This adds a third-party dependency to `Dabbler.Report`. Per the project's architecture principles, the core `Dabbler.DataGrid` must remain dependency-free, but `Dabbler.Report` is a separate package where PDF library dependency is expected and acceptable (similar to how `Dabbler.DataGrid.Grpc` depends on gRPC packages).

---

## 10. Rendering Order and Z-Index

### 10.1 OpenPDF's Approach

OpenPDF renders elements in the order they are added to the document. Later elements draw on top of earlier ones. There is no explicit z-index — rendering order IS the z-order.

The `Background` band in JasperReports (from Task 1.2) is rendered first on each page specifically so that all other content draws on top of it.

### 10.2 Dabbler Rendering Order

The `PdfExporter` should render `LayoutElement` entries in a specific order to produce correct visual results:

1. **Background fills** — rectangles with fill color, no border
2. **Images** — behind text content
3. **Text elements** — StaticText and TextField
4. **Lines and bordered rectangles** — on top of everything
5. **Decorative elements** — underlining, strikethrough (if supported)

**Recommendation:** Add a `RenderLayer` property to `LayoutElement` (set by `BandLayoutEngine` during composition) to control rendering order:

```csharp
public enum RenderLayer
{
    Background = 0,
    Image = 1,
    Content = 2,
    Border = 3,
    Foreground = 4
}
```

Or more simply, ensure `BandLayoutEngine` emits elements in the correct rendering order within each `PageLayout.Elements` list, so the exporter can iterate sequentially.

---

## 11. Summary and Implementation Priority

### What to Build (Ordered)

1. **Add PdfSharp NuGet package** to `Dabbler.Report.csproj`
2. **Implement `PdfExporter`** — coordinate translation, element rendering loop, font/style caching
3. **Add `ImageScaleMode`** enum and property to `ElementDefinition`
4. **Extend `StyleDefinition`** with individual border/padding sides
5. **Add `ReportMetadata`** default font properties

### What NOT to Build

- No table abstraction — bands handle tabular layout
- No page event system — headers/footers are pre-composed by `BandLayoutEngine`
- No automatic layout engine — that's `BandLayoutEngine`'s job
- No MigraDoc — PdfSharp's coordinate API is the right level

### Architecture Validation

The analysis confirms a clean separation of concerns:

```
RDF JSON → RdfParser → ReportDefinition
                            ↓
Data Query → ReportDataEngine → DataTable
                            ↓
BandLayoutEngine.ComposePages(ReportDefinition, DataTable)
            ↓                           ↓
    List<PageLayout>              (page breaks, band
    with positioned               composition, expression
    LayoutElements                 resolution all done here)
            ↓
    PdfExporter.ExportAsync(pages, stream)
            ↓
    PdfSharp renders each element at its
    pre-computed position. No layout logic.
            ↓
        PDF file
```

The `PdfExporter` is intentionally simple: it's a coordinate renderer, not a layout engine. All the complex logic (page breaks, group processing, expression evaluation, text wrapping decisions) lives in `BandLayoutEngine`. This separation means the PDF export can be implemented and tested independently of the layout logic.

### Features Deferred to v2

- PDF/A compliance (archival format)
- PDF bookmarks / table of contents links
- Embedded hyperlinks
- Digital signatures
- Password protection
- `ElementType.Table` for inline embedded tables
