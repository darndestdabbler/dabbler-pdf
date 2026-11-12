# DabblerPdf Visual Samples — Implementation Spec

> **Purpose:** Spec for a sample console app that generates PDFs for human visual inspection before publishing DabblerPdf as a public NuGet package.
> **Created:** 2026-04-09
> **Repo:** dabbler-pdf (standalone, separate from dabbler-components)

---

## Rationale

DabblerPdf has comprehensive unit tests that verify API correctness — valid PDF headers, correct point conversions, non-throwing render calls, text wrapping line counts, font name mappings, and color parsing. But none of those tests open a PDF and look at it. Before publishing a public NuGet package, we need a visual test harness that generates real PDFs a human can open and inspect for:

- Font rendering quality and correct glyph selection across families and styles
- Text wrapping accuracy — does word-wrapping match the measured heights?
- Element positioning — are coordinates translating correctly from inches to points?
- Color fidelity — do hex colors render as expected?
- Border and line rendering — correct weights, dash patterns, colors?
- Image scaling — do Stretch, ScaleToFit, and ActualSize produce the expected results?
- Multi-page behavior — do page breaks, headers, and footers land where they should?
- Overall professional appearance — does the output look like something you'd hand to a client?

---

## Project Structure

```
DabblerPdf.Samples/
├── DabblerPdf.Samples.csproj         ← Console app, references DabblerPdf project
├── Program.cs                         ← Entry point: runs all samples, reports results
├── SampleBase.cs                      ← Abstract base with shared helpers
├── Samples/
│   ├── S01_BasicText.cs               ← Font families, sizes, styles
│   ├── S02_TextWrapping.cs            ← Word wrap, CanGrow, long text
│   ├── S03_LinesAndShapes.cs          ← Lines, rectangles, borders, dash patterns
│   ├── S04_Colors.cs                  ← Color palette, backgrounds, alternating rows
│   ├── S05_Images.cs                  ← Embedded images, scale modes
│   ├── S06_TableLayout.cs             ← Grid-like layout with borders and alignment
│   ├── S07_MultiPage.cs               ← Pagination, headers/footers, page numbers
│   ├── S08_Padding.cs                 ← Cell padding: uniform, asymmetric, nested
│   ├── S09_InvoiceSample.cs           ← Realistic invoice document
│   └── S10_StressTest.cs              ← Large document, many pages, performance
├── Assets/
│   └── sample-logo.png                ← Test image for image rendering samples
└── Output/                            ← .gitignored; generated PDFs written here
```

### Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../src/DabblerPdf/DabblerPdf.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Update="Assets/**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

Adjust the `ProjectReference` path to match the actual DabblerPdf project location in the repo.

---

## Shared Infrastructure

### `SampleBase`

```csharp
// DabblerPdf.Samples/SampleBase.cs
namespace DabblerPdf.Samples;

public abstract class SampleBase
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string FileName { get; }

    public abstract void Generate(string outputDir);

    protected string OutputPath(string outputDir) =>
        Path.Combine(outputDir, FileName);

    protected void PrintResult(string outputDir, Stopwatch sw)
    {
        var path = OutputPath(outputDir);
        var size = new FileInfo(path).Length;
        Console.WriteLine($"  ✓ {Name} — {size / 1024.0:F1} KB, {sw.ElapsedMilliseconds} ms → {FileName}");
    }
}
```

### `Program.cs`

```csharp
// DabblerPdf.Samples/Program.cs
namespace DabblerPdf.Samples;

public class Program
{
    public static void Main(string[] args)
    {
        var outputDir = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "Output");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"DabblerPdf Visual Samples");
        Console.WriteLine($"Output: {outputDir}");
        Console.WriteLine(new string('─', 60));

        var samples = new SampleBase[]
        {
            new S01_BasicText(),
            new S02_TextWrapping(),
            new S03_LinesAndShapes(),
            new S04_Colors(),
            new S05_Images(),
            new S06_TableLayout(),
            new S07_MultiPage(),
            new S08_Padding(),
            new S09_InvoiceSample(),
            new S10_StressTest(),
        };

        var total = Stopwatch.StartNew();
        int passed = 0, failed = 0;

        foreach (var sample in samples)
        {
            try
            {
                sample.Generate(outputDir);
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {sample.Name} — FAILED: {ex.Message}");
                failed++;
            }
        }

        total.Stop();
        Console.WriteLine(new string('─', 60));
        Console.WriteLine($"Done: {passed} passed, {failed} failed, {total.ElapsedMilliseconds} ms total");
        Console.WriteLine($"\nOpen the Output folder to inspect the generated PDFs.");
    }
}
```

---

## Sample Specifications

Each sample targets a specific rendering capability. The descriptions below state what the generated PDF should contain so a human reviewer knows exactly what to look for.

### S01: Basic Text

**File:** `01-basic-text.pdf`
**Pages:** 1

**Content:**

The page is a visual type specimen sheet. It should contain the following sections, each with a label identifying what it is:

1. **Font families** — The same sentence ("The quick brown fox jumps over the lazy dog.") rendered in:
   - Helvetica (the default/fallback)
   - Times New Roman
   - Courier

2. **Font styles** — For each of the three families, render in:
   - Regular
   - Bold
   - Italic
   - Bold Italic

   This produces a 3×4 grid of 12 lines total.

3. **Font sizes** — Helvetica regular at sizes: 8pt, 10pt, 12pt, 14pt, 18pt, 24pt, 36pt. Each line labeled with its size.

4. **Special characters** — A line containing: ampersand (&), angle brackets (<>), quotes ("double" and 'single'), em dash (—), accented characters (é, ñ, ü), and a currency symbol (€).

**What to check:** Correct font selection (serif vs sans-serif vs monospace), bold/italic actually rendering as bold/italic (not just regular), size progression looking proportional, no missing glyphs or tofu boxes.

---

### S02: Text Wrapping

**File:** `02-text-wrapping.pdf`
**Pages:** 1–2

**Content:**

1. **Controlled wrapping comparison** — Three text blocks side by side at different widths (1.5", 3.0", 6.5") containing the same paragraph (roughly 200 words). Each block has a visible border so you can see the wrapping boundary.

2. **Single very long word** — A text element containing "Supercalifragilisticexpialidocious" at a narrow width (1.0") to verify character-level fallback when word wrapping fails.

3. **Hard line breaks** — Text with explicit `\n` characters. Verify each `\n` produces a new line, and `\n\n` produces a visible blank line.

4. **CanGrow demonstration** — Two elements at the same declared height (0.25"). One has short text ("Hello"), the other has a long paragraph. The CanGrow element should visually expand to fit. Draw a border around both to show the height difference.

5. **CanShrink demonstration** — An element with a declared height of 1.0" containing only "Hi". With CanShrink, it should shrink to a single line height. Show with and without CanShrink side by side, both with borders.

**What to check:** Word-wrapping breaking at word boundaries, not mid-character. Wrapped text staying within its border. Hard line breaks producing correct spacing. CanGrow/CanShrink visually working as expected.

---

### S03: Lines and Shapes

**File:** `03-lines-and-shapes.pdf`
**Pages:** 1

**Content:**

1. **Horizontal lines** — A series of horizontal lines at different weights: hairline (0.25pt), 0.5pt, 1pt, 2pt, 4pt. Each labeled.

2. **Line styles** — Three horizontal lines at 1pt: solid, dashed, dotted. Each labeled.

3. **Line colors** — Five horizontal lines at 1pt solid in: black, red (#FF0000), green (#00FF00), blue (#0000FF), gray (#999999).

4. **Rectangles** — A row of rectangles demonstrating:
   - Border only (no fill)
   - Fill only (no border)
   - Border + fill
   - Rounded corners (if supported, otherwise note this)
   - Different border styles (solid, dashed, dotted)

5. **Nested rectangles** — A large rectangle containing a smaller centered rectangle, demonstrating layering and position accuracy.

**What to check:** Line weights visually distinct from each other. Dash patterns visible and consistent. Colors correct. Rectangles with clean corners. Borders rendering at the correct position (not offset by half a pixel).

---

### S04: Colors

**File:** `04-colors.pdf`
**Pages:** 1

**Content:**

1. **Color swatches** — A grid of colored rectangles (roughly 1" × 0.5" each) showing:
   - Primary colors: red, green, blue
   - Secondary colors: cyan, magenta, yellow
   - Grayscale: white, light gray (#CCCCCC), medium gray (#999999), dark gray (#333333), black
   - Each swatch labeled with its hex code below

2. **Text colors** — The same sentence rendered in black, red, blue, green, dark gray, and a light color on a dark background (white text on #333333 background).

3. **Background fills** — Text boxes with different background colors: light yellow (#FFFFCC), light blue (#CCE5FF), light green (#CCFFCC), light red (#FFCCCC). Each containing readable black text.

4. **Alternating row simulation** — Ten rows of text where odd rows have a white background and even rows have a light gray (#F0F0F0) background, simulating the detail band alternating color feature.

**What to check:** Colors visually match their hex values. No color space confusion (CMYK vs RGB). White text readable on dark backgrounds. Alternating rows clearly distinguishable but not garish.

---

### S05: Images

**File:** `05-images.pdf`
**Pages:** 1

**Content:**

Uses `Assets/sample-logo.png` — a simple recognizable image (create a 200×100 pixel PNG with distinct content, like a colored rectangle with "DabblerPdf" text drawn on it).

1. **Scale modes** — The same image rendered three times in a 2" × 2" element:
   - **Stretch**: Image fills the full element (distorted if aspect ratio differs)
   - **ScaleToFit**: Image scales to fit within the element, maintaining aspect ratio, centered
   - **ActualSize**: Image renders at its native pixel dimensions, positioned at the element's top-left

   Each labeled. The element boundary drawn as a light gray dashed border so you can see the fit.

2. **Different element sizes** — The same image (ScaleToFit) in three different element sizes: 1"×1", 2"×1", 1"×2". Verify centering and aspect ratio preservation in each.

3. **Missing image placeholder** — An image element referencing a nonexistent file path. Verify a placeholder renders (not a crash).

**What to check:** Stretch visibly distorts a non-square image in a square element. ScaleToFit maintains proportions and centers. ActualSize ignores element bounds. No crashes on missing images.

---

### S06: Table Layout

**File:** `06-table-layout.pdf`
**Pages:** 1

**Content:**

Simulates a tabular data layout using positioned text elements, lines, and rectangles — the same way the Reports layout engine builds grids.

1. **Simple 4-column table** — Header row with bold text and a gray background (#E0E0E0), followed by 8 data rows with alternating white/#F5F5F5 backgrounds. Columns: "Product" (left-aligned, 2.5"), "Category" (left, 1.5"), "Price" (right-aligned, 1.0"), "In Stock" (center, 1.0"). Vertical and horizontal grid lines separating all cells.

2. **Alignment showcase** — Within the table, demonstrate:
   - Left alignment (text columns)
   - Right alignment (numeric Price column)
   - Center alignment (In Stock column)

3. **Column header underline** — A 2pt line below the header row, visually heavier than the 0.5pt grid lines.

**What to check:** Columns visually aligned. Right-aligned numbers have their decimal points (or right edges) aligned vertically. Grid lines intersect cleanly. Header row visually distinct from data rows.

---

### S07: Multi-Page

**File:** `07-multi-page.pdf`
**Pages:** 5+

**Content:**

1. **Page header on every page** — A header band with "DabblerPdf Multi-Page Sample" on the left and the date on the right.

2. **Page footer on every page** — A footer band with "Page {n} of {total}" centered.

3. **Report header** — On page 1 only, a larger title "Multi-Page Document Sample" above the page header content.

4. **Detail rows** — Generate enough rows (40+) to span at least 5 pages. Each row shows a row number and some filler text.

5. **Report footer** — On the last page only, "End of Report — {total} rows printed" below the last detail row.

**What to check:** Page headers on every page, report header on page 1 only, report footer on last page only. "Page X of Y" correct on every page (not "Page 1 of 0" — the backfill must work). Rows flowing naturally across page boundaries with no overlap or gaps.

---

### S08: Padding

**File:** `08-padding.pdf`
**Pages:** 1

**Content:**

1. **No padding** — A text element with a visible border, text touching the border edges.

2. **Uniform padding** — Same text and border, with 0.1" padding on all sides. Visible gap between text and border.

3. **Asymmetric padding** — Same text, with PaddingTop=0.05", PaddingBottom=0.15", PaddingLeft=0.2", PaddingRight=0.05". Visible difference in spacing on each side.

4. **Padding with wrapping** — A multi-line text element with padding. Verify that padding reduces the available text width (causing more wrapping) and adds vertical space above and below the text block.

5. **Side-by-side comparison** — Two identical text blocks at the same declared size, one with 0" padding and one with 0.15" padding, both with borders. The padding one should have less text area and potentially more wrap lines.

All elements should have visible borders so the padding is clearly distinguishable from the text area.

**What to check:** Padding creating visible space between text and border. Asymmetric padding visually different on each side. Padding reducing effective text area for wrapping.

---

### S09: Invoice Sample

**File:** `09-invoice.pdf`
**Pages:** 1–2

**Content:**

A realistic invoice document that exercises multiple features together:

1. **Company header** — Company name in large bold text (24pt), address in smaller text below, a horizontal rule separating header from body. Include the sample logo image (ScaleToFit) in the top-right corner.

2. **Invoice metadata** — Right-aligned block: "Invoice #: 10248", "Date: March 15, 2025", "Due: April 14, 2025".

3. **Bill To / Ship To** — Two side-by-side blocks with customer address information.

4. **Line items table** — Columns: Item, Description, Qty (right), Unit Price (right, currency format), Ext. Price (right, currency format). At least 8–10 line items. Header row with bold text on gray background. Alternating row colors.

5. **Subtotals section** — Right-aligned: Subtotal, Tax (7%), Total in bold. Horizontal rules separating the sections.

6. **Footer** — "Thank you for your business!" centered in italic, with a bottom border line.

**What to check:** This is the "does it look professional?" test. Everything together — fonts, alignment, colors, lines, images, numeric formatting — should produce something you'd be comfortable showing to a client. No visual glitches, no overlapping elements, correct arithmetic in the totals.

---

### S10: Stress Test

**File:** `10-stress-test.pdf`
**Pages:** 50+

**Content:**

1. Generate 1,000 detail rows across 50+ pages with headers and footers on every page.
2. Each row contains: row number, a random name (from a fixed seed so it's reproducible), a random decimal amount, and a running total.
3. Page header and footer on every page.
4. Track and report generation time.

**What to check:** The PDF opens without errors in standard viewers (Adobe Reader, Preview, Chrome). Page count is correct. No memory issues during generation. File size is reasonable (should be under a few MB). Generation completes in under 5 seconds on a modern machine.

---

## Sample Image Asset

Create a simple `Assets/sample-logo.png` programmatically if needed, or include a minimal hand-made PNG. Requirements:

- 200 × 100 pixels
- Distinct, recognizable content (e.g., a blue rectangle with white "DabblerPdf" text, or a simple geometric pattern)
- Small file size (under 10 KB)
- PNG format with no transparency (simplest case) and one with transparency (to test alpha handling)

If creating programmatically is easier, use `System.Drawing` or similar to generate it at build time, or just include a static file.

---

## Running the Samples

```bash
cd DabblerPdf.Samples
dotnet run
```

Output:
```
DabblerPdf Visual Samples
Output: /path/to/DabblerPdf.Samples/bin/Debug/net8.0/Output
────────────────────────────────────────────────────────
  ✓ Basic Text — 12.3 KB, 45 ms → 01-basic-text.pdf
  ✓ Text Wrapping — 8.7 KB, 32 ms → 02-text-wrapping.pdf
  ✓ Lines and Shapes — 6.1 KB, 18 ms → 03-lines-and-shapes.pdf
  ✓ Colors — 9.4 KB, 22 ms → 04-colors.pdf
  ✓ Images — 15.2 KB, 38 ms → 05-images.pdf
  ✓ Table Layout — 11.8 KB, 28 ms → 06-table-layout.pdf
  ✓ Multi-Page — 45.6 KB, 120 ms → 07-multi-page.pdf
  ✓ Padding — 7.3 KB, 20 ms → 08-padding.pdf
  ✓ Invoice Sample — 18.9 KB, 55 ms → 09-invoice.pdf
  ✓ Stress Test — 892.1 KB, 1,240 ms → 10-stress-test.pdf
────────────────────────────────────────────────────────
Done: 10 passed, 0 failed, 1,618 ms total

Open the Output folder to inspect the generated PDFs.
```

Then open each PDF and visually inspect against the "What to check" criteria listed above.

---

## Review Checklist

After generating all samples, review each PDF against this checklist before approving the NuGet release:

| # | Sample | What to verify | Pass? |
|---|--------|----------------|-------|
| 1 | Basic Text | All 3 font families render distinctly; bold/italic visually correct; sizes proportional; no missing glyphs | |
| 2 | Text Wrapping | Words break at boundaries; text stays within borders; CanGrow/CanShrink visually working | |
| 3 | Lines and Shapes | Line weights distinct; dash patterns visible; colors correct; rectangle corners clean | |
| 4 | Colors | Hex colors match expectations; text readable on backgrounds; alternating rows distinguishable | |
| 5 | Images | Stretch distorts; ScaleToFit preserves aspect ratio and centers; ActualSize ignores bounds; missing image doesn't crash | |
| 6 | Table Layout | Columns aligned; right-align numbers aligned; grid lines intersect cleanly; header row distinct | |
| 7 | Multi-Page | Headers/footers on every page; report header page 1 only; Page X of Y correct; no overlap at breaks | |
| 8 | Padding | Visible gap between text and border; asymmetric padding correct; padding reduces wrapping width | |
| 9 | Invoice | Professional appearance; no overlapping elements; correct numeric formatting; logo renders | |
| 10 | Stress Test | Opens without error; 50+ pages; under 5 seconds generation; reasonable file size | |

### Cross-Viewer Testing

Open each PDF in at least two of the following to catch renderer-specific issues:

- Adobe Acrobat Reader (Windows)
- Preview (macOS)
- Chrome/Edge built-in PDF viewer
- Firefox built-in PDF viewer
- SumatraPDF (Windows, lightweight alternative)

---

## Implementation Notes

### DabblerPdf API Usage

Each sample should use the DabblerPdf public API directly — not the Reports layout engine. The goal is to exercise the PDF library as a standalone NuGet consumer would use it. If a sample needs to create a `Document`, add pages, add text/image/shape elements, and write to a file, it should call those APIs directly.

If the DabblerPdf API currently only exposes high-level types designed for the Reports engine (e.g., it only accepts `LayoutPage` objects), and lacks a lower-level document-building API, that's valuable discovery — it means the public API surface may need expansion before the NuGet release. Document any API gaps encountered.

### Reproducibility

All samples must be deterministic. The stress test uses a fixed `Random` seed. No date-dependent content except the invoice date (use a hardcoded date, not `DateTime.Now`). This ensures the output is byte-identical across runs for regression comparison.

### .gitignore

Add to the repo's `.gitignore`:

```
DabblerPdf.Samples/Output/
```

The Output folder should not be committed. Generated PDFs are ephemeral artifacts for human review.

---

## Session Plan

This is a single-session task. The samples project is straightforward and self-contained.

### Session 1 of 1: Build the Samples Project

**Steps:**
1. Create the project structure: `DabblerPdf.Samples.csproj`, `Program.cs`, `SampleBase.cs`.
2. Create or source the `Assets/sample-logo.png` image.
3. Implement samples S01 through S10 in order. For each:
   a. Write the sample class extending `SampleBase`.
   b. Use the DabblerPdf API to construct the PDF content described in the spec.
   c. Run it and verify the PDF opens.
   d. If the DabblerPdf API doesn't support a needed operation, document the gap and implement a workaround or skip that section with a comment.
4. Run all samples end-to-end via `dotnet run`.
5. Open each generated PDF and perform a quick visual check.
6. Document any API gaps or rendering issues discovered in a `FINDINGS.md` file in the project root.

**Creates:** Entire `DabblerPdf.Samples/` project
**Ends with:** `dotnet run` generates 10 PDFs successfully; visual inspection passes; `FINDINGS.md` documents any issues
