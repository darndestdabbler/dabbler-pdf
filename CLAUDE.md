# CLAUDE.md -- DabblerPdf

DabblerPdf is a .NET 10 library for text measurement and coordinate-based
PDF generation, used by the `Dabbler.Report` reporting engine and by direct
consumers (NuGet package `DabblerPdf`). It is a **scoped** port of
[OpenPDF](https://github.com/LibrePDF/OpenPDF) (Java, LGPL-2.1 / MPL-2.0):
port only the subset needed for text measurement and coordinate-based
rendering, never the whole library.

Build and test:

```bash
dotnet build DabblerPdf.slnx
dotnet test tests/DabblerPdf.Tests/DabblerPdf.Tests.csproj
```

See `README.md` for the library overview and `docs/planning/solution-plan.md`
for the solution's plan.

<!-- dabbler:managed:start -->
@AGENTS.md

---

## Engine tail (Claude Code)

You are **Claude Code**. The managed body above arrived through the
`@AGENTS.md` import, which Claude Code expands at load time — `AGENTS.md`
is the one copy, so nothing here can drift from what the other engines
read.

<!-- dabbler:managed:end -->
