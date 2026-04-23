# Changelog

## 1.1.1 — 2026-04-22

### Fixed
- `Document.NewPage()` no longer stacks the first two logical pages into a
  single physical page's content stream when the caller writes to
  `PdfWriter.DirectContent` before the first `NewPage()` call. Previously,
  a loop of the shape
  ```csharp
  for (int i = 0; i < pages.Count; i++) {
      if (i > 0) document.NewPage();
      renderer.RenderPage(writer.DirectContent, pages[i]);
  }
  ```
  produced a PDF whose first physical page contained composer pages 1 and
  2 concatenated in the same content stream, with page 2's full-page
  background fill painting over page 1's visible output. After the fix,
  each `RenderPage` call produces exactly one physical PDF page. Single-
  page callers (one `DirectContent` write followed by `Close()`, no
  `NewPage()`) and callers who invoke `NewPage()` before every page are
  unaffected. No public API changes.

### Added
- Rendering-visibility regression tests under
  `tests/DabblerPdf.Tests/Pdf/` that exercise per-physical-page text
  extraction via PdfPig, replacing the byte-grep pattern that could not
  catch overdraw or off-page positioning.
- `DabblerPdf.Samples/Samples/S11_OrdersStyleReport.cs` and the
  committed `Output/11-orders-style-report.pdf` artifact — a landscape
  multi-page sample that mirrors the `dabbler-platform` Orders By
  Retail Business report, exercising the DirectContent reportHeader
  pattern against 1.1.1. Companion tests
  (`tests/DabblerPdf.Tests/Pdf/OrdersStyleSampleRenderingTests.cs`)
  assert title visibility, color state, and "Page X of N" correctness
  per physical page via PdfPig.

### Verified
- 1.1.1 reproduces the real report's title-on-page-1 rendering pattern
  end-to-end inside DabblerPdf itself (no consumer repo needed). The
  PdfPig rendering-visibility assertions all pass on the Orders-style
  sample; see `DabblerPdf.Samples/Samples/S11_FINDINGS.md` for the
  per-assertion breakdown and the push recommendation.

## 1.1.0 — 2026-04-21

### Added
- `Document.AddTitle`, `AddAuthor`, `AddSubject`, `AddKeywords`, `AddCreator`
  for staging values into the PDF document information dictionary (`/Info`).
- `Document.AddCreationDate()` (auto-now), `Document.AddCreationDate(DateTime)`
  (explicit), and `Document.SuppressCreationDate()` (opt-out).
- `DabblerPdf.Utilities.PdfDate.Format(DateTime)` — formats dates per PDF 1.7
  §7.9.4 (`D:YYYYMMDDHHmmSSOHH'mm'`).
- `PdfName` constants: `Title`, `Author`, `Subject`, `Keywords`, `Creator`,
  `ModDate` (the last is reserved for a future modification-date API and is
  not yet written by `PdfWriter`).

### Changed
- `PdfWriter.Close()` now writes any staged metadata into the `/Info`
  dictionary alongside the existing `/Producer` entry.
- Documents that do not explicitly stage or suppress a creation date now
  receive `/CreationDate` set to UTC at close time. Callers that need the
  prior output shape (only `/Producer`) should call
  `Document.SuppressCreationDate()`.

### Behavioural notes
- `AddXxx(null)` throws `ArgumentNullException`. Empty or whitespace input
  is a silent no-op (returns `false`); the previously staged value, if any,
  is preserved.
- All metadata methods throw `InvalidOperationException` after the document
  is closed.
- Repeated calls follow last-write-wins.
- Existing 1.0.0 PDFs round-trip with no semantic change other than the new
  `/CreationDate` key. Tooling that asserts on byte-exact output will need
  to either suppress the creation date or accept the new key.

## 1.0.0 — 2026-04-09

Initial public release.

### Features
- PDF document creation with single and multi-page support
- Text rendering: Helvetica, Times New Roman, Courier font families
- Font styles: regular, bold, italic, bold italic
- Text wrapping: word-level and character-level breaking
- Text measurement API for layout composition (`ITextMeasurer`)
- Images: Stretch, ScaleToFit, ActualSize scale modes (PNG, JPEG)
- Lines: horizontal and vertical, solid/dashed/dotted, configurable weight and color
- Rectangles: fill, border, or both, with solid/dashed/dotted borders
- Colors: full hex color support (#RRGGBB and #AARRGGBB)
- Padding: uniform and per-side (top/bottom/left/right)
- Page headers and footers
- Page numbering with backfill ("Page X of Y")
- Coordinate-based rendering with inches-to-points translation
