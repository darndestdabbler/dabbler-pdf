# Project Guidance — DabblerPdf

> **Purpose:** The single source of truth for durable strategic commitments
> (Principles) and specific rules and patterns (Conventions) that apply to
> this repo.

Read this file before every AI-led session. Use it as the first-stop reference
before changing library design, testing strategy, workflow assets, or
human-review conventions.

Session-set specs may narrow the scope for a particular effort, but they should
not silently violate anything here without explicit human approval.

The file has two top-level sections:

- **Principles** — durable strategic commitments (the *why* and *what*). Slow
  to change; every principle should explain the reasoning behind itself.
- **Conventions** — specific rules, patterns, values, and code styles (the
  *how*). Faster to change; often promoted from successful lessons or
  demoted from over-general principles.

When a principle implies a specific rule, put the principle under Principles
and the concrete rule under Conventions. At the end of every session set the
orchestrator proposes reorganization candidates — promotions from Lessons to
Conventions/Principles, demotions, relocations — and the human accepts or
rejects each (see `docs/ai-led-session-workflow.md` Step 9, post-notify).

---

## Principles

### Library Design

- **Scoped port, not complete port.** DabblerPdf ports only the OpenPDF
  subset needed for text measurement (`ITextMeasurer`) and coordinate-based
  PDF rendering. Do not port the rest of OpenPDF's surface — consumers that
  need richer features should be redirected to OpenPDF or a full PDF
  library, not accreted onto DabblerPdf.
- **Self-owned, not re-wrapped.** The goal is a library the team controls,
  not a thin wrapper around SkiaSharp or PdfSharp. Inside the library we
  own the types and the code paths.
- **Idiomatic C# over mechanical Java translation.** The source is Java
  OpenPDF, but this is a C# library: use properties over getters/setters,
  `ReadOnlySpan<T>` where it helps, `using` declarations, nullable reference
  types, and `IDisposable` contracts that match .NET expectations.
- **Stay on the current .NET LTS line unless the human explicitly approves
  a change.**
- **License discipline.** The project is dual-licensed LGPL-2.1-only AND
  MPL-2.0 to match OpenPDF. All source files must carry the appropriate
  license header.
- **Coordinate-first, not layout-engine.** DabblerPdf renders already-laid-out
  elements at explicit coordinates. Layout composition lives in the
  consumer (`Dabbler.Report`). Don't grow DabblerPdf into a layout engine —
  that scope is deliberately out.
- **No UI, no filesystem assumptions.** The library writes to streams
  callers supply. It does not open files, decode HTML, fetch URLs, or
  assume a working directory.

### Public API Stability

- **The public surface is a contract.** Once a type, method, or property
  is released in a NuGet package, breaking changes require a deliberate
  decision (bumped major version, change log entry, migration note).
- **Prefer additive changes.** New overloads, new optional parameters on
  builder-style APIs, new types — these are cheap. Removing or renaming
  public members is expensive and should be done only when the benefit is
  clear.
- **XML documentation comments are required on every public type and
  member.** `dotnet build` runs with `GenerateDocumentationFile=true`; a
  missing doc comment on a public API is a warning that must be fixed.
- **Avoid leaking OpenPDF-Java-ism into the C# surface.** If an OpenPDF
  type has a name or shape that reads strangely in C# (e.g., a boolean
  parameter with two positional true/false options), prefer a C#-idiomatic
  shape in the public API even when the internal implementation mirrors
  OpenPDF more directly.

### Testing Hierarchy

Testing follows this order of preference:

1. Strict unit tests
2. Integration tests
3. Sample-based review (PDFs generated from runnable samples)
4. Human review through the checklist editor

**Strict unit tests first.** Use them for any logic that can be isolated
from I/O — font metrics arithmetic, coordinate translation, encoding helpers,
byte-level PDF content stream assertions. Structure code so unit-testable
portions are factored into methods or services that do not require a
`Document`, `PdfWriter`, or real stream.

**Integration tests second.** When a feature requires a real `Document`,
`PdfWriter`, and produced byte stream, write an integration test that
drives the public API and asserts against the resulting stream (size
non-zero, valid PDF header, byte-level structural checks where they
matter).

**Sample-based review third.** For features whose correctness is visual
— text layout, image placement, multi-page footers, wrapping behavior —
add or update a sample under `DabblerPdf.Samples/Samples/` that produces
a real PDF. The sample exists so the human (and any downstream reviewer)
can open the PDF and see the feature working.

**Human review last.** Human review is a confidence and judgment layer,
not a replacement for the automated hierarchy. Use it for visual polish,
coherence, legibility, and other judgments that automation cannot score
well. If human review starts finding numerous broken features, the
automation strategy needs correction before more human review continues.

### Human Review Philosophy

> Human review judges visual quality and typographic feel, not
> correctness. Every functional claim in a review checklist must have
> automated coverage (unit, integration, or sample that exercises the
> same code path). Items that genuinely cannot be automated must be
> flagged as judgment items with justification.

- **Step N in the review checklist corresponds to Step N in the
  automated test or sample.** Identical actions. Identical parameters.
  Identical verifications. Not "equivalent" — identical. The discipline
  is mechanical on purpose: if "equivalent" were enough, every drift
  point between prose and automation would re-surface as a human
  surprise.
- **Parameter values are shared, not parallel.** The font name, size,
  position, color, image path, page dimensions the human will inspect
  are the same literal values the test or sample drove.
- **Tests pass and samples build before the human is notified.**
  Sequencing matters: the xUnit suite runs green first, the samples
  build and produce their expected outputs, `uat-coverage-review`
  returns VERIFIED on the pair, then the commit lands, then the human
  is notified. Asking the human to triage a red test or a crashing
  sample is the failure mode this principle exists to prevent.
- **Judgment items still get automated reachability.** An item flagged
  `IsJudgmentItem: true` is exempt from the matching-assertion
  requirement but not from the reachability requirement — a sample
  must still produce the PDF the human will look at, so the human is
  rendering judgment on a verified-live artifact rather than an
  exception. A one-sentence justification accompanies every judgment
  flag.
- **A filled review checklist does not review itself.** Between the
  human finishing the checklist and any downstream remediation work
  starting, run a dedicated **Review Session** to disposition each
  finding (Blocker / Substantive / Minor / Out of scope). Skipping
  this is how findings sit unaddressed for weeks or get folded
  opportunistically into an unrelated set, losing the human's signal.
- **Review Session (set type).** A lightweight 1-session set whose
  single deliverable is a disposition for every finding in a
  just-completed review checklist.
  - **Input:** the filled `<set-slug>-uat-checklist.json` committed at
    the end of the prior set's closeout.
  - **Steps:**
    1. Read every Review item where `Passes == false` or `Feedback`
       is non-empty (including auto-generated "Other" rows).
    2. Classify each finding as one of: **Blocker** (ship-stopping,
       must remediate before any other set starts), **Substantive**
       (real defect, becomes scope of a new remediation set),
       **Minor** (polish or style, fold into an adjacent planned
       set's spec), or **Out of scope** (scope creep or rejected
       suggestion, documented with rationale).
    3. Produce one of three outputs depending on severity: a stub
       spec for a new remediation set, a one-commit micro-fix plan
       if findings are small enough, or a signoff note if nothing
       requires action.
    4. Commit the disposition artifact alongside a short
       review-summary commit message.
  - **Ends with:** the next action queued — either a named next set,
    a named next single-session fix, or an explicit "no remediation
    needed" signoff. The human is not left holding the checklist.

### Precedence

When guidance appears in multiple places, apply it in this order:

1. Explicit human direction in the current conversation
2. Session-specific spec requirements
3. This document
4. Supporting planning notes

If those sources conflict in a meaningful way, stop and confirm with the
human before proceeding.

---

## Conventions

### Testing Conventions

- Factor pure-logic portions out of mixed-responsibility methods so they
  can be strict-unit-tested. Do not keep mixed-responsibility methods
  intact when the pure-logic portion can be split out.
- Integration tests should not become a substitute for unit tests when
  logic could have been isolated earlier.
- For features with visible output, pair automated tests with a sample
  in `DabblerPdf.Samples/Samples/` — the test proves correctness, the
  sample proves visible behavior.
- Do not commit samples whose output is not reviewable. If a sample
  cannot produce a PDF without manual setup, fix the sample or remove
  it.
- Byte-level PDF content stream assertions are acceptable when they
  catch a real bug, but prefer assertions against observable behavior
  (rendered text position, color state after drawing, dimensions of
  the produced page) when both paths are available.
- **Visibility assertions go through a rendering-aware parser.** Tests
  that assert "content is visible in the PDF" must exercise a
  rendering-aware parser that models per-physical-page visible text
  with bounding boxes (PdfPig, pymupdf, or equivalent). Raw
  content-stream byte greps are wiring-presence assertions only —
  they cannot catch content that was overdrawn, clipped, off-page, or
  rendered in an unavailable font. Single-page tests may still use
  byte-level asserts for specific structural properties (object IDs,
  dictionary keys), but visibility assertions always go through the
  rendering-aware parser. The canonical failure that motivated this
  rule is captured in `lessons-learned.md` → "String In Bytes" Is Not
  "Rendered Visible": a byte-grep test passed on a PDF whose title was
  overdrawn by a later full-page fill, and the regression surfaced
  only in human UAT.

### Human Review And Checklist Editor

- Use the checklist editor schema from
  `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`
  for checklist JSON files intended for human execution.
- Open and run those files through
  `https://darndestdabbler.github.io/uat-checklist-editor/`.
- Save human review results inline in the checklist JSON itself. Do
  not create a separate empty `*-findings.json` scaffold for pending
  review.
- **Generate a new checklist per session set.** Name the file
  `{session-set-slug}-uat-checklist.json` and place it inside that
  session set's folder. Do not re-use or re-run a checklist from an
  earlier session set.
- Include concrete test data (font names, sizes, page dimensions,
  sample file names) in the `HumanAction` field for any step that
  requires locating or verifying a specific element.
- When a session creates or refreshes a review checklist and that
  review has not yet been completed, treat the pending human step as
  a blocker for new sessions unless the human explicitly overrides it.
- Handoff messages for pending review must be unmistakable: name the
  checklist path, link the editor, say that the agent is waiting for
  the human response, and keep any optional parallel work low-risk.
- **Automated pre-verification per review item is mandatory.** Before
  committing a checklist and notifying the human, each functional
  `Review` item must have a matching unit test, integration test, or
  sample assertion that drives the same inputs and confirms the same
  observable outcome. Items whose expected outcome is a subjective
  visual judgment still require a sample that produces the PDF the
  human will evaluate, so the human is looking at a verified-live
  artifact. This is not optional. Human review is confidence and
  judgment, not first-pass operability verification.

### Code Style

- Namespaces: `DabblerPdf` (root), `DabblerPdf.Document`,
  `DabblerPdf.Pdf`, `DabblerPdf.Text`, `DabblerPdf.Image`,
  `DabblerPdf.Utilities`.
- PascalCase for public members; `_camelCase` for private fields.
- Nullable reference types enabled on all projects.
- All asynchronous methods return `Task` or `ValueTask` and use the
  `Async` suffix.
- Every source file carries the dual LGPL-2.1-only / MPL-2.0 license
  header that matches OpenPDF's header conventions.
- Public types and members carry XML documentation comments
  (`/// <summary>`, `<param>`, `<returns>`, `<remarks>`).
- Internal types are free of XML docs unless their shape is
  non-obvious to a future contributor.

### Workflow Expectations

- Before every session, read this file and `docs/planning/lessons-learned.md`.
- Keep `README.md`, `CHANGELOG.md`, XML doc comments, and sample
  `FINDINGS.md` / `HUMAN_FEEDBACK.md` current enough to describe the
  validated system rather than historical assumptions.
- Save verifier output raw and never edit session-review files after
  they are written.
- Log every AI-led session step in the active session set.
- AI instruction documents (`CLAUDE.md`, `AGENTS.md`, `GEMINI.md`)
  reference this file and `docs/ai-led-session-workflow.md` so future
  agent runs inherit the same durable expectations.
- When the human gives a session instruction or decision that appears
  durable enough to guide future sessions, ask whether it should be
  incorporated into this document.
- When a failure reveals a reusable strategy, recommend a corresponding
  update to `docs/planning/lessons-learned.md`.
- Before calling `route(task_type="session-verification")`, include
  related files the verifier might reasonably look for, or disclose
  them in an out-of-scope section of the prompt. Missing context
  produces false positives about "missing" code that actually lives
  in a sibling file; the verifier only sees what the prompt includes.
- Before physically deleting a type whose consumers are referenced by
  a later session or session set, count the consumer files. If a
  scheduled future session already owns the consumer rewrite, mark
  the type `[Obsolete]` instead of deleting it. The compiler warnings
  surface on every consumer so no reference slips through, the build
  stays green, and the deletion lands naturally in the session that
  rewrites the last consumer. Disclose the deviation in the
  end-of-session verification prompt.
- When the orchestrator model matches the routing target for a given
  task type, handle the task in-conversation rather than calling
  `route()`. A `route()` call into the orchestrator's own model pays
  the provider's API cost to summon another instance of the same
  judgment. The non-negotiable exception is `session-verification`,
  which MUST go through the router regardless, because the whole
  point is a cross-provider second opinion. Complements the existing
  Delegation Discipline criteria in
  `docs/ai-led-session-workflow.md`.
