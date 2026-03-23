// OpenPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using OpenPdf.Pdf;

namespace OpenPdf.Image;

/// <summary>
/// Creates a PDF image XObject stream from an <see cref="Image"/> instance.
/// Handles JPEG (DCTDecode pass-through) and PNG (FlateDecode with raw pixels).
/// </summary>
public sealed class PdfImage : PdfStream
{
    /// <summary>
    /// If this image has an alpha channel, this holds the soft mask stream
    /// that must be written as a separate indirect object.
    /// </summary>
    public PdfStream? SoftMask { get; }

    public PdfImage(Image image) : this(image, null)
    {
    }

    internal PdfImage(Image image, PdfIndirectReference? softMaskRef) : base()
    {
        Put(PdfName.Type, PdfName.XObject);
        Put(PdfName.Subtype, PdfName.Image);
        Put(PdfName.Width, new PdfNumber(image.PixelWidth));
        Put(PdfName.Height, new PdfNumber(image.PixelHeight));
        Put(PdfName.BitsPerComponent, new PdfNumber(image.BitsPerComponent));

        // Color space
        PdfName colorSpace = image.ColorComponents switch
        {
            1 => PdfName.DeviceGray,
            3 => PdfName.DeviceRgb,
            4 => PdfName.DeviceCmyk,
            _ => PdfName.DeviceRgb
        };
        Put(PdfName.ColorSpace, colorSpace);

        if (image is Jpeg)
        {
            // JPEG: pass raw bytes directly with DCTDecode filter
            SetRawBytes(image.RawData);
            Put(PdfName.Filter, PdfName.DctDecode);
        }
        else if (image is PngImage png)
        {
            // PNG: use decompressed pixel data, Flate-compressed for PDF
            SetRawBytes(png.DecompressedPixels);
            FlateCompress();

            // If the PNG has an alpha channel, create a soft mask
            if (png.AlphaChannel != null)
            {
                SoftMask = CreateSoftMask(png);
            }
        }

        if (softMaskRef != null)
        {
            Put(PdfName.SMask, softMaskRef);
        }
    }

    private static PdfStream CreateSoftMask(PngImage png)
    {
        var mask = new PdfStream(png.AlphaChannel!);
        mask.Put(PdfName.Type, PdfName.XObject);
        mask.Put(PdfName.Subtype, PdfName.Image);
        mask.Put(PdfName.Width, new PdfNumber(png.PixelWidth));
        mask.Put(PdfName.Height, new PdfNumber(png.PixelHeight));
        mask.Put(PdfName.BitsPerComponent, new PdfNumber(8));
        mask.Put(PdfName.ColorSpace, PdfName.DeviceGray);
        mask.FlateCompress();
        return mask;
    }

    /// <summary>
    /// Creates a PdfImage with soft mask handling. If the image has alpha,
    /// the soft mask is written as a separate indirect object first.
    /// </summary>
    public static PdfImage Create(Image image, Pdf.PdfWriter writer)
    {
        if (image is PngImage png && png.AlphaChannel != null)
        {
            // Write the soft mask as an indirect object first
            var softMask = CreateSoftMask(png);
            var softMaskRef = writer.AddIndirectObject(softMask);
            return new PdfImage(image, softMaskRef);
        }

        return new PdfImage(image);
    }
}
