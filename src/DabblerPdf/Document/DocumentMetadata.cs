// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Document;

/// <summary>
/// Internal carrier for the staged document-information-dictionary fields
/// that <see cref="Document"/> exposes via <c>AddTitle</c>, <c>AddAuthor</c>,
/// etc., and that <see cref="DabblerPdf.Pdf.PdfWriter"/> consumes when the
/// document is closed. A <c>null</c> string field means "do not write".
/// </summary>
internal sealed record DocumentMetadata(
    string? Title,
    string? Author,
    string? Subject,
    string? Keywords,
    string? Creator,
    DateTime? CreationDate,
    bool CreationDateSuppressed);
