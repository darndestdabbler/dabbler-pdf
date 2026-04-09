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

    public override void WriteTo(Stream outputStream)
    {
        outputStream.Write(Bytes!);
    }

    private static byte[] EncodeName(string name)
    {
        var sb = new StringBuilder("/");
        foreach (char c in name)
        {
            if (c < 33 || c > 126 || c == '#' || c == '/' || c == '%'
                || c == '(' || c == ')' || c == '<' || c == '>'
                || c == '[' || c == ']' || c == '{' || c == '}')
            {
                sb.Append('#');
                sb.Append(((int)c).ToString("X2"));
            }
            else
            {
                sb.Append(c);
            }
        }
        return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
    }

    public bool Equals(PdfName? other) => other is not null && _name == other._name;
    public override bool Equals(object? obj) => obj is PdfName other && Equals(other);
    public override int GetHashCode() => _name.GetHashCode();
    public int CompareTo(PdfName? other) => string.Compare(_name, other?._name, StringComparison.Ordinal);

    public static bool operator ==(PdfName? left, PdfName? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(PdfName? left, PdfName? right) => !(left == right);

    public override string ToString() => $"/{_name}";

    // --- Common PDF name constants ---

    // Document structure
    public static new readonly PdfName Type = new("Type");
    public static readonly PdfName Subtype = new("Subtype");
    public static readonly PdfName Catalog = new("Catalog");
    public static readonly PdfName Pages = new("Pages");
    public static readonly PdfName Page = new("Page");
    public static readonly PdfName Parent = new("Parent");
    public static readonly PdfName Kids = new("Kids");
    public static readonly PdfName Count = new("Count");
    public static readonly PdfName Contents = new("Contents");
    public static readonly PdfName Resources = new("Resources");
    public static readonly PdfName MediaBox = new("MediaBox");

    // Fonts
    public static readonly PdfName Font = new("Font");
    public static readonly PdfName BaseFont = new("BaseFont");
    public static readonly PdfName Encoding = new("Encoding");
    public static readonly PdfName WinAnsiEncoding = new("WinAnsiEncoding");

    // Streams and filters
    public static readonly PdfName Length = new("Length");
    public static readonly PdfName Filter = new("Filter");
    public static readonly PdfName FlateDecode = new("FlateDecode");
    public static readonly PdfName DctDecode = new("DCTDecode");

    // Graphics state
    public static readonly PdfName ExtGState = new("ExtGState");

    // Images
    public static readonly PdfName XObject = new("XObject");
    public static readonly PdfName Image = new("Image");
    public static readonly PdfName Width = new("Width");
    public static readonly PdfName Height = new("Height");
    public static readonly PdfName BitsPerComponent = new("BitsPerComponent");
    public static readonly PdfName ColorSpace = new("ColorSpace");
    public static readonly PdfName DeviceRgb = new("DeviceRGB");
    public static readonly PdfName DeviceGray = new("DeviceGray");
    public static readonly PdfName DeviceCmyk = new("DeviceCMYK");

    // Cross-reference and trailer
    public static readonly PdfName Size = new("Size");
    public static readonly PdfName Root = new("Root");
    public static readonly PdfName Info = new("Info");

    // Metadata
    public static readonly PdfName Producer = new("Producer");
    public static readonly PdfName CreationDate = new("CreationDate");

    // Procedure sets
    public static readonly PdfName ProcSet = new("ProcSet");
    public static readonly PdfName Pdf = new("PDF");
    public static readonly PdfName Text = new("Text");
    public static readonly PdfName ImageB = new("ImageB");
    public static readonly PdfName ImageC = new("ImageC");

    // Font descriptors
    public static readonly PdfName FontDescriptor = new("FontDescriptor");
    public static readonly PdfName FontName = new("FontName");
    public static readonly PdfName FirstChar = new("FirstChar");
    public static readonly PdfName LastChar = new("LastChar");
    public static readonly PdfName Widths = new("Widths");
    public static readonly PdfName Type1 = new("Type1");
    public static readonly PdfName TrueType = new("TrueType");
    public static readonly PdfName FontFile2 = new("FontFile2");
    public static readonly PdfName Length1 = new("Length1");

    // Image soft mask / transparency
    public static readonly PdfName SMask = new("SMask");
}
