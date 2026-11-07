# Verification Round 1

ISSUES FOUND

1. **S07_MultiPage.cs: Incorrect Y coordinate passed to `AddTemplate`**

   In `OnEndPage`, the code calls:
   ```csharp
   cb.AddTemplate(_totalPagesTemplate, startX + prefixWidth, PdfY(y - 10));
   ```
   But `DrawText` uses `PdfY(y)` internally (converting from top-down to bottom-up coordinates), and the text for `pagePrefix` is drawn at `y - 10` using `DrawText` which already applies `PdfY`. The `AddTemplate` call applies `PdfY` again to the same logical coordinate, resulting in a double-conversion. It should be:
   ```csharp
   cb.AddTemplate(_totalPagesTemplate, startX + prefixWidth, y - 10);
   ```
   — **or** — whatever raw PDF Y coordinate `DrawText` ultimately produces. The mismatch means the template and the "Page N of " text will render at different vertical positions.

2. **S07_MultiPage.cs: `Margin` and other constants used inside `HeaderFooterPageEvent` without clear access path**

   `HeaderFooterPageEvent` is a nested class referencing `Margin`, `ContentWidth`, `PageHeight`, `DrawRectFilled`, `DrawText`, `DrawHLine`, and `PdfY` — all presumably inherited/static members of `SampleBase`. As a `private class` nested inside `S07_MultiPage` (which extends `SampleBase`), it does **not** inherit from `SampleBase` and cannot access instance members. These must be `static` or `const` members of `SampleBase` for this to compile. Given the build passed with 0 errors, they presumably are static — but this is worth confirming because `IPdfPageEvent` callbacks receive `writer` and `document`, not a `SampleBase` instance.

3. **PdfTemplate.cs: Font resource names may collide with page-level font names**

   Template fonts are named `/F1`, `/F2`, etc. independently of the page's font numbering. If a page has fonts `/F1`, `/F2` and a template also uses `/F1`, there's no collision since template resources are in the template's own `/Resources` dictionary. This is **correct per PDF spec** — no issue, just confirming.

4. **PdfWriter.cs: `_templateCounter` is never reset between pages**

   `_imageCounter` and `_templateCounter` are document-level counters that are never reset in `EndCurrentPage` when `_pageXObjects` is cleared. This means template resource names increment across pages (`/Tpl1` on page 1, `/Tpl2` on page 2 for the same template). This is functionally correct (names just need to be unique within a page's resources, and they are), but it means the same template gets different resource names on different pages. **Not a bug**, but inconsistent with the test `AddTemplate_SameTemplateOnSamePage_ReuseResourceName` which verifies reuse — the reuse check in `AddPageTemplate` checks `_pageXObjects` which is cleared per page, so on a new page the same template gets a new name. This is fine.

5. **PdfWriter.cs: `_templateCounter` not reset per page causes ever-increasing names**

   While not incorrect, `_imageCounter` similarly isn't reset. On page 2, if the same template is placed, it gets `/Tpl2` instead of `/Tpl1`. The `AddTemplate_PlacedOnMultiplePages_ProducesValidPdf` test doesn't verify the resource name, so this passes. **Minor inconsistency** but not a correctness issue.

6. **PdfBody.Add with reserved reference — assumed but not shown**

   `PdfWriter.Close()` calls `_body.Add(formXObject, template.Reference, _outputStream)` — an overload that writes an object at a previously reserved reference. This method is not shown in the diff. If `PdfBody` doesn't have this overload, it would fail at runtime. Since build passes, it presumably exists, but it's a dependency not verified in this diff.

7. **S07_MultiPage.cs: `PdfY` coordinate conversion inconsistency in `OnEndPage`**

   `OnEndPage` computes `y = PageHeight - Margin` (already a bottom-relative coordinate if the coordinate system is PDF-native bottom-up). Then it calls `DrawText(cb, pagePrefix, _font, 9, startX, y - 10)`. If `DrawText` applies `PdfY()` (converting top-down to bottom-up), and `y` is already near the bottom of the page, the text could end up off-page. The coordinate convention needs to be consistent. This is the same root issue as #1.

**Summary of blocking issues:**
- **Issue #1/#7**: The `AddTemplate` call in `OnEndPage` likely places the template at a different Y coordinate than the "Page N of " text, breaking the visual "Page X of Y" pattern. This is a functional bug in the sample.

---

# Verification Round 2

## Verification Analysis

### Re-examining Issue 1: AddTemplate Y coordinate

Let me trace through the coordinate logic carefully.

**Context:** `OnEndPage` event handler, where `y` represents a top-down coordinate.

**Given values:**
- `PageHeight = 792` (Letter size)
- `Margin = 72`
- `y = PageHeight - Margin = 720` (top-down coordinate near the bottom of the page)

**PdfY definition:**
```csharp
protected static double PdfY(double y) => PageHeight - y;
```

**DrawText call:**
```csharp
DrawText(cb, text, font, 9, startX, y - 10);
```
- Receives top-down Y: `y - 10 = 710`
- Internally: `PdfY(710) = 792 - 710 = 82` (PDF bottom-up Y)
- Text is placed at PDF Y = 82 ✓

**AddTemplate call:**
```csharp
cb.AddTemplate(template, x, PdfY(y - 10));
```
- `PdfY(y - 10) = PdfY(710) = 82` (PDF bottom-up Y)
- AddTemplate uses this directly as a PDF coordinate in the `cm` transformation matrix
- Template is placed at PDF Y = 82 ✓

**Both produce PDF Y = 82.** The analysis is correct.

The key insight is that `DrawText` is a wrapper that accepts **top-down** coordinates and converts internally, while `AddTemplate` is a raw iTextSharp/PDF method that accepts **bottom-up** PDF coordinates directly. Therefore:
- `DrawText(... y - 10)` → needs top-down input → converts internally
- `AddTemplate(... PdfY(y - 10))` → needs bottom-up input → caller converts

This is consistent and correct. No double-conversion occurs.

### Re-examining Issue 2: Static/const member access
Confirmed — `Margin`, `ContentWidth`, `PageHeight`, `DrawText`, `DrawRectFilled`, `DrawHLine`, `PdfY` are all `const` or `static`. A nested class can access these without an instance reference. No issue.

### Re-examining Issues 3/4/5: Template counter
Template names like `/Tpl1`, `/Tpl2` are unique within each page's resource dictionary. Not resetting the counter means names increment across pages (`/Tpl1` on page 1, `/Tpl2` on page 2), which is valid — just not minimal. No functional issue.

### Build and Test Confirmation
- 0 warnings, 0 errors
- 562/562 tests passed
- All 10 samples produced valid output

VERIFIED
