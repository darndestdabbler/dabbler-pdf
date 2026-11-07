# Change Log: API Gap Enhancements

**Date**: 2026-04-09
**Session Set**: `api-gap-enhancements`
**Sessions**: 4

This session set addressed 7 API gaps discovered during the `dabbler-pdf-samples` visual testing session set. All changes are additive — no existing public APIs were modified or removed.

---

## 1. New Files Created

| File | Session | Purpose |
|------|---------|---------|
| `src/DabblerPdf/Text/TextLayout.cs` | 2 | Static utility class for text wrapping, measurement, and multi-line drawing |
| `src/DabblerPdf/Pdf/IPdfPageEvent.cs` | 3 | Interface for PDF document lifecycle callbacks (headers/footers) |
| `src/DabblerPdf/Pdf/PdfTemplate.cs` | 4 | Form XObject for deferred content (e.g., "Page X of Y") |
| `tests/DabblerPdf.Tests/Text/TextLayoutTests.cs` | 2 | 18 tests for TextLayout wrapping, measurement, and drawing |
| `tests/DabblerPdf.Tests/Pdf/PageEventTests.cs` | 3 | 9 tests for page event callback ordering and DirectContent drawing |
| `tests/DabblerPdf.Tests/Pdf/PdfTemplateTests.cs` | 4 | 10 tests for template creation, content, placement, and Page X of Y pattern |

## 2. Existing Files Modified

| File | Sessions | Changes |
|------|----------|---------|
| `src/DabblerPdf/Pdf/PdfContentByte.cs` | 1, 4 | Added `RoundRectangle()` (Bezier corner arcs), `AddTemplate()` (Form XObject placement), template-aware font registration in `SetFontAndSize` |
| `src/DabblerPdf/Image/Image.cs` | 1 | Added `ScaleToFill()` (scales up/down using `Math.Max` ratio) |
| `src/DabblerPdf/Pdf/PdfWriter.cs` | 3, 4 | Added `PageEvent` property, lifecycle hook integration (`OnOpenDocument`, `OnStartPage`, `OnEndPage`, `OnCloseDocument`), `CreateTemplate()`, `AddPageTemplate()`, template serialization in `Close()` |
| `src/DabblerPdf/Text/BaseFont.cs` | 4 | Added `CanEncode(char)` method, comprehensive XML docs on `WinAnsiEncoding`, `IdentityH`, and `CreateFont` |
| `tests/DabblerPdf.Tests/Text/BaseFontTests.cs` | 4 | Added `CanEncodeTests` class with 4 test methods (16 test cases) |
| `DabblerPdf.Samples/Samples/S01_BasicText.cs` | 4 | Added encoding support section demonstrating `CanEncode` |
| `DabblerPdf.Samples/Samples/S02_TextWrapping.cs` | 2 | Replaced manual wrapping with `TextLayout.WrapText`/`DrawText`, added alignment demos |
| `DabblerPdf.Samples/Samples/S03_LinesAndShapes.cs` | 1 | Added rounded rectangle demo |
| `DabblerPdf.Samples/Samples/S05_Images.cs` | 1 | Added `ScaleToFill` mode with clipping demo |
| `DabblerPdf.Samples/Samples/S07_MultiPage.cs` | 3, 4 | Refactored to use `IPdfPageEvent` for headers/footers, replaced pre-calculated page count with `PdfTemplate` pattern for "Page X of Y" |

## 3. New Public API

### `DabblerPdf.Text.TextLayout` (static class)

- `static List<string> WrapText(string text, BaseFont font, double fontSize, double maxWidth)` — Word-wraps text to fit within a width, handling explicit newlines
- `static double MeasureHeight(string text, BaseFont font, double fontSize, double maxWidth, double leading = 0)` — Measures total height of wrapped text (for CanGrow/CanShrink)
- `static int CountLines(string text, BaseFont font, double fontSize, double maxWidth)` — Counts wrapped lines
- `static double DrawText(PdfContentByte cb, string text, BaseFont font, double fontSize, double x, double y, double maxWidth, Alignment alignment = Alignment.Left, double leading = 0)` — Draws wrapped text with Left/Right/Center alignment, returns Y after last line
- `enum Alignment { Left, Right, Center }`

### `DabblerPdf.Pdf.IPdfPageEvent` (interface with default methods)

- `void OnOpenDocument(PdfWriter writer, Document document)` — Called once after document opens
- `void OnStartPage(PdfWriter writer, Document document)` — Called at start of each page
- `void OnEndPage(PdfWriter writer, Document document)` — Called at end of each page
- `void OnCloseDocument(PdfWriter writer, Document document)` — Called once before document closes

### `DabblerPdf.Pdf.PdfWriter`

- `IPdfPageEvent? PageEvent { get; set; }` — Page event handler property
- `PdfTemplate CreateTemplate(double width, double height)` — Creates a reusable Form XObject

### `DabblerPdf.Pdf.PdfContentByte`

- `void RoundRectangle(double x, double y, double width, double height, double radius)` — Appends a rounded rectangle path using Bezier arcs
- `void AddTemplate(PdfTemplate template, double x, double y)` — Places a PdfTemplate at the given PDF coordinates

### `DabblerPdf.Pdf.PdfTemplate`

- `double Width { get; }` — Template bounding box width
- `double Height { get; }` — Template bounding box height
- `PdfContentByte Content { get; }` — Content byte for deferred drawing

### `DabblerPdf.Image.Image`

- `void ScaleToFill(double targetWidth, double targetHeight)` — Scales to fill target area (scales up if needed), preserving aspect ratio

### `DabblerPdf.Text.BaseFont`

- `bool CanEncode(char c)` — Returns whether a character can be represented in the current encoding (WinAnsi)

## 4. Configuration Changes

None.

## 5. Test Coverage Summary

| Area | Tests Added | Session |
|------|-------------|---------|
| `RoundRectangle` | 7 | 1 |
| `ScaleToFill` | 5 | 1 |
| `TextLayout` (WrapText, MeasureHeight, CountLines, DrawText) | 18 | 2 |
| `IPdfPageEvent` (ordering, page numbers, DirectContent, null safety) | 9 | 3 |
| `PdfTemplate` (creation, content, placement, Form XObject, Page X of Y) | 10 | 4 |
| `CanEncode` (ASCII, accented, WinAnsi specials, CJK/emoji) | 4 methods (16 cases) | 4 |

**Final test count:** 562 passed, 0 failed (up from 495 at project start)

**All 10 visual samples pass after every session.**
