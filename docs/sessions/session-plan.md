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

### Session 8: Maintainer guide

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

### Session 9: Bump the version to 1.1.3

1. Register.
2. Set `<Version>` to `1.1.3` and rewrite `PackageReleaseNotes` in
   `src/DabblerPdf/DabblerPdf.csproj`, and add a `1.1.3` entry to
   `CHANGELOG.md`: no API change; first release built, validated and
   published from CI through trusted publishing. Check: `dotnet pack
   src/DabblerPdf/DabblerPdf.csproj -c Release -o <scratch dir>` exits 0
   and produces `DabblerPdf.1.1.3.nupkg`.

### Session 10: Release 1.1.3 (release: 1.1.3)

Proposed version: **1.1.3 (patch)** — Sessions 3–9 change no public
surface. Publication is the annotated `nuget-v1.1.3` tag, from which
`publish.yml` publishes to nuget.org; the operator creates the tag by hand
until the framework's tagging can name a `nuget-v` tag, and holds the
release if the close cannot see the publication.
