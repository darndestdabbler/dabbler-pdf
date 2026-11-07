# Dabbler PDF Samples Change-log

This update introduces the `DabblerPdf.Samples` project, a console application designed to demonstrate the library's core features through a series of generated PDF documents.

### New Files

-   `DabblerPdf.Samples/DabblerPdf.Samples.csproj` — Console app project targeting net10.0, references DabblerPdf.
-   `DabblerPdf.Samples/Program.cs` — Entry point: runs all 10 samples, reports pass/fail and timing.
-   `DabblerPdf.Samples/SampleBase.cs` — Abstract base class with PDF creation helpers, text wrapping, coordinate translation, and shape drawing.
-   `DabblerPdf.Samples/Samples/S01_BasicText.cs` — Demonstrates font families, styles, sizes, and special characters.
-   `DabblerPdf.Samples/Samples/S02_TextWrapping.cs` — Shows word wrapping, long word handling, and hard line breaks.
-   `DabblerPdf.Samples/Samples/S03_LinesAndShapes.cs` — Covers various line weights, styles, colors, and rectangles.
-   `DabblerPdf.Samples/Samples/S04_Colors.cs` — Examples of text colors, background fills, and color swatches.
-   `DabblerPdf.Samples/Samples/S05_Images.cs` — Illustrates image scaling modes and handling of missing images.
-   `DabblerPdf.Samples/Samples/S06_TableLayout.cs` — Creates a 4-column table with headers, alignment, and grid lines.
-   `DabblerPdf.Samples/Samples/S07_MultiPage.cs` — Generates a multi-page document with headers, footers, and page numbers.
-   `DabblerPdf.Samples/Samples/S08_Padding.cs` — Compares elements with no padding, uniform padding, and asymmetric padding.
-   `DabblerPdf.Samples/Samples/S09_InvoiceSample.cs` — A realistic invoice layout with a logo, line items, and totals.
-   `DabblerPdf.Samples/Samples/S10_StressTest.cs` — A performance test generating a 1000-row, 50+ page document.
-   `DabblerPdf.Samples/Assets/sample-logo.png` — Sample image asset for use in demonstrations.
-   `DabblerPdf.Samples/FINDINGS.md` — Documents API gaps and rendering observations discovered during sample creation.

### Modified Files

-   `.gitignore` — Added `DabblerPdf.Samples/Output/` to the exclusion list.
-   `DabblerPdf.slnx` — Added a project reference for `DabblerPdf.Samples`.

### New Classes

-   `SampleBase` (abstract) — Provides helper methods for PDF generation common to all samples.
-   `S01_BasicText`
-   `S02_TextWrapping`
-   `S03_LinesAndShapes`
-   `S04_Colors`
-   `S05_Images`
-   `S06_TableLayout`
-   `S07_MultiPage`
-   `S08_Padding`
-   `S09_InvoiceSample`
-   `S10_StressTest`

### Test Results

-   All 495 existing unit tests continue to pass.
-   10 visual samples generate successfully in under 1 second total.
-   Generated PDFs verified:
    -   `01-basic-text.pdf` (2.7KB)
    -   `02-text-wrapping.pdf` (2.1KB)
    -   `03-lines-and-shapes.pdf` (1.4KB)
    -   `04-colors.pdf` (1.6KB)
    -   `05-images.pdf` (7.6KB)
    -   `06-table-layout.pdf` (1.4KB)
    -   `07-multi-page.pdf` (1.9KB)
    -   `08-padding.pdf` (1.5KB)
    -   `09-invoice.pdf` (3.2KB)
    -   `10-stress-test.pdf` (45.7KB)