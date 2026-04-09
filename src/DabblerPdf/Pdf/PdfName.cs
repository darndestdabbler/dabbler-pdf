// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Text;

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents a PDF name object (e.g., <c>/Type</c>, <c>/Pages</c>).
/// Common names are defined as static readonly constants.
/// </summary>
public sealed class PdfName : PdfObject, IEquatable<PdfName>, IComparable<PdfName>
{
    private readonly string _name;

    /// <summary>
    /// Creates a PdfName from a string. The leading slash is added automatically
    /// if not already present.
    /// </summary>
    public PdfName(string name) : base(NAME)
    {
        _name = name.StartsWith('/') ? name[1..] : name;
        Bytes = EncodeName(_name);
    }

    /// <summary>The name without the leading slash.</summary>
    public string Value => _name;

    /// <inheritdoc/>
    public override void WriteTo(Stream outputStream)
    {
        outputStream.Write(Bytes!);
    }

    private static byte[] EncodeName(string name)
    {
        var sb = new StringBuilder("/");
        // Encode each character to its UTF-8 bytes, then hex-escape as needed per PDF spec
        foreach (char c in name)
        {
            if (c >= 33 && c <= 126 && c != '#' && c != '/' && c != '%'
                && c != '(' && c != ')' && c != '<' && c != '>'
                && c != '[' && c != ']' && c != '{' && c != '}')
            {
                sb.Append(c);
            }
            else
            {
                // Encode the character as UTF-8 bytes, then hex-escape each byte
                byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(new[] { c });
                foreach (byte b in utf8Bytes)
                {
                    sb.Append('#');
                    sb.Append(b.ToString("X2"));
                }
            }
        }
        return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
    }

    /// <inheritdoc/>
    public bool Equals(PdfName? other) => other is not null && _name == other._name;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PdfName other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _name.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(PdfName? other) => string.Compare(_name, other?._name, StringComparison.Ordinal);

    /// <summary>Determines whether two names are equal.</summary>
    /// <param name="left">The first name to compare.</param>
    /// <param name="right">The second name to compare.</param>
    public static bool operator ==(PdfName? left, PdfName? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two names are not equal.</summary>
    /// <param name="left">The first name to compare.</param>
    /// <param name="right">The second name to compare.</param>
    public static bool operator !=(PdfName? left, PdfName? right) => !(left == right);

    /// <inheritdoc/>
    public override string ToString() => $"/{_name}";

    // --- Common PDF name constants ---
    // These correspond to standard keys and values defined in the PDF specification.

    #region Document structure

    /// <summary>The <c>/Type</c> name.</summary>
    public static new readonly PdfName Type = new("Type");

    /// <summary>The <c>/Subtype</c> name.</summary>
    public static readonly PdfName Subtype = new("Subtype");

    /// <summary>The <c>/Catalog</c> name (document root type).</summary>
    public static readonly PdfName Catalog = new("Catalog");

    /// <summary>The <c>/Pages</c> name (page tree node type).</summary>
    public static readonly PdfName Pages = new("Pages");

    /// <summary>The <c>/Page</c> name (leaf page type).</summary>
    public static readonly PdfName Page = new("Page");

    /// <summary>The <c>/Parent</c> name.</summary>
    public static readonly PdfName Parent = new("Parent");

    /// <summary>The <c>/Kids</c> name (children of a page tree node).</summary>
    public static readonly PdfName Kids = new("Kids");

    /// <summary>The <c>/Count</c> name (total page count).</summary>
    public static readonly PdfName Count = new("Count");

    /// <summary>The <c>/Contents</c> name (page content stream reference).</summary>
    public static readonly PdfName Contents = new("Contents");

    /// <summary>The <c>/Resources</c> name.</summary>
    public static readonly PdfName Resources = new("Resources");

    /// <summary>The <c>/MediaBox</c> name (page boundary).</summary>
    public static readonly PdfName MediaBox = new("MediaBox");

    #endregion

    #region Fonts

    /// <summary>The <c>/Font</c> name.</summary>
    public static readonly PdfName Font = new("Font");

    /// <summary>The <c>/BaseFont</c> name.</summary>
    public static readonly PdfName BaseFont = new("BaseFont");

    /// <summary>The <c>/Encoding</c> name.</summary>
    public static readonly PdfName Encoding = new("Encoding");

    /// <summary>The <c>/WinAnsiEncoding</c> name.</summary>
    public static readonly PdfName WinAnsiEncoding = new("WinAnsiEncoding");

    #endregion

    #region Streams and filters

    /// <summary>The <c>/Length</c> name (stream byte length).</summary>
    public static readonly PdfName Length = new("Length");

    /// <summary>The <c>/Filter</c> name.</summary>
    public static readonly PdfName Filter = new("Filter");

    /// <summary>The <c>/FlateDecode</c> filter name (zlib/deflate compression).</summary>
    public static readonly PdfName FlateDecode = new("FlateDecode");

    /// <summary>The <c>/DCTDecode</c> filter name (JPEG compression).</summary>
    public static readonly PdfName DctDecode = new("DCTDecode");

    #endregion

    #region Graphics state

    /// <summary>The <c>/ExtGState</c> name (extended graphics state).</summary>
    public static readonly PdfName ExtGState = new("ExtGState");

    #endregion

    #region Images

    /// <summary>The <c>/XObject</c> name.</summary>
    public static readonly PdfName XObject = new("XObject");

    /// <summary>The <c>/Image</c> name (XObject subtype for images).</summary>
    public static readonly PdfName Image = new("Image");

    /// <summary>The <c>/Width</c> name.</summary>
    public static readonly PdfName Width = new("Width");

    /// <summary>The <c>/Height</c> name.</summary>
    public static readonly PdfName Height = new("Height");

    /// <summary>The <c>/BitsPerComponent</c> name.</summary>
    public static readonly PdfName BitsPerComponent = new("BitsPerComponent");

    /// <summary>The <c>/ColorSpace</c> name.</summary>
    public static readonly PdfName ColorSpace = new("ColorSpace");

    /// <summary>The <c>/DeviceRGB</c> color space name.</summary>
    public static readonly PdfName DeviceRgb = new("DeviceRGB");

    /// <summary>The <c>/DeviceGray</c> color space name.</summary>
    public static readonly PdfName DeviceGray = new("DeviceGray");

    /// <summary>The <c>/DeviceCMYK</c> color space name.</summary>
    public static readonly PdfName DeviceCmyk = new("DeviceCMYK");

    #endregion

    #region Cross-reference and trailer

    /// <summary>The <c>/Size</c> name (cross-reference table size).</summary>
    public static readonly PdfName Size = new("Size");

    /// <summary>The <c>/Root</c> name (document catalog reference).</summary>
    public static readonly PdfName Root = new("Root");

    /// <summary>The <c>/Info</c> name (document information dictionary).</summary>
    public static readonly PdfName Info = new("Info");

    #endregion

    #region Metadata

    /// <summary>The <c>/Producer</c> name.</summary>
    public static readonly PdfName Producer = new("Producer");

    /// <summary>The <c>/CreationDate</c> name.</summary>
    public static readonly PdfName CreationDate = new("CreationDate");

    #endregion

    #region Procedure sets

    /// <summary>The <c>/ProcSet</c> name.</summary>
    public static readonly PdfName ProcSet = new("ProcSet");

    /// <summary>The <c>/PDF</c> procedure set name.</summary>
    public static readonly PdfName Pdf = new("PDF");

    /// <summary>The <c>/Text</c> procedure set name.</summary>
    public static readonly PdfName Text = new("Text");

    /// <summary>The <c>/ImageB</c> procedure set name (grayscale images).</summary>
    public static readonly PdfName ImageB = new("ImageB");

    /// <summary>The <c>/ImageC</c> procedure set name (color images).</summary>
    public static readonly PdfName ImageC = new("ImageC");

    #endregion

    #region Font descriptors

    /// <summary>The <c>/FontDescriptor</c> name.</summary>
    public static readonly PdfName FontDescriptor = new("FontDescriptor");

    /// <summary>The <c>/FontName</c> name.</summary>
    public static readonly PdfName FontName = new("FontName");

    /// <summary>The <c>/FirstChar</c> name.</summary>
    public static readonly PdfName FirstChar = new("FirstChar");

    /// <summary>The <c>/LastChar</c> name.</summary>
    public static readonly PdfName LastChar = new("LastChar");

    /// <summary>The <c>/Widths</c> name (character width array).</summary>
    public static readonly PdfName Widths = new("Widths");

    /// <summary>The <c>/Type1</c> font subtype name.</summary>
    public static readonly PdfName Type1 = new("Type1");

    /// <summary>The <c>/TrueType</c> font subtype name.</summary>
    public static readonly PdfName TrueType = new("TrueType");

    /// <summary>The <c>/FontFile2</c> name (embedded TrueType font data).</summary>
    public static readonly PdfName FontFile2 = new("FontFile2");

    /// <summary>The <c>/Length1</c> name (uncompressed font file length).</summary>
    public static readonly PdfName Length1 = new("Length1");

    #endregion

    #region Image transparency

    /// <summary>The <c>/SMask</c> name (soft mask / alpha channel).</summary>
    public static readonly PdfName SMask = new("SMask");

    #endregion
}
