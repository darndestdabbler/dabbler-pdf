# DabblerPdf NuGet Release Preparation — Implementation Spec

> **Purpose:** Prepare DabblerPdf for public release as a NuGet package — XML doc comments, README, LICENSE, cleanup, and package generation.
> **Created:** 2026-04-09
> **Repo:** dabbler-pdf
> **Prerequisite:** All rendering fixes from `dabbler-pdf-rendering-fixes-spec.md` are complete and the visual samples pass inspection.

---

## Scope

This spec covers everything needed to go from "working library with passing tests" to "published NuGet package that a stranger can discover, evaluate, and use." The work falls into four areas:

1. **XML documentation comments** on all public API surfaces
2. **README.md** rewrite for NuGet and GitHub audiences
3. **LICENSE** file and cleanup (remove GEMINI.md)
4. **NuGet package configuration** and generation

---

## Session Plan

### Session 1 of 3: XML Documentation Comments

**Goal:** Every public type, method, property, enum, and constructor in the DabblerPdf library has an XML doc comment. These comments are the primary documentation surface for NuGet consumers — they appear in IntelliSense, on NuGet.org, and in generated API docs.

**Steps:**

1. **Inventory all public API surfaces.** Run a scan of every `.cs` file under `src/DabblerPdf/` and list all `public` classes, interfaces, structs, enums, methods, properties, constructors, and events. Record which ones already have `///` comments and which don't.

2. **Add `<summary>` comments to all public types.** Every public class, interface, struct, and enum gets a `<summary>` explaining what it is and when you'd use it. Examples:

   ```csharp
   /// <summary>
   /// Represents a PDF document that can contain one or more pages with text, images,
   /// lines, rectangles, and other visual elements. Use <see cref="AddPage"/> to add pages
   /// and <see cref="Save(Stream)"/> to write the document to a stream.
   /// </summary>
   public class PdfDocument { ... }
   ```

   ```csharp
   /// <summary>
   /// Controls how an image is scaled within its element bounds.
   /// </summary>
   public enum ImageScaleMode { ... }
   ```

3. **Add `<summary>` and `<param>` comments to all public methods and constructors.** Include `<returns>` for non-void methods and `<exception>` for methods that throw on bad input.

   ```csharp
   /// <summary>
   /// Adds a new page to the document with the specified dimensions.
   /// </summary>
   /// <param name="widthInches">Page width in inches (e.g., 8.5 for US Letter).</param>
   /// <param name="heightInches">Page height in inches (e.g., 11.0 for US Letter).</param>
   /// <returns>The newly created <see cref="PdfPage"/> for adding content.</returns>
   /// <exception cref="ArgumentOutOfRangeException">
   /// Thrown when <paramref name="widthInches"/> or <paramref name="heightInches"/> is less than or equal to zero.
   /// </exception>
   public PdfPage AddPage(double widthInches, double heightInches) { ... }
   ```

4. **Add `<summary>` comments to all public properties.** Include `<value>` when the meaning isn't obvious from the name.

   ```csharp
   /// <summary>
   /// Gets or sets the font family name. Supported families: Helvetica (default),
   /// Times New Roman, and Courier. Other names fall back to Helvetica.
   /// </summary>
   public string FontFamily { get; set; }
   ```

5. **Add `<summary>` comments to all enum members.**

   ```csharp
   /// <summary>
   /// Scales the image to fill the element bounds exactly, potentially distorting
   /// the aspect ratio if the element and image proportions differ.
   /// </summary>
   Stretch,

   /// <summary>
   /// Scales the image to fit within the element bounds while maintaining the
   /// original aspect ratio. The image is centered within the element.
   /// </summary>
   ScaleToFit,
   ```

6. **Enable XML documentation output in the .csproj.** Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to the project file. Build and fix any `CS1591` warnings (missing XML comments on public members).

7. **Build.** `dotnet build` must succeed with no `CS1591` warnings. If the project has `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, every public member must be documented.

**Guidelines for comment quality:**

- Write for someone who has never seen the codebase. Don't assume they know what RDF is or how the Reports engine works — DabblerPdf is a standalone PDF library.
- Use `<see cref="..."/>` to cross-reference related types and methods.
- For units, always state the unit: "in inches", "in points (1/72 inch)", "in pixels".
- For optional parameters with defaults, state the default: "Defaults to <c>Helvetica</c>."
- Don't repeat the member name in the summary. `/// <summary>Gets the font size.</summary>` on a property called `FontSize` is useless. Say what it means: `/// <summary>Gets or sets the font size in points. Defaults to 12.</summary>`
- For methods that wrap OpenPDF internals, describe the behavior, not the implementation. Consumers don't care that you call `BaseFont.CreateFont()` internally.

**Touches:** All `.cs` files under `src/DabblerPdf/` with public members, `DabblerPdf.csproj`
**Ends with:** `dotnet build` with no CS1591 warnings; every public API surface has XML docs
**Progress keys:** `session-1/inventory=complete`, `session-1/type-comments=complete`, `session-1/method-comments=complete`, `session-1/property-comments=complete`, `session-1/enum-comments=complete`, `session-1/build-clean=complete`

---

### Session 2 of 3: README, LICENSE, and Cleanup

**Goal:** The repo has a professional README suitable for NuGet.org and GitHub, a proper LICENSE file, and no leftover artifacts from the development process.

**Steps:**

1. **Delete `GEMINI.md`** from the repository root. This was a development artifact and should not be part of the public repo.

2. **Write `LICENSE`** file at the repository root. Use the license you intend for the public package. If MIT:

   ```
   MIT License

   Copyright (c) 2025-2026 [Your Name or Organization]

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
   ```

   Adjust the license choice based on your preference. If a different license is intended, substitute accordingly. The critical thing is that the LICENSE file exists, is valid, and matches the `PackageLicenseExpression` in the `.csproj`.

3. **Write `README.md`** at the repository root. The README serves two audiences: NuGet.org browsers (who see it on the package page) and GitHub visitors. Structure:

   **Section: Header / Badge area**
   ```markdown
   # DabblerPdf

   [![NuGet](https://img.shields.io/nuget/v/DabblerPdf.svg)](https://www.nuget.org/packages/DabblerPdf)
   [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

   A lightweight .NET library for generating PDF documents. Built on OpenPDF, DabblerPdf provides
   a clean API for creating pages with text, images, lines, rectangles, and styled content —
   no heavy dependencies, no design surface, just code-to-PDF.
   ```

   **Section: Features**
   - Bulleted list of key capabilities: text rendering with Helvetica/Times/Courier font families, bold/italic/bold-italic styles, text wrapping with word and character-level breaking, images with Stretch/ScaleToFit/ActualSize modes, lines and rectangles with solid/dashed/dotted borders, hex color support, padding, multi-page documents, etc.
   - Keep it scannable — someone deciding whether to install the package reads this in 10 seconds.

   **Section: Installation**
   ```markdown
   ## Installation

   ```bash
   dotnet add package DabblerPdf
   ```
   ```

   **Section: Quick Start**
   - A minimal code example (10–15 lines) that creates a one-page PDF with a title and a paragraph. This should compile and produce a working PDF. Use the actual DabblerPdf API — not pseudocode.
   - A slightly more involved example showing text styling, an image, and a line.

   **Section: API Overview**
   - Brief table or list of the main types and what they do. Not exhaustive — just enough for discoverability. Point to XML docs / IntelliSense for full details.

   **Section: Font Support**
   - Document the supported font families (Helvetica, Times New Roman, Courier) and styles (regular, bold, italic, bold italic). Note that these are the PDF standard Base 14 fonts — no font embedding required, guaranteed to render on every PDF viewer.

   **Section: Samples**
   - Mention the `DabblerPdf.Samples` project and how to run it. Link to the samples directory.

   **Section: License**
   - One-liner: "DabblerPdf is licensed under the [MIT License](LICENSE)." (or whichever license)

   **Section: Contributing**
   - Brief note on whether contributions are accepted, how to report issues, etc. Even a simple "Issues and PRs welcome" is fine.

4. **Review for leftover artifacts.** Check the repo for any files that shouldn't be in a public release:
   - `GEMINI.md` (already deleted in step 1)
   - Any `TODO.md`, `NOTES.md`, or personal development logs
   - Any hardcoded file paths, connection strings, or credentials in source files
   - Any `.env` files or secrets
   - Check `.gitignore` includes `DabblerPdf.Samples/Output/`, `bin/`, `obj/`, `*.user`, `*.suo`

5. **Verify README renders correctly.** View the README in a Markdown previewer. Check that code blocks are syntax-highlighted, badges render (once published), and the structure is clean.

**Creates:** `LICENSE`, rewritten `README.md`
**Deletes:** `GEMINI.md`, any other development artifacts
**Ends with:** Repo is clean; README and LICENSE present; no leftover artifacts
**Progress keys:** `session-2/delete-gemini=complete`, `session-2/license=complete`, `session-2/readme=complete`, `session-2/cleanup-audit=complete`

---

### Session 3 of 3: NuGet Package Configuration and Generation

**Goal:** The `.csproj` is configured with all NuGet metadata, the package builds successfully, and the `.nupkg` is ready for upload to NuGet.org.

**Steps:**

1. **Add NuGet metadata to `DabblerPdf.csproj`.** Add or update the following properties in the main `<PropertyGroup>`:

   ```xml
   <!-- Package identity -->
   <PackageId>DabblerPdf</PackageId>
   <Version>1.0.0</Version>
   <Authors>[Your Name]</Authors>
   <Company>[Your Company, if applicable]</Company>

   <!-- Description shown on NuGet.org -->
   <Description>A lightweight .NET library for generating PDF documents with text, images, lines, rectangles, and styled content. Built on OpenPDF with a clean, code-first API.</Description>

   <!-- NuGet.org metadata -->
   <PackageTags>pdf;pdf-generation;document;report;dotnet</PackageTags>
   <PackageProjectUrl>https://github.com/[your-username]/dabbler-pdf</PackageProjectUrl>
   <RepositoryUrl>https://github.com/[your-username]/dabbler-pdf</RepositoryUrl>
   <RepositoryType>git</RepositoryType>

   <!-- License -->
   <PackageLicenseExpression>MIT</PackageLicenseExpression>

   <!-- README on NuGet.org -->
   <PackageReadmeFile>README.md</PackageReadmeFile>

   <!-- XML docs included in package for IntelliSense -->
   <GenerateDocumentationFile>true</GenerateDocumentationFile>

   <!-- Package icon (optional but recommended) -->
   <!-- <PackageIcon>icon.png</PackageIcon> -->

   <!-- Release notes for this version -->
   <PackageReleaseNotes>Initial public release. Core PDF generation: text (Helvetica, Times, Courier with bold/italic), images (Stretch, ScaleToFit, ActualSize), lines, rectangles, borders, colors, padding, multi-page documents with headers and footers.</PackageReleaseNotes>

   <!-- Copyright -->
   <Copyright>Copyright (c) 2025-2026 [Your Name or Organization]</Copyright>

   <!-- Deterministic builds for reproducibility -->
   <Deterministic>true</Deterministic>

   <!-- Include symbols for debugging -->
   <IncludeSymbols>true</IncludeSymbols>
   <SymbolPackageFormat>snupkg</SymbolPackageFormat>
   ```

2. **Include README.md in the package.** Add an `<ItemGroup>` to include the README:

   ```xml
   <ItemGroup>
     <None Include="../README.md" Pack="true" PackagePath="" />
   </ItemGroup>
   ```

   Adjust the relative path based on where the `.csproj` is relative to the repo root.

3. **Optional: Add a package icon.** If you have a logo or icon (128×128 PNG recommended):

   ```xml
   <ItemGroup>
     <None Include="../icon.png" Pack="true" PackagePath="" />
   </ItemGroup>
   ```

   If not, skip this — NuGet shows a default icon. Can be added later.

4. **Verify the package dependency chain.** Run `dotnet pack` and inspect the generated `.nuspec` (inside the `.nupkg`, which is a ZIP file). Verify:
   - The OpenPDF dependency is listed correctly with the right version range
   - No unwanted transitive dependencies are pulled in
   - No internal project references (like the Samples project) leak into the package
   - The `lib/` folder contains the DabblerPdf assembly and XML docs

5. **Build the package.** 

   ```bash
   dotnet pack src/DabblerPdf/DabblerPdf.csproj -c Release -o ./nupkg
   ```

   This should produce:
   - `nupkg/DabblerPdf.1.0.0.nupkg`
   - `nupkg/DabblerPdf.1.0.0.snupkg` (symbols)

6. **Inspect the package contents.** Rename `.nupkg` to `.zip` and extract, or use `dotnet nuget` tooling. Verify:
   - `lib/net8.0/DabblerPdf.dll` (or the correct target framework) is present
   - `lib/net8.0/DabblerPdf.xml` (XML docs) is present
   - `README.md` is at the package root
   - `LICENSE` expression is in the `.nuspec`
   - No test assemblies, sample projects, or development artifacts are included
   - File size is reasonable (should be well under 1 MB for the library itself, plus whatever OpenPDF brings)

7. **Test the package locally.** Create a throwaway console project that references the local `.nupkg` via a local NuGet source:

   ```bash
   mkdir /tmp/test-dabblerpdf && cd /tmp/test-dabblerpdf
   dotnet new console
   dotnet nuget add source /path/to/nupkg --name local
   dotnet add package DabblerPdf --version 1.0.0 --source local
   ```

   Write a minimal program that creates a one-page PDF:

   ```csharp
   // Minimal smoke test using the installed NuGet package
   // Should produce a valid PDF file
   ```

   Verify:
   - The package installs without errors
   - IntelliSense shows XML doc comments on all public types and methods
   - The minimal program compiles and produces a valid PDF
   - No missing dependencies at runtime

8. **Run the full test suite one final time.**

   ```bash
   dotnet test
   ```

   All tests must pass.

9. **Run the samples one final time.**

   ```bash
   cd DabblerPdf.Samples
   dotnet run
   ```

   All 10 samples should generate successfully. Quick visual spot-check of S09 (invoice) to confirm no regressions.

10. **Document the release.** Create or update `CHANGELOG.md`:

    ```markdown
    # Changelog

    ## 1.0.0 — [Date]

    Initial public release.

    ### Features
    - PDF document creation with single and multi-page support
    - Text rendering: Helvetica, Times New Roman, Courier font families
    - Font styles: regular, bold, italic, bold italic
    - Text wrapping: word-level and character-level breaking
    - Images: Stretch, ScaleToFit, ActualSize scale modes
    - Lines: horizontal and vertical, solid/dashed/dotted, configurable weight and color
    - Rectangles: fill, border, or both, with solid/dashed/dotted borders
    - Colors: full hex color support (#RRGGBB and #AARRGGBB)
    - Padding: uniform and per-side (top/bottom/left/right)
    - Page headers and footers
    - Page numbering with backfill ("Page X of Y")
    ```

**Creates:** Updated `DabblerPdf.csproj`, `CHANGELOG.md`, generated `.nupkg` and `.snupkg`
**Ends with:** Package builds; local install test passes; IntelliSense works; all tests pass; all samples pass
**Progress keys:** `session-3/csproj-metadata=complete`, `session-3/pack-build=complete`, `session-3/package-inspection=complete`, `session-3/local-install-test=complete`, `session-3/changelog=complete`

---

## Publishing (Manual — Not Part of This Spec)

After all three sessions are complete and the package is verified, publishing to NuGet.org is a manual step:

```bash
dotnet nuget push nupkg/DabblerPdf.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push nupkg/DabblerPdf.1.0.0.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

The API key is obtained from your NuGet.org account. The symbols package (`.snupkg`) is pushed to the same source and is automatically associated with the main package.

After publishing:
- Verify the package appears on NuGet.org within a few minutes
- Verify the README renders correctly on the package page
- Verify the license link works
- Install the package from NuGet.org (not the local source) in a fresh project to confirm end-to-end

---

## Code Style

- **XML doc comments:** Use `///` triple-slash format. Always include `<summary>`. Use `<param>`, `<returns>`, `<exception>`, `<see cref="..."/>`, `<c>...</c>`, and `<value>` as appropriate.
- **README:** Use standard GitHub-flavored Markdown. Keep code examples minimal and compilable.
- **No breaking changes:** This spec adds documentation and metadata only. No API changes.
