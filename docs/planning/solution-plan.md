# Solution plan — DabblerPdf

> Imported from the operator's brief (`docs/planning/brief.md`, 2026-11-07)
> by Session 1. The substance is the operator's; this file arranges it for
> the framework. Session 2 challenges the cuts and decisions below and
> breaks the phases into numbered sessions.

## Objective

Keep `DabblerPdf` — a scoped, dependency-free C# port of OpenPDF for
.NET 10 that provides font metrics for layout and coordinate-based PDF
generation — something a successor can rebuild, test and republish
without the operator. Concretely, when this plan is done:

- the repository's GitHub front page and nuget.org's project link show
  the real library (`main`), not the March 2026 `OpenPdf.NET` port;
- nothing from the retired v1 workflow is left looking live;
- every push and pull request builds and tests on a clean runner;
- publication to nuget.org happens from CI on a version tag through
  trusted publishing, with no API key on anyone's machine;
- one maintainer document says how to clone, build, test, run the
  samples, pack, release, pin, and meet the license obligations, and the
  README says what to do if the library stops being maintained;
- release `1.1.3` has gone out through that pipeline as its proof.

Consumers: `dabbler-platform` (`Dabbler.Reports.Core`,
`Dabbler.Reports.Export`, pinned `DabblerPdf >= 1.1.2`) and anyone who
installs the package. The README's quick start is the public contract.

Out of scope: new library features, porting more of OpenPDF, growing into
a layout engine. Library changes after this plan are named by
`dabbler-platform`'s reporting work.

## Production split

Not applicable. DabblerPdf is a library: nothing runs separately in
production and nothing talks to a database. It runs in-process inside
whatever application references it.

## Handoff artifacts

| Part | Handed over as | Command |
|------|----------------|---------|
| `src/DabblerPdf` | NuGet package `DabblerPdf` (`.nupkg` + `.snupkg`) on nuget.org | `dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o {output}` |

The tests and samples are not handed over (`IsPackable=false` on the test
project; the samples are a console app for review). Today's release is a
hand `dotnet nuget push`; the plan moves it to a CI workflow on a
`nuget-v<version>` tag (Decision 3).

## Existing solution and the framework layout

`DabblerPdf.slnx` lists three projects:

| Project | Path | References |
|---------|------|------------|
| `DabblerPdf` (library, packable) | `src/DabblerPdf/` | none |
| `DabblerPdf.Samples` (console app, 11 samples, committed output PDFs) | `DabblerPdf.Samples/` | `DabblerPdf` |
| `DabblerPdf.Tests` (xUnit + PdfPig, 29 test files) | `tests/DabblerPdf.Tests/` | `DabblerPdf`, `DabblerPdf.Samples` |

Fit with the framework's module-per-folder layout:

- **Fits.** Each project is a `.csproj` in its own folder, all three are
  listed in the root solution file, and each reaches a sibling by
  `<ProjectReference>`. Tests follow the `{name}Tests.cs` convention under
  folders mirroring the library's namespaces (`Document`, `Image`, `Pdf`,
  `Text`, `Utilities`).
- **Does not quite fit, kept as is.** `DabblerPdf.Samples/` sits at the
  repository root rather than beside the library under `src/`, and the
  test project references the samples project so rendering-visibility
  tests drive the exact sample code path. Moving the samples would change
  paths named in the README, the committed output PDFs and consumers'
  memory for no functional gain. **No restructuring is proposed**; the
  person may approve one with this plan if they want it.
- **Scaffold to fix.** `dabbler.yaml`'s one suite `covers: ["."]` and
  `test_roots: ["."]`; Decision 4 narrows it.

## Modules

**One module: `DabblerPdf`.** The samples and tests are its satellites,
not modules.

- **Responsibility.** Measure text (Base 14 and TrueType font metrics,
  width/ascent/descent, word and character wrapping) and write
  already-laid-out elements — text, lines, rectangles, JPEG/PNG images,
  page events, `/Info` metadata — at explicit coordinates into a PDF.
  Coordinate translation between top-left inches and PDF points.
- **Contract.**
  - *Going in:* a caller-supplied writable stream; a `Document` opened
    before drawing and closed once; coordinates in PDF points (or
    translated through `CoordinateTranslator`); fonts by Base 14 name or
    a TrueType file path; image bytes in JPEG or PNG.
  - *Coming out:* a well-formed PDF written to that stream on `Close()`,
    with the content visible where it was drawn on the page it was drawn
    for; metric values consistent with what is rendered. Public types and
    members are documented; the public surface is stable within a major
    version (additive changes only).
  - *Failure:* `ArgumentException` for invalid input (bad colour, size,
    path, image data); `InvalidOperationException` for lifecycle misuse
    (a page before `Open()`, anything after `Close()`, no writer), an
    unbalanced graphics-state stack, or an unreadable font file;
    `NotSupportedException` for image formats, PNG colour types or font
    types the port does not handle.
- **Dependency direction.** `Samples → DabblerPdf`; `Tests → DabblerPdf`
  and `Tests → Samples`. The library depends on nothing but the BCL.
- **Why one cut.** The library is small, ships as one package, and has
  one consumer shape; splitting text metrics from rendering would add a
  second package and version-coordination cost with no consumer asking
  for metrics alone.
- **Deferred cuts.**
  1. *Separate `DabblerPdf.Metrics` package* for consumers that only
     measure text. Deferred until a consumer needs it.
  2. *Moving `DabblerPdf.Samples` under `src/` or `samples/`.* Deferred;
     see the layout note above.
  3. *Packaging through `dabbler.yaml`'s `packaging:` block.* Deferred
     until the framework's `release: tag` can name a `nuget-v` tag; until
     then a release is a version-bump session followed by the operator's
     hand tag (the arrangement `dabbler-platform` records as its deferred
     cut 12).

## Decisions for Session 2 to challenge

Each is the operator's leaning from the brief.

1. **Branches.** The operator switches GitHub's default to `main`;
   `master` (the `OpenPdf.NET` port) is renamed to
   `archive/openpdf-port-2026-03` rather than deleted.
2. **v1 relics.** Delete `ai-router/`, `docs/ai-led-session-workflow.md`,
   `docs/claude-instructions/`, `docs/gemini-instructions/` and
   `GEMINI.md`; replace the v1 body of `AGENTS.md` outside the managed
   fence with a short project note; move `docs/session-sets/*` under
   `docs/session-sets/_archived/` with a README saying they are history
   (archive, not `dabbler session migrate`). Import what still holds from
   `project-guidance.md` and `lessons-learned.md` into
   `docs/MAINTAINING.md` and retire `project-status.md`.
3. **CI and publication.** `.github/workflows/build.yml` builds and tests
   on push and pull request. `.github/workflows/publish.yml` packs and
   pushes to nuget.org on an annotated `nuget-v<version>` tag through
   trusted publishing (OIDC), as `dabbler-platform` and
   `Dabbler.AccessHarvester.Tool` do, failing before packing unless the
   tag's version equals `<Version>` in the csproj; a `workflow_dispatch`
   dry run pushes to GitHub Packages. The operator creates the
   nuget.org trusted-publishing policy (owner `darndestdabbler`,
   *Workflow File* matching the filename exactly).
4. **`dabbler.yaml`.** The suite covers `src`, `tests` and
   `DabblerPdf.Samples`, with `test_roots: [tests]`, `expensive: true` and
   `dotnet test --blame-hang-timeout 5m`. `packaging:` stays undeclared,
   with a comment explaining the hand tag (deferred cut 3).
5. **Maintainer guide and policy.** `docs/MAINTAINING.md`;
   `.github/dependabot.yml` for the .NET SDK and test packages; a README
   section "If this library stops being maintained" (vendor the source
   under either license); fix the `LICENSE` attribution line, which still
   names "openpdf-dotnet contributors".
6. **Release `1.1.3`.** No library change; it proves the tag-driven
   pipeline from a machine that holds no key. If anything consumer-facing
   lands first, it is `1.2.0` instead.

## Phases and key deliverables

Alignment comes first: the repository's documents, suite and layout
fitted to the projects that exist, before any other work.

| Phase | Deliverables | Brief's proposed session |
|-------|--------------|--------------------------|
| A. Alignment and hygiene | `master` archived (after the operator's default-branch switch); v1 relics deleted or archived; `AGENTS.md` top rewritten; `dabbler.yaml` suite narrowed with hang limit (Decisions 1, 2, 4) | 3, with the suite in 6 |
| B. Continuous integration | `build.yml` green on a clean runner (Decision 3, build half) | 4 |
| C. Trusted publishing | `publish.yml` with the version-equality gate and a dry run; operator configures nuget.org (Decision 3, publish half) | 5 |
| D. Maintainer guide | `docs/MAINTAINING.md`, Dependabot, `LICENSE` fix, README fallback section (Decision 5) | 6 |
| E. Release 1.1.3 | Version and `CHANGELOG.md` bump; operator hand-tags `nuget-v1.1.3`; CI publishes (Decision 6). A `(release: 1.1.3)` session if the framework can name the tag by then. | 7 |

After E the work is consumer-driven: `dabbler-platform`'s reporting names
the next change here. Proposed release version for this plan: **1.1.3
(patch)** — nothing in A–D changes the library's public surface.

## What carries over from the v1 planning documents

To be imported into `docs/MAINTAINING.md` in phase D; the rest of those
files is v1 workflow machinery (router, session sets, checklist editor)
and retires with them.

- From `project-guidance.md`: scoped port, not complete port;
  coordinate-first, not a layout engine; writes to caller-supplied
  streams; stay on the current .NET LTS line; license headers on every
  source file; public API is a contract (additive changes, XML docs on
  every public member); the testing hierarchy (unit, integration,
  sample-based review, human review); visibility assertions go through a
  rendering-aware parser (PdfPig); code style (namespaces, casing,
  nullable, `Async` suffix).
- From `lessons-learned.md`: smoke-test a packed `.nupkg` from a
  throwaway consumer before publishing; "string in bytes" is not
  "rendered visible"; don't run build and test in parallel against the
  same worktree; isolate tests that are flaky under parallelism in a
  dedicated collection.

## Rationale: Session 2 challenge

Session 2 (2026-11-07) challenged the cuts and decisions above. Where an
answer below differs from the text above, this section is the decision;
the numbered sessions in `docs/sessions/session-plan.md` carry it out.

### The module

- **Should `DabblerPdf` exist as one module?** Yes. It is one package with
  one consumer shape (`dabbler-platform` measures and draws through the same
  `Document`/`PdfWriter` lifetime), and the operator's goal is that a
  successor can rebuild and republish it — every extra package is one more
  version to coordinate and one more thing to hand over. The samples and
  tests stay satellites, not modules: neither is handed over.
- **Is each contract clause a promise something can prove?**
  - *Coming out — well-formed PDF, content visible where drawn, metrics
    consistent with rendering.* Proven today by the integration tests and
    the PdfPig rendering-visibility tests (`Pdf/*RenderingTests.cs`,
    `RenderingVisibilityDisciplineTests.cs`, `TextLayoutTests.cs`). Holds.
  - *Failure — `ArgumentException`, `InvalidOperationException`,
    `NotSupportedException`.* Proven by the existing `Assert.Throws` tests
    (18 `ArgumentException`, 4 `ArgumentNullException`, 13
    `InvalidOperationException`, 1 `NotSupportedException`). Holds; the
    single `NotSupportedException` test is thin, but the clause is not
    expected to change and no session is spent on it.
  - *Public surface stable within a major version (additive only).*
    **Not provable today** — nothing fails when a public member is removed
    or changed. The cheapest proof is the .NET SDK's own package validation
    (`EnablePackageValidation` with `PackageValidationBaselineVersion`
    `1.1.2`): `dotnet pack` fails on a breaking change against the last
    published package. Two csproj properties, no new dependency. Added in
    the trusted-publishing session so every CI pack enforces it.
  - *Public members documented.* Proven by `GenerateDocumentationFile`
    with the compiler's missing-XML-doc warnings; holds as is.
  - *Library depends on nothing but the BCL.* Proven by the library csproj
    having no `PackageReference`; a reviewer sees one being added in the
    diff. No architecture test is added: for a single project with zero
    package references it would test the csproj against itself.
- **Does the dependency direction hide the decisions most likely to
  change?** Mostly yes. What is most likely to change is driven by
  `dabbler-platform`'s reporting (new drawing or font needs), and those
  land inside the library behind the existing `Document`/`PdfContentByte`
  surface; consumers depend on the library, never the reverse. One
  exposure: `Tests → Samples` means a sample change can break a test. That
  is deliberate (the rendering tests drive the exact sample code path, the
  lesson of the 1.1.1/1.1.2 multi-page fix) and stays.

### Deferred cuts

1. *Separate `DabblerPdf.Metrics` package.* **Stays deferred.** No consumer
   measures without drawing; the cut is reopened only when one asks.
2. *Moving `DabblerPdf.Samples` under `src/` or `samples/`.* **Closed —
   not doing it.** It would rewrite README paths, the committed output PDFs'
   location and the tests' project reference for no functional gain. It is
   no longer listed as deferred; the person may reopen it with a plan
   revision.
3. *Packaging through `dabbler.yaml`'s `packaging:` block.* **Decided —
   declared in a packaging session of its own** (Session 7). The brief's
   objections were that a `pack`-only block needed a `version.json` and
   that `release: tag` names a `vsix-v` tag. The framework (3.29.0) now
   substitutes `{version}` in the pack from the release session's heading
   (`dabbler packaging --version`), and the session plan's rules require a
   packaging session wherever *Handoff artifacts* are named. The block
   declares the pack only (`dotnet pack src/DabblerPdf/DabblerPdf.csproj
   -c Release -o {output}`, version from the csproj `<Version>` that the CI
   gate also reads) and **no nuget.org push with a key**: nuget.org
   publication stays CI's, on the tag, through trusted publishing. If the
   framework refuses a block without a push, that session reports blocked
   with the refusal rather than adding a credential.

### The six decisions

1. **Branches — accepted.** Renaming `master` to
   `archive/openpdf-port-2026-03` keeps the port's history reachable;
   deleting it buys nothing. Both the default-branch switch and the rename
   are GitHub-side and the operator's (GitHub's branch rename keeps
   redirects); they are a precondition of Session 4, not a session step.
   The local clone's `origin/HEAD` still points at `master` and is reset
   once the default moves.
2. **v1 relics — accepted, widened, and split.** `utils/`
   (`kill_conhost.py`, `load_env.py`, v1 router helpers) joins the deletion
   list, and `CLAUDE.md`'s v1 top (above `@AGENTS.md`) is rewritten along
   with `AGENTS.md`'s, because both still point at `ai-router` and the v1
   workflow. The session sets are archived, not migrated, as the operator
   leans. `project-guidance.md`, `lessons-learned.md` and
   `project-status.md` are **not** retired in hygiene: they are retired in
   the maintainer-guide session, after `docs/MAINTAINING.md` has imported
   what holds, together with the two links that still point at
   `project-guidance.md` (README and a test's doc comment).
3. **CI and publication — accepted, with two additions.** `build.yml` on
   push and pull request; `publish.yml` on an annotated `nuget-v<version>`
   tag through OIDC trusted publishing, failing before packing unless the
   tag's version equals the csproj `<Version>`, with a `workflow_dispatch`
   dry run to GitHub Packages. Additions: (a) package validation against
   1.1.2 (above) so the pack proves the additive-only promise; (b) before
   pushing, the workflow builds a throwaway console project against the
   packed `.nupkg` from a local source — the lessons-learned rule "smoke-test
   a packed `.nupkg` from a throwaway consumer", made mechanical.
4. **`dabbler.yaml` — accepted, moved, and half overridden.** The suite
   narrowing (`covers: [src, tests, DabblerPdf.Samples]`, `test_roots:
   [tests]`, `expensive: true`, `dotnet test --blame-hang-timeout 5m`)
   moves to Session 3, because the session plan requires the first
   numbered session to adopt the solution and declare the suite. The
   "leave `packaging:` undeclared" half is overridden by deferred cut 3.
5. **Maintainer guide and policy — accepted, Dependabot narrowed.**
   `docs/MAINTAINING.md`, the README section "If this library stops being
   maintained", and the `LICENSE` attribution fix (it names
   "openpdf-dotnet contributors"; the copyright holder is
   `darndestdabbler`, as in the csproj) stay together. Dependabot moves to
   the CI session and covers `nuget` and `github-actions`, not the SDK:
   there is no `global.json`, and CI takes the latest `10.0.x` SDK, so SDK
   patches arrive without a bot. The test project's floating versions
   (`17.*`, `2.*`, `0.1.*`) are pinned to exact versions in the same
   session — a floating build is not reproducible on a successor's runner,
   and Dependabot cannot propose bumps to a wildcard.
6. **Release `1.1.3` — accepted as a patch.** Nothing in Sessions 3–9
   changes the public surface (package validation and CI are build
   properties, not API). The version bump (`<Version>`,
   `PackageReleaseNotes`, `CHANGELOG.md`) is an ordinary session, and the
   release is a step-less `(release: 1.1.3)` session per the session plan's
   rules. Its publication is the `nuget-v1.1.3` tag that CI publishes from;
   until the framework's tagging can name a `nuget-v` tag, the operator
   creates that tag by hand, and holds the release (`hold-release`) if the
   close cannot see the publication.

### Where the session plan's rules override the brief's decomposition

| Brief | Session plan | Why |
|-------|--------------|-----|
| 3 Hygiene | 3 Adopt the solution, narrow the suite | The first numbered session must adopt the solution and declare the suite. |
| — | 4 Hygiene | Moved down one. |
| 4 CI | 5 CI (+ Dependabot, pinned test packages) | Dependabot belongs with the workflows it updates. |
| 5 Trusted publishing | 6 Trusted publishing (+ package validation, nupkg smoke consumer) | Makes the contract and the lessons-learned rule mechanical. |
| — | 7 Packaging (`packaging:` block only) | Required where *Handoff artifacts* are named. |
| 6 `dabbler.yaml` + guide | 8 Maintainer guide (+ retire v1 planning docs) | The suite moved to 3; the guide describes CI and release, so it follows them. |
| 7 Bump to 1.1.3 | 9 Bump to 1.1.3, then 10 Release 1.1.3 `(release: 1.1.3)` | A release is a step-less session of its own. |

Operator preconditions: the GitHub default-branch switch and the `master`
rename before Session 4; the nuget.org trusted-publishing policy (owner
`darndestdabbler`, *Workflow File* `publish.yml`) before Session 6 closes;
the `nuget-v1.1.3` tag for Session 10.
