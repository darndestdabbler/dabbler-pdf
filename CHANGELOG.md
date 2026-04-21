# Changelog

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
