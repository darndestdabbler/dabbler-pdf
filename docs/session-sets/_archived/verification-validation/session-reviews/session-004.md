# Verification Round 1

**ISSUES FOUND**

### Issue 1
**Severity:** Minor

**Issue:** The method `PdfWriter.AddPageImage` has a redundant `PdfWriter` parameter. As an instance method, it can access its own members via `this`, making the parameter unnecessary and the call site in `PdfContentByte` slightly confusing.

**Location:** `src/DabblerPdf/Pdf/PdfWriter.cs`, line 211
```csharp
internal PdfName AddPageImage(Image.Image image, PdfWriter writer)
```

**Location:** `src/DabblerPdf/Pdf/PdfContentByte.cs`, line 528
```csharp
var resourceName = _writer.AddPageImage(image, _writer);
```

**Fix:**
1.  In `src/DabblerPdf/Pdf/PdfWriter.cs`, change the method signature and implementation:
    ```csharp
    // Before
    internal PdfName AddPageImage(Image.Image image, PdfWriter writer)
    {
        if (!_imageObjects.TryGetValue(image, out var imageRef))
        {
            // Create the PDF image XObject (handles soft mask if needed)
            var pdfImage = Image.PdfImage.Create(image, writer);
            imageRef = _body.Add(pdfImage, _outputStream);
            _imageObjects[image] = imageRef;
        }
    // ...
    }

    // After
    internal PdfName AddPageImage(Image.Image image)
    {
        if (!_imageObjects.TryGetValue(image, out var imageRef))
        {
            // Create the PDF image XObject (handles soft mask if needed)
            var pdfImage = Image.PdfImage.Create(image, this);
            imageRef = _body.Add(pdfImage, _outputStream);
            _imageObjects[image] = imageRef;
        }
    // ...
    }
    ```
2.  In `src/DabblerPdf/Pdf/PdfContentByte.cs`, update the call site:
    ```csharp
    // Before
    var resourceName = _writer.AddPageImage(image, _writer);

    // After
    var resourceName = _writer.AddPageImage(image);
    ```

---

# Verification Round 2

ISSUES FOUND

Issue â†’ Location â†’ Fix
---
**Inadequate Test Coverage:** The new image deduplication logic in `PdfWriter` is not covered by a test. While the implementation appears correct, a test is required to verify that adding the same `Image` instance to multiple pages results in only one image XObject being written to the PDF file, preventing regressions. â†’ `tests/DabblerPdf.Tests/Pdf/PdfWriterTests.cs` â†’ Add a new test class `ImageRegistrationTests` with the following test case to verify the document-level image cache (`_imageObjects`) is working as intended.

```csharp
public class ImageRegistrationTests
{
    [Fact]
    public void AddPageImage_SameImageOnMultiplePages_WritesImageXObjectOnce()
    {
        var (writer, ms, doc) = CreateWriter();
        // A minimal 1x1 black pixel transparent PNG
        var pngBytes = new byte[] {
            0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1f, 0x15, 0xc4,
            0x89, 0x00, 0x00, 0x00, 0x0c, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9c, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0d, 0x0a, 0x2d, 0xb4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44, 0xae,
            0x42, 0x60, 0x82
        };
        var image = DabblerPdf.Image.Image.GetInstance(pngBytes);
        doc.Open();

        // Page 1: Add the image. This should create the XObject.
        doc.NewPage();
        writer.AddPageImage(image);

        // Page 2: Add the same image instance. This should reuse the existing XObject.
        doc.NewPage();
        writer.AddPageImage(image);

        doc.Close();

        var output = Encoding.ASCII.GetString(ms.ToArray());

        // Count occurrences of the image XObject definition header.
        var occurrences = System.Text.RegularExpressions.Regex.Matches(output, "/Subtype /Image").Count;

        // The image object should only be written once to the PDF body.
        Assert.Equal(1, occurrences);
    }
}
```

---

# Verification Round 3

ISSUES FOUND

### Issue: Inconsistent Image Resource Naming
Image XObject resource names (e.g., `/Im1`) are generated using a document-level counter (`_imageCounter`), while font resource names (e.g., `/F1`) are generated using a page-level counter (`_pageFonts.Count`).

This leads to image names being unique across the entire document (e.g., `/Im1` on page 1, `/Im2` on page 2) rather than being reused on a per-page basis (e.g., `/Im1` on page 1, `/Im1` on page 2 for the first image on that page). This is inconsistent, inefficient, and makes the `_imageCounter` field redundant as the page-level dictionary count can be used directly, matching the font implementation.

---

**→ Location:** `src/DabblerPdf/Pdf/PdfWriter.cs`

```csharp
// Lines 42-43
private readonly Dictionary<Image.Image, PdfIndirectReference> _imageObjects = new();
private int _imageCounter;
```
```csharp
// Lines 206-208
_imageCounter++;
var resourceName = new PdfName($"Im{_imageCounter}");
_pageXObjects[resourceName] = imageRef;
```

---

**→ Fix:**

1.  Remove the `_imageCounter` field.
2.  Update the resource name generation in `AddPageImage` to use the page-level `_pageXObjects` dictionary count.

```diff
--- a/src/DabblerPdf/Pdf/PdfWriter.cs
+++ b/src/DabblerPdf/Pdf/PdfWriter.cs
@@ -41,7 +41,6 @@
     private readonly Dictionary<string, PdfIndirectReference> _fontObjects = new();
     // Document-level image cache: Image instance → indirect reference
     private readonly Dictionary<Image.Image, PdfIndirectReference> _imageObjects = new();
-    private int _imageCounter;
 
     /// <summary>The document this writer is associated with.</summary>
     public Document.Document? Document { get; internal set; }
@@ -199,8 +198,7 @@
                 return name;
         }
 
-        _imageCounter++;
-        var resourceName = new PdfName($"Im{_imageCounter}");
+        var resourceName = new PdfName($"Im{_pageXObjects.Count + 1}");
         _pageXObjects[resourceName] = imageRef;
         return resourceName;
     }

```
