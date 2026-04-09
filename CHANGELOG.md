# Changelog

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
