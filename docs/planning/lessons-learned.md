# Lessons Learned

> **Purpose:** Capture durable tactics, failure patterns, and workflow lessons
> that should influence future sessions before the same mistake repeats.
> **Status:** Start small in one file. If this grows too large, split it into a
> `lessons-learned/` folder with a short index and topic files.

Review this file before every AI-led session.

Unlike `project-guidance.md`, which captures durable principles and
conventions, this file is for practical lessons learned from actual execution
in this repo. A lesson that has been applied as the right call in at least
two different contexts is a promotion candidate for Conventions (or, rarely,
Principles) in `project-guidance.md`. See `docs/ai-led-session-workflow.md`
Step 9 (run after the Step 8 notify) for the end-of-session-set reorganization
review.

## How To Use This File

- Read it before starting a new session.
- Add entries when a failure, surprise, or avoidable friction reveals a better
  repeatable strategy.
- Prefer short, action-oriented lessons over long retrospectives.
- When a lesson has proven itself in two or more different contexts, propose
  promoting it to `project-guidance.md` — as a Convention if it is a specific
  rule, or a Principle if it is durable strategy. Never delete a lesson; only
  move it.

## Entry Template

Use this structure for new entries:

```markdown
## <Short lesson title>

- **Context:** What kind of work exposed the issue
- **Failure or friction:** What went wrong
- **Lesson:** The durable takeaway
- **Action for future sessions:** What the AI orchestrator should do next time
```

## Current Lessons

## Do Not Run Build And Test In Parallel Against The Same Worktree

- **Context:** Full-solution validation runs on Windows
- **Failure or friction:** Running `dotnet build` and `dotnet test` in parallel
  caused file-lock contention and obscured the real validation result.
- **Lesson:** In this repo, build and test should be run sequentially unless
  there is a specific reason to parallelize isolated targets safely.
- **Action for future sessions:** Prefer sequential validation, and if lock
  contention appears, use `dotnet build-server shutdown` plus conservative
  build settings before retrying.

## Send Related Files To The Cross-Provider Verifier Or Disclaim What Is Out Of Scope

- **Promoted to:** `project-guidance.md` → Conventions → Workflow Expectations.
  Applied successfully across multiple session sets.
- **Original entry preserved below for context.**
- **Context:** End-of-session cross-provider verification via the AI router
- **Failure or friction:** Round-1 verification produced false positives
  claiming that methods were missing from a file, when those methods
  actually lived in a sibling file that was not included in the
  verification prompt.
- **Lesson:** The verifier only sees what the prompt includes. If the session
  touches one file in a conceptual area, related files in the same area
  should be supplied too, or the prompt should explicitly say what lives
  in which sibling file and is not being changed this session, so the
  verifier does not hallucinate missing code.
- **Action for future sessions:** Before calling the router for verification,
  list related files the verifier might reasonably look for and either
  include them or add a "files not in scope but present in the repo"
  section to the prompt.

## Mark `[Obsolete]` Instead Of Deleting Cascading Types When A Future Session Already Owns The Consumer Rewrite

- **Promoted to:** `project-guidance.md` → Conventions → Workflow Expectations.
  Applied successfully across multiple session sets where physically
  deleting a widely-referenced type would force a downstream session over
  its boundaries.
- **Original entry preserved below for context.**
- **Context:** A session was instructed to physically delete types whose
  consumers numbered in the dozens, with most of those consumers owned
  by a later scheduled session.
- **Failure or friction:** Physical deletion would have forced the later
  session's rewrite into the current conversation, violating the
  one-session-per-conversation rule and overrunning the spec's session
  boundaries.
- **Lesson:** When a spec calls for physical deletion of a type whose
  consumers number in the dozens and a later session or set is already
  scheduled to rewrite those consumers, marking the type `[Obsolete]`
  preserves the deletion signal (compiler warnings on every consumer)
  while keeping the build green and respecting session boundaries.
- **Action for future sessions:** Before physically deleting a type, count
  consumer files. If a scheduled future session already owns the consumer
  rewrite, mark `[Obsolete]` instead and disclose the deviation in the
  cross-provider verification prompt. The deletion lands naturally in the
  session that rewrites the last consumer.

## Align Checklist Wording To The Test's Literal Assertions Early To Avoid `uat-coverage-review` Round Escalation

- **Context:** The `uat-coverage-review` gate between a generated checklist
  and the matching automated coverage.
- **Failure or friction:** Most round-to-round churn on the coverage gate
  comes from wording drift between checklist verifications and test
  assertions rather than functional coverage gaps. Each mismatch escalates
  a round because the verifier is explicitly tuned to prefer false-positive
  `ISSUES_FOUND` over false-negative `VERIFIED`.
- **Lesson:** When authoring a review checklist for a new finding, write
  the verification text against the EXACT assertion the test will land —
  same API (prefer structured `Assert` / `Expect` patterns over
  substring matches against rendered output), same positional claims
  (don't say "below X" unless the test asserts position). Review your
  own checklist through a "would the verifier accept this as an
  identical match" lens before the first route call. Alternatively,
  generate the checklist FROM the finished test rather than the other
  way around, since the test's language is the mechanically-verifiable
  ground truth.
- **Action for future sessions:** Before routing `uat-coverage-review`,
  do a two-column dry-run in your head: every `Verifications[i].Text`
  next to the test line that will be paired with it. If the wording
  diverges, tighten it now — a wording pass costs zero API dollars; a
  coverage-review round costs real money.

## `route(task_type=documentation)` Wraps Content In A Code-Documentation Template

- **Context:** Routing a change-log generation call through the AI router.
- **Failure or friction:** The `documentation` prompt template at
  `ai-router/prompt-templates/task-types/documentation.md` is shaped for
  "document this code" work (purpose / public API / usage examples /
  caveats). When the content is narrative meta-prose like a change log,
  the wrapped prompt produces a `400 Bad Request` from the provider.
  The same content sent with the plain system prompt returns 200.
- **Lesson:** The router's per-task-type template meaningfully alters the
  prompt shape. For narrative docs (change logs, READMEs, summaries,
  any free-form prose), use `task_type="summarization"` (no template in
  `prompt-templates/task-types/`, so content passes through raw) rather
  than `task_type="documentation"`, which is reserved for code
  documentation. If a mysterious `400` surfaces in the router but a
  direct API call returns 200, suspect the task-template wrapping first.
- **Action for future sessions:** Reserve `task_type="documentation"`
  for code-doc generation. For change logs, release notes, session
  summaries, and similar narrative prose, use `task_type="summarization"`.

## Heuristics That Earn Their Keep: Stop Tuning The Router Complexity Score

- **Context:** AI router complexity scoring (`complexity.weights` and
  `task_type_scores` in `router-config.yaml`). The 1-100 heuristic
  maps every incoming task to a tier via four weighted components
  (context length, keyword signals, task type, explicit hint).
- **Failure or friction:** None — the heuristic works well enough.
  The temptation is to invest engineering in tightening it
  (embeddings, ML-based prediction, per-repo calibration) to squeeze
  the escalation rate lower.
- **Lesson:** The current heuristic plus the 2-try escalation safety
  net is the intended stopping point. When the heuristic misjudges,
  escalation catches it and the metrics log records the pattern. The
  engineering time saved is worth more than the handful of retried
  API calls the tighter heuristic would prevent. Do not introduce ML
  or embeddings here; it is a false economy.
- **Action for future sessions:** If a review surfaces "the router
  should predict complexity better", point at this entry and the
  comment block above `complexity:` in `router-config.yaml`. The
  only acceptable knob changes are adjusting the existing weights or
  adding keywords to the high/low lists — both of which are data
  entry, not engineering.

## Verifier Selection Is Rule-Based, Not Map-Based

- **Context:** Cross-provider verifier selection (`verification.py`
  `pick_verifier_model`, `router-config.yaml` verification section).
- **Failure or friction:** An earlier implementation used a static
  `cross_provider_map` that tied specific models to specific verifier
  targets. Retiring or swapping a model meant hunting through a pairing
  table in addition to the `models:` section.
- **Lesson:** Verifier eligibility is a role (different provider,
  enabled as verifier, tier match, cheapest survivor), not a
  user-to-user binding. The RBAC analogy holds: assign permissions to
  the role, not to the named account. Preferred pairings become optional
  hints that only take effect when they survive the rules.
- **Action for future sessions:** When adding a model to
  `router-config.yaml`, add it with `is_enabled_as_verifier: false`
  until calibration data justifies promoting it. When retiring a
  model, flip `is_enabled_as_verifier: false` first, confirm the
  rules-based fallback lands on an acceptable verifier, then remove
  the entry. Never re-introduce a map-first selection pattern.

## Mechanical-Gate Task Types Calibrate To Sonnet-Cost For Adequate Quality

- **Context:** The `uat-coverage-review` gate that performs per-item
  Step-N = Step-N correspondence checks between a generated checklist
  and a matching test file.
- **Failure or friction:** Opus at high effort on a tiny per-item
  prompt (~2–4 KB in, one-word verdict out) is overkill for mechanical
  pattern matching with placeholder substitution. Opus-gate cost runs
  ~$0.20–$0.30 per item, plus auto-verifier cost. Moreover, when the
  orchestrator is itself Opus, an Opus-pinned route pays the provider
  to re-summon another instance of the current judgment.
- **Lesson:** Before pinning a task type to Opus via
  `routing.task_type_overrides`, distinguish mechanical correspondence
  from synthesis:
  - **Mechanical correspondence** (gate checks, schema validation,
    step-by-step parity, redaction audits) → Sonnet with cross-provider
    verify covers it. Cheap-model risk is false-VERIFIED; mitigate with
    explicit prompt instruction ("prefer false-positive ISSUES_FOUND
    over false-negative VERIFIED") and keep auto-verify on.
  - **Synthesis** (plan generation, spec expansion, novel test
    authoring) → Opus earns its cost.
- **Action for future sessions:** When a task type is paired
  (generator + gate), apply the split: pin the synthesis half to
  Opus via `task_type_overrides`, let the mechanical half resolve via
  the router's cost-tier picker (or pin it to Sonnet explicitly). The
  paired `uat-plan-generation` (synthesis → Opus) and
  `uat-coverage-review` (mechanical → Sonnet) is the canonical example.

## Isolate Flaky-Under-Parallel Tests In A Dedicated Collection; Skip Deterministic Failures Explicitly

- **Context:** Full-suite test runs that combine many integration-level
  tests with shared resources.
- **Failure or friction:** A test that passed in isolation could fail
  under full-suite parallelism because of shared-resource contention.
  Another test could fail deterministically even in isolation because
  the code path was genuinely broken. Treating both as "flaky" hid the
  real failure behind a shared label.
- **Lesson:** Split disposition by symptom, not by label:
  - **Passes alone, fails under parallelism** → move to a dedicated
    collection marked
    `[CollectionDefinition("...", DisableParallelization = true)]`.
    The collection still runs, still gates, but no other collection runs
    in parallel with it. Carry a `TODO(tracked):` comment pointing at
    the proper root-cause investigation.
  - **Fails deterministically even in isolation** → use
    `[Fact(Skip = "<precise rationale>")]`. Do not hide a real failure
    behind a "flaky" label — hiding green lies is worse than a visible
    skip, because future sessions lose the signal that a specific code
    path is broken.
- **Action for future sessions:** When a test fails, run it in
  isolation before deciding disposition. If isolated-pass ≠ suite-pass,
  it goes in a dedicated serialized collection. If isolated-fail, it
  gets `[Skip]` with a full rationale.

## Cross-Repo Session Sets — Each Repo Owns Its Own Spec; Coordinate Via The Build Artifact

- **Context:** A session set whose work has to land in two repos
  (e.g., `pdf-info-metadata-api` — library code in `dabbler-pdf`,
  consumer wiring in `dabbler-platform`). Each repo runs its own
  orchestrator with its own activity log; the workflow is not
  multi-repo aware.
- **Failure or friction:** A naive approach copies the same spec into
  both repos with identical session numbering. Then both `find_active
  _session_set` views see the same incomplete set, both Step 9
  reorgs run, and the activity logs disagree on what step the work
  is at. The change-log generation fires twice with conflicting
  views.
- **Lesson:** Each repo gets its own spec.md with session numbering
  scoped to the work that actually runs in that repo. The two specs
  link to each other for context but are independently complete:
  - `dabbler-pdf` had a 3-session imported spec, of which only
    Session 2 (library implementation) ran here. Sessions 1 and 3
    were referenced for context but their work and logging happened
    elsewhere.
  - `dabbler-platform` had a 2-session spec — Session 1 (design,
    already done) and Session 2 (consumer wiring, still pending) —
    matching what runs in that repo.
  - The handoff is the build artifact: `dabbler-pdf` packs the
    nupkg, `dabbler-platform` consumes it. The artifact's
    reachability (local feed config, version pin in
    `Directory.Packages.props`) is what the cross-repo dependency
    is expressed through.
  - Each repo's change-log describes what shipped from that repo.
    The two together document the full feature.
- **Action for future sessions:** When carving a session set whose
  work spans repos, write one spec per repo, scoped to that repo's
  sessions, with explicit pointers to the sibling spec and the
  artifact handoff. Do not re-use the same spec wholesale across
  both repos; if you do, treat the cross-pointing sessions as
  read-only context and only log the sessions that actually run in
  the local repo.

## After Packing A NuGet, Smoke-Test By Consuming The Actual `.nupkg` From A Throwaway Project

- **Context:** Releasing a new version of a NuGet package (here:
  `DabblerPdf 1.1.0`). Internal `tests/DabblerPdf.Tests/` exercises
  the same code paths but via a `<ProjectReference>`, not the packed
  artifact.
- **Failure or friction:** A project-reference test cannot detect:
  package metadata problems (missing dependencies, wrong target
  framework moniker, broken `PackageReadmeFile` reference); public
  API surface that compiles internally but is `internal` in the
  packed dll; `<InternalsVisibleTo>` leaks that change behavior in
  consumer projects; stale README that nuget.org will render to
  visitors. The first the team would learn about any of these is a
  consumer reporting a broken install.
- **Lesson:** After `dotnet pack`, set up a throwaway console
  project outside the repo with a `<PackageReference>` to the new
  version, restore from a local-folder feed pointing at `nupkg/`,
  and run a small program that exercises every new public method
  added in the release. Cheap (~2 minutes), independent of the
  source tree, and would catch all the failure modes above before
  the artifact reaches nuget.org or downstream consumers.
- **Action for future sessions:** For any NuGet version bump, after
  `dotnet pack` and before the public push, build a throwaway
  consumer that installs the packed `.nupkg`, exercises the new
  public surface, parses any output the public API produces (here:
  the resulting PDF bytes), and asserts the new behavior end-to-end.
  Document the smoke result in the session activity log so the
  artifact's consumability is evidence on the record. Delete the
  throwaway project after the run; do not commit it.

## "String In Bytes" Is Not "Rendered Visible" — Use A Rendering-Aware Parser

- **Context:** The `directcontent-multipage-fix-and-tests` session set
  was carved on 2026-04-22 after a real-world report PDF from
  `dabbler-platform` visibly rendered composer-page 2 on physical
  page 1 (column headers, no title), even though a consumer-side
  regression test confidently asserted the title literal was present
  in the PDF. The consumer test inflated the FlateDecode content
  streams and greped for the literal `"Orders by Retail Business"`
  — it passed because the text operator for the title WAS in
  physical page 1's content stream, positioned correctly. The
  bytes did not lie. But a later full-page white background fill
  (`0 0 792 612 re f`) in the same content stream painted over
  everything drawn before it. Adobe Reader showed a blank top half.
- **Failure or friction:** The regression test that shipped with
  the feature passed, human UAT caught the bug, and the team had
  to carve a follow-up session set to fix both the library bug
  and the testing gap. The deeper failure was design, not
  execution: a "literal in bytes" assertion cannot catch
  overdrawing, clipping, off-page positioning, or unavailable-font
  substitution. It is the PDF equivalent of asserting a DOM string
  contains a substring — which Playwright-the-community figured
  out years ago was not a visibility check.
- **Lesson:** For any test whose purpose is "content X is visible
  in the PDF," assert through a rendering-aware parser that
  models what a viewer will display per physical page —
  [PdfPig](https://github.com/UglyToad/PdfPig) for per-page text
  + bounding-box APIs in .NET, or an equivalent rasterize-then-
  diff approach. Raw content-stream byte greps are structural
  assertions (useful for "object ID N is present" or
  "/Info dictionary contains Title"), not visibility assertions.
  The one-sentence rule: bytes prove wiring, parsers prove
  rendering.
- **Action for future sessions:** When authoring or reviewing a
  test whose failure mode is "the user sees nothing," drive the
  assertion through PdfPig's `PdfDocument.Open(bytes).GetPage(n)`
  text-extraction API and check per-physical-page visibility.
  Reserve byte-level stream inspection for structural properties
  that a parser cannot surface, and call out that distinction
  explicitly in the test's XML doc comment so the next maintainer
  does not regress the coverage shape.
