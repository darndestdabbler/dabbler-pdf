# Change-log: Sample Visual Fixes

This update addresses several visual rendering issues identified during a review of sample PDFs, improving text layout, color handling, and image rendering accuracy.

## Fixes & Enhancements

*   **Graphics State:** Fixed fill color "bleeding" into subsequent text or shapes by wrapping `DrawRectFilled`, `DrawRectFilledAndStroked`, and colored `DrawText` calls in `SaveState`/`RestoreState` blocks.
*   **Text Layout:**
    *   The `y` parameter for `DrawText` methods now represents the **visual top** of the text, not the font baseline. This provides more intuitive positioning.
    *   The `WrapText` method now breaks words at the character level if a single word is longer than the available line width, preventing text overflow.
*   **Image Rendering:** Added a clipping rectangle for images drawn using `ActualSize` to prevent them from drawing outside their intended bounds.

## Breaking Changes

*   **`DrawText` Y-Coordinate:** The `y` parameter for `TextLayout.DrawText` and `SampleBase.DrawText` has been changed. It now specifies the visual top of the first line of text. All existing calls to `DrawText` may require vertical adjustment to account for the font's ascent.

## Test Coverage

*   562 existing tests continue to pass.
*   4 existing `DrawText` tests were updated to reflect the new y-coordinate behavior.
*   3 new tests were added to verify character-level word breaking in `WrapText`.
*   **Total Passing Tests: 565**