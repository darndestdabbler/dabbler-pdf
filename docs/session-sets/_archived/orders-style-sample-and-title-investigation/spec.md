# Orders-Style Sample And Title-Rendering Investigation Spec

> **Purpose:** Add a standalone DabblerPdf sample that mirrors the
> `dabbler-platform` Orders By Retail Business report — landscape
> multi-page, prominent title in the reportHeader band, column headers
> per page, grouped detail rows, page footer, report footer — so the
> "title is visible on physical page 1" claim can be verified
> visually inside DabblerPdf itself before 1.1.1 is pushed. If the
> sample shows the title, 1.1.1 ships; if it does not, the sample
> serves as the minimal in-library repro and drives a deeper
> investigation into whatever pipeline step is still overdrawing,
> mis-coloring, or mispositioning the title.
> **Created:** 2026-04-22
> **Session Set:** `docs/session-sets/orders-style-sample-and-title-investigation/`
> **Prerequisite:**
> [`directcontent-multipage-fix-and-tests`](../directcontent-multipage-fix-and-tests/change-log.md)
> session set complete (DabblerPdf 1.1.1 packaged in
> `nupkg/DabblerPdf.1.1.1.nupkg`).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

On 2026-04-22, `dabbler-platform` shipped its own
`pdf-info-metadata-uat-remediation` set. Human UAT still reports the
report title ("Orders by Retail Business") missing from page 1 of
`/reports/orders-by-customer` even after:

- The caller-level wiring fix in
  `src/Libs/Dabbler.Reports.Blazor/ReportPdfExporterFactory.cs` (Title
  and Author now flow from `ReportMetadata` into the PDF `/Info`
  dictionary — Adobe Reader's Document Properties dialog now shows
  both).
- The DabblerPdf 1.1.1 fix to `PdfWriter.EndCurrentPage` that
  stopped composer-page-1 + composer-page-2 from stacking onto a
  single physical PDF page when the caller uses `DirectContent`
  across multiple pages.

The PdfPig regression tests added by `directcontent-multipage-fix-and-tests`
cover **text-level per-physical-page visibility** via bounding-box
extraction. That is the right assertion shape, but it is not a
rendering-equivalent check — PdfPig extracts every text operator
regardless of overdraw, z-order, color state, or clipping. A title
can be "visible to PdfPig" (literal appears in the page's text
stream at the correct position, correct font, correct size) and
"invisible to a human" (e.g., painted over by a later
`0 0 792 612 re f` fill, or rendered in the same color as the
background due to graphics-state leak).

An adjacent signal: the existing
[`DabblerPdf.Samples/HUMAN_FEEDBACK.md`](../../../DabblerPdf.Samples/HUMAN_FEEDBACK.md)
documents that six of the ten shipped samples already exhibit
**"the most recent background color becomes the font color of the
next-generated text"** — a classic graphics-state leak where `rg`
(non-stroking color) is not reset after filling a background
rectangle, and the next text block inherits the leaked color via
the default text-rendering mode (fill with non-stroking color).
That is an adjacent risk to the stacking bug, not the same one,
and could equally explain a "missing title" symptom when the
human's eye is the arbiter.

A DabblerPdf-internal sample that drives the real-world report
pattern against 1.1.1 is the cleanest way to answer the user's
decision question: **"should I push 1.1.1 as-is, or is there still
a rendering bug the stacking fix did not address?"**

## Goals

1. A new sample under `DabblerPdf.Samples/Samples/` that mirrors the
   Orders By Retail Business report in every way that could
   plausibly affect title rendering:
   - Landscape 11×8.5 page size (`PageSize.Letter` rotated, or a
     custom `Rectangle(792, 612)`).
   - `reportHeader` band drawn first on page 1: Helvetica-Bold
     18pt title at left, small italic date stamp at right,
     horizontal rule below. Uses explicit `0.101961 0.101961 0.180392 rg`
     (dark navy) for the title fill color — mirroring the
     `reportTitle` style in the real RDF.
   - `pageHeader` band drawn on every page: gray-background column
     headers (`Order #`, `Order Date`, `Ship Date`, `Status`,
     `Items`, `Total`) with a `0.941176 g` fill rectangle and
     `0.2 g` text. This is the operator pattern that triggers the
     color-leak failure mode in the existing samples, so the new
     sample's title-on-page-1 test is only meaningful after this
     band has been emitted on pages 2+.
   - At least two group headers with light-blue filled
     backgrounds (`0.839216 0.894118 0.941176 rg`), each followed
     by several detail rows with plain-text numbers and currency.
   - Page footer with `Wednesday, April 22, 2026` on the left and
     `Page N of M` on the right — same PdfTemplate pattern as the
     existing `S07_MultiPage` sample.
   - Report footer on the last page: grand-total band with a
     bold `Grand Total` label, count, and amount.
   - Enough synthetic rows that the output spans **at least 3
     physical pages**. Exercising the NewPage boundary more than
     once is a hedge against a fix that works for the first
     transition but regresses on later ones.

2. The sample is added to the `Program.cs` sample roster as
   `S11_OrdersStyleReport` (or the next available `S##` prefix)
   and produces `11-orders-style-report.pdf` in the `Output/`
   folder alongside the other samples.

3. An automated rendering-visibility assertion piggy-backs on the
   existing PdfPig test project, asserting per-physical-page that:
   - Page 1 contains the title literal "Sample Orders by Region"
     (or whatever the sample uses — do not reuse the literal from
     the real report; keep the sample independent) as a
     Helvetica-Bold word with Y-coordinate in the top 20% of the
     page (bounding-box top Y, PdfPig's top-left origin).
   - Page 1 does NOT contain `Page 2 of N` in its footer slot —
     the stacking regression would reintroduce that.
   - Every physical page has exactly one `Page X of N` footer.
   - The title's fill color (per the content stream color state
     immediately before the `Tj` operator) is the dark navy the
     sample set, not the background color of any band drawn
     earlier.

4. **Human visual verification is the gating outcome.** The sample
   PDF is opened in Adobe Reader (or the human's preferred PDF
   viewer) and the human confirms the title is rendered as
   expected. That confirmation is what unlocks the 1.1.1 push;
   the PdfPig assertions are a fast feedback loop during
   development, not a substitute.

## Non-goals

- **No `PdfExporter`-style composition engine** in DabblerPdf.
  The sample writes directly to `writer.DirectContent` using the
  same coordinate + band math the real `PdfExporter` would
  produce, but it does NOT introduce a new band-layout helper
  into DabblerPdf itself. The sample is a fixture, not an API.
- **No changes to DabblerPdf's public API.** If the sample
  reveals a library bug, a follow-up session set handles it.
- **No cross-repo work.** This set stays entirely within
  `dabbler-pdf`. The `dabbler-platform` consumption of 1.1.1 is
  still queued by the stub spec drafted in
  `directcontent-multipage-fix-and-tests` Session 2; this set does
  not pre-empt that.
- **No visual-regression rasterize+diff framework.** The
  [`risk notes in the prior set`](../directcontent-multipage-fix-and-tests/spec.md)
  are still binding: SSIM-threshold image diffs are not worth
  their baseline-maintenance cost at this stage. The human eye
  + PdfPig assertions cover the decision.

---

## Session Plan

### Session 1 of 1 — Build the sample, assert rendering visibility, diagnose if needed

**Location of work:** `dabbler-pdf/` (this repo).

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`. Note the new
   **"String In Bytes" Is Not "Rendered Visible"** lesson —
   the PdfPig-based assertions in this session must honor it by
   checking bounding boxes and color state, not just substring
   presence.

2. **Add `Samples/S11_OrdersStyleReport.cs`** using the existing
   sample pattern (see `Samples/S07_MultiPage.cs` and
   `Samples/S09_InvoiceSample.cs` for conventions — DirectContent,
   coordinate helpers, PdfTemplate for totals page count). The
   sample MUST:
   - open a landscape `Document`,
   - write composer-page 1's `reportHeader` band to DirectContent
     **before** calling `NewPage()` — this is the exact pattern
     that hit the 1.1.1 stacking bug; it is also the pattern the
     real `PdfExporter` uses,
   - loop the subsequent pages writing `pageHeader` + grouped
     detail bands + `pageFooter`, calling `document.NewPage()`
     at the top of each iteration except the first,
   - draw a `reportFooter` on the last page, then `Close()`,
   - use `IPdfPageEvent` for the per-page `pageFooter` so the
     `Page N of M` template flows naturally,
   - populate the title element with an explicit fill color set
     immediately before the `Tj` operator (`SetColorFill` or the
     equivalent DirectContent API) so the test can assert the
     expected color is on the graphics-state stack at title
     render time,
   - generate at least 40 synthetic detail rows so the output
     paginates to 3+ physical pages.

3. **Register the sample** in `Program.cs` between `S10_StressTest`
   and the end of the roster.

4. **Run the sample suite.** `dotnet run --project DabblerPdf.Samples`
   must produce `Output/11-orders-style-report.pdf` successfully.
   Open it in the host's PDF viewer (Adobe Reader if available)
   and visually confirm:
   - Page 1 shows the title at the top.
   - Title color matches the intended dark navy — not the
     background-fill color of any later band.
   - Each page has its own `Page X of N` footer.
   - Column headers repeat on every page.
   - Group backgrounds render behind their header text, not over
     subsequent rows.

   Capture this visual inspection as a signed paragraph in a new
   `Samples/S11_FINDINGS.md` (or as an addition to the existing
   `DabblerPdf.Samples/FINDINGS.md`, noting the date and the
   1.1.1 package version observed).

5. **Add PdfPig rendering-visibility assertions** under
   `tests/DabblerPdf.Tests/` in a new file
   `OrdersStyleSampleRenderingTests.cs` (or extend an existing
   rendering-visibility file, at orchestrator discretion):
   - Drive the sample programmatically (call the same code path
     the runner calls — do not duplicate the band-drawing
     logic in the test), capture the `byte[]`, hand to
     `PdfDocument.Open(bytes)`.
   - Assert page count equals the expected number of physical
     pages (derived from the sample's row count + rows per
     page math).
   - On page 1: a word whose text equals the sample's title
     literal, Helvetica-Bold, with bounding-box top Y within the
     top 20% of the page. No `Page 2 of` footer on page 1.
   - On every page: exactly one `Page X of N` footer.
   - Title color assertion: parse the content stream for page 1,
     locate the title's `Tj` operator, walk backward to the most
     recent `rg`/`RG` operator, and assert it matches the
     intended dark-navy triple. This is the structural equivalent
     of "the title is not rendered in an invisible color."

6. **Build and test.** Full DabblerPdf solution:
   `dotnet build DabblerPdf.slnx` then `dotnet test DabblerPdf.slnx`.
   Both must pass. The sample suite (`dotnet run --project
   DabblerPdf.Samples`) must also produce the new PDF cleanly.

7. **Branch on the outcome.**

   - **If the sample renders the title visibly AND all PdfPig
     assertions pass:** the 1.1.1 fix is sufficient. Update
     `CHANGELOG.md` with a brief "1.1.1 verified via
     orders-style sample" note (no version bump — 1.1.1 is
     already packaged). Prepare a tight `change-log.md` that
     recommends the human push the existing 1.1.1 nupkg.

   - **If the sample reveals a rendering bug** (title overdrawn,
     leaked color, off-page, etc.): do NOT try to fix it in this
     session. Instead, capture the exact evidence (the offending
     content-stream excerpt, the rendered-vs-expected position
     data, the reasoning about root cause) and produce:
     - A short `diagnosis.md` under this set's folder naming the
       hypothesized root cause and the content-stream evidence.
     - A stub follow-up spec at
       `docs/session-sets/title-rendering-fix-<specific-symptom>/spec.md`
       with a focused 1-session plan for the fix.
     - A note in the change-log.md recommending the human HOLD
       the 1.1.1 push until the follow-up set ships (re-package
       as 1.1.2 after fix; the orchestrator should NOT attempt
       to re-use the 1.1.1 version number).

8. **Cross-provider verification** via
   `route(task_type="session-verification")`. Include in the
   prompt: the new sample file, the new PdfPig test, the
   findings/diagnosis artifact, and an out-of-scope section
   naming the `PdfWriter.EndCurrentPage` change that shipped in
   1.1.1 and the `PdfExporter` pattern in `dabbler-platform`
   (neither is changed this session). Ask the verifier to
   independently judge whether the sample genuinely reproduces
   the Orders-by-Retail-Business pattern the user's real report
   uses — not whether the assertions pass.

9. **Generate `change-log.md`** via
   `route(task_type="summarization", max_tier=1)`. The content
   depends on the Step 7 branch — see the two outputs in that
   step.

10. **Commit, push, notify** via the Pushover helper. The
    notification summary MUST clearly state whether the
    orchestrator recommends pushing 1.1.1 or holding.

11. **Step 9 reorganization review.** Short — this is a
    focused set and will usually produce "no changes
    recommended." The lesson worth watching for promotion is the
    "String In Bytes" Is Not "Rendered Visible" lesson — if it
    was applied in a second context here (by including
    bounding-box + color-state assertions), note that as the
    second application and leave it in Lessons until a third
    context lands.

**Creates:** `DabblerPdf.Samples/Samples/S11_OrdersStyleReport.cs`,
`tests/DabblerPdf.Tests/OrdersStyleSampleRenderingTests.cs` (or
equivalent), `DabblerPdf.Samples/Output/11-orders-style-report.pdf`
(the generated PDF, for the human to inspect — commit this file
so the human can open it from a clean clone),
`change-log.md`, optional `diagnosis.md` + stub
follow-up spec (only if the sample reveals a bug).

**Touches:** `DabblerPdf.Samples/Program.cs` (register the new
sample), `CHANGELOG.md` (verification note or hold note).

**Ends with:** The human has a visibly-inspectable PDF in hand
that uses the same DirectContent + reportHeader-band + multi-page
pattern as the real Orders By Retail Business report. The
orchestrator has made a recommendation in the change log about
pushing 1.1.1. The next step belongs to the human.

**Progress keys:** `session-1/read-planning`,
`session-1/sample-implementation`,
`session-1/sample-runner-registration`,
`session-1/sample-generation`,
`session-1/visual-inspection-findings`,
`session-1/pdfpig-assertions`,
`session-1/build-and-test`, `session-1/outcome-branch`,
`session-1/verification`, `session-1/change-log`,
`session-1/commit-push-notify`, `session-1/reorg-review`.

---

## Risk notes

- **Sampling distance from the real report matters.** If the
  sample drifts too far from the Orders By Retail Business
  structure (different band order, different color-state
  pattern, no explicit color leak setup), it will not reliably
  reproduce the symptom the human observes. Sanity-check the
  sample by cross-referencing the real RDF at
  `../../../../dabbler-platform/src/Apps/AcmeCoffeeOnline.Web/Reports/orders-by-customer.rdf.json`
  before adding the test assertions.
- **Color-state leak is a plausible-but-separate failure mode.**
  The title is drawn BEFORE any background fills in both the real
  report and the spec'd sample, so a `rg`-leak from a later band
  cannot blot out the title by color alone. But if the fix
  migration or the sample's DirectContent ordering differs, the
  leak could surface on the title. The color-state assertion in
  Step 5 is the guard.
- **Adobe Reader vs other viewers.** The human's observation is
  "Adobe Reader shows no title." Different PDF viewers
  occasionally disagree on edge cases (font substitution, z-order
  of translucent overlays, color-space interpretation). The
  orchestrator should mention in the change-log which viewer(s)
  they used for their own spot-check. If the sample renders
  cleanly in Adobe Reader, that is the answer the human needs.
- **Don't ship the bug re-fix in this set.** If Step 7 reveals a
  real rendering bug, Step 7's stub spec is the correct
  delivery — not a mid-session pivot to fix and re-package.
  Splitting investigation from fix keeps the human's verification
  signal clean and the version-number semantics honest.
- **Commit the generated PDF to the repo.** This is a reversal
  of the usual "no generated artifacts in git" stance. The
  rationale is that the sample PDF IS the deliverable the human
  opens to make the push decision — having it committed means
  a clean clone produces it identically, and a future reviewer
  can see the exact bytes the decision was made on. Place it
  under `DabblerPdf.Samples/Output/` and `.gitignore` the rest
  of that folder if it is not already.
