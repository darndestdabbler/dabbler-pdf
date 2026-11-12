# Verification Round 1

I'll systematically verify each checklist item.

## 1. Do the source file changes correctly fix the identified issues?

**Fix 1: Image.ScaleToFit — prevent scaling up**
```csharp
if (ratio < 1.0)
{
    ScaledWidth *= ratio;
    ScaledHeight *= ratio;
}
```
✅ Correct. When image is smaller than target, both ratios > 1.0, so `ratio > 1.0`, and the guard prevents scaling up.

**Fix 2: Jpeg — segment length validation**
```csharp
if (segmentLength < 2)
    throw new ArgumentException($"Invalid JPEG segment length {segmentLength} at offset {offset}.");
```
✅ Correct. JPEG segment length includes the 2-byte length field itself, so minimum valid value is 2.

**Fix 3: PngImage — truncated chunk throws instead of silent break**
```csharp
if (chunkDataStart + chunkLength + 4 > data.Length) // +4 for CRC
    throw new ArgumentException($"PNG chunk '{chunkType}' is truncated.");
```
✅ Correct. Previously this would have silently broken out of the loop.

**Fix 4: PngImage — sub-byte stride calculation**
```csharp
int rowBytes = (PixelWidth * GetBitsPerPixel() + 7) / 8; // Actual data bytes per row
```
With new `GetBitsPerPixel()` method. ✅ Correct. For example, 4-bit grayscale, width=4: `(4*4+7)/8 = 2 bytes`, which is right. Previously using `GetBytesPerPixel()` (which returns `Math.Max(1, ...)`) would have given `1*4 = 4` bytes, which is wrong for sub-byte formats.

**Fix 5: PdfImage — skip SoftMask when softMaskRef already provided**
```csharp
if (png.AlphaChannel != null && softMaskRef == null)
```
✅ Correct. When called from `Create()` with a pre-written soft mask ref, we no longer create a redundant `SoftMask` object.

## 2. Are the fixes complete and not introducing new bugs?

- **ScaleToFit**: The fix is clean. No edge case concerns — exact fit (`ratio == 1.0`) correctly doesn't scale.
- **Jpeg segment length**: Positioned correctly after the length read, before using `segmentLength` for offset arithmetic. ✅
- **PNG truncated chunk**: The check uses `chunkDataStart + chunkLength + 4` which accounts for length(4) + type(4) + data(chunkLength) + CRC(4). `chunkDataStart = offset + 8`, so total from `offset` is `8 + chunkLength + 4 = chunkLength + 12`. ✅
- **Sub-byte stride**: `GetBytesPerPixel()` is still used for filter distance (the `bytesPerPixel` parameter to `Unfilter`), which is correct per PNG spec — filter operations use byte-level offsets with minimum 1. The `rowBytes` correctly uses bit-level calculation. ✅
- **PdfImage**: The `SoftMask` property is only set when `softMaskRef == null` AND alpha exists. When `softMaskRef != null`, only the `/SMask` dictionary entry is set. ✅

**Potential concern**: In `ExpandSubByte`, the row stride calculation `int rowBytes = (PixelWidth * bitDepth + 7) / 8;` matches the stride in `ProcessPixelData`. The unfiltered data passed to `ExpandSubByte` has already had filter bytes removed, and rows are `rowBytes` wide. The indexing `y * rowBytes + ...` is correct. ✅

## 3. Do the new tests adequately cover the fixed behaviors?

| Fix | Test Coverage |
|-----|--------------|
| ScaleToFit no-scale-up | `ScaleToFit_ImageAlreadySmaller_DoesNotScaleUp` ✅ |
| Jpeg segment length < 2 | `Jpeg_InvalidSegmentLength_Throws` (length=0), `Jpeg_SegmentLengthOne_Throws` (length=1) ✅ |
| PNG truncated chunk | `Png_TruncatedChunk_Throws` ✅ |
| Sub-byte stride | `Png_4BitGrayscale_ParsesCorrectly` ✅ |
| PdfImage redundant SoftMask | No direct test — but this is an optimization, not a correctness issue observable from public API without mocking `PdfWriter`. Acceptable gap given integration tests cover the `Create()` path. ⚠️ Minor gap, acceptable. |

## 4. Does JpegTests.cs cover spec requirements?

| Requirement | Tests |
|-------------|-------|
| Header parsing | `Jpeg_SOF0_Baseline_ParsesDimensions`, `Jpeg_SOF2_Progressive_ParsesDimensions`, `Jpeg_SOF1_ExtendedSequential_ParsesDimensions` ✅ |
| Dimension extraction | All SOF tests verify width/height ✅ |
| Color space detection | `Jpeg_Grayscale_HasOneComponent`, `Jpeg_Rgb_HasThreeComponents`, `Jpeg_Cmyk_HasFourComponents` ✅ |
| Marker navigation | `Jpeg_SkipsAppMarkers_BeforeSOF`, `Jpeg_SkipsCommentMarker`, `Jpeg_HandlesPaddingBytes` ✅ |
| Error handling | 6 error tests covering empty, short, invalid SOI, missing SOF, truncated SOF, invalid segment length ✅ |
| Edge cases | 1x1 dimensions, 4096x4096 dimensions ✅ |

22 tests total — comprehensive coverage. ✅

## 5. Are all files that should have been created/touched accounted for?

| File | Spec | Status |
|------|------|--------|
| `tests/.../Image/JpegTests.cs` | Creates | ✅ Created (22 tests) |
| `src/.../Image/Image.cs` | Touches (if fixes needed) | ✅ ScaleToFit fix |
| `src/.../Image/Jpeg.cs` | Touches (if fixes needed) | ✅ Segment length validation |
| `src/.../Image/PngImage.cs` | Touches (if fixes needed) | ✅ Truncated chunk + stride fix |
| `src/.../Image/PdfImage.cs` | Touches (if fixes needed) | ✅ SoftMask guard |
| `tests/.../Image/ImageTests.cs` | Touches (if gaps found) | ✅ 3 new tests added |
| `tests/.../Image/ImageTestHelpers.cs` | Not in spec but needed | ✅ `CreateSubBytePng` added |
| `tests/.../Image/ImageIntegrationTests.cs` | Touches (if gaps found) | Not touched — no evidence of PNG edge case gaps there. Acceptable. |

## 6. Is the code idiomatic C# and consistent with project style?

- XML doc comments on public APIs ✅
- SPDX license headers ✅
- `ReadOnlySpan<byte>` and `AsSpan` usage ✅
- Collection expressions (`[]`) consistent with C# 12 ✅
- `sealed` on concrete implementations ✅
- Naming conventions (PascalCase methods, `_camelCase` fields) ✅
- Test naming: `ClassName_Scenario_Expectation` pattern ✅
- Test organization with section comments ✅

**One minor style observation**: `JpegTests` has its own `WriteUInt16BE` helper that duplicates the one in `ImageTestHelpers`. Not a bug, and test helpers often duplicate for locality. Acceptable.

---

**VERIFIED**

All 5 identified issues are correctly fixed without introducing new bugs. Test coverage is comprehensive (22 new Jpeg tests, 3 new Image tests, new sub-byte PNG helper). Build passes clean (0 warnings, 0 errors, 488 tests passing). Code is idiomatic and consistent with project conventions.
