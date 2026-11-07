# Brief: DabblerPdf under the v3 Dabbler workflow

> The operator's brief for Session 1 (author or import the plan) and Session 2
> (challenge it and break it into sessions). Written 2026-11-07, the day this
> repository was bootstrapped onto the Dabbler AI Orchestration extension
> 3.29.0 and made public on GitHub. The substance here is the operator's.
> `docs/planning/project-guidance.md`, `docs/planning/lessons-learned.md` and
> `docs/planning/project-status.md` are v1-era plan material to import where
> they still hold; `docs/ai-led-session-workflow.md` describes the retired v1
> workflow and is not to be followed.

## What this repository is

`DabblerPdf` is a scoped C# port of OpenPDF (Java, LGPL-2.1 / MPL-2.0) for
.NET 10: font metrics for layout composition and coordinate-based PDF
generation (text, lines, rectangles, images, page events), with no
dependencies. It is one project, `src/DabblerPdf/DabblerPdf.csproj`, one test
project (`tests/DabblerPdf.Tests`, 493 facts and theories across 29 files,
PdfPig-based rendering-visibility assertions among them) and one sample
console app (`DabblerPdf.Samples`, eleven samples whose output PDFs are
committed so a clean clone reproduces them byte for byte).

Published: `DabblerPdf` 1.0.0, 1.1.0, 1.1.1 and 1.1.2 on nuget.org, the last on
2026-04-22 from commit `fdd0fa9` on `main`, packed and pushed by hand with an
API key from a developer machine. The nuspec names this repository as its
source. Dual-licensed LGPL-2.1-only AND MPL-2.0, matching OpenPDF; the
`LICENSE` file still says "openpdf-dotnet contributors".

## Who consumes it

- `dabbler-platform`: `Dabbler.Reports.Core` and `Dabbler.Reports.Export` take
  `DabblerPdf >= 1.1.2` and ship `THIRD-PARTY-NOTICES.txt` for the LGPL/MPL
  text. Every report a migrated Access application exports to PDF goes
  through this library.
- Anyone else who installs the package. The README's quick start is the
  contract.

Production split: not applicable. This is a library; one module; it is handed
over as a NuGet package. `dotnet pack src/DabblerPdf/DabblerPdf.csproj -c
Release -o {output}` is the pack.

## State the plan must account for

1. **Two branches, the wrong default.** `main` is the real DabblerPdf (33
   commits ahead); `master` is the March 2026 `OpenPdf.NET` port with a
   different assembly name and a submodule-based README, and it is GitHub's
   default branch, so the repository's front page and the nuget.org project
   link both show the wrong thing. The operator will set the default to
   `main` from GitHub; the plan decides what becomes of `master` (rename to
   `archive/openpdf-port-2026-03` is the operator's leaning).
2. **v1 relics.** `ai-router/`, `docs/ai-led-session-workflow.md`,
   `docs/claude-instructions/`, `docs/gemini-instructions/`, `GEMINI.md`, the
   v1 body of `AGENTS.md` outside the managed fence, and eight
   `docs/session-sets/*` directories. None of it runs under v3.
3. **No CI.** Nothing builds or tests on push, and publication has been a
   developer-machine `dotnet nuget push` with a key.
4. **`dabbler.yaml` is the bootstrap scaffold.** One suite, `dotnet test`
   over `.`; it should cover `src`, `tests` and `DabblerPdf.Samples` with
   `test_roots: [tests]`. No `packaging:` block; the framework's `release:
   tag` makes a `vsix-v<version>` tag and a `pack`-only block takes its
   version from a `version.json` this repository does not keep. Until a
   framework session lets `release: tag` name its tag, a release here is an
   operator's hand tag after an ordinary version-bump session — the same
   arrangement `dabbler-platform` records as its deferred cut 12.

## Why now

The operator retires in roughly four years. `dabbler-platform`'s reporting
depends on this package, and staff who inherit that migration must be able to
rebuild and republish it, or copy it in and own it under either license.
Public source (done today), a build that runs anywhere, publication that needs
no key on anyone's machine, and one maintainer document are what make that
true.

## Decisions the plan must make (Session 2 challenges each)

1. **Branches.** Default to `main`; `master` renamed to an archive branch or
   deleted.
2. **v1 relics.** Delete `ai-router/`, the instruction folders and
   `GEMINI.md`; replace the v1 body of `AGENTS.md` with a short project note
   above the managed fence; move `docs/session-sets/*` under
   `docs/session-sets/_archived/` with a README saying they are history
   (operator leans: archive, do not `dabbler session migrate` eight sets
   nobody will reopen). Import what still holds from `project-guidance.md`
   and `lessons-learned.md` into `docs/MAINTAINING.md` (decision 5) and
   retire `project-status.md`.
3. **CI and publication.** `.github/workflows/build.yml` builds and tests on
   push and pull request; `.github/workflows/publish.yml` packs and pushes to
   nuget.org on an annotated `nuget-v<version>` tag through trusted publishing
   (OIDC), the pattern `dabbler-platform` and
   `Dabbler.AccessHarvester.Tool` already use. The operator creates the
   trusted-publishing policy on nuget.org; its *Workflow File* field must
   match the workflow filename exactly and the owner is `darndestdabbler`.
   Gate: the `nuget-v` tag's version must equal `<Version>` in the csproj or
   the workflow fails before packing.
4. **`dabbler.yaml`.** Narrow the suite's `covers` and `test_roots`; add
   `expensive: true` and a hang limit (`--blame-hang-timeout 5m`); leave
   `packaging:` undeclared with the comment explaining the hand tag until the
   framework can name a `nuget-v` tag.
5. **Maintainer guide and policy.** `docs/MAINTAINING.md`: clone, build,
   test, run the samples, pack, release by tag, how a consumer pins, the
   licensing obligations of a redistributor; `.github/dependabot.yml` for the
   .NET SDK and test packages; a README section "If this library stops being
   maintained" that says the fallback is to vendor the source under either
   license. Fix the `LICENSE` attribution line.
6. **Release `1.1.3`.** No library change; the release exists to prove the
   tag-driven pipeline end to end from a machine that holds no key. If
   anything consumer-facing lands first, it is `1.2.0` instead.

## The decomposition Session 2 is asked to challenge

- **Session 3: Repository hygiene.** Decisions 1 and 2: archive `master`
  (operator does the GitHub default-branch switch first), delete the v1
  relics, archive the session sets, rewrite the top of `AGENTS.md`.
- **Session 4: Continuous integration.** Decision 3, build half:
  `.github/workflows/build.yml` on push and pull request, green on a clean
  runner.
- **Session 5: Trusted publishing.** Decision 3, publish half:
  `.github/workflows/publish.yml` on `nuget-v*` with the version-equality
  gate; a `workflow_dispatch` dry run against GitHub Packages like
  `dabbler-platform`'s. The operator configures nuget.org before this session
  closes.
- **Session 6: `dabbler.yaml` and the maintainer guide.** Decisions 4 and 5:
  narrow the suite, write `docs/MAINTAINING.md`, add Dependabot, fix the
  `LICENSE` line, add the README fallback section.
- **Session 7: Bump to `1.1.3`.** Version and `CHANGELOG.md`; the operator
  hand-tags `nuget-v1.1.3` and CI publishes. Becomes `### Session 7: Release
  1.1.3 (release: 1.1.3)` if the framework can name the tag by then.

Then consumer-driven: `dabbler-platform`'s reporting work names the next
change here.
