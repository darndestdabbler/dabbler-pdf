# Verification Round 1

**ISSUES FOUND**

The claim of "0 CS1591 warnings" is incorrect given the number of public APIs missing required XML documentation tags. While `<summary>` tags are present, many methods, constructors, operators, and records are missing `<param>`, `<returns>`, or `<value>` tags, which would trigger CS1591 warnings when `<GenerateDocumentationFile>` is enabled.

1.  **Issue**: Missing `<param>` tags in constructors.
    *   **Location**: `src/DabblerPdf/Document/Rectangle.cs`
    *   **Fix**:
        ```csharp
        /// <summary>
        /// Creates a rectangle from explicit coordinates.
        /// </summary>
        /// <param name="left">Lower-left X coordinate.</param>
        /// <param name="bottom">Lower-left Y coordinate.</param>
        /// <param name="right">Upper-right X coordinate.</param>
        /// <param name="top">Upper-right Y coordinate.</param>
        public Rectangle(double left, double bottom, double right, double top)
        // ...

        /// <summary>
        /// Creates a rectangle with lower-left at origin (0, 0).
        /// Convenient for page sizes: <c>new Rectangle(width, height)</c>.
        /// </summary>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public Rectangle(double width, double height)
        // ...

        /// <summary>Creates a copy of an existing rectangle.</summary>
        /// <param name="other">The rectangle to copy.</param>
        public Rectangle(Rectangle other)
        ```

2.  **Issue**: Missing `<param>` tags on operators.
    *   **Location**: `src/DabblerPdf/Document/BaseColor.cs`
    *   **Fix**:
        ```csharp
        /// <summary>Determines whether two colors have equal RGB values.</summary>
        /// <param name="left">The first color to compare.</param>
        /// <param name="right">The second color to compare.</param>
        public static bool operator ==(BaseColor? left, BaseColor? right) =>
        // ...

        /// <summary>Determines whether two colors have different RGB values.</summary>
        /// <param name="left">The first color to compare.</param>
        /// <param name="right">The second color to compare.</param>
        public static bool operator !=(BaseColor? left, BaseColor? right) => !(left == right);
        ```

3.  **Issue**: Missing `<param>` tag in method.
    *   **Location**: `src/DabblerPdf/Pdf/PdfObject.cs`
    *   **Fix**:
        ```csharp
        /// <summary>
        /// Writes the PDF representation of this object to the given stream.
        /// </summary>
        /// <param name="outputStream">The stream to write to.</param>
        public virtual void WriteTo(Stream outputStream)
        ```

4.  **Issue**: Multiple methods missing `<param>` tags.
    *   **Location**: `src/DabblerPdf/Pdf/PdfDictionary.cs`
    *   **Fix**:
        ```csharp
        /// <summary>
        /// Sets a key-value pair. If <paramref name="value"/> is <c>null</c> or
        /// <see cref="PdfNull"/>, the entry is removed (matching OpenPDF behavior).
        /// </summary>
        /// <param name="key">The key (a <see cref="PdfName"/>).</param>
        /// <param name="value">The value to associate with the key.</param>
        public void Put(PdfName key, PdfObject? value)
        // ...

        /// <summary>Gets the value associated with the given key, or <c>null</c> if not found.</summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The associated <see cref="PdfObject"/> or <c>null</c>.</returns>
        public PdfObject? Get(PdfName key) => _entries.GetValueOrDefault(key);

        /// <summary>Returns <c>true</c> if the dictionary contains the given key.</summary>
        /// <param name="key">The key to check for.</param>
        /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
        public bool Contains(PdfName key) => _entries.ContainsKey(key);

        /// <summary>Removes the entry with the given key, if present.</summary>
        /// <param name="key">The key of the entry to remove.</param>
        public void Remove(PdfName key) => _entries.Remove(key);

        // All GetAs... methods need a param tag, for example:
        /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfDictionary"/>, or <c>null</c>.</summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The value as a <see cref="PdfDictionary"/>, or <c>null</c>.</returns>
        public PdfDictionary? GetAsDictionary(PdfName key) => Get(key) as PdfDictionary;
        ```

5.  **Issue**: Missing `<param>` tags in constructor and operators.
    *   **Location**: `src/DabblerPdf/Pdf/PdfName.cs`
    *   **Fix**:
        ```csharp
        /// <summary>
        /// Creates a PdfName from a string. The leading slash is added automatically
        /// if not already present.
        /// </summary>
        /// <param name="name">The name string (e.g., "Type").</param>
        public PdfName(string name) : base(NAME)
        // ...

        /// <summary>Determines whether two names are equal.</summary>
        /// <param name="left">The first name to compare.</param>
        /// <param name="right">The second name to compare.</param>
        public static bool operator ==(PdfName? left, PdfName? right) =>
        // ...

        /// <summary>Determines whether two names are not equal.</summary>
        /// <param name="left">The first name to compare.</param>
        /// <param name="right">The second name to compare.</param>
        public static bool operator !=(PdfName? left, PdfName? right) => !(left == right);
        ```

6.  **Issue**: Multiple methods and indexer missing `<param>` or `<value>` tags.
    *   **Location**: `src/DabblerPdf/Pdf/PdfArray.cs`
    *   **Fix**:
        ```csharp
        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <value>The element at the specified index.</value>
        public PdfObject this[int index]
        // ...

        /// <summary>Appends an element to the end of the array.</summary>
        /// <param name="obj">The <see cref="PdfObject"/> to add.</param>
        public void Add(PdfObject obj)
        // ...

        /// <summary>Inserts an element at the specified index.</summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="obj">The <see cref="PdfObject"/> to insert.</param>
        public void Insert(int index, PdfObject obj)
        // ...

        /// <summary>Removes the element at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index) => _elements.RemoveAt(index);

        /// <summary>Returns <c>true</c> if the array contains the given object.</summary>
        /// <param name="obj">The object to locate in the array.</param>
        /// <returns><c>true</c> if the object is found; otherwise, <c>false</c>.</returns>
        public bool Contains(PdfObject obj) => _elements.Contains(obj);
        ```

7.  **Issue**: Constructor and method missing `<param>` and `<returns>` tags.
    *   **Location**: `src/DabblerPdf/Pdf/PdfIndirectObject.cs`
    *   **Fix**:
        ```csharp
        /// <summary>Creates an indirect object wrapper.</summary>
        /// <param name="number">The object number.</param>
        /// <param name="obj">The PDF object to wrap.</param>
        /// <param name="generation">The generation number (usually 0).</param>
        public PdfIndirectObject(int number, PdfObject obj, int generation = 0)
        // ...

        /// <summary>
        /// Writes the indirect object to the stream and returns the byte offset
        /// at which the object started (for the xref table).
        /// </summary>
        /// <param name="outputStream">The stream to write to.</param>
        /// <returns>The byte offset of the start of the object.</returns>
        public long WriteTo(Stream outputStream)
        ```

8.  **Issue**: Record and methods missing documentation.
    *   **Location**: `src/DabblerPdf/Pdf/PdfBody.cs`
    *   **Fix**:
        ```csharp
        /// <summary>
        /// Reserves the next object number and returns an indirect reference to it.
        /// The object has not been written yet.
        /// </summary>
        /// <returns>A new <see cref="PdfIndirectReference"/>.</returns>
        public PdfIndirectReference ReserveReference()
        // ...

        /// <summary>
        /// Writes a PDF object to the output stream as an indirect object.
        /// Returns the indirect reference for the newly written object.
        /// </summary>
        /// <param name="obj">The <see cref="PdfObject"/> to add.</param>
        /// <param name="outputStream">The stream to write to.</param>
        /// <returns>An indirect reference to the newly written object.</returns>
        public PdfIndirectReference Add(PdfObject obj, Stream outputStream)
        // ...

        /// <summary>
        /// Writes a PDF object with a pre-reserved object number.
        /// </summary>
        /// <param name="obj">The <see cref="PdfObject"/> to write.</param>
        /// <param name="reference">The pre-reserved reference containing the object number.</param>
        /// <param name="outputStream">The stream to write to.</param>
        public void Add(PdfObject obj, PdfIndirectReference reference, Stream outputStream)
        // ...

        /// <summary>
        /// An entry in the cross-reference table: object number and byte offset.
        /// </summary>
        /// <param name="ObjectNumber">The object number in the PDF file.</param>
        /// <param name="Offset">The byte offset of the object from the start of the file.</param>
        public readonly record struct XrefEntry(int ObjectNumber, long Offset);
        ```

9.  **Issue**: Method missing `<param>` tag.
    *   **Location**: `src/DabblerPdf/Pdf/PdfPage.cs`
    *   **Fix**:
        ```csharp
        /// <summary>Sets the content stream reference for this page.</summary>
        /// <param name="contentsRef">An indirect reference to the content stream.</param>
        public void SetContents(PdfIndirectReference contentsRef)
        ```

10. **Issue**: Method missing `<param>` tag.
    *   **Location**: `src/DabblerPdf/Pdf/PdfPages.cs`
    *   **Fix**:
        ```csharp
        /// <summary>Adds a page reference to the /Kids array.</summary>
        /// <param name="pageRef">An indirect reference to the page or pages node to add.</param>
        public void AddPage(PdfIndirectReference pageRef)
        ```

11. **Issue**: Method missing `<param>` and `<returns>` tags.
    *   **Location**: `src/DabblerPdf/Image/PdfImage.cs`
    *   **Fix**:
        ```csharp
        /// <summary>
        /// Creates a PdfImage with soft mask handling. If the image has alpha,
        /// the soft mask is written as a separate indirect object first.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="writer">The writer to use for adding indirect objects.</param>
        /// <returns>A new <see cref="PdfImage"/> instance.</returns>
        public static PdfImage Create(Image image, Pdf.PdfWriter writer)
        ```

12. **Issue**: Multiple methods missing documentation tags.
    *   **Location**: `src/DabblerPdf/Text/BaseFont.cs`
    *   **Fix**: Add appropriate `<param>` and `<returns>` tags to `CreateFont`, `GetWidthPoint`, `GetCharWidth`, `GetFontDescriptor`, and `CanEncode`. Example:
        ```csharp
        /// <summary>
        /// Measures the width of a string at the given font size, in points.
        /// This is the key method for the reporting engine's text measurement.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="fontSize">The font size in points.</param>
        /// <returns>The width of the text in points.</returns>
        public double GetWidthPoint(string text, double fontSize)
        ```

13. **Issue**: Multiple methods missing documentation tags.
    *   **Location**: `src/DabblerPdf/Text/TextLayout.cs`
    *   **Fix**: Add appropriate `<param>` and `<returns>` tags to `WrapText`, `MeasureHeight`, `CountLines`, and `DrawText`. Example:
        ```csharp
        /// <summary>
        /// Breaks text into lines that fit within the given width.
        /// Handles explicit line breaks (\n) and word-wrapping at word boundaries.
        /// </summary>
        /// <param name="text">The text to wrap.</param>
        /// <param name="font">The font to use for measurement.</param>
        /// <param name="fontSize">The font size in points.</param>
        /// <param name="maxWidth">The maximum width for each line in points.</param>
        /// <returns>A list of lines, each fitting within <paramref name="maxWidth"/> points.</returns>
        public static List<string> WrapText(string text, BaseFont font, double fontSize, double maxWidth)
        ```

---

# Verification Round 2

**ISSUES FOUND**

While the build now passes without CS1591 warnings (which only check for missing `<summary>` tags), many new public methods and properties are still missing the required `<param>` and `<returns>` tags as specified in the session goal. This is a regression from the standards applied in Round 1.

### High-Severity: Widespread Missing `<param>` Tags

Issue â†’ Location â†’ Fix
---
1.  **Issue:** Numerous methods in `PdfContentByte` are missing `<param>` tags for their arguments, making the API difficult to use without inspecting the source.
    **Location:** `src/DabblerPdf/Pdf/PdfContentByte.cs` (affecting 23 public methods)
    **Fix:** Add `<param>` tags to the XML doc comments for the following methods: `SetLineWidth`, `SetLineCap`, `SetLineJoin`, `SetLineDash`, `SetMiterLimit`, `SetColorStroke`, `SetColorFill`, `MoveTo`, `LineTo`, `CurveTo`, `Rectangle`, `ConcatCtm`, `SetTextMatrix` (both overloads), `SetFontAndSize` (both overloads), `ShowText`, `SetCharacterSpacing`, `SetWordSpacing`, `SetLeading`, `AddImage` (both overloads), `AddTemplate`.

    *Example for `SetLineWidth`*:
    ```csharp
    /// <summary>Sets the line width (PDF <c>w</c> operator).</summary>
    /// <param name="width">The new line width in points.</param>
    public void SetLineWidth(double width)
    ```
    *Example for `AddImage`*:
    ```csharp
    /// <summary>
    /// Adds an image with an explicit transformation matrix.
    /// The matrix maps from image space (1x1 unit square) to page coordinates.
    /// Parameters a-f form the CTM: [a b c d e f].
    /// Typical usage: a = width, d = height, e = x, f = y (others 0).
    /// </summary>
    /// <param name="image">The image to add.</param>
    /// <param name="a">The 'a' component of the transformation matrix (x scale).</param>
    /// <param name="b">The 'b' component of the transformation matrix (x skew).</param>
    /// <param name="c">The 'c' component of the transformation matrix (y skew).</param>
    /// <param name="d">The 'd' component of the transformation matrix (y scale).</param>
    /// <param name="e">The 'e' component of the transformation matrix (x translation).</param>
    /// <param name="f">The 'f' component of the transformation matrix (y translation).</param>
    public void AddImage(Image.Image image, double a, double b, double c, double d, double e, double f)
    ```

### Medium-Severity: Missing `<returns>` and `<param>` Tags

Issue â†’ Location â†’ Fix
---
2.  **Issue:** Methods with return values in `PdfObject` are missing `<returns>` tags.
    **Location:** `src/DabblerPdf/Pdf/PdfObject.cs`
    **Fix:** Add `<returns>` tags to `ToBytes` and all `Is...` methods (`IsBoolean`, `IsNumber`, etc.).

    *Example for `ToBytes`*:
    ```csharp
    /// <summary>Returns the PDF representation as a byte array.</summary>
    /// <returns>A new byte array containing the object's PDF representation.</returns>
    public virtual byte[] ToBytes()
    ```
    *Example for `IsBoolean`*:
    ```csharp
    /// <summary>Returns <c>true</c> if this object is a PDF boolean.</summary>
    /// <returns><c>true</c> if this object is a <see cref="PdfBoolean"/>, otherwise <c>false</c>.</returns>
    public bool IsBoolean() => Type == BOOLEAN;
    ```
3.  **Issue:** The `GetAs...` helper methods in `PdfDictionary` are missing `<param>` and `<returns>` tags.
    **Location:** `src/DabblerPdf/Pdf/PdfDictionary.cs`, lines 80-92
    **Fix:** Add `<param>` and `<returns>` tags to all six `GetAs...` methods.

    *Example for `GetAsDictionary`*:
    ```csharp
    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfDictionary"/>, or <c>null</c>.</summary>
    /// <param name="key">The key of the entry to retrieve.</param>
    /// <returns>The value as a <see cref="PdfDictionary"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfDictionary? GetAsDictionary(PdfName key) => Get(key) as PdfDictionary;
    ```
4.  **Issue:** Methods in the `Image` base class are missing `<param>` tags, and factory methods also lack `<returns>` tags.
    **Location:** `src/DabblerPdf/Image/Image.cs`
    **Fix:** Add appropriate tags to `SetAbsolutePosition`, `ScaleToFit`, `ScaleToFill`, `ScaleAbsolute`, `ScalePercent`, and both `GetInstance` overloads.

    *Example for `ScaleToFit`*:
    ```csharp
    /// <summary>
    /// Scales the image to fit within the given dimensions while preserving
    /// the aspect ratio. The image will be scaled down (never up) to fit.
    /// </summary>
    /// <param name="maxWidth">The maximum width to fit into, in points.</param>
    /// <param name="maxHeight">The maximum height to fit into, in points.</param>
    public void ScaleToFit(double maxWidth, double maxHeight)
    ```
    *Example for `GetInstance`*:
    ```csharp
    /// <summary>
    /// Loads an image from a file path. Detects the format from the file extension.
    /// </summary>
    /// <param name="path">The full path to the image file.</param>
    /// <returns>A new <see cref="Image"/> instance (e.g., <see cref="Jpeg"/> or <see cref="PngImage"/>).</returns>
    public static Image GetInstance(string path)
    ```
5.  **Issue:** Key factory methods in `PdfWriter`, `PngImage`, and `BaseFont` are missing `<param>` and `<returns>` tags.
    **Location:** `src/DabblerPdf/Pdf/PdfWriter.cs`, `src/DabblerPdf/Image/PngImage.cs`, `src/DabblerPdf/Text/BaseFont.cs`
    **Fix:** Add the missing tags to `PdfWriter.GetInstance`, `PdfWriter.CreateTemplate`, `PngImage.GetImage`, and `BaseFont.CreateFont`.

    *Example for `PdfWriter.CreateTemplate`*:
    ```csharp
    /// <summary>
    /// Creates a new PdfTemplate (Form XObject) that can be placed on pages
    /// and filled in later.
    /// </summary>
    /// <param name="width">The width of the template's bounding box in points.</param>
    /// <param name="height">The height of the template's bounding box in points.</param>
    /// <returns>A new <see cref="PdfTemplate"/> instance.</returns>
    public PdfTemplate CreateTemplate(double width, double height)
    ```

### Low-Severity: Minor Omissions

Issue â†’ Location â†’ Fix
---
6.  **Issue:** Public property `Document` in `PdfWriter` is undocumented.
    **Location:** `src/DabblerPdf/Pdf/PdfWriter.cs`, line 42
    **Fix:**
    ```csharp
    /// <summary>The document this writer is associated with.</summary>
    public Document.Document? Document { get; internal set; }
    ```
7.  **Issue:** Missing documentation on several other public methods and properties across the project.
    **Location:** Multiple files.
    **Fix:** Add missing documentation for:
      - `PdfName(string name)` constructor (`<param>`)
      - `PdfArray` indexer (`<param>`)
      - `PdfCrossReferenceTable.WriteTo` (`<param>`s, `<returns>`)
      - `CoordinateTranslator.InchesToPoints` (`<param>`, `<returns>`)
      - `CoordinateTranslator.PointsToInches` (`<param>`, `<returns>`)

    *Example for `PdfCrossReferenceTable.WriteTo`*:
    ```csharp
    /// <summary>
    /// Writes the xref table to the output stream and returns the byte offset
    /// where the xref section started (for the <c>startxref</c> pointer).
    /// </summary>
    /// <param name="outputStream">The PDF output stream.</param>
    /// <param name="entries">The list of object numbers and their byte offsets.</param>
    /// <param name="size">The total number of objects in the document (for the /Size entry).</param>
    /// <returns>The byte offset of the start of the 'xref' keyword.</returns>
    public static long WriteTo(Stream outputStream, IReadOnlyList<XrefEntry> entries, int size)
    ```
