# Change Log: PDF Info Dictionary Metadata API

This document records the changes delivered by the `pdf-info-metadata-api` session set from the perspective of the `DabblerPdf` repository. This release delivers the library-side implementation for PDF metadata manipulation.

## Session 2 of 3: Library Implementation

### Summary
This session implemented the public API surface for managing the PDF Info Dictionary metadata. `Document` now exposes methods to add /Title, /Author, /Subject, /Keywords, /Creator, and /CreationDate entries. A new `SuppressCreationDate()` method allows reverting to the 1.0.0 behavior of not emitting /CreationDate by default. Metadata is written to the PDF during `PdfWriter.Close()`. Validation rules ensure `null` arguments throw `ArgumentNullException`, empty/whitespace strings are silently ignored, and attempts to modify metadata after `Document.Close()` throw `InvalidOperationException`. Last-write-wins semantics apply to multiple calls for the same metadata key. A new `DabblerPdf.Utilities.PdfDate` helper was added for precise PDF date formatting per section 7.9.4 of the PDF 1.7 specification. The API is additive, maintaining backward compatibility, with `SuppressCreationDate()` restoring the exact byte-for-byte output of `DabblerPdf 1.0.0`.

### File Changes
- **Created:** `src/DabblerPdf/Document/Document.Metadata.cs`
- **Created:** `src/DabblerPdf/Document/DocumentMetadata.cs` (internal record)
- **Created:** `src/DabblerPdf/Utilities/PdfDate.cs`
- **Created:** `tests/DabblerPdf.Tests/Utilities/PdfDateTests.cs`
- **Created:** `tests/DabblerPdf.Tests/Document/DocumentMetadataTests.cs`
- **Created:** `tests/DabblerPdf.Tests/Pdf/PdfInfoDictionaryTests.cs`
- **Modified:** `src/DabblerPdf/Pdf/PdfName.cs` (added `Title`, `Author`, `Subject`, `Keywords`, `Creator`, `ModDate` constants)
- **Modified:** `src/DabblerPdf/Document/Document.cs` (made `partial`; added `EnsureNotClosed` guard)
- **Modified:** `src/DabblerPdf/Pdf/PdfWriter.cs` (wired metadata into the `/Info` dictionary at `Close()`)
- **Modified:** `src/DabblerPdf/DabblerPdf.csproj` (version `1.0.0` → `1.1.0`)
- **Modified:** `CHANGELOG.md` (1.1.0 entry)
- **Modified:** `README.md` (followup commit `6316184`: documented the metadata API for the nuget.org package page)

### Verification
All 601 unit tests passed with 0 failures. Cross-provider verification by `gemini-pro` identified two minor issues: a duplicate test helper was removed, and a false-positive TFM-naming finding was dismissed. 19/19 consumer smoke checks against the packed `nupkg/DabblerPdf.1.1.0.nupkg` from a throwaway project confirmed correct API behavior, validation, and PDF output.

### Released As
DabblerPdf 1.1.0, packed as `nupkg/DabblerPdf.1.1.0.nupkg` and `nupkg/DabblerPdf.1.1.0.snupkg`.

---
*Note: The consumer wiring for this feature set is implemented in the `dabbler-platform` repository, which will produce its own change log entry.*