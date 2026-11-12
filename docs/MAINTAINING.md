# Maintaining DabblerPdf

This guide is for whoever builds, tests and releases DabblerPdf next. You
need no key on any machine for it: CI builds every push, and nuget.org
publication goes through trusted publishing on a tag.

## Clone, build, test

You need the .NET 10 SDK and git.

```bash
git clone https://github.com/darndestdabbler/dabbler-pdf.git
cd dabbler-pdf
dotnet build DabblerPdf.slnx
dotnet test DabblerPdf.slnx --blame-hang-timeout 5m
```

`DabblerPdf.slnx` holds three projects:

| Project | Role |
|---|---|
| `src/DabblerPdf` | The library, and the only thing that ships |
| `tests/DabblerPdf.Tests` | xUnit tests; references the library and the samples |
| `DabblerPdf.Samples` | A console program that writes the sample PDFs |

The test command is the whole suite, and it is the same command CI runs
(`.github/workflows/build.yml`, on every push and pull request to `main`).
The `--blame-hang-timeout` makes a hung test fail with a dump instead of
stalling the run.

Run build and test one after the other, never in parallel against the same
worktree. On Windows the two contend for file locks, and the failure you see
is the lock rather than the code. If lock errors show up anyway, run
`dotnet build-server shutdown` and try again.

## Run the samples

```bash
dotnet run --project DabblerPdf.Samples -- DabblerPdf.Samples/Output
```

The eleven samples (`S01_BasicText` through `S11_OrdersStyleReport`) each
write a PDF into the folder you pass. With no argument they write into an
`Output` folder beside the built executable. Open the PDFs to check
anything whose correctness is visual.

## Pack

```bash
dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o out
```

This writes `DabblerPdf.<version>.nupkg` and its `.snupkg` symbols package.
The version is the csproj's `<Version>`. Package validation is on
(`EnablePackageValidation`). A pack fails if the public surface breaks
compared with `PackageValidationBaselineVersion`, which should name the
last version published.

Before you publish, smoke-test the packed `.nupkg` from a throwaway consumer
outside the repository. A project-reference test cannot catch package
metadata errors, a missing README or a member that is `internal` in the
packed dll:

```bash
dotnet new console -o /tmp/smoke
dotnet add /tmp/smoke package DabblerPdf --version <version> --source "$PWD/out" --package-directory /tmp/smoke-packages
dotnet build /tmp/smoke
```

`publish.yml` runs this same smoke test before it pushes anything. Run it
yourself as well when a release adds public surface, and have the throwaway
program call the new members and open the PDF it writes. Do not commit the
throwaway project.

## Release

A release is a `nuget-v<version>` tag on `main`, and
`.github/workflows/publish.yml` publishes it:

1. On `main`, set `<Version>` and `<PackageReleaseNotes>` in
   `src/DabblerPdf/DabblerPdf.csproj`, and add the version's entry to
   `CHANGELOG.md`. Use a patch version for fixes, a minor one for additive
   API and a major one for any break.
2. Optionally, start a dry run: run the `publish` workflow by hand
   (`workflow_dispatch`). It builds, tests, packs and smoke-tests, then
   pushes to GitHub Packages rather than nuget.org.
3. Tag the commit and push the tag:

   ```bash
   git tag nuget-v1.2.3
   git push origin nuget-v1.2.3
   ```

The workflow refuses a tag that does not match `<Version>`. It then
restores, builds, runs the whole suite, packs, smoke-tests, logs in to
nuget.org through trusted publishing (`NuGet/login`, as user
`darndestdabbler`) and pushes the `.nupkg` and `.snupkg`. No API key is
stored anywhere. The trust is a policy on nuget.org that names this
repository and `publish.yml`. Anyone who takes over publishing needs that
policy on their own nuget.org account, or must be added to the package's
owners.

After the release, set `PackageValidationBaselineVersion` to the version
just published.

## How a consumer pins a version

Through NuGet, with an exact version:

```xml
<PackageReference Include="DabblerPdf" Version="[1.1.2]" />
```

With central package management, the same version goes in
`Directory.Packages.props` (`<PackageVersion Include="DabblerPdf"
Version="[1.1.2]" />`). A bare `Version="1.1.2"` is a minimum, and a restore
may resolve a higher version. The brackets pin it.

Through a git submodule, check out the release tag in the submodule
(`git -C lib/dabbler-pdf checkout nuget-v1.1.2`) and commit the submodule
pointer.

## License obligations for redistributors

DabblerPdf is a port of OpenPDF and is dual-licensed under LGPL-2.1-only
and MPL-2.0, the same as OpenPDF. A redistributor chooses either license.
This summary is not legal advice; the license texts in `LICENSE` and at the
links there govern.

Under either license:

- Keep the `LICENSE` file and the SPDX license header at the top of every
  source file. Do not remove or alter the copyright notices, which credit
  Bruno Lowagie and Paulo Soares for the original work and `darndestdabbler`
  for the port.
- If you distribute DabblerPdf, modified or not, as source or as the
  compiled `DabblerPdf.dll`, tell the people who receive it where they can
  get its source. This repository's URL satisfies that for an unmodified
  copy. For a modified copy, make your modified source available.

Under **MPL-2.0** the copyleft is per file. Modified DabblerPdf files stay
under MPL-2.0 and their source must be available. Your own files in the same
application can carry any license you like.

Under **LGPL-2.1-only** the copyleft covers the library as a whole. Changes
to DabblerPdf are released under the LGPL. An application that only uses the
library keeps its own license, as long as the user can replace
`DabblerPdf.dll` with a modified version. Shipping it as a separate assembly
does that. Merging it into your own assembly does not, unless you supply the
means to relink. The license also requires you to allow reverse engineering
for debugging such modifications.

Test-only dependencies (xUnit and PdfPig, both Apache-2.0) are not in the
package and impose nothing on consumers.

## Principles that hold

- **Scoped port, not a complete port.** Port only the OpenPDF subset needed
  for text measurement and coordinate-based rendering. A consumer that needs
  more belongs on OpenPDF or a full PDF library. Do not add those features
  here.
- **Coordinate-first, not a layout engine.** DabblerPdf renders
  already-laid-out elements at explicit coordinates. Layout lives in the
  consumer (`Dabbler.Report`).
- **Streams in, nothing assumed.** The library writes to streams the caller
  supplies. It opens no files, fetches no URLs and assumes no working
  directory.
- **Stay on the current .NET LTS line**, unless the maintainer deliberately
  decides otherwise.
- **License headers on every source file**: the SPDX
  `LGPL-2.1-only AND MPL-2.0` header the existing files carry.
- **The public API is a contract.** Prefer additive changes: new overloads,
  new optional parameters, new types. A breaking change needs a major
  version, a `CHANGELOG.md` entry and a migration note, and package
  validation enforces this at pack time. Every public type and member
  carries an XML documentation comment (`GenerateDocumentationFile` is on).
  A C#-idiomatic public shape is preferred over a mechanical Java
  translation.

## Testing

Use the kinds of test in this order of preference:

1. **Unit tests** for logic that can be isolated from I/O: font metrics,
   coordinate translation, encoding, date formatting. Factor pure logic out
   of mixed methods so it can be unit-tested.
2. **Integration tests** that drive `Document` and `PdfWriter` and assert
   against the produced bytes.
3. **Sample-based review.** For visual features, add or update a sample in
   `DabblerPdf.Samples/Samples/` so the output can be opened and inspected.
4. **Human review** for visual quality and typographic feel. It adds
   confidence; it does not replace the levels above.

A source file's tests are the file named after it (`BaseFont.cs` is tested
by `BaseFontTests.cs`).

**Bytes prove wiring; parsers prove rendering.** A test that asserts
content is *visible* must use a rendering-aware parser,
[PdfPig](https://github.com/UglyToad/PdfPig) (`PdfDocument.Open(bytes)
.GetPage(n)`), and check per physical page. A byte search of a content
stream proves the operator is present. It cannot see text that was
overdrawn, clipped, placed off-page or set in an unavailable font. This
rule exists because a byte-grep test once passed on a report whose title
was painted over by a later full-page fill. Byte-level assertions remain
right for structural facts such as object IDs, `/Info` keys and the
cross-reference table. `RenderingVisibilityDisciplineTests` and
`MultipageDirectContentRenderingTests` are the examples.

**Flaky and broken tests are not the same thing.** Run a failing test on
its own first. If it passes alone and fails in the full suite, move it into
a dedicated collection with
`[CollectionDefinition("...", DisableParallelization = true)]` and a
`TODO(tracked):` comment naming the root cause to investigate. If it fails
alone too, mark it `[Fact(Skip = "<precise reason>")]`. A visible skip is
honest; calling a real failure flaky hides it.

## Code style

- Namespaces: `DabblerPdf`, `DabblerPdf.Document`, `DabblerPdf.Pdf`,
  `DabblerPdf.Text`, `DabblerPdf.Image`, `DabblerPdf.Utilities`.
- PascalCase for public members, `_camelCase` for private fields.
- Nullable reference types are on in every project.
- Asynchronous methods return `Task` or `ValueTask` and end in `Async`.
- Use idiomatic C#: properties over getters and setters, `using`
  declarations, `IDisposable` where .NET expects it.

## Dependencies

Dependabot (`.github/dependabot.yml`) proposes NuGet and GitHub Actions
updates weekly. The library itself has no package dependencies. Keep it
that way, because every dependency would ship to every consumer.
