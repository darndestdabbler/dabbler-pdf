# Session plan

> **Purpose:** the numbered sessions this repository runs, in order. The
> first two set the project up; everything after them is the work.
> **Workflow:** Full

---

## Sessions

### Session 1: Author or import the solution plan

1. Register.
2. The brief is the prompt this session was started with, or
   `docs/planning/brief.md`: what the solution is — its purpose, who uses
   it, what it must do and what is out of scope, what success looks like,
   and whether a plan or notes already exist. With neither, ask the person
   who started the session for it and write their answer to that path
   before any work. The plan's substance is theirs: do not search
   neighbouring directories for one, and do not draft one from the folder
   name. Before any module is proposed, ask them how production is split:
   what runs separately in production, and which part may talk to the
   database? Offer the default -- an application tier that calls an API
   tier, and only the API tier talks to the database. Then create — or
   import — `docs/planning/solution-plan.md`: the
   objective a reader can act on; the *Production split* they answered;
   the *Handoff artifacts*, what each separately running part is handed
   over as, with the standard command for its form -- for .NET an IIS site
   package (`dotnet publish`) or a Windows Service (a Worker Service
   published for a service host), for Java a runnable jar or a war (`mvn
   package` writing into `{output}`, which `packaging.pack` requires --
   say through a property the POM's jar or war plugin takes as its
   output directory, `-Dhandoff.dir={output}`) or an image (Spring
   Boot's `build-image`), for the database a
   SQL migration script (`dotnet ef migrations script --idempotent`); the
   real form is produced later, and a tutorial produces the easiest one to
   test; the modules, with what each is
   responsible for, the contract each exposes (what must be true going
   in, what is guaranteed coming out, how it fails), the dependency
   direction, the reason for each cut and the cuts deferred — one module
   is a fine answer, and most small solutions are one; and the phases or
   feature areas with their key deliverables, each scoped to a handful of
   focused AI sessions. A repository that already has
   `docs/planning/project-plan.md` amends that file instead.
   This repository already holds a solution or build at its root:
   `DabblerPdf.slnx`. Read its structure before
   planning -- the projects, their references and their tests -- and
   say in the plan whether it suits the framework's module-per-folder
   layout, a project each reaching a sibling by project reference, and
   what does not. Plan the first sessions as alignment: Dabbler's
   documents, suites and module boundaries fitted to the projects that
   exist, before any feature work. Restructuring existing code is the
   person's to approve with the plan.
3. Where the plan has several modules, say in it how each is a project:
   a `.csproj` listed in the root solution file for .NET, or a Maven module
   listed in the parent `pom.xml`, each reaching a sibling by project
   reference -- a `<ProjectReference>`, or a dependency at
   `${project.version}`. The first numbered session adopts the solution
   already there and adds only what the plan names that is missing.

The framework then verifies the work cross-provider, runs the suites as
the run of record and closes the session. These are the framework's,
not steps to plan.

This session writes the plan and nothing else: it creates, edits or
deletes no code, project, build file, test, `dabbler.yaml` suite or
packaging.

**Creates:** `docs/planning/solution-plan.md`. A later revision is just
another plan session that amends the same file.

### Session 2: Challenge the plan, then break it into numbered sessions

1. Register.
2. Read `docs/planning/solution-plan.md` (or `docs/planning/project-plan.md`
   where that is the file this repository keeps its plan in) and challenge
   its cuts: for each
   module, whether it should exist, whether its contract is a promise
   something can prove, and whether the dependency direction hides the
   decisions most likely to change; decide each deferred cut or say why it
   stays deferred. Record the answers in the plan's rationale.
3. Break the plan into numbered sessions appended to this file. Each
   session is a focused unit of
   work one AI coding session can complete: one
   `### Session <N>: <title>` heading, and its steps as a top-level
   ordered list. Step 1 registers the session and the rest are the
   work: cross-provider verification, the run of record and the close
   are the framework's, never steps to plan. Never
   write a step that says "run the tests" without saying which run it
   means. Order sessions so earlier ones unblock later ones — a module
   before the modules that depend on it — and keep at most ~3 work steps
   per session.

   The first numbered session adopts the solution already there rather
   than writing a skeleton over it: it adds only the projects, references
   and architecture test the plan names that are missing, and makes sure
   `dabbler.yaml` declares the test suite under `testing.suites`, with
   `expensive: true` where its run is the run of record and a hang limit
   in its command -- `dotnet test --blame-hang-timeout 5m`, or
   Surefire's `forkedProcessTimeoutInSeconds`. No session re-creates a
   project that exists.

   Packaging and releases are sessions of their own. Where the plan
   names *Handoff artifacts*, plan one packaging session -- early, with
   project files only, or later -- that writes the `packaging:` block in
   `dabbler.yaml` and does nothing else; its check is `dabbler packaging
   --rehearse`, which runs the pack and pushes nothing. A release is a
   session with no steps, headed `### Session <N>: Release <version> (release:
   <version>)`; propose its version -- patch, minor or major by what the
   sessions it releases change -- for the person to approve with the plan.

The framework then verifies the work cross-provider, runs the suites as
the run of record and closes the session. These are the framework's,
not steps to plan.

This session writes the plan and nothing else: it creates, edits or
deletes no code, project, build file, test, `dabbler.yaml` suite or
packaging.

**Creates:** the numbered session list the rest of this repository runs,
starting with the session that adopts the solution.

> Do NOT hand-author `sessions.json`. The first `session start`
> bootstraps it from this plan — state files are the writers' job.

### Session 3: Adopt the solution and narrow the test suite

1. Register.
2. Adopt `DabblerPdf.slnx` as it stands: the plan names no missing project,
   reference or architecture test (see *Rationale: Session 2 challenge* in
   `docs/planning/solution-plan.md`), so none is added and none of the three
   projects is re-created. In `dabbler.yaml`, change the `dotnet` suite to
   `covers: [src, tests, DabblerPdf.Samples]`, `test_roots: [tests]`,
   `expensive: true`, command `dotnet test DabblerPdf.slnx
   --blame-hang-timeout 5m`, and `select` `dotnet test DabblerPdf.slnx
   --blame-hang-timeout 5m --filter {names}`. Check: `dotnet test
   DabblerPdf.slnx --blame-hang-timeout 5m` (the whole suite, the command
   the suite now declares) exits 0.
3. Replace the scaffold's packaging comment in `dabbler.yaml` with one line
   saying the `packaging:` block is written by Session 7 and that nuget.org
   publication is CI's on a `nuget-v<version>` tag. Check: `dabbler
   affected` exits 0 and loads the edited file.

This session adopts and declares; it changes no code, project or test.

### Session 4: Repository hygiene

Precondition (operator, GitHub): the default branch is `main` and `master`
is renamed to `archive/openpdf-port-2026-03`.

1. Register.
2. Delete the v1 relics: `ai-router/`, `utils/`,
   `docs/ai-led-session-workflow.md`, `docs/claude-instructions/`,
   `docs/gemini-instructions/` and `GEMINI.md`, and any `.gitignore` lines
   that only served them. Check: `dotnet build DabblerPdf.slnx` exits 0 and
   a `node -e` check confirms none of those paths exists.
3. Move every `docs/session-sets/<set>/` to
   `docs/session-sets/_archived/<set>/` with `git mv`, and add
   `docs/session-sets/_archived/README.md` saying they are the v1
   workflow's history, closed, and not to be resumed or migrated. Check: a
   `node -e` check confirms `docs/session-sets/` holds only `_archived/`.
4. Rewrite the v1 text above the managed fence in `AGENTS.md` and above
   `@AGENTS.md` in `CLAUDE.md` as a short project note: what DabblerPdf
   is, that it is a scoped port, the build and test commands, and pointers
   to `README.md` and `docs/planning/solution-plan.md`. Leave the managed
   body and engine tails untouched. Check: a `node -e` check confirms
   neither file mentions `ai-router` or `ai-led-session-workflow`.

### Session 5: Continuous integration

1. Register.
2. Pin the test project's floating package versions (`17.*`, `2.*`,
   `0.1.*`) in `tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj` to the
   exact versions restore resolves today. Check: `dotnet test
   DabblerPdf.slnx --blame-hang-timeout 5m` (the whole suite) exits 0.
3. Write `.github/workflows/build.yml`: on push and pull request to
   `main`, on `ubuntu-latest`, `actions/setup-dotnet` with `10.0.x`, then
   `dotnet restore`, `dotnet build DabblerPdf.slnx -c Release
   --no-restore` and `dotnet test DabblerPdf.slnx -c Release --no-build
   --blame-hang-timeout 5m`. Check: `dotnet build DabblerPdf.slnx -c
   Release` exits 0 locally, and a `node -e` check confirms the workflow
   names those three commands. Green on a clean runner is confirmed from
   the push's Actions run, linked in the session notes.
4. Write `.github/dependabot.yml` covering the `nuget` ecosystem at `/`
   and `github-actions` at `/`, weekly. Check: a `node -e` check confirms
   both ecosystems are declared.

### Session 6: Trusted publishing

1. Register.
2. Add package validation to `src/DabblerPdf/DabblerPdf.csproj`:
   `EnablePackageValidation` true and `PackageValidationBaselineVersion`
   `1.1.2`, so a pack fails on a breaking change to the public surface.
   Check: `dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o
   <scratch dir>` exits 0 (validation passes against the published 1.1.2).
3. Write `.github/workflows/publish.yml`: on push of a `nuget-v*` tag,
   fail before packing unless the tag's version equals `<Version>` in the
   csproj; build and run the whole suite (`dotnet test DabblerPdf.slnx -c
   Release --blame-hang-timeout 5m`); `dotnet pack` the library; build a
   throwaway console project that references `DabblerPdf` at that version
   from the pack's output folder as a local source; then push the
   `.nupkg` and `.snupkg` to nuget.org through trusted publishing
   (`id-token: write`, `NuGet/login`, owner `darndestdabbler`). A
   `workflow_dispatch` run does everything but pushes to GitHub Packages
   instead of nuget.org. Check: a `node -e` check confirms the tag
   trigger, the version gate, the smoke consumer and both push targets.
   Operator, before this session closes: create the nuget.org
   trusted-publishing policy with *Workflow File* `publish.yml`, and run
   the `workflow_dispatch` dry run once; its run is linked in the notes.

### Session 7: Packaging

1. Register.
2. Write the `packaging:` block in `dabbler.yaml` and nothing else: `pack`
   argv `["dotnet", "pack", "src/DabblerPdf/DabblerPdf.csproj", "-c",
   "Release", "-o", "{output}"]`, the version coming from the csproj's
   `<Version>` that the CI gate reads, and no nuget.org push with a
   credential — nuget.org publication is CI's on the tag. If the framework
   refuses a block without a push, report the step blocked with the
   refusal rather than adding a credential. Check: `dabbler packaging
   --rehearse` exits 0.

### Session 8: Make the TrueType tests run on Linux

Every `build.yml` run since Session 5 has failed, and `publish.yml` runs
the same suite on `ubuntu-latest`, so the 1.1.3 release would fail at the
tag. The 17 TrueType tests in
`tests/DabblerPdf.Tests/Text/TrueTypeFontTests.cs` and
`tests/DabblerPdf.Tests/MvpValidationTests.cs` read Arial from
`C:\Windows\Fonts\arial.ttf`, and their `SkipIfNoArial` helper calls
`Assert.Fail` when the file is absent, so on a Linux runner a missing
font is 17 failures, not 17 skips. The test project only; no library
code changes, no font installation in a workflow.

1. Register.
2. Vendor Liberation Sans Regular (SIL Open Font License 1.1,
   metric-compatible with Arial, from the `liberationfonts/liberation-fonts`
   2.1.5 release) as `tests/DabblerPdf.Tests/Fonts/LiberationSans-Regular.ttf`
   with the release's `LICENSE` beside it as
   `tests/DabblerPdf.Tests/Fonts/LICENSE`, both copied to the test output
   directory (`CopyToOutputDirectory` `PreserveNewest`). Check: a `node -e`
   check confirms both files are tracked and the csproj names the `Fonts`
   folder.
3. In both test files, point the font-path constant at that fixture,
   relative to `AppContext.BaseDirectory`, and replace `SkipIfNoArial` with
   a precondition that fails naming the fixture path when it is missing,
   so the suite reports a broken checkout rather than a skip. Change the
   two `ArialMT` assertions (the `PostscriptName` check and Scenario 8's
   `/BaseFont /ArialMT`) to `LiberationSans`; leave the width and metric
   tolerances alone, since the metrics match Arial. Rewrite the
   `TrueTypeFontTests` class doc comment, which cites the Windows path.
   Check: `dotnet test DabblerPdf.slnx --blame-hang-timeout 5m` (the whole
   suite) exits 0 with 614 passed and 0 skipped, and no tracked file under
   `tests/` names `Windows\Fonts`. Green on a clean runner is confirmed
   from the push's Actions run, linked in the session notes; the open
   Dependabot pull requests go green once rebased onto it.

### Session 9: Maintainer guide

1. Register.
2. Write `docs/MAINTAINING.md`: clone, build, test (the whole suite,
   `dotnet test DabblerPdf.slnx --blame-hang-timeout 5m`), run the samples,
   pack, release by `nuget-v<version>` tag through `publish.yml`, how a
   consumer pins a version, and a redistributor's LGPL-2.1/MPL-2.0
   obligations; import what still holds from `project-guidance.md` and
   `lessons-learned.md` (listed in the plan's *What carries over*). Then
   delete `docs/planning/project-guidance.md`,
   `docs/planning/lessons-learned.md` and `docs/planning/project-status.md`,
   and repoint the links to `project-guidance.md` in `README.md` and in
   `tests/DabblerPdf.Tests/Pdf/RenderingVisibilityDisciplineTests.cs`'s doc
   comment to `docs/MAINTAINING.md`. Check: a `node -e` check confirms no
   tracked file outside `docs/session-sets/_archived/` and
   `docs/transcripts/` names the three deleted files.
3. Add the README section "If this library stops being maintained" (vendor
   the source under either license, keep the notices), link
   `docs/MAINTAINING.md` from the README and from the project note in
   `AGENTS.md`, and fix the `LICENSE` attribution line that names
   "openpdf-dotnet contributors" to name `darndestdabbler`. Check: a
   `node -e` check confirms the README heading exists and `LICENSE` no
   longer contains `openpdf-dotnet`.

### Session 10: Bump the version to 1.1.3

1. Register.
2. Set `<Version>` to `1.1.3` and rewrite `PackageReleaseNotes` in
   `src/DabblerPdf/DabblerPdf.csproj`, and add a `1.1.3` entry to
   `CHANGELOG.md`: no API change; first release built, validated and
   published from CI through trusted publishing. Check: `dotnet pack
   src/DabblerPdf/DabblerPdf.csproj -c Release -o <scratch dir>` exits 0
   and produces `DabblerPdf.1.1.3.nupkg`.

### Session 11: Release 1.1.3 (release: 1.1.3)

Proposed version: **1.1.3 (patch)** — Sessions 3–10 change no public
surface. Publication is the annotated `nuget-v1.1.3` tag, from which
`publish.yml` publishes to nuget.org; the operator creates the tag by hand
until the framework's tagging can name a `nuget-v` tag, and holds the
release if the close cannot see the publication.

### Session 12: Make the publish smoke test restore from the packed output

1. Register.
2. In `.github/workflows/publish.yml`, step "Smoke-test the package": the
   `dotnet add package --source "$GITHUB_WORKSPACE/out"` call records the
   reference, but the `dotnet build` that follows restores with the default
   sources only, so a version that is not yet on nuget.org fails with
   `NU1102` (the 2026-11-08 `workflow_dispatch` dry run, run 42482132357,
   failed exactly there, and a `nuget-v1.1.3` tag would have failed the same
   way). Give the smoke project its own `nuget.config` that names
   `$GITHUB_WORKSPACE/out` as a source beside nuget.org (`dotnet new
   nugetconfig` then `dotnet nuget add source`), or restore with explicit
   `--source` arguments and build `--no-restore`. The step's check is a
   `workflow_dispatch` run of `publish.yml` that reaches "Push to GitHub
   Packages" green; record the run id in the step's notes.
3. Cross-provider verification.
4. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

After the session has pushed and its dry run is green, the operator tags
`nuget-v1.1.3` by hand and confirms 1.1.3 on nuget.org. Session 11's
framework-made `session-011` tag published nothing: with a `pack`-only
declaration and no `version.json`, the router falls back to a session tag
and records the release as published (deferred cut 12 in `dabbler-platform`
tracks the framework fix).

### Session 13: WrapText keeps interior spaces, and content-stream compression can be turned off

1. Register.
2. `TextLayout.WrapText` (`src/DabblerPdf/Text/TextLayout.cs`) splits each
   paragraph with `Split(' ', StringSplitOptions.RemoveEmptyEntries)` and
   rejoins the words with one space, so every run of spaces becomes a single
   space even when the text already fits: `"New Address Eff:  03/01/2024"`
   comes back as `"New Address Eff: 03/01/2024"`. Change it so that:
   - a paragraph that fits within `maxWidth` is returned exactly as given,
     leading, interior and trailing spaces included;
   - a paragraph that does not fit breaks only at a run of spaces; the run at
     which a break is taken is dropped (no line ends or starts with it), and
     every other run is kept at its full length;
   - explicit `\n` breaks, the empty-string result and the character-level
     break of a word wider than `maxWidth` behave as they do today.

   `CountLines`, `MeasureHeight` and `DrawText` follow, because they call
   `WrapText`. Say in the XML doc of `WrapText` what happens to spaces. In
   `tests/DabblerPdf.Tests/Text/TextLayoutTests.cs` add tests for: a
   double-spaced line that fits is unchanged; `"CT  06516"` keeps both
   spaces; a break taken at a double space leaves neither line with a
   leading or trailing space; a double space elsewhere on a wrapped line
   survives; `DrawText` emits the double space in its `Tj` operand. The
   step's check: `dotnet test DabblerPdf.slnx --filter
   "FullyQualifiedName~TextLayoutTests"` passes, the 21 existing tests
   unchanged in what they assert.
3. `PdfWriter.FlushCurrentPage` and `PdfTemplate` deflate a content stream
   only when its uncompressed length is over 128 bytes, so a small page is
   written as plain operators and the same page with one more line is
   written with `/Filter /FlateDecode`; nothing documents this and nothing
   turns it off. Add a public `bool CompressContentStreams` property to
   `PdfWriter`, default `true`, read when a page or a template created by
   that writer is flushed: `false` writes page and template content streams
   uncompressed whatever their length. Font programs and images are not
   affected. In `tests/DabblerPdf.Tests/Pdf/PdfWriterTests.cs` and
   `PdfTemplateTests.cs` add tests for: default settings, a content stream of
   128 bytes or fewer has no `/Filter` and one over 128 bytes has
   `/FlateDecode`; with `CompressContentStreams = false` a page and a
   template over 128 bytes have no `/Filter` and their text is found in the
   file's bytes. In `README.md`, state the 128-byte rule and the property in
   the section that describes testing generated PDFs, and say that a
   byte-level text search needs one or the other. The step's check: `dotnet
   test DabblerPdf.slnx --filter
   "FullyQualifiedName~PdfWriterTests|FullyQualifiedName~PdfTemplateTests"`
   passes.
4. Pin what the maintainer findings left unverified. `BaseFont`'s XML doc
   says a character outside Windows-1252 is replaced with `?`; no test
   proves it for drawn text. Add tests (in
   `tests/DabblerPdf.Tests/Text/BaseFontTests.cs` or
   `tests/DabblerPdf.Tests/Pdf/TextIntegrationTests.cs`) that a Base 14 font
   with `WinAnsiEncoding` draws a Polish and a Cyrillic string without
   throwing, that each character outside Windows-1252 is written as `?`,
   and that `GetWidthPoint` measures it as `?`. If the code does something
   else, change the code to do what the doc says. State the limit in
   `README.md` where fonts are described. (`WrapText` on a single word wider
   than `maxWidth` is already covered by `SingleLongWord_CharacterBroken`.)
   The step's check: the new tests pass.
5. Cross-provider verification.
6. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
7. Close-out.

Non-goals: no encoding beyond Windows-1252 and no font embedding change; no
compression-level option, only on or off; no change to how fonts or images
are compressed; no version change (Session 14's).

Why now (2026-11-12): the TRIMS application, building on DabblerPdf 1.1.3,
reported that `WrapText` destroys the double-space separators its legacy
label format depends on, and that byte-level tests pass for a small page and
fail when the page grows past the undocumented compression threshold. Both
were confirmed in the source. `dabbler-platform` takes this release before
its own `2.0.0`.

### Session 14: Windows-1252 characters measure at their real width

1. Register.
2. Every one of the twelve Base 14 text fonts (the Helvetica, Times and
   Courier families) holds a width of 0 for Windows-1252 codes 128–159 in
   `src/DabblerPdf/Text/BuiltinFonts.cs`, so `€`, `‚`, `ƒ`, `„`, `…`, `†`,
   `‡`, `ˆ`, `‰`, `Š`, `‹`, `Œ`, `Ž`, `‘`, `’`, `“`, `”`, `•`, `–`, `—`,
   `˜`, `™`, `š`, `›`, `œ`, `ž` and `Ÿ` are drawn with their glyphs and
   measured as nothing. Fill those codes from the Adobe Core 14 AFM metrics
   that OpenPDF ships (its `pdf/fonts/*.afm` resources), taking each code's
   width by the glyph name Windows-1252 gives it (an unencoded `C -1` entry
   counts), and say where the numbers came from in a comment. Check codes
   32–127 and 160–255 of the same twelve fonts against the same files and
   correct any that differ. Codes 129, 141, 143, 144 and 157, which
   Windows-1252 leaves undefined, stay 0. In
   `tests/DabblerPdf.Tests/Text/BuiltinFontsTests.cs` add tests that, in each
   of the twelve fonts, no code Windows-1252 defines from 32 to 255 has width
   0, and that `€`, `–`, `—`, `•`, `’` and `”` have the AFM's widths in
   Helvetica, Times-Roman and Courier. The step's check: `dotnet test
   DabblerPdf.slnx --filter
   "FullyQualifiedName~BuiltinFontsTests|FullyQualifiedName~Type1FontTests"`
   passes.
3. `TrueTypeFont` (`src/DabblerPdf/Text/TrueTypeFont.cs`) builds its width
   table by Windows-1252 code, but `GetCharWidth` looks it up by the
   caller's Unicode value, so a TrueType font measures `€`, the typographic
   quotes and dashes, and every character outside Windows-1252 as 0, while
   it draws them as their glyphs or as `?`. Measure the way `Type1Font` does
   since Session 13 (through the Windows-1252 code, `?` when there is none),
   with the mapping in one place both classes use. In
   `tests/DabblerPdf.Tests/Text/TrueTypeFontTests.cs`, the test that
   asserts `GetCharWidth(300)` is 0 now asserts the width of `?`; add tests
   with the Liberation Sans fixture that `€` measures as the font's Euro
   advance (not 0) and that `ł` and `Ж` measure as `?`. In `README.md`'s
   Font Support section, say that a TrueType font with `WinAnsiEncoding`
   has the same Windows-1252 limit. The step's check: `dotnet test
   DabblerPdf.slnx --filter
   "FullyQualifiedName~TrueTypeFontTests|FullyQualifiedName~Type1FontTests"`
   passes.
4. Cross-provider verification.
5. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

Non-goals: no encoding beyond Windows-1252 (Session 16's); no change to the
Symbol and ZapfDingbats tables, which use their own built-in encodings; no
change to what is drawn, only to what is measured; no version change
(Session 15's).

Why now (2026-11-12): found while closing Session 13. Text with a euro sign,
typographic quotes or dashes measures short, so `WrapText` puts too much on
a line and right and centre alignment are off; `dabbler-platform` measures
every report field with the Base 14 fonts. Session 13 fixed Base 14
measurement of characters outside Windows-1252 and named TrueType a
non-goal; this session lifts that, because the TrueType path has the same
mismatch.

### Session 15: Bump the version to 1.2.0

1. Register.
2. Set `<Version>` to `1.2.0` and rewrite `PackageReleaseNotes` in
   `src/DabblerPdf/DabblerPdf.csproj`, set `PackageValidationBaselineVersion`
   to `1.1.3` (the last published version), and add a `1.2.0` entry to
   `CHANGELOG.md`: added, `PdfWriter.CompressContentStreams`; changed,
   `TextLayout.WrapText` (and `CountLines`, `MeasureHeight`, `DrawText`
   through it) keeps runs of spaces, so text that relied on the collapse
   wraps and measures differently; fixed, Base 14 widths for Windows-1252
   codes 128–159, and Base 14 and TrueType fonts measure a character
   outside Windows-1252 as the `?` they draw (both change measured widths,
   so wrapped text can break differently); documented, the 128-byte
   compression rule and the Windows-1252 replacement with `?`. The step's
   check: `dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o
   <scratch dir>` exits 0, package validation against 1.1.3 included, and
   produces `DabblerPdf.1.2.0.nupkg`.
3. Cross-provider verification.
4. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

Proposed version: **1.2.0 (minor)**. `CompressContentStreams` is an added
public member, and the `WrapText` and width changes are fixes that alter
output, which a patch should not do silently.

There is no `(release: 1.2.0)` session: Session 11 showed that with a
pack-only declaration the router makes a `session-<N>` tag that publishes
nothing. After this session has pushed, the operator runs the dry run
(`gh workflow run publish.yml`), and when it is green tags by hand as
`docs/MAINTAINING.md` → *Release* says (`git tag -a nuget-v1.2.0 -m "Release
1.2.0"`, `git push origin nuget-v1.2.0`) and confirms 1.2.0 on nuget.org.
TRIMS takes 1.2.0, and `dabbler-platform` Session 27 waits for it.

### Session 16: TrueType fonts with Identity-H draw any character the font has

1. Register.
2. `BaseFont.CreateFont(ttfPath, BaseFont.IdentityH, true)` today returns
   the same Windows-1252 font as `WinAnsiEncoding` (the encoding argument
   only keys the cache), and `BaseFont.IdentityH`'s doc says "not yet
   implemented". Make it return a Unicode TrueType font, a new internal
   class beside `TrueTypeFont` (ported from OpenPDF's
   `TrueTypeFontUnicode`), that:
   - measures each character by its own glyph's advance, through the
     font's `cmap` and its full `hmtx` (not a 256-entry table); a character
     the font has no glyph for, and a surrogate pair, is glyph 0
     (`.notdef`) and measures as its advance; `CanEncode` is true exactly
     when the font has the glyph;
   - is drawn by `ShowText` as 2-byte glyph IDs in a hex string
     (`<0024004B> Tj`), which needs `PdfContentByte` to know the font set by
     `SetFontAndSize`;
   - is written as a Type0 font, `/Encoding /Identity-H`, whose descendant
     is a `CIDFontType2` with `/CIDSystemInfo` (Adobe, Identity, 0),
     `/CIDToGIDMap /Identity`, the font descriptor with the font file as
     `FontFile2`, a `/W` array for the glyphs used, and a `/ToUnicode` CMap
     that maps each used glyph back to its characters, so copying,
     searching and text extraction get the text.

   `/W` and `/ToUnicode` depend on everything the document draws, so the
   writer reserves the font's reference when the font is first set and
   writes its objects at `Close`, from the glyphs recorded for that writer.
   A `BaseFont` is shared across documents by `CreateFont`'s cache, so the
   used-glyph record lives in the writer, not the font. The writer keys font
   objects by PostScript name today, so the same `.ttf` used with both
   encodings in one document needs two resources: key them so it gets two.
   A Base 14 font with `IdentityH` throws `ArgumentException`: it has no
   glyphs to embed. The `WinAnsiEncoding` TrueType path is unchanged. The
   tests go in a file named after the new class, using the Liberation Sans
   fixture: a Polish, a Cyrillic and a Greek string are drawn and PdfPig
   extracts exactly that text from the page; the measured width of a string
   is the sum of its glyphs' advances; a character the font lacks (a CJK
   ideograph) measures as `.notdef` and `CanEncode` is false; one font
   drawn in two documents gives each document its own `/W`; the same file
   with both encodings in one document draws both texts; text drawn into a
   `PdfTemplate` is extracted too. Update `BaseFont`'s docs for
   `IdentityH`, `WinAnsiEncoding` and `CreateFont`, and the README's Font
   Support section: how to draw Unicode text, what it covers, and its
   limits (no shaping, no right-to-left, no characters beyond U+FFFF, the
   whole font file embedded until Session 17). The step's check: `dotnet
   test DabblerPdf.slnx --filter
   "FullyQualifiedName~TrueType|FullyQualifiedName~TextIntegrationTests|FullyQualifiedName~BaseFontTests"`
   passes.
3. `TextLayout`'s character-level break (`BreakWord`) can cut a word between
   the two halves of a surrogate pair, or between a letter and its
   combining mark. Break between text elements (`StringInfo`) instead. Add
   tests to `tests/DabblerPdf.Tests/Text/TextLayoutTests.cs` that neither is
   split. The step's check: `dotnet test DabblerPdf.slnx --filter
   "FullyQualifiedName~TextLayoutTests"` passes.
4. Cross-provider verification.
5. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
6. Close-out.

Non-goals: no shaping, ligatures or right-to-left text (Arabic, Hebrew,
Indic and Thai scripts need a shaping engine, which this port does not
take); no glyphs beyond U+FFFF (they draw as `.notdef`); no fallback from
one font to another for a missing glyph; no OpenType CFF (`.otf`) fonts; no
vertical writing; no subsetting (Session 17's); no change to the
`WinAnsiEncoding` paths; no version change (Session 18's).

Why now (2026-11-12): the operator asked for Unicode text. Every PDF viewer
draws it from an embedded font with Identity-H encoding; DabblerPdf has the
constant and the TrueType reader but writes only one-byte Windows-1252
fonts, so Polish, Cyrillic, Greek and CJK text comes out as `?`.
`dabbler-platform` Session 28 builds report fonts on this.

### Session 17: Embed only the glyphs an Identity-H font uses

1. Register.
2. A TrueType font is embedded as its whole file today, so a document that
   draws one word in a CJK font carries megabytes of glyphs. For an
   Identity-H font, write `FontFile2` at `Close` as a subset (ported from
   OpenPDF's `TrueTypeFontSubSet`): glyph 0, every glyph the writer
   recorded, and every component of a composite glyph among them,
   recursively; glyph IDs keep their numbers (an unused glyph's outline is
   empty), so `/CIDToGIDMap /Identity` and `/W` stand; the tables a PDF
   TrueType font program needs (`head`, `hhea`, `maxp`, `loca`, `glyf`,
   `hmtx`, and `cvt `, `fpgm` and `prep` when present) are written with
   their checksums. The font's `/BaseFont` and `/FontName` take a subset
   tag, six uppercase letters and `+` (ISO 32000-1, 9.6.4). The tests go
   in a file named after the subsetting class, with the Liberation Sans
   fixture: a document drawing "Hello" embeds a font program under a tenth
   of the full file's size; the subset parses back with `TtfReader` and
   keeps each used glyph's advance; a composite glyph's components are in
   the subset and PdfPig extracts the text that uses it; the tag has the
   required form; two documents drawing different text embed different
   subsets. The README's Font Support section says Identity-H fonts are
   subset. The step's check: `dotnet test DabblerPdf.slnx --filter
   "FullyQualifiedName~TrueType"` passes.
3. Cross-provider verification.
4. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

Non-goals: no subsetting of `WinAnsiEncoding` TrueType fonts (a small file
uses Identity-H); no option to turn subsetting off; no change to hinting
instructions; no CFF fonts; no version change (Session 18's).

Why now (2026-11-12): Unicode fonts are large, and without subsetting every
generated PDF carries the whole file.

### Session 18: Bump the version to 1.3.0

1. Register.
2. Set `<Version>` to `1.3.0` and rewrite `PackageReleaseNotes` in
   `src/DabblerPdf/DabblerPdf.csproj`, set `PackageValidationBaselineVersion`
   to `1.2.0`, and add a `1.3.0` entry to `CHANGELOG.md`: added, Unicode
   TrueType fonts with `BaseFont.IdentityH` (Type0 fonts with a ToUnicode
   map, subset when embedded); changed, `CreateFont` with `IdentityH` on a
   TrueType file returns a Unicode font where it used to return the
   Windows-1252 one, and with a Base 14 name throws; fixed, `TextLayout`
   never splits a surrogate pair or a combining sequence. The step's check:
   `dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o <scratch
   dir>` exits 0, package validation against 1.2.0 included, and produces
   `DabblerPdf.1.3.0.nupkg`.
3. Cross-provider verification.
4. The `dotnet` suite once against the verified tree, recorded as the run of
   record.
5. Close-out.

Proposed version: **1.3.0 (minor)**. Identity-H was documented as not yet
implemented, so giving it its documented meaning adds a feature; no public
member is removed or changes signature.

As with 1.2.0, the operator runs the dry run after this session has pushed,
tags `nuget-v1.3.0` by hand when it is green, and confirms 1.3.0 on
nuget.org. `dabbler-platform` Session 28 waits for it.
