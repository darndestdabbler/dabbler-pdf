# DabblerPdf Verification & Validation Change Log

## Summary

This document details the changes made during a 7-session Verification & Validation (V&V) cycle for the DabblerPdf library. The cycle focused on independent code review, security hardening, bug fixing, and increasing test coverage. No new features were added. The process involved routing source code to AI for review, addressing findings, and systematically closing gaps in the unit and integration test suite. This effort significantly improved the library's robustness, security, and maintainability, increasing the total test count from 204 to 495.

## Sessions Overview

| Session | Title                      | Scope                                  | Key Changes & Additions                                                                                                  |
| :------ | :------------------------- | :------------------------------------- | :----------------------------------------------------------------------------------------------------------------------- |
| 1       | Document Layer             | `Document/*`                           | Fixed `Document.Close()` bug. Added `RectangleTests`, `PageSizeTests`. Increased test coverage for `BaseColor` and `Document`. |
| 2       | PDF Primitives             | `Pdf/*` (primitives)                   | Fixed critical `PdfNumber` scientific notation bug. Hardened `PdfArray`, `PdfString`, `PdfName`. Added 61 primitive tests. |
| 3       | PDF Structure              | `Pdf/*` (structure)                    | Made `PdfPages` immutable after completion. Optimized `PdfCrossReferenceTable`. Added 21 structural tests.               |
| 4       | PDF Rendering              | `PdfContentByte`, `PdfStream`, `PdfWriter` | Fixed zlib compression. Added image deduplication. Added `PdfWriterTests` and extended `PdfContentByte` coverage.      |
| 5       | Text & Fonts               | `Text/*`                               | Made font cache thread-safe. Fixed Symbol/ZapfDingbats encoding. Added TTF parser bounds checking. Added 48 font tests.    |
| 6       | Image Handling             | `Image/*`                              | Hardened JPEG and PNG parsers against malformed data. Fixed `ScaleToFit` logic. Added 22 `JpegTests`.                    |
| 7       | Security & Final Validation | Full Solution                          | Applied 8 security fixes (decompression bomb, integer overflow, TTF parsing). Validated all fixes with new tests.        |

## Source File Changes

-   **`BaseColor.cs`**: Added sanitization for non-finite double values (`Infinity`/`NaN`) to prevent exceptions.
-   **`BaseFont.cs`**: Replaced `Dictionary` with `ConcurrentDictionary` for the internal font cache to ensure thread safety.
-   **`BuiltinFonts.cs`**: Added missing width metrics for Times-Roman, Times-Bold, Times-Italic, and Times-BoldItalic fonts for characters 161-191.
-   **`Document.cs`**: Fixed state management in `Close()` to prevent potential resource leaks if `Open()` was never called.
-   **`Image.cs`**: Corrected `ScaleToFit` logic to prevent upscaling an image when not desired.
-   **`Jpeg.cs`**: Added validation for marker segment lengths to prevent reading past segment boundaries or the end of the file.
-   **`PdfArray.cs`**: Added `null` checks to prevent `null` elements from being added to the array.
-   **`PdfBoolean.cs`**: Made constructor private to enforce the singleton pattern.
-   **`PdfContentByte.cs`**: Added escaping for control characters (`(`, `)`, `\`, etc.) in PDF strings.
-   **`PdfCrossReferenceTable.cs`**: Optimized by caching byte arrays for free cross-reference entries, reducing allocations in a loop.
-   **`PdfImage.cs`**: Prevented creation of a redundant `/SoftMask` dictionary when one is already provided.
-   **`PdfName.cs`**: Corrected non-ASCII character encoding to use UTF-8 bytes as required by the PDF specification.
-   **`PdfNull.cs`**: Made constructor private and removed a redundant `WriteTo` override.
-   **`PdfNumber.cs`**: Changed `double`-to-`string` formatting to prevent scientific notation, which is invalid in the PDF spec.
-   **`PdfPages.cs`**: Added an idempotency guard to the `Complete()` method to prevent mutation after the page tree is finalized.
-   **`PdfStream.cs`**: Switched from `DeflateStream` to `ZLibStream` for spec-compliant Flate compression.
-   **`PdfString.cs`**: The `OriginalBytes` property now returns a defensive copy to prevent internal state mutation. Optimized the `WriteHex` method.
-   **`PdfWriter.cs`**: Implemented image deduplication via a document-level cache to reduce file size. Throws `ArgumentException` for unregisterable fonts instead of failing silently.
-   **`PngImage.cs`**: Implemented security hardening, including decompression bomb protection (50MB limit), dimension limits, and checked arithmetic to prevent integer overflows. Fixed sub-byte pixel stride calculation and now throws an exception on truncated data chunks.
-   **`TrueTypeFont.cs`**: Added bounds checking and validation logic to the `TtfReader` to handle malformed table counts and `cmap` segments, preventing parsing errors and potential DoS vectors.
-   **`Type1Font.cs`**: Correctly omits the `/Encoding` entry for Symbol and ZapfDingbats fonts per the PDF specification.

## New Test Files Created

-   `tests/DabblerPdf.Tests/Document/PageSizeTests.cs` (5 tests)
-   `tests/DabblerPdf.Tests/Document/RectangleTests.cs` (4 tests)
-   `tests/DabblerPdf.Tests/Image/ImageTestHelpers.cs` (Helper class for test image generation)
-   `tests/DabblerPdf.Tests/Image/JpegTests.cs` (22 tests)
-   `tests/DabblerPdf.Tests/Pdf/PdfPrimitiveTests.cs` (61 tests)
-   `tests/DabblerPdf.Tests/Pdf/PdfStructureTests.cs` (21 tests)
-   `tests/DabblerPdf.Tests/Pdf/PdfWriterTests.cs` (21 tests)
-   `tests/DabblerPdf.Tests/Text/BuiltinFontsTests.cs` (14 tests)
-   `tests/DabblerPdf.Tests/Text/PdfEncodingsTests.cs` (16 tests)
-   `tests/DabblerPdf.Tests/Text/Type1FontTests.cs` (18 tests)

## Existing Test Files Extended

-   **`tests/DabblerPdf.Tests/Document/BaseColorTests.cs`**: 7 new tests for hash code consistency, equality, and handling of non-finite and out-of-range values.
-   **`tests/DabblerPdf.Tests/Document/DocumentTests.cs`**: 4 new integration tests covering page numbering and state management edge cases (`Close()` idempotency, new page after close).
-   **`tests/DabblerPdf.Tests/Image/ImageTests.cs`**: 6 new tests covering `ScaleToFit` edge cases and validation of security fixes for PNG/JPEG parsing.
-   **`tests/DabblerPdf.Tests/Pdf/PdfContentByteTests.cs`**: 18 new tests for control character escaping, clipping paths, and additional text/graphics state operators.
-   **`tests/DabblerPdf.Tests/Pdf/PdfStreamTests.cs`**: 2 new tests for round-trip decompression correctness and handling of empty content.

## Security Hardening (Session 7)

-   **PNG Decompression Bomb**: Limited decompressed ZLIB stream output to 50MB to prevent resource exhaustion from malicious images (`PngImage.cs`).
-   **PNG Dimension Validation**: Enforced maximum image dimensions of 32,767 pixels and a total pixel count of 100 million to prevent denial-of-service via excessive memory allocation (`PngImage.cs`).
-   **PNG Integer Overflow**: Added `checked` arithmetic to pixel processing loops to prevent overflows when calculating buffer offsets with malicious image dimensions (`PngImage.cs`).
-   **JPEG Bounds Validation**: Added checks to ensure JPEG marker segment lengths do not exceed the remaining file length, preventing out-of-bounds reads (`Jpeg.cs`).
-   **TTF Table Count Validation**: Validated the `numTables` field in TrueType font headers against the file size to prevent reading past font data boundaries (`TrueTypeFont.cs`).
-   **TTF Cmap Robustness**: Added guards to skip malformed or excessively large segments in `cmap` formats 4 and 12, preventing potential infinite loops or denial-of-service during font parsing (`TrueTypeFont.cs`).
-   **Non-Finite Color Component Handling**: Added sanitization to `BaseColor` to handle `double.NaN` and `double.Infinity`, preventing exceptions in downstream rendering code.

## Bug Fixes

-   **`PdfNumber` Scientific Notation**: `PdfNumber` no longer generates PDF-invalid scientific notation for `double` values.
-   **`Document` State Management**: Fixed a bug where calling `Close()` on a `Document` without `Open()` being called first could lead to inconsistent state.
-   **ZLIB vs. DEFLATE Compression**: Switched `PdfStream` to use the correct ZLIB wrapper instead of raw DEFLATE for `FlateDecode` compression.
-   **Symbol/ZapfDingbats Encoding**: Corrected `Type1Font` creation to omit the `/Encoding` dictionary for these special fonts, as required by the PDF specification.
-   **`PdfArray` Null Elements**: `PdfArray` now correctly throws an `ArgumentNullException` if a `null` element is added.
-   **`PdfString` Defensive Copying**: The `OriginalBytes` property on `PdfString` now returns a clone, preventing external mutation of the internal state.
-   **`PngImage` Sub-Byte Stride**: Fixed a calculation error for the stride of PNG images with bit depths less than 8 (e.g., 1, 2, 4-bit).
-   **`Image.ScaleToFit` Upscaling**: `ScaleToFit` logic was corrected to not upscale an image when the `scaleUp` parameter is `false`.
-   **`PngImage` Truncated Chunks**: The PNG parser now throws an exception when encountering a truncated data chunk instead of failing silently.
-   **Non-ASCII `PdfName` Encoding**: Fixed `PdfName` to correctly handle non-ASCII characters by encoding their UTF-8 bytes.

## Test Coverage Summary

-   **Initial Test Count**: 204
-   **Final Test Count**: 495
-   **Total Tests Added**: 291

This V&V cycle established dedicated test coverage for previously untested areas, including:
-   Document structure (`PageSize`, `Rectangle`).
-   All low-level PDF primitive types (`PdfBoolean`, `PdfNull`, `PdfNumber`, `PdfName`, `PdfString`, `PdfLiteral`).
-   Core PDF file structure classes (`PdfPage`, `PdfPages`, `PdfCatalog`).
-   The main orchestration class, `PdfWriter`.
-   Font and text components (`Type1Font`, `PdfEncodings`, `BuiltinFonts`).
-   JPEG image header parsing (`Jpeg.cs`).

## Declined Changes

The following changes were suggested during code reviews but were intentionally not implemented:

-   **Convert `Rectangle` to `record`**: Deemed too invasive for a V&V-focused session set.
-   **Rename `PdfObject.Type`**: Considered a significant breaking change and out of scope for V&V.
-   **Introduce `ReadOnlySpan`**: Performance benefits were not deemed critical for the current use cases.
-   **Remove unused `embedded` parameter from `BaseFont`**: Avoided to maintain API compatibility.
-   **Add full surrogate pair or Mac Roman encoding support**: This would require new features and dependencies, which was outside the scope of V&V.
-   **Implement Path Traversal Security Fix**: Declined because the library is designed to operate on streams, not file paths. The responsibility for path validation lies with the calling application.
-   **Implement Font Cache Eviction Policy**: The current behavior (cache forever) matches the original OpenPDF library and changing it was considered out of scope for V&V.