# Project work plan — sessions

**Written by `dabbler` as a fold of `activity-log.json`.**
Hand edits are overwritten by the next append. The record is the log;
this page is one view of it.

---

## Sessions

The numbered sessions are declared from `session-plan.md`; each one's task is what its own plan step declared.

| # | Session | Releasable | Declared |
| ---: | --- | --- | --- |
| 1 | Author or import the solution plan | no | 2026-11-07 |
| 2 | Challenge the plan, then break it into numbered sessions | no | 2026-11-07 |

### Session 1 — Author or import the solution plan

**Releasable: no.**

Import the operator's brief (docs/planning/brief.md) into docs/planning/solution-plan.md: an objective a reader can act on (keep DabblerPdf rebuildable, testable and republishable by someone who holds no key on any machine, for dabbler-platform's reporting and any other consumer); the Production split the brief answers (not applicable -- a dependency-free library, one module); the Handoff artifact (a NuGet package from `dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o {output}`); the single module DabblerPdf with its responsibility, contract (inputs, guarantees, failure modes), dependency direction (Samples and Tests depend on the library; Tests also reach Samples by project reference) and the cuts deferred; an assessment of the existing DabblerPdf.slnx against the framework's module-per-folder layout (three projects all listed in the slnx and joined by ProjectReference; the Samples project sits at the root rather than under a folder, and the test project references Samples -- both kept, no restructuring proposed); the six decisions the brief asks the plan to make (branches, v1 relics, CI and trusted publishing, dabbler.yaml suite, maintainer guide and policy, release 1.1.3) carried as the operator's leanings for Session 2 to challenge; and the phases -- alignment first (repository hygiene, dabbler.yaml suite fitted to src/tests/Samples), then CI, trusted publishing, maintainer guide, and the 1.1.3 release -- each scoped to a handful of sessions, with what still holds from project-guidance.md and lessons-learned.md noted for import into docs/MAINTAINING.md later.

### Session 2 — Challenge the plan, then break it into numbered sessions

**Releasable: no.**

Challenge the cuts and decisions in docs/planning/solution-plan.md and record the answers as a rationale section in that file: for the one module DabblerPdf, whether it should exist, whether each clause of its contract is a promise something can prove (and the cheapest proof where one is missing, e.g. SDK package validation against 1.1.2 for the additive-only surface), whether the dependency direction hides the decisions most likely to change, and a decision on each deferred cut (separate metrics package, moving the samples, the packaging block) and on each of the brief's six decisions. Then append the numbered sessions to docs/sessions/session-plan.md, starting at Session 3 with the session that adopts the existing DabblerPdf.slnx (narrowing the dotnet suite in dabbler.yaml to src, tests and DabblerPdf.Samples with test_roots [tests], expensive: true and a --blame-hang-timeout 5m hang limit, adding no project), followed by repository hygiene, continuous integration, trusted publishing, a packaging session that writes only the packaging: block (checked by dabbler packaging --rehearse), the maintainer guide, the 1.1.3 version bump, and a step-less Release 1.1.3 (release: 1.1.3) session proposed as a patch release.
