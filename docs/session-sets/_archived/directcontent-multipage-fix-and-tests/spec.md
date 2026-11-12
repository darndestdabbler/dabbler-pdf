# DirectContent Multipage Fix And Rendering-Test Discipline Spec

> **Purpose:** Fix the `Document.NewPage()` bug that stacks composer
> pages 1 + 2 into a single physical PDF page when the caller uses
> `PdfWriter.DirectContent` exclusively, and establish a rendering-
> visibility test discipline for DabblerPdf so a "string is present in
> content stream" test can never again pass while the string is
> invisible to the user.
> **Created:** 2026-04-22
> **Session Set:** `docs/session-sets/directcontent-multipage-fix-and-tests/`
> **Prerequisite:** The
> [`pdf-info-metadata-api`](../pdf-info-metadata-api/spec.md) session
> set is complete end-to-end (library sessions 1–2 shipped; the
> consume/regression session ran in `dabbler-platform` as a mirror;
> DabblerPdf 1.1.0 is the authoritative release). Before starting this
> set, the orchestrator should close that set out by adding the mirror
> entry for Session 3 and generating its `change-log.md` — that is
> cleanup, not new work.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

A real-world export from `dabbler-platform` on 2026-04-22 produced a
6-page PDF whose first physical page visibly rendered the column
headers and data rows of composer-page 2 (with footer "Page 2 of 7"),
while composer-page 1's content — including the "Orders by Retail
Business" title heading — was overdrawn and invisible. The composer
produced 7 logical pages; the PDF contained 6 physical pages; physical
page 1 held composer-pages 1 + 2 in the same content stream;
physical pages 2–6 held composer-pages 3–7 cleanly.

Evidence gathered in `dabbler-platform` before this spec was carved:

- **pymupdf text extraction of physical page 1** shows two overlapping
  bands — composer-page 1 text at PDF-coordinate Y ≈ 555 (top) down
  through Y ≈ 53 (its own "Page 1 of 7" footer), and composer-page 2
  text starting at Y ≈ 561 and ending at Y ≈ 53 (its own "Page 2 of 7"
  footer). Both sets of `Tm` operators live in physical page 1's
  content stream.
- **Inflated content stream for physical page 1** contains two
  `0 0 792 612 re / f` full-page white background fills — one at
  stream offset 4 (from composer-page 1's `RenderPage`), a second at
  stream offset 4674 (from composer-page 2's `RenderPage`). The
  second fill paints over everything drawn by composer-page 1,
  including the title.
- **Font resources** (`/F1` Helvetica-Bold through `/F4`
  Helvetica-BoldOblique) and positions are all correct — this is not
  a font or positioning bug.
- **Physical pages 2–6** each contain exactly one composer-page of
  content with a matching "Page N of 7" footer, so the bug is
  specifically a one-time stacking at the boundary between
  `document.Open()` and the first `document.NewPage()` call.

## Root cause

[`src/DabblerPdf/Pdf/PdfWriter.cs`](../../../src/DabblerPdf/Pdf/PdfWriter.cs)
lines 94–112, `EndCurrentPage(Rectangle pageSize)`:

```csharp
internal void EndCurrentPage(Rectangle pageSize)
{
    if (!_firstPage)
    {
        PageEvent?.OnEndPage(this, Document!);
        FlushCurrentPage(Document!.PreviousPageSize ?? pageSize);

        DirectContent.Reset();
        _pageFonts.Clear();
        _pageXObjects.Clear();
    }
    else
    {
        // First page: don't reset — content may have been written to DirectContent
        // before the first NewPage() call (common for single-page documents).
        _firstPage = false;
    }
}
```

The first-page branch intentionally skips flush + `DirectContent.Reset`
on the assumption that the caller wrote to DirectContent expecting
that content to go to page 1 and only called `NewPage()` because they
were done with the document. That is correct for the single-page
pattern. It is wrong for the multi-page pattern the Dabbler.Reports
`PdfExporter` uses:

```csharp
for (int i = 0; i < pages.Count; i++)
{
    if (i > 0)
        document.NewPage();
    var cb = writer.DirectContent;
    renderer.RenderPage(cb, pages[i]);
}
```

Trace for a 7-composer-page export:

1. `Document.Open()` → `_pageNumber = 0`, `_firstPage = true`.
2. `RenderPage(pages[0])` writes composer-page 1 content to DirectContent.
3. `NewPage()` #1 → `EndCurrentPage` takes the `_firstPage == true`
   branch: **does not flush, does not reset DirectContent**, sets
   `_firstPage = false`. `_pageNumber++` → 1.
4. `RenderPage(pages[1])` writes composer-page 2 content to the SAME
   DirectContent buffer (now containing pages 1 + 2 concatenated).
5. `NewPage()` #2 → `EndCurrentPage` takes the `_firstPage == false`
   branch: flushes the accumulated buffer (pages 1 + 2) as physical
   page 1's content stream, resets DirectContent. `_pageNumber++` → 2.
6. Subsequent iterations behave correctly — one composer-page per
   physical page.

Result: 7 composer-pages → 6 physical pages, with composer-pages 1
and 2 stacked onto physical page 1. Composer-page 2 is drawn after
composer-page 1 in the shared content stream, so its full-page white
background fill paints over composer-page 1's entire visible output.

## Why this is surfacing now

The `Dabbler.Reports.Export.PdfExporter` is the first known consumer
that writes to DirectContent across multiple pages. Prior DabblerPdf
test coverage used single-page documents or used the managed
`Document.Add(...)` pipeline, both of which avoid the broken branch.
The comment in `EndCurrentPage` ("common for single-page documents")
accurately describes the tested scenario and was a reasonable
assumption at the time — but the multi-page DirectContent pattern
hit the gap.

---

## Goals

1. **Correct multipage behaviour with DirectContent.** After the fix,
   any sequence of `RenderPage → NewPage → RenderPage → ...` produces
   one physical PDF page per `RenderPage` call, with each page's
   content isolated from the others.
2. **Single-page compatibility preserved.** Callers that write to
   DirectContent once and then call `Close()` (without any
   `NewPage()`) must still produce a valid single-page PDF with all
   their content on page 1. The `Close()` path already handles the
   `_pageNumber == 0` case by calling `NewPage()` — that path must
   continue to work.
3. **Rendering-visibility test discipline.** DabblerPdf adopts a test
   pattern that asserts "content is visible on physical page N" via a
   rendering-aware parser, not via raw content-stream byte greps.
   At least one new regression test exercises the multi-page
   DirectContent pattern and would have caught the current bug.
4. **Lesson captured and enforced.** The "string-in-bytes is not
   rendered-visible" lesson lands in
   `docs/planning/lessons-learned.md` and, if it is applied again in
   a second context in a future set, gets promoted to `project-
   guidance.md` per the usual reorganization workflow.

## Non-goals

- **No API surface changes** to `Document` or `PdfWriter`. The fix is
  internal.
- **No consumer-side work in dabbler-platform.** That will happen in a
  cross-repo mirror session after the library ships a new release
  (see "Downstream coordination").
- **No visual-regression rasterize-and-SSIM scaffolding** in this set.
  Per-page structural assertions via a text-extraction parser are
  sufficient for the library's internal tests and catch the current
  failure. Visual regression can be considered in a later set if it
  earns its keep.

---

## Session Plan

### Session 1 of 2 — Reproduce, fix, library-level regression test

**Location of work:** `dabbler-pdf/` (this repo).

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.

2. **Reproduce the bug locally** with a minimal integration test in
   `tests/` that drives the exact pattern from `PdfExporter`:

   ```
   open document
   for i in 0..N-1:
       if i > 0: document.NewPage()
       write to writer.DirectContent: a unique marker per page
   close document
   ```

   Inflate each physical page's content stream and assert the number
   of physical pages equals N, and that each physical page's stream
   contains exactly its own marker. The test should fail on the
   current code before any fix.

3. **Fix the flush-skip logic in `PdfWriter.EndCurrentPage`.** Two
   shapes are on the table; pick one after reproducing the bug and
   disclose the reasoning in the Session 1 verification prompt:

   - **(a) Flush whenever DirectContent has content.** Change the
     `_firstPage` branch to still check whether anything has been
     written to DirectContent; if yes, flush and reset. The
     single-page pattern (DirectContent → Close) still works because
     `Close()` issues its own flush.
   - **(b) Drop the first-page special case entirely.** Always flush
     in `EndCurrentPage`. `Close()` already ensures at least one page
     by calling `NewPage()` when `_pageNumber == 0`; verify that path
     still produces a valid single-page PDF for a caller that wrote
     DirectContent and then called `Close()` (no `NewPage()`).

   (a) is narrower and preserves the author's original intent. (b) is
   simpler and has less state to reason about. The orchestrator's job
   is to weigh those against the existing test evidence.

4. **Library-level regression test.** Rather than extending the
   minimal repro from step 2, author a permanent regression test
   that:
   - drives the multi-page DirectContent pattern,
   - asserts physical page count equals the logical page count,
   - extracts text from each physical page via
     [PdfPig](https://github.com/UglyToad/PdfPig) (or an equivalent
     MIT-licensed pure-.NET PDF text extractor — pick one in this
     session; document the choice),
   - asserts per-physical-page text contains the page's expected
     marker and does NOT contain any other page's marker.

   This test is the durable "rendering-visibility" coverage the library
   has been missing. Place it under `tests/` alongside existing
   DabblerPdf tests; name the file for discoverability
   (e.g., `MultipageDirectContentRenderingTests.cs`).

5. **Build and test.**

6. **Cross-provider verification** via
   `route(task_type="session-verification")`. Include the fix, the
   new test, and an out-of-scope disclosure naming the existing
   single-page tests so the verifier does not hunt for regression
   proof they already cover.

7. **Commit, push, notify.** No change log yet (multi-session set).

**Creates:** One new test file (e.g.,
`MultipageDirectContentRenderingTests.cs`) and a chosen PDF-text-
extraction test dependency. **Touches:**
`src/DabblerPdf/Pdf/PdfWriter.cs` (the fix). **Ends with:** The bug
is fixed in source, a permanent test proves it, the rest of the
existing DabblerPdf suite stays green. **Progress keys:**
`session-1/read-planning`, `session-1/repro-test`, `session-1/fix`,
`session-1/regression-test`, `session-1/build-and-test`,
`session-1/verification`, `session-1/commit-push-notify`.

---

### Session 2 of 2 — Rendering-test discipline, lesson, prep for release

**Location of work:** `dabbler-pdf/` (this repo).

**Steps:**

1. Read planning docs.

2. **Document the rendering-test discipline** in
   `docs/planning/project-guidance.md` under
   **Conventions → Testing Conventions** (or create the subsection if
   it does not exist). Short, action-oriented text. Core rule:

   > Tests that assert "content is visible in the PDF" must exercise
   > a rendering-aware parser that models per-physical-page visible
   > text with bounding boxes (PdfPig, pymupdf, or equivalent). Raw
   > content-stream byte greps are wiring-presence assertions only —
   > they cannot catch content that was overdrawn, clipped,
   > off-page, or rendered in an unavailable font. Single-page tests
   > may still use byte-level asserts for specific structural
   > properties (object IDs, dictionary keys), but visibility
   > assertions always go through the rendering-aware parser.

3. **Second regression test** that exercises a different visibility
   failure mode — e.g., a DirectContent write that places text
   off-page, or behind a later opaque rectangle, or in an
   unavailable font — and asserts the rendering-aware parser
   reports the text as **not visible** (or as visible at the
   unexpected position). This confirms the discipline catches more
   than just the bug that drove this set.

4. **Bump version and CHANGELOG.** The fix is a library bug fix.
   Consult `docs/planning/project-guidance.md` → version-bump rules
   and update `CHANGELOG.md` with a user-facing note naming the fix.
   If the project's release process is automated, trigger it per the
   normal procedure; otherwise, leave the release for a
   human-triggered step and note it in the change log.

5. **Cross-repo mirror prep.** This fix affects `dabbler-platform`'s
   PDF export — physical page count will go from 6 to 7 on the
   orders-by-customer sample once the new DabblerPdf is consumed.
   Draft a short follow-up spec stub at
   `../dabbler-platform/docs/session-sets/pdf-newpage-multipage-fix-consumption/spec.md`
   that:
   - bumps `Directory.Packages.props` from `DabblerPdf 1.1.0` to the
     version this set ships,
   - extends
     `tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs`
     with a rendering-visibility assertion (PdfPig per-page)
     mirroring the discipline from this set,
   - adds a new Layer 3 Playwright end-to-end test that drives the
     web app's real export button, intercepts the downloaded PDF,
     and runs PdfPig-per-page assertions against it,
   - closes the Finding 2 remediation gap from the
     `pdf-info-metadata-uat-remediation` set.

   The stub spec can be a one-session plan; do not execute it in this
   set. The next orchestrator in `dabbler-platform` picks it up after
   the new DabblerPdf ships. Use the Cross-Repo Mirror Convention
   (see the consumer repo's `docs/planning/project-guidance.md`) so
   the platform-side session set has the right mirror-entry shape.

6. **Build, test, change log, cross-provider verification.**

7. **Commit, push, notify.** This is the last session — generate
   `change-log.md` via
   `route(task_type="summarization", max_tier=1)`.

8. **Step 9 reorganization review.** With the discipline lesson fresh,
   the usual review — see if the new lesson already deserves
   promotion, and whether anything else has accumulated enough
   second-context evidence to move.

**Creates:** `docs/planning/project-guidance.md` convention
addition; a second regression test; a stub follow-up spec in
`../dabbler-platform`; `change-log.md`. **Touches:**
`CHANGELOG.md`, `docs/planning/project-guidance.md`,
`docs/planning/lessons-learned.md` (reorganization). **Ends with:**
The library ships a release with the fix, the rendering-test
discipline is captured as a convention, and the consumer repo has a
concrete queued follow-up for upgrading.

**Progress keys:** `session-2/read-planning`,
`session-2/convention-addition`, `session-2/second-regression-test`,
`session-2/version-bump-and-changelog`, `session-2/mirror-stub`,
`session-2/build-and-test`, `session-2/change-log`,
`session-2/verification`, `session-2/commit-push-notify`,
`session-2/reorg-review`.

---

## Downstream coordination

After this set ships:

- `dabbler-platform` consumes the new DabblerPdf via the stub spec
  drafted in Session 2 (`pdf-newpage-multipage-fix-consumption` or
  whatever the human names it).
- The Layer 3 Playwright end-to-end test is authored on the
  consumer side because it needs the web app + browser + real data.
- Finding 2 from
  [`pdf-info-metadata-uat-remediation`](../../../../dabbler-platform/docs/session-sets/pdf-info-metadata-uat-remediation/change-log.md)
  is re-opened only in the sense that its disposition ("stale
  runtime, no code fix") was incorrect. The new consumption set
  properly closes it with code.

## Risk notes

- **Single-page regressions.** The fix must not break the DirectContent-
  then-Close single-page pattern. If the regression-test harness
  does not already cover that pattern, add coverage before landing
  the fix.
- **Off-page / hidden content tests are fragile.** Session 2's second
  regression test (visibility failure modes) should avoid
  implementation-specific assumptions — e.g., do not assume a
  particular implementation of z-order or a particular font
  substitution. Assert only on outcomes the chosen parser exposes
  consistently across versions.
- **Baseline test-framework choice.** PdfPig is the obvious pick
  (MIT, pure .NET, active maintenance), but verify license
  compatibility with DabblerPdf's dual LGPL-2.1 / MPL-2.0 license
  before adding it to `tests/`.
- **Do not ship a visual-regression framework in this set.**
  Rasterize + SSIM scaffolding adds a native-image-library
  dependency and a baseline-maintenance burden. It earns its keep
  for UI platforms, not for a layout-free PDF library. If a later
  use case justifies it, a follow-up set can add it.
