// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace DabblerPdf.Text;

/// <summary>
/// Builds a TrueType font program holding only the glyphs a document draws. Ported from
/// OpenPDF's <c>TrueTypeFontSubSet</c>.
/// </summary>
/// <remarks>
/// Glyph 0, every used glyph and, recursively, every component of a composite among them keep
/// their outlines; every other glyph's outline is empty, so glyph IDs keep their numbers and
/// <c>/CIDToGIDMap /Identity</c> and <c>/W</c> stand. Only the tables a PDF TrueType font
/// program needs are written, each with its checksum.
/// </remarks>
internal static class TrueTypeFontSubSet
{
    /// <summary>The tables written, in tag order.</summary>
    private static readonly string[] TableTags = ["cvt ", "fpgm", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "prep"];

    /// <summary>The tables written only when the font has them.</summary>
    private static readonly string[] OptionalTags = ["cvt ", "fpgm", "prep"];

    // Composite glyph component flags
    private const int Arg1And2AreWords = 0x0001;
    private const int WeHaveAScale = 0x0008;
    private const int MoreComponents = 0x0020;
    private const int WeHaveAnXAndYScale = 0x0040;
    private const int WeHaveATwoByTwo = 0x0080;

    /// <summary>The font program of <paramref name="fontData"/> holding only <paramref name="usedGlyphs"/>.</summary>
    internal static byte[] Create(byte[] fontData, IEnumerable<int> usedGlyphs)
    {
        var reader = new TrueTypeFont.TtfReader(fontData);
        var directory = reader.ReadTableDirectory();
        bool shortLoca = reader.ReadHead(Entry(directory, "head")).IndexToLocFormat == 0;

        var glyf = Table(fontData, directory, "glyf");
        int[] loca = ReadLoca(Table(fontData, directory, "loca"), shortLoca);
        byte[] newGlyf = SubsetGlyf(glyf, loca, GlyphClosure(glyf, loca, usedGlyphs), out int[] newLoca);

        var tables = new List<(string Tag, byte[] Data)>();
        foreach (string tag in TableTags)
        {
            if (OptionalTags.Contains(tag) && !directory.ContainsKey(tag))
                continue;
            byte[] data = tag switch
            {
                "glyf" => newGlyf,
                "loca" => WriteLoca(newLoca, shortLoca),
                _ => Table(fontData, directory, tag).ToArray(),
            };
            tables.Add((tag, data));
        }
        return Assemble(BinaryPrimitives.ReadUInt32BigEndian(fontData), tables);
    }

    /// <summary>
    /// The subset tag for a font holding <paramref name="usedGlyphs"/>: six uppercase letters and
    /// <c>+</c> (ISO 32000-1, 9.6.4), derived from the glyphs, so the same glyphs get the same tag.
    /// </summary>
    internal static string SubsetTag(IEnumerable<int> usedGlyphs)
    {
        byte[] hash = SHA256.HashData(usedGlyphs.SelectMany(BitConverter.GetBytes).ToArray());
        var tag = new StringBuilder(7);
        for (int i = 0; i < 6; i++)
            tag.Append((char)('A' + hash[i] % 26));
        return tag.Append('+').ToString();
    }

    private static TrueTypeFont.TableEntry Entry(Dictionary<string, TrueTypeFont.TableEntry> directory, string tag) =>
        directory.TryGetValue(tag, out var entry)
            ? entry
            : throw new InvalidOperationException($"The TrueType font has no '{tag}' table to subset.");

    private static ReadOnlySpan<byte> Table(byte[] fontData, Dictionary<string, TrueTypeFont.TableEntry> directory, string tag)
    {
        var entry = Entry(directory, tag);
        return fontData.AsSpan(entry.Offset, entry.Length);
    }

    /// <summary>Each glyph's offset into <c>glyf</c>, and one past the last glyph's end.</summary>
    private static int[] ReadLoca(ReadOnlySpan<byte> loca, bool shortLoca)
    {
        var offsets = new int[loca.Length / (shortLoca ? 2 : 4)];
        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = shortLoca
                ? BinaryPrimitives.ReadUInt16BigEndian(loca[(i * 2)..]) * 2
                : (int)BinaryPrimitives.ReadUInt32BigEndian(loca[(i * 4)..]);
        return offsets;
    }

    private static byte[] WriteLoca(int[] offsets, bool shortLoca)
    {
        var loca = new byte[offsets.Length * (shortLoca ? 2 : 4)];
        for (int i = 0; i < offsets.Length; i++)
        {
            if (shortLoca)
                BinaryPrimitives.WriteUInt16BigEndian(loca.AsSpan(i * 2), (ushort)(offsets[i] / 2));
            else
                BinaryPrimitives.WriteUInt32BigEndian(loca.AsSpan(i * 4), (uint)offsets[i]);
        }
        return loca;
    }

    /// <summary>Glyph 0, the used glyphs, and every component of a composite among them, recursively.</summary>
    private static SortedSet<int> GlyphClosure(ReadOnlySpan<byte> glyf, int[] loca, IEnumerable<int> usedGlyphs)
    {
        var glyphs = new SortedSet<int>();
        var pending = new Stack<int>(usedGlyphs.Prepend(0));
        while (pending.TryPop(out int glyph))
        {
            if (glyph < 0 || glyph >= loca.Length - 1 || !glyphs.Add(glyph))
                continue;
            int start = loca[glyph], end = loca[glyph + 1];
            // A simple glyph has numberOfContours >= 0; a composite's components follow its 10-byte header
            if (end - start < 10 || BinaryPrimitives.ReadInt16BigEndian(glyf[start..]) >= 0)
                continue;
            for (int p = start + 10; p + 4 <= end;)
            {
                int flags = BinaryPrimitives.ReadUInt16BigEndian(glyf[p..]);
                pending.Push(BinaryPrimitives.ReadUInt16BigEndian(glyf[(p + 2)..]));
                p += 4 + ((flags & Arg1And2AreWords) != 0 ? 4 : 2);
                if ((flags & WeHaveAScale) != 0)
                    p += 2;
                else if ((flags & WeHaveAnXAndYScale) != 0)
                    p += 4;
                else if ((flags & WeHaveATwoByTwo) != 0)
                    p += 8;
                if ((flags & MoreComponents) == 0)
                    break;
            }
        }
        return glyphs;
    }

    /// <summary>
    /// The <c>glyf</c> table with only <paramref name="glyphs"/>' outlines, and its <c>loca</c>
    /// offsets: every glyph keeps its ID, and one left out has an empty outline.
    /// </summary>
    private static byte[] SubsetGlyf(ReadOnlySpan<byte> glyf, int[] loca, SortedSet<int> glyphs, out int[] newLoca)
    {
        newLoca = new int[loca.Length];
        using var output = new MemoryStream();
        for (int glyph = 0; glyph < loca.Length - 1; glyph++)
        {
            newLoca[glyph] = (int)output.Length;
            if (!glyphs.Contains(glyph))
                continue;
            output.Write(glyf[loca[glyph]..loca[glyph + 1]]);
            // A short loca stores offsets halved, so each glyph starts at an even offset
            if (output.Length % 2 != 0)
                output.WriteByte(0);
        }
        newLoca[^1] = (int)output.Length;
        return output.ToArray();
    }

    /// <summary>
    /// The font file of <paramref name="tables"/>: the table directory, each table's checksum,
    /// each table padded to four bytes, and <c>head</c>'s <c>checkSumAdjustment</c> for the whole file.
    /// </summary>
    private static byte[] Assemble(uint sfntVersion, List<(string Tag, byte[] Data)> tables)
    {
        int count = tables.Count;
        int entrySelector = BitOperations.Log2((uint)count);
        int searchRange = 16 << entrySelector;
        int offset = 12 + count * 16;
        var font = new byte[offset + tables.Sum(t => Pad4(t.Data.Length))];
        var span = font.AsSpan();

        BinaryPrimitives.WriteUInt32BigEndian(span, sfntVersion);
        BinaryPrimitives.WriteUInt16BigEndian(span[4..], (ushort)count);
        BinaryPrimitives.WriteUInt16BigEndian(span[6..], (ushort)searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(span[8..], (ushort)entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(span[10..], (ushort)(count * 16 - searchRange));

        int headOffset = -1;
        for (int i = 0; i < count; i++)
        {
            var (tag, data) = tables[i];
            var table = span.Slice(offset, data.Length);
            data.CopyTo(table);
            if (tag == "head")
            {
                // head's checksum is taken with checkSumAdjustment zero
                BinaryPrimitives.WriteUInt32BigEndian(table[8..], 0);
                headOffset = offset;
            }

            int record = 12 + i * 16;
            Encoding.ASCII.GetBytes(tag, span[record..]);
            BinaryPrimitives.WriteUInt32BigEndian(span[(record + 4)..], Checksum(table));
            BinaryPrimitives.WriteUInt32BigEndian(span[(record + 8)..], (uint)offset);
            BinaryPrimitives.WriteUInt32BigEndian(span[(record + 12)..], (uint)data.Length);
            offset += Pad4(data.Length);
        }

        BinaryPrimitives.WriteUInt32BigEndian(span[(headOffset + 8)..], 0xB1B0AFBA - Checksum(span));
        return font;
    }

    private static int Pad4(int length) => (length + 3) & ~3;

    /// <summary>The sum of <paramref name="data"/> as big-endian 32-bit words, the last padded with zeros.</summary>
    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 4)
        {
            uint word = 0;
            for (int j = 0; j < 4; j++)
                word = (word << 8) | (i + j < data.Length ? data[i + j] : 0u);
            sum += word;
        }
        return sum;
    }
}
