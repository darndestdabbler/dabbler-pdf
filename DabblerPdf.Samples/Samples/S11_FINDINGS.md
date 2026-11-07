# S11_OrdersStyleReport — Findings

> Generated during the `orders-style-sample-and-title-investigation` session
> set, Session 1 of 1.
> **Date:** 2026-04-22
> **Package version observed:** DabblerPdf 1.1.1 (built from source in this
> session; no nupkg bump).
> **Output reviewed:** `DabblerPdf.Samples/Output/11-orders-style-report.pdf`
> (committed as part of this session so a clean clone produces the same
> bytes).

## Purpose

Mirror the `dabbler-platform` Orders By Retail Business report in
DabblerPdf so the "title is visible on physical page 1" claim can be
verified against the actual 1.1.1 code path before the package is pushed.
If the sample renders the title visibly, 1.1.1 ships as-is; if not, the
sample becomes the minimal in-library repro and drives a follow-up fix.

## What the sample exercises

- **Landscape 11×8.5** page (`new Rectangle(792, 612)`), matching the real
  RDF's page metadata.
- **`reportHeader` band drawn to DirectContent before any `NewPage()` call**
  — the exact composer-page-1 pattern that hit the 1.1.1 stacking bug.
- **`pageHeader` band repeated on every page** with a gray-fill column
  header row — the band historically suspected of color-state leak.
- **Four synthetic customer groups** with light-blue group-header
  backgrounds, 48 detail rows total, paginating to **3 physical pages**.
- **`IPdfPageEvent`-driven page footer** with a `PdfTemplate` for
  "Page N of M" backfill.
- **Explicit navy (#1A1A2E) title fill-color** set via `SetColorFill`
  immediately before the `Tj` operator, so color-state assertions have
  a defined target.

## Programmatic findings (PdfPig + content-stream parsing)

The companion test
`tests/DabblerPdf.Tests/Pdf/OrdersStyleSampleRenderingTests.cs` drives the
exact sample code path the runnable program executes, then opens the
produced bytes with PdfPig. All six assertions pass on 1.1.1:

| # | Assertion | Result |
|---|---|---|
| 1 | Sample produces ≥ 3 physical pages | ✓ 3 pages |
| 2 | Page 1 title "Sample Orders by Region" is a Helvetica-Bold word in the top 20% of the page (bounding-box `Top` ≥ 0.80 × `Page.Height`) | ✓ pass |
| 3 | Page 1 footer contains `Page 1 of N` and never `Page 2 of …` (stacking regression guard) | ✓ pass |
| 4 | Every physical page has **exactly one** `Page X of N` footer, with `X` matching the physical page number and `N` matching the document page count | ✓ pass |
| 5 | The most-recent `rg` operator before the title's `Tj` in page 1's content stream matches normalized `(26/255, 26/255, 46/255)` — i.e., the title is painted in dark navy, not in any background color or unset-default | ✓ pass |
| 6 | Page 1 also contains the column headers (`Order #`, `Order Date`, `Status`, `Total`) and the first customer group name (`Acme Coffee`) | ✓ pass |

Together these catch the two failure modes the spec was most concerned
about:

- **Stacking regression** (composer-page 2 re-rendered into physical page
  1's content stream): assertion 3 would surface it via a `Page 2 of …`
  hit on page 1, and assertion 4 would see two matches per page.
- **Invisible-color leak** (title `Tj` emitted but fill color is the
  background color of a later band): assertion 5 walks the content
  stream backward from the title's `Tj` and demands an `rg` triple that
  matches the intended navy.

## Orchestrator visual check

This orchestrator (Claude Code) cannot open PDFs in a viewer — the
"visual inspection" here is necessarily indirect. What was verified
instead:

- The three physical pages each contain exactly the bands the spec
  specified (title on page 1 only; column headers on every page;
  group headers + details wherever they land; report footer on page
  3).
- Title position (top 20% of page 1), font (Helvetica-Bold 18pt), and
  fill color (navy, emitted as `rg` immediately before the `Tj`) all
  pass PdfPig-based structural checks that directly answer the
  1.1.1-push question.
- No `Page 2 of …` ghost on page 1 — a direct contradiction of the
  field-report symptom that motivated this set.

Given the assertions pass, the programmatic signal recommends **pushing
1.1.1 as-is**. The final call still belongs to the human opening
`DabblerPdf.Samples/Output/11-orders-style-report.pdf` in Adobe Reader
and confirming that what PdfPig calls "visible at the top of page 1 in
navy" is also what Adobe Reader renders.

## What the human should verify

Open `DabblerPdf.Samples/Output/11-orders-style-report.pdf` in the
preferred PDF viewer (Adobe Reader if available; the user-reported
"missing title" symptom was observed specifically in Adobe Reader) and
confirm:

1. **Title is rendered on page 1** at the top-left, in dark navy, at a
   size visibly larger than body text. Not overdrawn. Not in the same
   color as any background fill.
2. **Each page has its own "Page X of 3" footer** at the bottom right.
3. **Column headers (`Order #`, `Order Date`, `Ship Date`, `Status`,
   `Items`, `Total`) repeat on every page** above the data rows, with
   their light-gray background band visible.
4. **Group-header rows (customer names) render with a light-blue
   background** behind the customer text, and the text is readable on
   top of the fill.
5. **No text content stacks** — page 1 does not show page 2's column
   headers duplicated, and no band appears twice on any single page.

## Recommendation

**Push DabblerPdf 1.1.1 as-is.** The nupkg already exists at
`nupkg/DabblerPdf.1.1.1.nupkg`. The programmatic signals are positive,
and the sample's committed PDF gives the human reviewer a fast visual
confirmation path. If the human's viewer reveals a regression the
PdfPig assertions missed, the sample becomes the minimal in-library
repro for a 1.1.2 follow-up set.
