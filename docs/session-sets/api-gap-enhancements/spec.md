# DabblerPdf API Gap Enhancements — Implementation Spec

> **Purpose:** Address the 7 API gaps discovered during the `dabbler-pdf-samples` visual testing session set (see `DabblerPdf.Samples/FINDINGS.md`).
> **Created:** 2026-04-09
> **Repo:** dabbler-pdf

---

## Gap Summary

The visual samples project discovered these gaps in the DabblerPdf public API:

| # | Gap | Severity | Addressed In |
|---|-----|----------|-------------|
| 1 | No built-in text wrapping | High | Session 2 |
| 2 | No built-in CanGrow / CanShrink | N/A — report-engine concern | Session 2 (via `MeasureHeight`) |
| 3 | No built-in headers / footers (page events) | High | Session 3 |
| 4 | No "Page X of Y" backfill (PdfTemplate) | Medium | Session 4 |
| 5 | No rounded rectangle primitive | Low | Session 1 |
| 6 | Image ScaleToFit never scales up | Low | Session 1 |
| 7 | Special characters / encoding limits | Medium | Session 4 |

**Design principle:** These are library-level enhancements to DabblerPdf's public API. They do NOT add report-engine concepts (bands, detail rows, group headers). They give standalone NuGet consumers — and the Dabbler.Report engine — better primitives to build on.

---

## Session Plan

### Session 1 of 4: PdfContentByte & Image Enhancements

**Addresses:** Gaps #5 (rounded rectangle), #6 (image scale-up)

#### Step 1: Add `RoundRectangle` to `PdfContentByte`

Add a public method to `PdfContentByte` that draws a rectangle with rounded corners using Bézier curves (the `CurveTo` method already exists).

```csharp
// PdfContentByte.cs
/// <summary>
/// Appends a rectangle with rounded corners to the current path.
/// Uses cubic Bézier arcs at each corner.
/// </summary>
/// <param name="x">Lower-left X coordinate in points.</param>
/// <param name="y">Lower-left Y coordinate in points.</param>
/// <param name="width">Width in points.</param>
/// <param name="height">Height in points.</param>
/// <param name="radius">Corner radius in points. Clamped to half the smaller dimension.</param>
public void RoundRectangle(double x, double y, double width, double height, double radius)
```

**Implementation notes:**
- Clamp `radius` to `Math.Min(width / 2, height / 2)` to prevent invalid geometry.
- Use the standard Bézier approximation for quarter circles: control point offset = `radius * 0.5523` (the magic number `4 * (sqrt(2) - 1) / 3`).
- Construct the path: `MoveTo` → 4 sides, each with `LineTo` + `CurveTo` for the corner arc → `ClosePath`.
- The path is appended but NOT painted — the caller uses `Stroke()`, `Fill()`, or `FillStroke()` as usual, matching the `Rectangle()` API pattern.

#### Step 2: Add `ScaleToFill` to `Image`

Add a method that scales an image to fill the target area (scaling up if needed), preserving aspect ratio. The image may overflow one dimension.

```csharp
// Image.cs
/// <summary>
/// Scales the image to fill the given dimensions while preserving the aspect ratio.
/// Unlike <see cref="ScaleToFit"/>, this will scale up if the image is smaller
/// than the target area. The image fills the entire target area — one dimension
/// matches exactly, the other may exceed the target bounds.
/// </summary>
public void ScaleToFill(double targetWidth, double targetHeight)
```

**Implementation notes:**
- Compute `widthRatio = targetWidth / ScaledWidth` and `heightRatio = targetHeight / ScaledHeight`.
- Use `Math.Max(widthRatio, heightRatio)` (vs. `Math.Min` in `ScaleToFit`).
- Apply the ratio unconditionally (removes the `if (ratio < 1.0)` guard from `ScaleToFit`).

#### Step 3: Unit tests

- `RoundRectangle`: verify PDF operators emitted (moveto, lineto, curveto, closepath), radius clamping, zero radius degenerates to a regular rectangle.
- `ScaleToFill`: verify scaling up, scaling down, exact fit, asymmetric aspect ratios, and that aspect ratio is preserved.

#### Step 4: Update samples

- **S03_LinesAndShapes**: Add a rounded rectangle to the Section 4 rectangle row (labeled "Rounded").
- **S05_Images**: Add a `ScaleToFill` demo alongside the existing Stretch/ScaleToFit/ActualSize demos.
- Rebuild and verify all 10 samples still pass.

**Creates:** No new files — edits to existing source files  
**Touches:** `PdfContentByte.cs`, `Image.cs`, test files, `S03_LinesAndShapes.cs`, `S05_Images.cs`

---

### Session 2 of 4: Text Layout API

**Addresses:** Gap #1 (text wrapping), Gap #2 (CanGrow/CanShrink measurement)

#### Step 1: Design the `TextLayout` class

Create `src/DabblerPdf/Text/TextLayout.cs` in the `DabblerPdf.Text` namespace. This is a stateless utility class (all static methods) that provides text wrapping, measurement, and multi-line drawing.

```csharp
namespace DabblerPdf.Text;

/// <summary>
/// Provides text wrapping, measurement, and multi-line rendering utilities.
/// Works with any <see cref="BaseFont"/> and renders via <see cref="PdfContentByte"/>.
/// </summary>
public static class TextLayout
{
    /// <summary>Text alignment options for multi-line rendering.</summary>
    public enum Alignment { Left, Right, Center }

    /// <summary>
    /// Breaks text into lines that fit within the given width.
    /// Handles explicit line breaks (\n) and word-wrapping at word boundaries.
    /// </summary>
    /// <returns>A list of lines, each fitting within <paramref name="maxWidth"/> points.</returns>
    public static List<string> WrapText(string text, BaseFont font, double fontSize, double maxWidth)

    /// <summary>
    /// Measures the total height (in points) that wrapped text would occupy.
    /// Useful for CanGrow / CanShrink calculations.
    /// </summary>
    /// <param name="leading">Line spacing in points. If 0, defaults to fontSize * 1.2.</param>
    /// <returns>Total height in points including all wrapped lines.</returns>
    public static double MeasureHeight(string text, BaseFont font, double fontSize,
        double maxWidth, double leading = 0)

    /// <summary>
    /// Counts the number of wrapped lines.
    /// </summary>
    public static int CountLines(string text, BaseFont font, double fontSize, double maxWidth)

    /// <summary>
    /// Draws wrapped text onto a PdfContentByte at the specified position.
    /// Text flows downward from (x, y) in PDF coordinates (bottom-left origin).
    /// </summary>
    /// <param name="x">Left edge X position in points.</param>
    /// <param name="y">Top of first line Y position in points (PDF coords — y decreases downward).</param>
    /// <param name="maxWidth">Maximum width for wrapping in points.</param>
    /// <param name="alignment">Text alignment within the maxWidth column.</param>
    /// <param name="leading">Line spacing in points. If 0, defaults to fontSize * 1.2.</param>
    /// <returns>The Y position after the last line (in PDF coordinates).</returns>
    public static double DrawText(PdfContentByte cb, string text, BaseFont font, double fontSize,
        double x, double y, double maxWidth,
        Alignment alignment = Alignment.Left, double leading = 0)
}
```

#### Step 2: Implement word wrapping

The wrapping algorithm:
1. Split on `\n` to get paragraphs.
2. For each paragraph, split on spaces to get words.
3. Accumulate words into a line until the next word would exceed `maxWidth` (measured via `BaseFont.GetWidthPoint`).
4. When a single word exceeds `maxWidth`, include it as its own line (no character-level breaking needed for v1).
5. Empty paragraphs (from `\n\n`) produce a blank line.

#### Step 3: Implement aligned drawing

For `DrawText`:
1. Call `WrapText` to get lines.
2. For each line, calculate the X offset based on alignment:
   - Left: `x` (no offset)
   - Right: `x + maxWidth - lineWidth`
   - Center: `x + (maxWidth - lineWidth) / 2`
3. Use `cb.BeginText()`, `SetFontAndSize`, `SetTextMatrix`, `ShowText`, `EndText` for each line.
4. Advance Y by `leading` after each line.
5. Return the final Y position.

#### Step 4: Unit tests

- `WrapText`: short text (no wrap), exact-fit text, multi-word wrapping, single long word, explicit `\n` and `\n\n`, empty string, very narrow width.
- `MeasureHeight`: compare against `CountLines * leading`, verify default leading = `fontSize * 1.2`.
- `DrawText`: verify correct PDF operators emitted for each alignment mode, verify returned Y position.

#### Step 5: Update samples

- **S02_TextWrapping**: Replace the manual `SampleBase.WrapText` calls with `TextLayout.WrapText` and `TextLayout.DrawText`. Demonstrate all three alignments.
- **SampleBase**: Consider replacing the internal `WrapText` / `DrawWrappedText` helpers with calls to `TextLayout` (or leave them as-is since they work — the samples are consumers, not the API).
- Rebuild and verify all 10 samples still pass.

**Creates:** `src/DabblerPdf/Text/TextLayout.cs`, test file  
**Touches:** `S02_TextWrapping.cs`, possibly `SampleBase.cs`

---

### Session 3 of 4: Page Events

**Addresses:** Gap #3 (no built-in headers / footers)

#### Step 1: Define `IPdfPageEvent` interface

Create `src/DabblerPdf/Pdf/IPdfPageEvent.cs`:

```csharp
namespace DabblerPdf.Pdf;

/// <summary>
/// Callback interface for PDF document lifecycle events.
/// Implement this interface and assign to <see cref="PdfWriter.PageEvent"/>
/// to receive notifications when pages are created.
/// </summary>
public interface IPdfPageEvent
{
    /// <summary>Called once after the document is opened.</summary>
    void OnOpenDocument(PdfWriter writer, Document.Document document) { }

    /// <summary>
    /// Called at the start of each page, before any content is drawn.
    /// Use this to draw page headers, background elements, or watermarks.
    /// </summary>
    void OnStartPage(PdfWriter writer, Document.Document document) { }

    /// <summary>
    /// Called at the end of each page, just before the page content is finalized.
    /// Use this to draw page footers or page numbers.
    /// </summary>
    void OnEndPage(PdfWriter writer, Document.Document document) { }

    /// <summary>Called once after the document is closed.</summary>
    void OnCloseDocument(PdfWriter writer, Document.Document document) { }
}
```

**Design notes:**
- Use default interface methods (C# 8+) so implementers only override what they need.
- Matches the OpenPDF `PdfPageEvent` pattern for familiarity.
- The writer provides `DirectContent` for drawing; the document provides `PageNumber` and `PageSize`.

#### Step 2: Add `PageEvent` property to `PdfWriter`

```csharp
// PdfWriter.cs
/// <summary>
/// Gets or sets the page event handler. Set before calling <see cref="Document.Document.Open"/>.
/// </summary>
public IPdfPageEvent? PageEvent { get; set; }
```

#### Step 3: Integrate callbacks into the Document/PdfWriter lifecycle

Wire the callbacks into existing methods:

- **`PdfWriter.Open()`** — After writing the PDF header, call `PageEvent?.OnOpenDocument(this, Document)`.
- **`PdfWriter.AddPage()`** — At the start of `AddPage`, if a previous page exists, call `PageEvent?.OnEndPage(this, Document)` (for the previous page), then after setting up the new page, call `PageEvent?.OnStartPage(this, Document)`.
- **`PdfWriter.Close()`** — Before finalizing, call `PageEvent?.OnEndPage(this, Document)` for the last page, then `PageEvent?.OnCloseDocument(this, Document)`.

**Critical:** The `OnEndPage` for page N must be called BEFORE the content stream for page N is flushed. This allows the event handler to draw footer content into `DirectContent` that gets included on that page.

#### Step 4: Unit tests

- Verify callback ordering: Open → StartPage(1) → EndPage(1) → StartPage(2) → EndPage(2) → Close.
- Verify `DirectContent` drawing in `OnStartPage` appears on the correct page.
- Verify `DirectContent` drawing in `OnEndPage` appears on the correct page.
- Verify `document.PageNumber` is correct inside each callback.
- Verify null `PageEvent` doesn't break anything (existing tests should still pass).

#### Step 5: Update samples

- **S07_MultiPage**: Refactor to use an `IPdfPageEvent` implementation for headers and footers instead of the manual `DrawPageHeader` / `DrawPageFooter` calls. The page event handler draws the header/footer, and the main loop only generates detail rows and calls `doc.NewPage()` when needed.
- Rebuild and verify all 10 samples still pass.

**Creates:** `src/DabblerPdf/Pdf/IPdfPageEvent.cs`, test file  
**Touches:** `PdfWriter.cs`, `S07_MultiPage.cs`

---

### Session 4 of 4: PdfTemplate & Encoding Documentation

**Addresses:** Gap #4 (Page X of Y backfill), Gap #7 (encoding limitations)

#### Step 1: Implement `PdfTemplate`

Create `src/DabblerPdf/Pdf/PdfTemplate.cs`. A `PdfTemplate` is a Form XObject (a reusable content block) that can be placed on a page but have its content filled in later — after all pages are known.

```csharp
namespace DabblerPdf.Pdf;

/// <summary>
/// A reusable content block (PDF Form XObject) whose content can be
/// written after placement. Used for deferred content like "Page X of Y"
/// where the total page count isn't known until all pages are generated.
/// </summary>
public class PdfTemplate
{
    /// <summary>Width of the template bounding box in points.</summary>
    public double Width { get; }

    /// <summary>Height of the template bounding box in points.</summary>
    public double Height { get; }

    /// <summary>
    /// Gets the content byte for drawing into this template.
    /// Content can be written at any time before the document is closed.
    /// </summary>
    public PdfContentByte Content { get; }

    /// <summary>
    /// Creates a new template with the given dimensions.
    /// </summary>
    internal PdfTemplate(PdfWriter writer, double width, double height)
}
```

#### Step 2: Add template support to `PdfWriter`

```csharp
// PdfWriter.cs
/// <summary>
/// Creates a new PdfTemplate (Form XObject) that can be placed on pages
/// and filled in later.
/// </summary>
public PdfTemplate CreateTemplate(double width, double height)
```

#### Step 3: Add template placement to `PdfContentByte`

```csharp
// PdfContentByte.cs
/// <summary>
/// Places a PdfTemplate at the given position on the current page.
/// The template content can be written at any time before document close.
/// </summary>
public void AddTemplate(PdfTemplate template, double x, double y)
```

**Implementation notes:**
- `PdfTemplate` is backed by a PDF Form XObject (Type `/XObject`, Subtype `/Form`).
- `CreateTemplate` allocates an indirect object reference for the template.
- `AddTemplate` registers the template as a page resource (like images) and emits `q` → `cm` → `/TplN Do` → `Q`.
- The template's content stream is written to its backing `PdfContentByte`.
- At `Close()` time, `PdfWriter` serializes each template's content byte into its Form XObject stream, which gets written to the PDF file. Since indirect object references are forward-referenced, the content can be filled in after placement.

#### Step 4: "Page X of Y" convenience pattern

This is a usage pattern, not a separate class. Document it and demonstrate it in the sample:

```csharp
// Create template for total page count
var totalPagesTemplate = writer.CreateTemplate(30, 12);

// On each page, draw "Page N of " and place the template
cb.BeginText();
cb.ShowText($"Page {doc.PageNumber} of ");
cb.EndText();
cb.AddTemplate(totalPagesTemplate, x, y);

// After all pages are generated, fill in the template
totalPagesTemplate.Content.BeginText();
totalPagesTemplate.Content.SetFontAndSize(font, 9);
totalPagesTemplate.Content.SetTextMatrix(0, 0);
totalPagesTemplate.Content.ShowText(doc.PageNumber.ToString());
totalPagesTemplate.Content.EndText();
```

#### Step 5: Document encoding limitations

Add XML doc comments to `BaseFont.WinAnsiEncoding` and `CreateFont` that clearly explain the encoding limitations:

```csharp
/// <summary>
/// Windows-1252 encoding. Supports ASCII plus Western European characters
/// including accented letters (é, ñ, ü), the Euro sign (€), em/en dashes,
/// and typographic quotes. Characters outside this range are replaced with '?'.
///
/// For full Unicode support, use <see cref="IdentityH"/> encoding with an
/// embedded TrueType font. (Note: Identity-H support is planned for a future release.)
/// </summary>
public const string WinAnsiEncoding = "Cp1252";
```

Add a public helper method to `BaseFont` for checking character support:

```csharp
/// <summary>
/// Returns whether the given character can be represented in the current encoding.
/// For WinAnsi fonts, returns true for characters in the Windows-1252 range.
/// </summary>
public bool CanEncode(char c)
```

#### Step 6: Unit tests

- `PdfTemplate`: verify template creation, content writing, placement on multiple pages, content written after placement still appears.
- `CanEncode`: verify returns true for ASCII, accented chars, Euro sign; false for CJK, emoji.
- Integration test: "Page X of Y" pattern with template produces correct output.

#### Step 7: Update samples

- **S07_MultiPage**: Replace the pre-calculated page count with a `PdfTemplate` pattern so "Page X of Y" is filled in at close time. Combine with the page events from Session 3 for a clean implementation.
- **S01_BasicText**: Add a note in the special characters section about encoding limits and the `CanEncode` method.
- Rebuild and verify all 10 samples still pass.

**Creates:** `src/DabblerPdf/Pdf/PdfTemplate.cs`, test file  
**Touches:** `PdfWriter.cs`, `PdfContentByte.cs`, `BaseFont.cs`, `S07_MultiPage.cs`, `S01_BasicText.cs`

---

## Cross-Cutting Concerns

### Coordinate System

All new APIs use PDF coordinates (bottom-left origin, Y-up, units in points), matching the existing `PdfContentByte` API. The `TextLayout.DrawText` method documents its `y` parameter as "the baseline of the first line" and returns the Y position after the last line so callers can continue placing content below.

### API Consistency

- New methods on `PdfContentByte` follow the existing pattern: append to the content stream, don't paint automatically.
- New methods on `Image` follow the existing scaling API pattern.
- `TextLayout` is stateless to match the library's low-level philosophy — no retained objects, no layout engine.

### Backward Compatibility

All changes are additive. No existing public APIs are modified or removed. All 495 existing tests must continue to pass after each session.

### Test Coverage

Each session adds unit tests for the new APIs. Existing integration tests (`MvpValidationTests`, `TextIntegrationTests`, `DrawingIntegrationTests`) must remain green.

### Sample Updates

Each session updates the relevant samples to exercise the new APIs, providing visual validation alongside unit tests. All 10 samples must continue to pass after each session.

---

## Files Modified Across All Sessions

### New Files
| File | Session | Description |
|------|---------|-------------|
| `src/DabblerPdf/Text/TextLayout.cs` | 2 | Text wrapping, measurement, multi-line drawing |
| `src/DabblerPdf/Pdf/IPdfPageEvent.cs` | 3 | Page lifecycle event interface |
| `src/DabblerPdf/Pdf/PdfTemplate.cs` | 4 | Form XObject for deferred content |

### Modified Files
| File | Sessions | Changes |
|------|----------|---------|
| `src/DabblerPdf/Pdf/PdfContentByte.cs` | 1, 4 | `RoundRectangle`, `AddTemplate` |
| `src/DabblerPdf/Image/Image.cs` | 1 | `ScaleToFill` |
| `src/DabblerPdf/Pdf/PdfWriter.cs` | 3, 4 | `PageEvent`, `CreateTemplate`, lifecycle hooks |
| `src/DabblerPdf/Text/BaseFont.cs` | 4 | `CanEncode`, improved XML docs |
| `DabblerPdf.Samples/Samples/S02_TextWrapping.cs` | 2 | Use `TextLayout` API |
| `DabblerPdf.Samples/Samples/S03_LinesAndShapes.cs` | 1 | Rounded rectangle demo |
| `DabblerPdf.Samples/Samples/S05_Images.cs` | 1 | `ScaleToFill` demo |
| `DabblerPdf.Samples/Samples/S07_MultiPage.cs` | 3, 4 | Page events, PdfTemplate |
