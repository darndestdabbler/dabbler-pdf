// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// A reusable content block (PDF Form XObject) whose content can be
/// written after placement. Used for deferred content like "Page X of Y"
/// where the total page count isn't known until all pages are generated.
/// </summary>
public class PdfTemplate
{
    private static readonly PdfName FormName = new("Form");
    private static readonly PdfName BBoxName = new("BBox");

    private readonly Dictionary<PdfName, PdfIndirectReference> _fonts = new();

    /// <summary>Width of the template bounding box in points.</summary>
    public double Width { get; }

    /// <summary>Height of the template bounding box in points.</summary>
    public double Height { get; }

    /// <summary>
    /// Gets the content byte for drawing into this template.
    /// Content can be written at any time before the document is closed.
    /// </summary>
    public PdfContentByte Content { get; }

    /// <summary>The reserved indirect reference for this template's Form XObject.</summary>
    internal PdfIndirectReference Reference { get; }

    /// <summary>
    /// Creates a new template with the given dimensions.
    /// </summary>
    internal PdfTemplate(PdfWriter writer, double width, double height, PdfIndirectReference reference)
    {
        Width = width;
        Height = height;
        Reference = reference;
        Content = new PdfContentByte(writer, this);
    }

    /// <summary>
    /// Registers a font for use within this template's resources.
    /// Returns the resource name (e.g., /F1) used in the template's content stream.
    /// </summary>
    internal PdfName AddFont(PdfName baseFontName, PdfIndirectReference fontRef)
    {
        foreach (var (name, existingRef) in _fonts)
        {
            if (existingRef.Number == fontRef.Number)
                return name;
        }

        var resourceName = new PdfName($"F{_fonts.Count + 1}");
        _fonts[resourceName] = fontRef;
        return resourceName;
    }

    /// <summary>
    /// Serializes this template into a PDF Form XObject stream.
    /// Called by <see cref="PdfWriter.Close"/> after all content has been written.
    /// </summary>
    /// <param name="compress">The writer's <see cref="PdfWriter.CompressContentStreams"/>:
    /// when true, content over 128 bytes is Flate-compressed.</param>
    internal PdfStream ToFormXObject(bool compress)
    {
        var contentBytes = Content.ToByteArray();
        var stream = new PdfStream(contentBytes);
        stream.Put(PdfName.Type, PdfName.XObject);
        stream.Put(PdfName.Subtype, FormName);
        stream.Put(BBoxName, new PdfArray(0, 0, Width, Height));

        if (_fonts.Count > 0)
        {
            var resources = new PdfDictionary();
            var fontDict = new PdfDictionary();
            foreach (var (name, fontRef) in _fonts)
            {
                fontDict.Put(name, fontRef);
            }
            resources.Put(PdfName.Font, fontDict);
            stream.Put(PdfName.Resources, resources);
        }

        if (compress && contentBytes.Length > 128)
            stream.FlateCompress();

        return stream;
    }
}
