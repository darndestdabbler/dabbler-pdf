// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Document;

/// <summary>
/// Methods that stage values for the PDF document information dictionary
/// (<c>/Info</c>). Values are buffered on the <see cref="Document"/> and
/// written by <see cref="DabblerPdf.Pdf.PdfWriter"/> when the document is
/// closed. The shape mirrors OpenPDF's <c>addTitle</c>, <c>addAuthor</c>,
/// etc.
/// </summary>
public partial class Document
{
    private string? _title;
    private string? _author;
    private string? _subject;
    private string? _keywords;
    private string? _creator;
    private DateTime? _creationDate;
    private bool _creationDateSuppressed;

    /// <summary>
    /// Stages a value for the document's <c>/Title</c> entry. May be called
    /// before <see cref="Close"/>; the last call wins.
    /// </summary>
    /// <param name="title">The title to stage. Empty or whitespace-only
    /// values are ignored (the previously staged value, if any, is
    /// preserved).</param>
    /// <returns><c>true</c> if the value was staged; <c>false</c> if the
    /// input was empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public bool AddTitle(string title) => StageString(title, v => _title = v);

    /// <summary>
    /// Stages a value for the document's <c>/Author</c> entry. May be called
    /// before <see cref="Close"/>; the last call wins.
    /// </summary>
    /// <param name="author">The author to stage. Empty or whitespace-only
    /// values are ignored.</param>
    /// <returns><c>true</c> if the value was staged; <c>false</c> if the
    /// input was empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="author"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public bool AddAuthor(string author) => StageString(author, v => _author = v);

    /// <summary>
    /// Stages a value for the document's <c>/Subject</c> entry. May be called
    /// before <see cref="Close"/>; the last call wins.
    /// </summary>
    /// <param name="subject">The subject to stage. Empty or whitespace-only
    /// values are ignored.</param>
    /// <returns><c>true</c> if the value was staged; <c>false</c> if the
    /// input was empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subject"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public bool AddSubject(string subject) => StageString(subject, v => _subject = v);

    /// <summary>
    /// Stages a value for the document's <c>/Keywords</c> entry. May be called
    /// before <see cref="Close"/>; the last call wins.
    /// </summary>
    /// <param name="keywords">The keywords to stage. Empty or whitespace-only
    /// values are ignored.</param>
    /// <returns><c>true</c> if the value was staged; <c>false</c> if the
    /// input was empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keywords"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public bool AddKeywords(string keywords) => StageString(keywords, v => _keywords = v);

    /// <summary>
    /// Stages a value for the document's <c>/Creator</c> entry. May be called
    /// before <see cref="Close"/>; the last call wins.
    /// </summary>
    /// <param name="creator">The creator to stage. Empty or whitespace-only
    /// values are ignored.</param>
    /// <returns><c>true</c> if the value was staged; <c>false</c> if the
    /// input was empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="creator"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public bool AddCreator(string creator) => StageString(creator, v => _creator = v);

    /// <summary>
    /// Stages <see cref="DateTime.UtcNow"/> as the document's <c>/CreationDate</c>
    /// at the time of the call. Clears any prior <see cref="SuppressCreationDate"/>.
    /// </summary>
    /// <returns><c>true</c>.</returns>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    /// <remarks>
    /// If neither <c>AddCreationDate</c> nor <see cref="SuppressCreationDate"/>
    /// is called, <see cref="Close"/> auto-stages <see cref="DateTime.UtcNow"/>
    /// captured at close time (a sub-second difference from calling this
    /// method explicitly).
    /// </remarks>
    public bool AddCreationDate()
    {
        EnsureNotClosed();
        _creationDate = DateTime.UtcNow;
        _creationDateSuppressed = false;
        return true;
    }

    /// <summary>
    /// Stages an explicit <c>/CreationDate</c>. Clears any prior
    /// <see cref="SuppressCreationDate"/>.
    /// </summary>
    /// <param name="creationDate">The creation date to stage. The
    /// <see cref="DateTime.Kind"/> determines the formatted offset:
    /// <see cref="DateTimeKind.Utc"/> uses <c>Z</c>;
    /// <see cref="DateTimeKind.Local"/> and
    /// <see cref="DateTimeKind.Unspecified"/> use the local UTC offset.</param>
    /// <returns><c>true</c>.</returns>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public bool AddCreationDate(DateTime creationDate)
    {
        EnsureNotClosed();
        _creationDate = creationDate;
        _creationDateSuppressed = false;
        return true;
    }

    /// <summary>
    /// Opts out of writing <c>/CreationDate</c>. Restores the
    /// <c>DabblerPdf 1.0.0</c> output shape (only <c>/Producer</c> in the
    /// info dictionary). Cleared by any subsequent <see cref="AddCreationDate()"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The document has been closed.</exception>
    public void SuppressCreationDate()
    {
        EnsureNotClosed();
        _creationDate = null;
        _creationDateSuppressed = true;
    }

    /// <summary>
    /// Returns a snapshot of the staged metadata. Used by
    /// <see cref="DabblerPdf.Pdf.PdfWriter"/> at close time and by tests.
    /// </summary>
    internal DocumentMetadata GetMetadata() => new(
        _title,
        _author,
        _subject,
        _keywords,
        _creator,
        _creationDate,
        _creationDateSuppressed);

    private bool StageString(string value, Action<string> assign)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureNotClosed();
        if (string.IsNullOrWhiteSpace(value))
            return false;
        assign(value);
        return true;
    }
}
