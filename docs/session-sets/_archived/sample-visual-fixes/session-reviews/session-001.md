# Verification Round 1

ISSUES FOUND

1.  **Severity**: Major
    **Issue**: Inconsistent State Management Pattern Creates New State Pollution Path
    **Location**: `SampleBase.cs`, lines 100-109, `DrawText(..., BaseColor color)`
    **Fix**: The `DrawText` overload that accepts a color parameter manually resets the fill color to a hardcoded `BaseColor.Black` after drawing. This pollutes the graphics state, defeating the purpose of the primary fix. This method should use the same robust `SaveState`/`RestoreState` pattern applied to `DrawRectFilled` to ensure graphics state is properly isolated.

    ```csharp
    // FROM
    protected static void DrawText(PdfContentByte cb, string text, BaseFont font, double size, double x, double y, BaseColor color)
    {
        cb.BeginText();
        cb.SetFontAndSize(font, size);
        cb.SetColorFill(color);
        cb.SetTextMatrix(x, PdfY(y));
        cb.ShowText(text);
        cb.EndText();
        cb.SetColorFill(BaseColor.Black);
    }
    
    // TO
    protected static void DrawText(PdfContentByte cb, string text, BaseFont font, double size, double x, double y, BaseColor color)
    {
        cb.SaveState();
        cb.BeginText();
        cb.SetFontAndSize(font, size);
        cb.SetColorFill(color);
        cb.SetTextMatrix(x, PdfY(y));
        cb.ShowText(text);
        cb.EndText();
        cb.RestoreState();
    }
    ```

---

# Verification Round 2

**VERIFIED**

The fix is correct and complete. The colored `DrawText` overload now properly uses `SaveState()`/`RestoreState()` to isolate the graphics state, and the manual `SetColorFill(BaseColor.Black)` reset has been removed. All three state-changing helpers are consistent:

1. **`DrawRectFilled`** — `SaveState()` → set fill → fill → `RestoreState()` ✓
2. **`DrawRectFilledAndStroked`** — `SaveState()` → set fill/stroke/linewidth → fill-stroke → `RestoreState()` ✓
3. **Colored `DrawText`** — `SaveState()` → set font/color/matrix → show text → `RestoreState()` ✓

The no-color `DrawText` overload correctly remains unchanged, as it doesn't modify fill/stroke state. The `DrawLabel` helper delegates to the colored `DrawText`, so it inherits the fix. No direct `SetColorFill`/`Fill` calls exist outside of `SaveState`/`RestoreState` pairs.
