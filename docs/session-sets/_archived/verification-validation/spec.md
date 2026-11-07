# Session Set: DabblerPdf Verification & Validation

**Total Sessions:** 7
**Focus:** Independent code review and test gap analysis of the completed DabblerPdf port. Each session sends the source code to Gemini for cross-provider code review, then addresses findings and fills test coverage gaps.

**Approach:** This is a V&V session set — no new features. Each session:
1. Reads the source files in scope
2. Routes them to Gemini via `route(task_type="code-review")` for independent review
3. Addresses any issues found (bugs, style, correctness)
4. Adds tests for untested classes in scope
5. Build + test
6. End-of-session verification

---

## Session 1 of 7: Document Layer

**Scope:** `src/DabblerPdf/Document/` — BaseColor, Document, PageSize, Rectangle

**Steps:**

1. **Code review via ai-router** — Send all 4 source files to `route(task_type="code-review")` with context about the project purpose (OpenPDF port, coordinate-based rendering).
2. **Address findings** — Fix any bugs, correctness issues, or style problems identified.
3. **Test gap analysis** — PageSize and Rectangle have no unit tests. Review existing BaseColorTests and DocumentTests for completeness.
4. **Add missing tests:**
   - `PageSizeTests.cs` — verify standard dimensions (Letter, A4, etc.), immutability
   - `RectangleTests.cs` — construction, Width/Height properties, coordinate semantics
   - Extend existing test files if coverage gaps are found
5. **Build and test**

**Creates:**
- `tests/DabblerPdf.Tests/Document/PageSizeTests.cs`
- `tests/DabblerPdf.Tests/Document/RectangleTests.cs`

**Touches:**
- `src/DabblerPdf/Document/*.cs` (if fixes needed)
- `tests/DabblerPdf.Tests/Document/BaseColorTests.cs` (if gaps found)
- `tests/DabblerPdf.Tests/Document/DocumentTests.cs` (if gaps found)

---

## Session 2 of 7: PDF Primitives

**Scope:** `src/DabblerPdf/Pdf/` — PdfObject, PdfArray, PdfDictionary, PdfBoolean, PdfNull, PdfNumber, PdfName, PdfString, PdfLiteral

**Steps:**

1. **Code review via ai-router** — Send all 9 primitive type files for review. Context: these are the low-level PDF object types that everything else builds on. Correctness of serialization (WriteTo) is critical.
2. **Address findings** — Fix any issues, especially in PDF serialization format.
3. **Test gap analysis** — PdfObjectTests exists but PdfBoolean, PdfNull, PdfNumber, PdfName, PdfString, PdfLiteral have no dedicated tests. Check what PdfObjectTests already covers.
4. **Add missing tests:**
   - `PdfPrimitiveTests.cs` — dedicated tests for PdfBoolean, PdfNull, PdfNumber, PdfName, PdfString, PdfLiteral
   - Focus on serialization correctness (WriteTo output matches PDF spec format)
   - Edge cases: special characters in PdfString, name escaping in PdfName, number formatting
5. **Build and test**

**Creates:**
- `tests/DabblerPdf.Tests/Pdf/PdfPrimitiveTests.cs`

**Touches:**
- `src/DabblerPdf/Pdf/Pdf*.cs` (if fixes needed)
- `tests/DabblerPdf.Tests/Pdf/PdfObjectTests.cs` (if gaps found)

---

## Session 3 of 7: PDF Structure

**Scope:** `src/DabblerPdf/Pdf/` — PdfBody, PdfPage, PdfPages, PdfCatalog, PdfIndirectObject, PdfIndirectReference, PdfCrossReferenceTable, PdfTrailer

**Steps:**

1. **Code review via ai-router** — Send all 8 structural files. Context: these manage the PDF file structure — object numbering, page tree, cross-reference table, and trailer. Byte-level correctness matters.
2. **Address findings** — Fix any issues found, especially in xref table formatting and trailer output.
3. **Test gap analysis** — None of these classes have dedicated tests. They're exercised indirectly via integration tests but lack unit coverage.
4. **Add missing tests:**
   - `PdfStructureTests.cs` — unit tests for PdfBody (object numbering, offset tracking), PdfPage/PdfPages (tree structure), PdfCatalog (root dictionary), PdfIndirectObject/Reference (serialization), PdfCrossReferenceTable (entry formatting), PdfTrailer (output format)
5. **Build and test**

**Creates:**
- `tests/DabblerPdf.Tests/Pdf/PdfStructureTests.cs`

**Touches:**
- `src/DabblerPdf/Pdf/*.cs` (if fixes needed)

---

## Session 4 of 7: PDF Rendering

**Scope:** `src/DabblerPdf/Pdf/` — PdfContentByte, PdfStream, PdfWriter

**Steps:**

1. **Code review via ai-router** — Send PdfContentByte (551 lines, the main rendering API), PdfStream (compression), and PdfWriter (orchestrator). Context: PdfContentByte accumulates PDF operators for drawing; PdfWriter orchestrates the complete PDF output including fonts, images, and resources.
2. **Address findings** — These are the most complex files in the project. Pay attention to state management, resource handling, and operator correctness.
3. **Test gap analysis** — PdfContentByteTests and PdfStreamTests exist. PdfWriter is only tested via integration tests. Review existing tests for completeness.
4. **Add or extend tests:**
   - `PdfWriterTests.cs` — unit tests for PdfWriter (font registration, image handling, page lifecycle, resource dictionary construction)
   - Extend PdfContentByteTests if gaps found (graphics state save/restore, clipping, transform matrix)
   - Extend PdfStreamTests if gaps found (compression edge cases)
5. **Build and test**

**Creates:**
- `tests/DabblerPdf.Tests/Pdf/PdfWriterTests.cs`

**Touches:**
- `src/DabblerPdf/Pdf/PdfContentByte.cs` (if fixes needed)
- `src/DabblerPdf/Pdf/PdfStream.cs` (if fixes needed)
- `src/DabblerPdf/Pdf/PdfWriter.cs` (if fixes needed)
- `tests/DabblerPdf.Tests/Pdf/PdfContentByteTests.cs` (if gaps found)
- `tests/DabblerPdf.Tests/Pdf/PdfStreamTests.cs` (if gaps found)

---

## Session 5 of 7: Text & Fonts

**Scope:** `src/DabblerPdf/Text/` — BaseFont, Type1Font, TrueTypeFont, BuiltinFonts, PdfEncodings

**Steps:**

1. **Code review via ai-router** — Send all 5 text/font files. Context: BaseFont is the abstract factory; Type1Font wraps the 14 standard PDF fonts; TrueTypeFont parses .ttf files for embedding; BuiltinFonts has metrics for standard fonts; PdfEncodings handles WinAnsi (Windows-1252).
2. **Address findings** — Font metrics accuracy and TrueType parsing correctness are critical for text measurement.
3. **Test gap analysis** — BaseFontTests and TrueTypeFontTests exist. Type1Font, BuiltinFonts, and PdfEncodings have no dedicated tests.
4. **Add missing tests:**
   - `Type1FontTests.cs` — verify standard font creation, metric lookups, width calculations
   - `PdfEncodingsTests.cs` — WinAnsi encoding/decoding, special character handling, byte↔Unicode round-trip
   - `BuiltinFontsTests.cs` — spot-check metrics for a few standard fonts against known values
   - Extend existing tests if gaps found
5. **Build and test**

**Creates:**
- `tests/DabblerPdf.Tests/Text/Type1FontTests.cs`
- `tests/DabblerPdf.Tests/Text/PdfEncodingsTests.cs`
- `tests/DabblerPdf.Tests/Text/BuiltinFontsTests.cs`

**Touches:**
- `src/DabblerPdf/Text/*.cs` (if fixes needed)
- `tests/DabblerPdf.Tests/Text/BaseFontTests.cs` (if gaps found)
- `tests/DabblerPdf.Tests/Text/TrueTypeFontTests.cs` (if gaps found)

---

## Session 6 of 7: Image Handling

**Scope:** `src/DabblerPdf/Image/` — Image, Jpeg, PngImage, PdfImage

**Steps:**

1. **Code review via ai-router** — Send all 4 image files. Context: Image is the abstract base; Jpeg parses JPEG headers (SOF markers); PngImage parses PNG structure (IDAT decompression, filter removal, alpha separation); PdfImage creates PDF image XObjects.
2. **Address findings** — PNG parsing is the most complex (469 lines). Pay special attention to filter algorithms, alpha channel handling, and edge cases in malformed input.
3. **Test gap analysis** — ImageTests and ImageIntegrationTests exist. Jpeg has no dedicated tests. Review integration tests for completeness.
4. **Add or extend tests:**
   - `JpegTests.cs` — JPEG header parsing, dimension extraction, color space detection
   - Review ImageIntegrationTests for PNG edge cases (grayscale, palette, interlaced)
   - Extend existing tests if coverage gaps found
5. **Build and test**

**Creates:**
- `tests/DabblerPdf.Tests/Image/JpegTests.cs`

**Touches:**
- `src/DabblerPdf/Image/*.cs` (if fixes needed)
- `tests/DabblerPdf.Tests/Image/ImageTests.cs` (if gaps found)
- `tests/DabblerPdf.Tests/Image/ImageIntegrationTests.cs` (if gaps found)

---

## Session 7 of 7: Cross-Cutting Review & Final Validation

**Scope:** Full solution — integration tests, CoordinateTranslator, MvpValidationTests, cross-cutting concerns

**Steps:**

1. **Security review via ai-router** — Send the full source tree (all 33 source files) for `route(task_type="security-review")`. Focus on: input validation, buffer handling in PNG/JPEG parsing, stream handling, potential DoS vectors (decompression bombs, malformed fonts).
2. **Address findings** — Fix any security issues identified.
3. **Integration test review** — Review DrawingIntegrationTests, TextIntegrationTests, ImageIntegrationTests, and MvpValidationTests for completeness. Ensure they cover:
   - Multi-page documents
   - Mixed content (text + images + graphics on same page)
   - Font embedding with content
   - Error paths (invalid inputs, missing fonts)
4. **CoordinateTranslator review** — Verify CoordinateTranslatorTests covers all conversion scenarios.
5. **Add any final tests** to close remaining gaps.
6. **Final build and full test run**
7. **Generate change-log.md** — Document all changes made across all 7 sessions.

**Creates:**
- `docs/session-sets/verification-validation/change-log.md`

**Touches:**
- Any source or test file that needs fixes
- `tests/DabblerPdf.Tests/Utilities/CoordinateTranslatorTests.cs` (if gaps found)
- `tests/DabblerPdf.Tests/MvpValidationTests.cs` (if gaps found)
