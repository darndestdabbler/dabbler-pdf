# Project Status — DabblerPdf

> **Purpose:** Capture everything a fresh orchestrator needs to know about
> the current state of this repository so a new session can start without
> re-reading every historical artifact.
> **Read order:** This file first, then the authoritative files it points
> to.

---

## Project in one paragraph

DabblerPdf is a scoped port of OpenPDF (Java, LGPL-2.1 / MPL-2.0) to
idiomatic C# targeting .NET 10. It provides two capabilities for the
`Dabbler.Report` reporting engine and for direct consumers: **text
measurement** (font metrics for layout composition) and **coordinate-based
PDF generation** (rendering pre-positioned report elements). The port is
deliberately narrow — only the OpenPDF subset needed for those two
capabilities is ported, not the full library. The workflow (spec → sessions
→ cross-provider verification → commit → automated tests → human sample
review → change log → lessons learned) keeps quality high across sessions,
and the custom `ai-router/` Python module routes reasoning tasks to the
cheapest capable model while enforcing cross-provider verification.

---

## Where durable content lives now

Everything below is the authoritative source for its topic.

| Topic | Authoritative file |
|---|---|
| Durable strategic commitments (principles, conventions) | `docs/planning/project-guidance.md` |
| Practical lessons from execution | `docs/planning/lessons-learned.md` |
| Session-set workflow — procedure, rules, router usage, verification, human review, close-out | `docs/ai-led-session-workflow.md` |
| Orchestrator bootstrap (API keys, import snippets) | `CLAUDE.md`, `AGENTS.md`, `GEMINI.md` |
| Router design rationale (rules-first verifier selection, per-task `on_disagreement`, complexity-heuristic stopping point) | Comment blocks inline in `ai-router/router-config.yaml` |
| LLM instructions used for every routed task | `ai-router/prompt-templates/system-prompts.md`, `task-prompts.md`, `verification.md` |
| Human review checklist schema and editor behavior | `uat-checklist-editor` repo (separate): `checklist-schema.json`, `index.html`, `README.md`, `sample-checklist.json` |

If a fact is not in one of these files, treat it as unresolved rather than
reconstructing it from memory.

---

## Canonical repo layout

```
<repo-root>/
├── CLAUDE.md
├── AGENTS.md
├── GEMINI.md
├── README.md
├── CHANGELOG.md
├── LICENSE
├── DabblerPdf.slnx
├── ai-router/
│   ├── __init__.py
│   ├── config.py
│   ├── metrics.py
│   ├── models.py
│   ├── notifications.py
│   ├── prompting.py
│   ├── providers.py
│   ├── report.py
│   ├── requirements.txt
│   ├── router-config.yaml
│   ├── router-metrics.jsonl        (runtime log, not checked in fresh)
│   ├── session_log.py
│   ├── utils.py
│   ├── verification.py
│   └── prompt-templates/
│       ├── system-prompts.md
│       ├── task-prompts.md
│       └── verification.md
├── src/
│   └── DabblerPdf/                 (the library itself)
├── tests/
│   └── DabblerPdf.Tests/           (xUnit unit and integration tests)
├── DabblerPdf.Samples/             (runnable samples that emit PDFs)
├── nupkg/                          (local NuGet package output)
├── utils/                          (Python helpers: load_env, kill_conhost)
└── docs/
    ├── ai-led-session-workflow.md
    ├── planning/
    │   ├── project-status.md       (this file)
    │   ├── project-guidance.md
    │   └── lessons-learned.md
    └── session-sets/               (one folder per session set)
```

The human review checklist editor (`checklist-schema.json`, `index.html`,
`README.md`, `sample-checklist.json`) lives in the separate
`darndestdabbler/uat-checklist-editor` repo.

---

## Consumer relationship

DabblerPdf is consumed by the `access-to-blazor` project (which migrates
WPF controls to Blazor and needs text measurement + PDF generation for its
reporting engine). The consumer references DabblerPdf either as a NuGet
package (`DabblerPdf`) or via a git submodule + project reference. This
shapes two constraints:

- **Scoped port, not complete port.** Only the OpenPDF subset actually
  needed for `ITextMeasurer` and coordinate-based rendering gets ported.
- **Self-owned library.** The goal was to replace two external dependencies
  (SkiaSharp + PdfSharp) with a single library the team controls.

---

## Working voice (carried forward)

- **Don't over-engineer.** Prefer simple answers that cover real use cases
  over clever ones that hedge against speculative futures.
- **Don't propose new custom tools.** Extensions to existing files and
  existing tools are preferred over new surfaces.
- **Push back when warranted.** Real pushback with reasoning is more
  valuable than agreement. Cross-provider review itself sometimes
  surfaces incorrect findings; verifying before implementing is part of
  the discipline.
- **Favor boring, readable code over clever code.** This is a library
  that other projects will reference — surprises cost everyone.
- **Budget tool calls carefully.** Multi-session work benefits from
  clean partial handoffs over rushed completion in one session.
