# GEMINI.md -- DabblerPdf (Gemini)

## Project Overview

DabblerPdf is a scoped port of [OpenPDF](https://github.com/LibrePDF/OpenPDF)
(Java, LGPL-2.1 / MPL-2.0) to idiomatic C# targeting .NET 10. It provides
text measurement and coordinate-based PDF generation for the
`Dabbler.Report` reporting engine and for direct consumers of the library.

This is a SCOPED port. Do not port the entire OpenPDF library -- only the
subset needed for text measurement and coordinate-based PDF rendering.

Consumers reference DabblerPdf as a NuGet package (`DabblerPdf`) or as a
git submodule with a project reference.

## Required reading before every session

- `README.md` -- public-facing library overview and quick-start
- `docs/planning/project-guidance.md` -- durable principles + conventions
- `docs/planning/lessons-learned.md` -- repo-specific failure patterns and
  tactics learned from prior sessions
- `docs/ai-led-session-workflow.md` -- the authoritative session-set
  workflow. This is the single source of truth for the step-by-step
  procedure, the AI router usage, cross-provider verification, sample-based
  review handling, **the Step 7 verifier-disagreement resolution procedure
  (four human options, disagreement logging)**, the Step 9 reorganization
  review (run after the Step 8 notify so the human is not blocking the
  completion signal), **the Delegation Discipline rules (when to route vs.
  handle directly)**, and the mandatory rules list. Do not duplicate that
  content here.

When the human says **"Start the next session"**, follow the workflow in
`docs/ai-led-session-workflow.md` exactly. What follows below is only the
Gemini-specific bootstrap the workflow needs.

## Gemini-specific bootstrap

### API key export (Windows)

```bash
export GEMINI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')" | tr -d '\r')
export ANTHROPIC_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('ANTHROPIC_API_KEY', 'User')" | tr -d '\r')
export OPENAI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('OPENAI_API_KEY', 'User')" | tr -d '\r')
.venv/Scripts/python.exe -c "
import os
missing = [k for k in ('ANTHROPIC_API_KEY', 'GEMINI_API_KEY', 'OPENAI_API_KEY') if not os.environ.get(k)]
if missing:
    raise SystemExit(f'Missing environment variables: {missing}')
print('API keys OK')
"
```

If keys are missing, stop and tell the human.

### AI router import

```python
import importlib.util, sys

def load_ai_router():
    spec = importlib.util.spec_from_file_location(
        'ai_router', 'ai-router/__init__.py',
        submodule_search_locations=['ai-router'])
    mod = importlib.util.module_from_spec(spec)
    sys.modules['ai_router'] = mod
    spec.loader.exec_module(mod)
    return mod

ar = load_ai_router()
route = ar.route
```

Use `.venv/Scripts/python.exe` to run Python scripts on Windows. The same
module exposes `send_session_complete_notification()`, which reads
`PUSHOVER_API_KEY` and `PUSHOVER_USER_KEY` from the environment or Windows
User environment.

### Delegation Discipline (pointer)

Your role is orchestrator, not solo coder. Reasoning tasks -- code review,
security review, architecture, analysis, documentation, test generation,
session verification -- **always** go through `route()`. Only handle work
directly when it is mechanical, single-file, and under ~50 lines.

This is not cost paranoia; it is the design of the workflow. The router
already picks the cheapest capable model and applies the correct reasoning
depth from `router-config.yaml`. Your job is to pass the right `task_type`
and let the router handle the rest.

See `docs/ai-led-session-workflow.md` -> **Delegation Discipline** for the
full criteria, the human-tunable thresholds (in `ai-router/router-config.yaml`
under `delegation:`), and the rationale.

## Building & Testing

```bash
# Build
dotnet build DabblerPdf.slnx

# Unit and integration tests
dotnet test tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj

# Sample-based review (generate PDFs for human visual inspection):
dotnet run --project DabblerPdf.Samples
# Produces PDFs under DabblerPdf.Samples/Output/ -- open and review each.
```

## Solution Structure

- `src/DabblerPdf/` -- the library itself
- `tests/DabblerPdf.Tests/` -- xUnit unit and integration tests
- `DabblerPdf.Samples/` -- runnable sample programs that emit PDFs for
  human visual review (see `Samples/` and `Output/`)
- `ai-router/` -- routing code, prompt templates, and workflow utilities
- `docs/` -- workflow docs, planning docs, and session sets
- `utils/` -- small Python helpers (env loading, conhost cleanup)

Code style conventions (namespaces, casing, nullable, `Async` suffix, license
headers) live in `docs/planning/project-guidance.md` -> Conventions -> Code Style.
