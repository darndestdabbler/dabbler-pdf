# GEMINI.md — Instructions for Gemini Code Assist


## Cross-Session Workflow

At the start of every session, read and follow the instructions in these files:

1. @docs/shared-instructions/baton.md — Read the latest BATON
2. @docs/shared-instructions/issue-tracking.md — Register project, create session, retrieve issues
3. @docs/gemini-instructions/api-calling.md — How to make API calls (Python helper preferred)

At the end of every session, follow the session-end instructions in `baton.md` and `issue-tracking.md`.

---

## Project: openpdf-dotnet

**Purpose:** Port a scoped subset of OpenPDF (Java, LGPL-2.1 / MPL-2.0) to idiomatic C# targeting .NET 10. This library provides text measurement and coordinate-based PDF generation for the Dabbler.Report reporting engine.

**Why this exists:** The `access-to-blazor` project is migrating WPF controls to Blazor. The reporting engine needs:
1. **Text measurement** — font metrics for layout composition (`ITextMeasurer`)
2. **PDF generation** — coordinate-based rendering of pre-positioned report elements

Rather than depending on two external libraries (SkiaSharp + PdfSharp), we port the relevant subset of OpenPDF to have a single, self-owned library that does both.

**Key constraint:** This is a SCOPED port. See `docs/porting-scope.md` for exactly which OpenPDF classes to port and which to skip. Do NOT port the entire OpenPDF library.

**Consumer:** The `access-to-blazor` project will reference this library via git submodule + project reference.

**Key files:**
- `docs/porting-scope.md` — What to port, what to skip, API design guidance
- `docs/openpdf-concept-study.md` — Architectural analysis from the WPF project (reference only)

**License:** LGPL-2.1-only AND MPL-2.0 (matching OpenPDF's dual license). All source files must include the appropriate license header.

---

## IV&V Role

When performing IV&V on this project, focus on:
- **Porting fidelity** — Does the C# implementation match OpenPDF's behavior?
- **Scope creep** — Is the port staying within the scoped subset in `porting-scope.md`?
- **License compliance** — Are LGPL-2.1 / MPL-2.0 headers on all ported files?
- **Idiomatic C#** — Is the code C#-native (not Java translated line-by-line)?
- **Test coverage** — Are ported classes tested against known OpenPDF behaviors?

---

## Build & Test

```powershell
# Build
dotnet build OpenPdf.slnx

# Run tests
dotnet test tests/OpenPdf.Tests/OpenPdf.Tests.csproj
```
