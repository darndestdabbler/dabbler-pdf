# DabblerPdf Samples — Findings

> Generated during the `dabbler-pdf-samples` session set, Session 1 of 1.
> Date: 2026-04-09

## API Gaps Discovered

### 1. No Built-in Text Wrapping

The `PdfContentByte` API provides only low-level `ShowText()` for single-line rendering. There is no built-in word-wrapping or paragraph layout. The samples project implements manual word-wrapping in `SampleBase.WrapText()` using `BaseFont.GetWidthPoint()` for measurement.

**Impact:** Any NuGet consumer needing multi-line text will need to implement their own wrapping logic, or a higher-level text layout API should be added to DabblerPdf before public release.

### 2. No Built-in CanGrow / CanShrink

These are report-engine concepts. The PDF library has no notion of element containers that auto-size. The samples demonstrate these visually by manually calculating wrapped line counts and adjusting rectangle heights.

**Impact:** Expected — these are Dabbler.Report features, not PDF-level primitives.

### 3. No Built-in Headers / Footers

Multi-page documents require manually drawing header/footer content on each page. There is no page event or callback mechanism (unlike OpenPDF's `PdfPageEvent`).

**Impact:** The Dabbler.Report engine will handle this, but standalone NuGet consumers must draw headers/footers manually per page.

### 4. No "Page X of Y" Backfill

The total page count must be calculated before rendering begins. OpenPDF supports a `PdfTemplate` mechanism for deferred content (write a placeholder, fill in the total at close). DabblerPdf does not have this — the samples pre-calculate total pages.

**Impact:** Minor. Pre-calculation is straightforward for report-style documents.

### 5. No Rounded Rectangle Primitive

`PdfContentByte.Rectangle()` draws sharp corners only. Rounded corners would require manual Bezier curve construction. The S03 sample notes this limitation.

**Impact:** Low. Rounded corners are rarely needed in report output.

### 6. Image ScaleToFit Never Scales Up

`Image.ScaleToFit()` only scales down. If the image is smaller than the target area, it remains at its native pixel size. This is by design (matches OpenPDF behavior) but may surprise users expecting "fit to fill."

**Impact:** Documented behavior. Users can use `ScaleAbsolute()` for explicit sizing.

### 7. Special Characters / Encoding

WinAnsi encoding (Cp1252) covers most Western European characters but does not support the full Unicode range. Characters like the Euro sign (€), accented letters (é, ñ, ü), and em dash (—) work correctly. Characters outside the WinAnsi range are replaced with '?'.

**Impact:** Acceptable for the target use case (English-language business reports). Full Unicode would require Identity-H encoding with embedded TrueType fonts.

## Rendering Observations

- **Font rendering:** All 14 standard PDF fonts render correctly. Bold, italic, and bold-italic variants are visually distinct.
- **Line weights:** Hairline (0.25pt) through 4pt lines are visually distinct and correctly weighted.
- **Dash patterns:** Solid, dashed, and dotted patterns render as expected.
- **Colors:** RGB colors render accurately. Grayscale optimization (single-component G/g operators) works correctly.
- **Images:** PNG images load and render correctly. The three scale modes (Stretch, ScaleToFit, ActualSize) behave as documented. Missing image files throw `FileNotFoundException` — gracefully caught in the sample.
- **Multi-page:** Page breaks, per-page headers/footers, and "Page X of Y" all work correctly with pre-calculated page counts.
- **Stress test:** 1,000 rows across 50+ pages generate in under 1 second with a 46 KB file size. No memory issues observed.

## Recommendations for NuGet Release

1. **Add a high-level text layout helper** — Even a simple `DrawWrappedText(cb, text, font, size, x, y, maxWidth)` would significantly improve the standalone developer experience.
2. **Consider adding page events** — A callback on `NewPage()` would make headers/footers much easier for standalone consumers.
3. **Document the coordinate system prominently** — Bottom-left origin with Y-up is the #1 source of confusion for new PDF library users.
