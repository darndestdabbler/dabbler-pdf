// DabblerPdf.NET — A .NET port of OpenPDF (https://github.com/LibrePDF/OpenPDF)
// Original work Copyright (C) 1999-2024 by Bruno Lowagie and Paulo Soares.
// Ported to C# under the same license terms.
//
// SPDX-License-Identifier: LGPL-2.1-only AND MPL-2.0

using System.Buffers.Binary;
using DabblerPdf.Pdf;

namespace DabblerPdf.Text;

/// <summary>
/// Represents a TrueType font loaded from a .ttf file. Parses the font's binary
/// tables to extract glyph widths, metrics, and the font program for PDF embedding.
/// </summary>
internal sealed class TrueTypeFont : BaseFont
{
    private readonly byte[] _fontData;
    private readonly int[] _widths; // WinAnsi char code (0–255) → width in 1/1000 em
    private readonly FontDescriptor _descriptor;
    private readonly string _familyName;
    private readonly int _flags;
    private readonly int _unitsPerEm;
    private readonly bool _isBold;
    private readonly bool _isItalic;

    /// <summary>
    /// Loads a TrueType font from a byte array containing the .ttf file data.
    /// </summary>
    internal TrueTypeFont(string postscriptName, byte[] fontData) : base(postscriptName)
    {
        _fontData = fontData;
        var reader = new TtfReader(fontData);

        // Read the offset table to locate individual tables
        var tables = reader.ReadTableDirectory();

        // Parse required tables
        var head = reader.ReadHead(tables["head"]);
        _unitsPerEm = head.UnitsPerEm;

        var hhea = reader.ReadHhea(tables["hhea"]);
        var hmtx = reader.ReadHmtx(tables["hmtx"], hhea.NumOfLongHorMetrics);
        var cmap = reader.ReadCmapFormat4(tables["cmap"]);
        var os2 = tables.TryGetValue("OS/2", out var os2Entry)
            ? reader.ReadOs2(os2Entry)
            : null;
        var post = tables.TryGetValue("post", out var postEntry)
            ? reader.ReadPost(postEntry)
            : null;
        var name = tables.TryGetValue("name", out var nameEntry)
            ? reader.ReadName(nameEntry)
            : null;

        _familyName = name?.FamilyName ?? postscriptName;
        _isBold = head.MacStyle.HasFlag(TtfMacStyle.Bold);
        _isItalic = head.MacStyle.HasFlag(TtfMacStyle.Italic);

        // Build WinAnsi widths: map each WinAnsi byte value → Unicode → glyph ID → width
        double scale = 1000.0 / _unitsPerEm;
        _widths = new int[256];
        for (int i = 0; i < 256; i++)
        {
            char unicode = PdfEncodings.WinAnsiToChar(i);
            if (unicode == '\uFFFD' || unicode == '\0')
                continue;

            if (cmap.TryGetValue(unicode, out int glyphId) && glyphId < hmtx.Length)
            {
                _widths[i] = (int)Math.Round(hmtx[glyphId] * scale);
            }
        }

        // Build font descriptor
        double ascent = os2 != null ? os2.STypoAscender * scale : hhea.Ascent * scale;
        double descent = os2 != null ? os2.STypoDescender * scale : hhea.Descent * scale;
        double capHeight = os2 != null && os2.SCapHeight != 0 ? os2.SCapHeight * scale : ascent;
        double italicAngle = post?.ItalicAngle ?? 0;
        double bboxLlx = head.XMin * scale;
        double bboxLly = head.YMin * scale;
        double bboxUrx = head.XMax * scale;
        double bboxUry = head.YMax * scale;

        _descriptor = new FontDescriptor(ascent, capHeight, descent, italicAngle,
            bboxLlx, bboxLly, bboxUrx, bboxUry);

        // Compute flags
        _flags = ComputeFlags(os2, post);
    }

    public override int GetCharWidth(int c)
    {
        if (c >= 0 && c < _widths.Length)
            return _widths[c];
        return 0;
    }

    public override double GetFontDescriptor(int key, double fontSize)
    {
        double raw = key switch
        {
            ASCENT => _descriptor.Ascent,
            CAPHEIGHT => _descriptor.CapHeight,
            DESCENT => _descriptor.Descent,
            ITALICANGLE => _descriptor.ItalicAngle,
            BBOXLLX => _descriptor.BBoxLLX,
            BBOXLLY => _descriptor.BBoxLLY,
            BBOXURX => _descriptor.BBoxURX,
            BBOXURY => _descriptor.BBoxURY,
            _ => 0
        };
        return raw * fontSize / 1000.0;
    }

    /// <summary>
    /// Creates the complete set of PDF objects needed to embed this TrueType font.
    /// Returns the main font dictionary. The font descriptor and font stream are
    /// written as indirect objects via the writer.
    /// </summary>
    internal PdfDictionary CreateFontDictionary(PdfWriter writer)
    {
        // 1. Embed the font file as a stream
        var fontStream = new PdfStream(_fontData);
        fontStream.FlateCompress();
        fontStream.Put(PdfName.Length1, new PdfNumber(_fontData.Length));
        var fontStreamRef = writer.AddIndirectObject(fontStream);

        // 2. Build the FontDescriptor dictionary
        var descriptorDict = new PdfDictionary(PdfName.FontDescriptor);
        descriptorDict.Put(PdfName.FontName, new PdfName(PostscriptName));
        descriptorDict.Put(new PdfName("Flags"), new PdfNumber(_flags));
        descriptorDict.Put(new PdfName("FontBBox"), new PdfArray(
            _descriptor.BBoxLLX, _descriptor.BBoxLLY,
            _descriptor.BBoxURX, _descriptor.BBoxURY));
        descriptorDict.Put(new PdfName("ItalicAngle"), new PdfNumber(_descriptor.ItalicAngle));
        descriptorDict.Put(new PdfName("Ascent"), new PdfNumber(_descriptor.Ascent));
        descriptorDict.Put(new PdfName("Descent"), new PdfNumber(_descriptor.Descent));
        descriptorDict.Put(new PdfName("CapHeight"), new PdfNumber(_descriptor.CapHeight));
        descriptorDict.Put(new PdfName("StemV"), new PdfNumber(_isBold ? 140 : 80));
        descriptorDict.Put(PdfName.FontFile2, fontStreamRef);
        var descriptorRef = writer.AddIndirectObject(descriptorDict);

        // 3. Build the Widths array (FirstChar=32, LastChar=255)
        var widthsArray = new PdfArray();
        for (int i = 32; i <= 255; i++)
            widthsArray.Add(new PdfNumber(_widths[i]));

        // 4. Build the main font dictionary
        var fontDict = new PdfDictionary(PdfName.Font);
        fontDict.Put(PdfName.Subtype, PdfName.TrueType);
        fontDict.Put(PdfName.BaseFont, new PdfName(PostscriptName));
        fontDict.Put(PdfName.Encoding, PdfName.WinAnsiEncoding);
        fontDict.Put(PdfName.FirstChar, new PdfNumber(32));
        fontDict.Put(PdfName.LastChar, new PdfNumber(255));
        fontDict.Put(PdfName.Widths, widthsArray);
        fontDict.Put(PdfName.FontDescriptor, descriptorRef);

        return fontDict;
    }

    private int ComputeFlags(Os2Table? os2, PostTable? post)
    {
        int flags = 0;
        // Bit 1 (value 2): FixedPitch
        if (os2 != null && os2.Panose is { Length: >= 4 } && os2.Panose[3] == 9)
            flags |= 1; // FixedPitch
        // Bit 2 (value 4): Serif — check panose[1]
        if (os2 != null && os2.Panose is { Length: >= 2 } && os2.Panose[1] >= 2 && os2.Panose[1] <= 10)
            flags |= 2; // Serif
        // Bit 6 (value 32): Nonsymbolic (uses standard Latin encoding)
        flags |= 32;
        // Bit 7 (value 64): Italic
        if (_isItalic)
            flags |= 64;
        return flags;
    }

    /// <summary>
    /// Extracts the PostScript font name from the raw .ttf data, without fully parsing the font.
    /// Used by BaseFont.CreateFont() to determine the font name before constructing the full object.
    /// </summary>
    internal static string ExtractPostscriptName(byte[] fontData)
    {
        var reader = new TtfReader(fontData);
        var tables = reader.ReadTableDirectory();
        if (!tables.TryGetValue("name", out var nameEntry))
            throw new InvalidOperationException("TrueType font file does not contain a 'name' table.");
        var nameTable = reader.ReadName(nameEntry);
        return nameTable?.PostscriptName
            ?? nameTable?.FamilyName
            ?? throw new InvalidOperationException("Could not determine font name from TrueType file.");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TrueType binary parser
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Table directory entry: tag, checksum, offset, length.</summary>
    private readonly record struct TableEntry(int Offset, int Length);

    [Flags]
    private enum TtfMacStyle : ushort
    {
        None = 0,
        Bold = 1,
        Italic = 2,
    }

    private readonly record struct HeadTable(
        int UnitsPerEm, TtfMacStyle MacStyle,
        short XMin, short YMin, short XMax, short YMax,
        short IndexToLocFormat);

    private readonly record struct HheaTable(short Ascent, short Descent, int NumOfLongHorMetrics);

    private sealed class Os2Table
    {
        public short STypoAscender { get; init; }
        public short STypoDescender { get; init; }
        public short SCapHeight { get; init; }
        public ushort UsWeightClass { get; init; }
        public byte[]? Panose { get; init; }
    }

    private sealed class PostTable
    {
        public double ItalicAngle { get; init; }
        public int IsFixedPitch { get; init; }
    }

    private sealed class NameTable
    {
        public string? FamilyName { get; init; }
        public string? PostscriptName { get; init; }
    }

    /// <summary>
    /// Low-level reader for big-endian TrueType binary data.
    /// </summary>
    private readonly ref struct TtfReader
    {
        private readonly ReadOnlySpan<byte> _data;

        public TtfReader(byte[] data)
        {
            _data = data.AsSpan();
        }

        private ushort ReadUInt16(int offset) => BinaryPrimitives.ReadUInt16BigEndian(_data[offset..]);
        private short ReadInt16(int offset) => BinaryPrimitives.ReadInt16BigEndian(_data[offset..]);
        private uint ReadUInt32(int offset) => BinaryPrimitives.ReadUInt32BigEndian(_data[offset..]);
        private int ReadInt32(int offset) => BinaryPrimitives.ReadInt32BigEndian(_data[offset..]);

        /// <summary>Reads the table directory and returns a map of tag → (offset, length).</summary>
        public Dictionary<string, TableEntry> ReadTableDirectory()
        {
            // Offset table: sfVersion(4) + numTables(2) + searchRange(2) + entrySelector(2) + rangeShift(2) = 12
            int numTables = ReadUInt16(4);
            var tables = new Dictionary<string, TableEntry>(numTables);

            for (int i = 0; i < numTables; i++)
            {
                int recordOffset = 12 + i * 16;
                string tag = System.Text.Encoding.ASCII.GetString(_data.Slice(recordOffset, 4));
                // skip checkSum (4 bytes)
                int tableOffset = (int)ReadUInt32(recordOffset + 8);
                int tableLength = (int)ReadUInt32(recordOffset + 12);
                tables[tag] = new TableEntry(tableOffset, tableLength);
            }

            return tables;
        }

        public HeadTable ReadHead(TableEntry entry)
        {
            int o = entry.Offset;
            // head table layout (key fields):
            // +18: unitsPerEm (uint16)
            // +36: xMin, yMin, xMax, yMax (int16 each)
            // +44: macStyle (uint16)
            // +50: indexToLocFormat (int16)
            int unitsPerEm = ReadUInt16(o + 18);
            short xMin = ReadInt16(o + 36);
            short yMin = ReadInt16(o + 38);
            short xMax = ReadInt16(o + 40);
            short yMax = ReadInt16(o + 42);
            var macStyle = (TtfMacStyle)ReadUInt16(o + 44);
            short indexToLocFormat = ReadInt16(o + 50);
            return new HeadTable(unitsPerEm, macStyle, xMin, yMin, xMax, yMax, indexToLocFormat);
        }

        public HheaTable ReadHhea(TableEntry entry)
        {
            int o = entry.Offset;
            // hhea table layout:
            // +4: ascent (int16)
            // +6: descent (int16)
            // +34: numOfLongHorMetrics (uint16)
            short ascent = ReadInt16(o + 4);
            short descent = ReadInt16(o + 6);
            int numOfLongHorMetrics = ReadUInt16(o + 34);
            return new HheaTable(ascent, descent, numOfLongHorMetrics);
        }

        /// <summary>
        /// Reads the hmtx table and returns advance widths for each glyph.
        /// </summary>
        public int[] ReadHmtx(TableEntry entry, int numOfLongHorMetrics)
        {
            int o = entry.Offset;
            // Each longHorMetric: advanceWidth(uint16) + lsb(int16) = 4 bytes
            // After that: leftSideBearing entries only (2 bytes each), reusing the last advanceWidth
            int totalGlyphs = entry.Length >= numOfLongHorMetrics * 4
                ? numOfLongHorMetrics + (entry.Length - numOfLongHorMetrics * 4) / 2
                : numOfLongHorMetrics;

            var widths = new int[totalGlyphs];
            int lastWidth = 0;

            for (int i = 0; i < numOfLongHorMetrics; i++)
            {
                lastWidth = ReadUInt16(o + i * 4);
                widths[i] = lastWidth;
            }

            // Remaining glyphs use the last advance width
            for (int i = numOfLongHorMetrics; i < totalGlyphs; i++)
            {
                widths[i] = lastWidth;
            }

            return widths;
        }

        /// <summary>
        /// Reads the cmap table and returns a Unicode (BMP) → glyph ID mapping.
        /// Prefers platform 3 (Windows), encoding 1 (Unicode BMP) or
        /// platform 0 (Unicode), encoding 3 (Unicode BMP).
        /// </summary>
        public Dictionary<char, int> ReadCmapFormat4(TableEntry entry)
        {
            int o = entry.Offset;
            // cmap header: version(2) + numSubtables(2)
            int numSubtables = ReadUInt16(o + 2);

            // Find a suitable subtable
            int subtableOffset = -1;
            for (int i = 0; i < numSubtables; i++)
            {
                int recOffset = o + 4 + i * 8;
                int platformId = ReadUInt16(recOffset);
                int encodingId = ReadUInt16(recOffset + 2);
                int offset = (int)ReadUInt32(recOffset + 4);

                // Prefer Windows Unicode BMP (3,1) or Unicode BMP (0,3)
                if ((platformId == 3 && encodingId == 1) || (platformId == 0 && encodingId == 3))
                {
                    subtableOffset = o + offset;
                    break;
                }
                // Fallback to any platform 0 encoding
                if (platformId == 0 && subtableOffset < 0)
                {
                    subtableOffset = o + offset;
                }
            }

            if (subtableOffset < 0)
                return new Dictionary<char, int>();

            int format = ReadUInt16(subtableOffset);
            if (format == 4)
                return ReadCmapFormat4Subtable(subtableOffset);
            if (format == 12)
                return ReadCmapFormat12Subtable(subtableOffset);

            // Try other subtables for format 4
            for (int i = 0; i < numSubtables; i++)
            {
                int recOffset = o + 4 + i * 8;
                int offset = (int)ReadUInt32(recOffset + 4);
                int so = o + offset;
                if (ReadUInt16(so) == 4)
                    return ReadCmapFormat4Subtable(so);
            }

            return new Dictionary<char, int>();
        }

        private Dictionary<char, int> ReadCmapFormat4Subtable(int o)
        {
            var map = new Dictionary<char, int>();
            // Format 4 header:
            // format(2) + length(2) + language(2) + segCountX2(2) + searchRange(2) + entrySelector(2) + rangeShift(2)
            int segCount = ReadUInt16(o + 6) / 2;
            int headerSize = 14;

            // Arrays follow the header: endCode[segCount], reservedPad(2), startCode[segCount],
            // idDelta[segCount], idRangeOffset[segCount], glyphIdArray[...]
            int endCodeStart = o + headerSize;
            int startCodeStart = endCodeStart + segCount * 2 + 2; // +2 for reservedPad
            int idDeltaStart = startCodeStart + segCount * 2;
            int idRangeOffsetStart = idDeltaStart + segCount * 2;

            for (int i = 0; i < segCount; i++)
            {
                int endCode = ReadUInt16(endCodeStart + i * 2);
                int startCode = ReadUInt16(startCodeStart + i * 2);
                short idDelta = ReadInt16(idDeltaStart + i * 2);
                int idRangeOffset = ReadUInt16(idRangeOffsetStart + i * 2);

                if (startCode == 0xFFFF)
                    break;

                for (int c = startCode; c <= endCode; c++)
                {
                    int glyphId;
                    if (idRangeOffset == 0)
                    {
                        glyphId = (c + idDelta) & 0xFFFF;
                    }
                    else
                    {
                        // glyphIdArray offset calculation:
                        // address = idRangeOffsetStart + i*2 + idRangeOffset + (c - startCode)*2
                        int glyphIdOffset = idRangeOffsetStart + i * 2 + idRangeOffset + (c - startCode) * 2;
                        if (glyphIdOffset + 1 < _data.Length)
                        {
                            glyphId = ReadUInt16(glyphIdOffset);
                            if (glyphId != 0)
                                glyphId = (glyphId + idDelta) & 0xFFFF;
                        }
                        else
                        {
                            glyphId = 0;
                        }
                    }

                    if (glyphId != 0 && c <= 0xFFFF)
                    {
                        map[(char)c] = glyphId;
                    }
                }
            }

            return map;
        }

        private Dictionary<char, int> ReadCmapFormat12Subtable(int o)
        {
            var map = new Dictionary<char, int>();
            // Format 12: fixed32(4) + length(4) + language(4) + numGroups(4)
            int numGroups = ReadInt32(o + 12);
            int groupStart = o + 16;

            for (int i = 0; i < numGroups; i++)
            {
                int go = groupStart + i * 12;
                uint startCharCode = ReadUInt32(go);
                uint endCharCode = ReadUInt32(go + 4);
                uint startGlyphId = ReadUInt32(go + 8);

                for (uint c = startCharCode; c <= endCharCode && c <= 0xFFFF; c++)
                {
                    int glyphId = (int)(startGlyphId + (c - startCharCode));
                    if (glyphId != 0)
                        map[(char)c] = glyphId;
                }
            }

            return map;
        }

        public Os2Table? ReadOs2(TableEntry entry)
        {
            if (entry.Length < 78)
                return null;

            int o = entry.Offset;
            // OS/2 table fields:
            // +4: usWeightClass (uint16)
            // +32: panose[10] (bytes)
            // +68: sTypoAscender (int16)
            // +70: sTypoDescender (int16)
            // +88: sCapHeight (int16) — only in version >= 2

            var panose = new byte[10];
            _data.Slice(o + 32, 10).CopyTo(panose);

            short sCapHeight = 0;
            int version = ReadUInt16(o);
            if (version >= 2 && entry.Length >= 90)
                sCapHeight = ReadInt16(o + 88);

            return new Os2Table
            {
                UsWeightClass = ReadUInt16(o + 4),
                Panose = panose,
                STypoAscender = ReadInt16(o + 68),
                STypoDescender = ReadInt16(o + 70),
                SCapHeight = sCapHeight,
            };
        }

        public PostTable ReadPost(TableEntry entry)
        {
            int o = entry.Offset;
            // post table: format(fixed32 = 4 bytes), italicAngle(fixed16.16 = 4 bytes), ...
            // +4: italicAngle
            // +12: isFixedPitch (uint32)
            int italicAngleFixed = ReadInt32(o + 4);
            double italicAngle = italicAngleFixed / 65536.0;
            int isFixedPitch = entry.Length >= 16 ? ReadInt32(o + 12) : 0;

            return new PostTable
            {
                ItalicAngle = italicAngle,
                IsFixedPitch = isFixedPitch,
            };
        }

        public NameTable? ReadName(TableEntry entry)
        {
            int o = entry.Offset;
            // name table: format(2) + count(2) + stringOffset(2) + nameRecords[count]
            int count = ReadUInt16(o + 2);
            int stringOffset = o + ReadUInt16(o + 4);

            string? familyName = null;
            string? psName = null;

            for (int i = 0; i < count; i++)
            {
                int recOffset = o + 6 + i * 12;
                int platformId = ReadUInt16(recOffset);
                int encodingId = ReadUInt16(recOffset + 2);
                // int languageId = ReadUInt16(recOffset + 4);
                int nameId = ReadUInt16(recOffset + 6);
                int length = ReadUInt16(recOffset + 8);
                int offset = ReadUInt16(recOffset + 10);

                // Only read nameId 1 (family) and 6 (PostScript name)
                if (nameId != 1 && nameId != 6)
                    continue;

                int strStart = stringOffset + offset;
                if (strStart + length > _data.Length)
                    continue;

                string value;
                if (platformId == 3 || (platformId == 0 && encodingId >= 1))
                {
                    // UTF-16 BE
                    value = DecodeUtf16Be(strStart, length);
                }
                else if (platformId == 1)
                {
                    // Mac Roman (approximate as ASCII)
                    value = System.Text.Encoding.ASCII.GetString(_data.Slice(strStart, length));
                }
                else
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                switch (nameId)
                {
                    case 1 when familyName == null:
                        familyName = value;
                        break;
                    case 6 when psName == null:
                        psName = value;
                        break;
                }
            }

            return new NameTable { FamilyName = familyName, PostscriptName = psName };
        }

        private string DecodeUtf16Be(int offset, int length)
        {
            var chars = new char[length / 2];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)ReadUInt16(offset + i * 2);
            }
            return new string(chars);
        }
    }
}
