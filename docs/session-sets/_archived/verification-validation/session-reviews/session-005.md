# Verification Round 1

### Code Review

**Overall:** The code is of high quality, with excellent test coverage. The changes address important aspects like thread safety, spec compliance, and data accuracy. The TrueType parser is robust. The review identifies one likely bug and several minor areas for improvement.

---

**Issue:** Incorrect character widths for lowercase i-variants in Helvetica.
**â†’ Location:** `src/DabblerPdf/Text/BuiltinFonts.cs`, lines 246-249
**â†’ Fix:** The widths for `igrave`, `iacute`, `icircumflex`, and `idieresis` (char codes 236-239) are set to 278, which is the width of the uppercase `I` variants. According to the Adobe Font Metrics (AFM) file, they should have the same width as lowercase `i` (222).

```diff
-        w[236] = 278; // igrave (corrected from 222)
-        w[237] = 278; // iacute (corrected from 222)
-        w[238] = 278; // icircumflex (corrected from 222)
-        w[239] = 278; // idieresis (corrected from 222)
+        w[236] = 222; // igrave
+        w[237] = 222; // iacute
+        w[238] = 222; // icircumflex
+        w[239] = 222; // idieresis

```

---

**Issue:** TrueType font caching is keyed by file path, which is inefficient if the same font is accessed via different paths.
**â†’ Location:** `src/DabblerPdf/Text/BaseFont.cs`, line 62
**â†’ Fix:** Cache TrueType fonts by their PostScript name instead of the file path. This requires reading the PostScript name *before* the cache lookup, but avoids redundant font parsing and memory usage.

```csharp
// In BaseFont.cs, CreateFont method
public static BaseFont CreateFont(string name, string encoding, bool embedded)
{
    if (BuiltinFonts.IsStandardFont(name))
    {
        string key = $"{name}-{encoding}"; // Key for standard fonts remains the same
        return _fontCache.GetOrAdd(key, _ => new Type1Font(name));
    }

    if (IsTrueTypeFontFile(name))
    {
        // For TrueType, read data and key by PostScript name to avoid path duplicates
        byte[] fontData = File.ReadAllBytes(name);
        string psName = TrueTypeFont.ExtractPostscriptName(fontData);
        string key = $"{psName}-{encoding}";
        return _fontCache.GetOrAdd(key, _ => new TrueTypeFont(psName, fontData));
    }

    throw new ArgumentException(
        $"Font '{name}' is not a standard PDF font and is not a valid TrueType font file path.",
        nameof(name));
}
```

---

**Issue:** Redundant and slightly incorrect manual bounds check in the TrueType cmap parser.
**â†’ Location:** `src/DabblerPdf/Text/TrueTypeFont.cs`, line 462
**â†’ Fix:** The `ReadUInt16` method already performs a sufficient bounds check (`CanRead(offset, 2)`). The manual check `if (glyphIdOffset + 1 < _data.Length)` is both redundant and less precise (it should be `+ 2 <=`). Remove it for clarity and correctness.

```diff
-                        if (glyphIdOffset + 1 < _data.Length)
-                        {
-                            glyphId = ReadUInt16(glyphIdOffset);
-                            if (glyphId != 0)
-                                glyphId = (glyphId + idDelta) & 0xFFFF;
-                        }
-                        else
-                        {
-                            glyphId = 0;
-                        }
+                        glyphId = ReadUInt16(glyphIdOffset);
+                        if (glyphId != 0)
+                            glyphId = (glyphId + idDelta) & 0xFFFF;

```

---

**Issue:** Serif flag logic in `ComputeFlags` could be more precise.
**â†’ Location:** `src/DabblerPdf/Text/TrueTypeFont.cs`, line 215
**â†’ Fix:** The Panose specification is more detailed. While the current check is not strictly wrong, a more robust check would also include `panose[0]` (Family Type) to ensure the font is in the "Latin Text" class before checking the serif style. This prevents misclassifying decorative or script fonts that happen to have a specific `bSerifStyle` value.

```diff
         // Bit 2 (value 2): Serif — check panose[0] (family type) and panose[1] (serif style)
-        if (os2 != null && os2.Panose is { Length: >= 2 } && os2.Panose[1] >= 2 && os2.Panose[1] <= 10)
+        if (os2?.Panose is { Length: >= 2 } && os2.Panose[0] == 2 && os2.Panose[1] >= 2 && os2.Panose[1] <= 10)
             flags |= 2; // Serif

```
