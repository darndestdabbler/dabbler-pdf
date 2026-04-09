// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

namespace DabblerPdf.Pdf;

/// <summary>
/// Represents a PDF dictionary: a set of key-value pairs where keys are <see cref="PdfName"/>s.
/// Serialized as <c>&lt;&lt; /Key1 value1 /Key2 value2 &gt;&gt;</c>.
/// </summary>
public class PdfDictionary : PdfObject
{
    private static readonly byte[] DictOpen = "<<"u8.ToArray();
    private static readonly byte[] DictClose = ">>"u8.ToArray();
    private static readonly byte[] Space = " "u8.ToArray();
    private static readonly byte[] Newline = "\n"u8.ToArray();

    private readonly Dictionary<PdfName, PdfObject> _entries = new();

    /// <summary>Creates an empty PDF dictionary.</summary>
    public PdfDictionary() : base(DICTIONARY) { }

    /// <summary>Creates a PDF dictionary with a <c>/Type</c> entry set to the given name.</summary>
    /// <param name="type">The value for the <c>/Type</c> entry.</param>
    public PdfDictionary(PdfName type) : base(DICTIONARY)
    {
        Put(PdfName.Type, type);
    }

    /// <summary>The number of key-value pairs in this dictionary.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Sets a key-value pair. If <paramref name="value"/> is <c>null</c> or
    /// <see cref="PdfNull"/>, the entry is removed (matching OpenPDF behavior).
    /// </summary>
    public void Put(PdfName key, PdfObject? value)
    {
        if (value == null || value is PdfNull)
        {
            _entries.Remove(key);
        }
        else
        {
            _entries[key] = value;
        }
    }

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

    /// <summary>The keys in this dictionary.</summary>
    public IReadOnlyCollection<PdfName> Keys => _entries.Keys;

    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfDictionary"/>, or <c>null</c>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value as a <see cref="PdfDictionary"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfDictionary? GetAsDictionary(PdfName key) => Get(key) as PdfDictionary;

    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfArray"/>, or <c>null</c>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value as a <see cref="PdfArray"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfArray? GetAsArray(PdfName key) => Get(key) as PdfArray;

    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfString"/>, or <c>null</c>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value as a <see cref="PdfString"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfString? GetAsString(PdfName key) => Get(key) as PdfString;

    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfNumber"/>, or <c>null</c>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value as a <see cref="PdfNumber"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfNumber? GetAsNumber(PdfName key) => Get(key) as PdfNumber;

    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfName"/>, or <c>null</c>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value as a <see cref="PdfName"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfName? GetAsName(PdfName key) => Get(key) as PdfName;

    /// <summary>Gets the value for <paramref name="key"/> as a <see cref="PdfBoolean"/>, or <c>null</c>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value as a <see cref="PdfBoolean"/>, or <c>null</c> if not found or wrong type.</returns>
    public PdfBoolean? GetAsBoolean(PdfName key) => Get(key) as PdfBoolean;

    /// <inheritdoc/>
    public override void WriteTo(Stream outputStream)
    {
        outputStream.Write(DictOpen);
        foreach (var (key, value) in _entries)
        {
            outputStream.Write(Space);
            key.WriteTo(outputStream);
            outputStream.Write(Space);
            value.WriteTo(outputStream);
        }
        outputStream.Write(DictClose);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"<< {string.Join(" ", _entries.Select(kv => $"{kv.Key} {kv.Value}"))} >>";
    }
}
