using System.Collections.Concurrent;
using PdfSharpCore.Fonts;

namespace VulnScan.Web.Services;

public sealed class PdfFontResolver : IFontResolver
{
    public const string DefaultFontName = "VulnScanCjk";
    public const string DefaultFontNameBold = "VulnScanCjkBold";

    private static readonly (string Path, string Style)[] FontCandidates =
    [
        (@"C:\Windows\Fonts\msyh.ttc", "Regular"),
        (@"C:\Windows\Fonts\msyhbd.ttc", "Bold"),
        (@"C:\Windows\Fonts\simsun.ttc", "Regular"),
        (@"C:\Windows\Fonts\kaiu.ttf", "Regular"),
        (@"C:\Windows\Fonts\msjh.ttc", "Regular"),
        (@"C:\Windows\Fonts\msjhl.ttc", "Regular"),
        (@"C:\Windows\Fonts\mingliu.ttc", "Regular"),
    ];

    private static readonly ConcurrentDictionary<string, byte[]> FontCache = new(StringComparer.OrdinalIgnoreCase);

    string IFontResolver.DefaultFontName => DefaultFontName;

    public byte[]? GetFont(string faceName)
    {
        if (FontCache.TryGetValue(faceName, out var cached))
            return cached;

        byte[]? loaded = faceName switch
        {
            DefaultFontName => LoadFont("Regular"),
            DefaultFontNameBold => LoadFont("Bold"),
            _ => null,
        };

        if (loaded is not null)
            FontCache.TryAdd(faceName, loaded);

        return loaded;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var name = isBold ? DefaultFontNameBold : DefaultFontName;
        return new FontResolverInfo(name, mustSimulateBold: false, mustSimulateItalic: false);
    }

    private static byte[]? LoadFont(string style)
    {
        foreach (var (path, fontStyle) in FontCandidates)
        {
            if (!string.Equals(fontStyle, style, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!File.Exists(path))
                continue;

            var raw = File.ReadAllBytes(path);

            if (IsTtc(raw))
            {
                var ttf = ExtractFirstTtfFromTtc(raw);
                if (ttf is not null)
                    return ttf;

                continue;
            }

            return raw;
        }

        return null;
    }

    private static bool IsTtc(byte[] data)
    {
        return data.Length > 4
            && data[0] == 't' && data[1] == 't' && data[2] == 'c' && data[3] == 'f';
    }

    private static byte[]? ExtractFirstTtfFromTtc(byte[] raw)
    {
        try
        {
            if (raw.Length < 16)
                return null;

            var numFonts = ReadUInt32BE(raw, 8);
            if (numFonts < 1)
                return null;

            var baseOffset = ReadUInt32BE(raw, 12);
            if (baseOffset < 12 || baseOffset >= (uint)raw.Length)
                return null;

            // Read the TTF header at baseOffset to determine total size
            // Offset subtable: sfVersion(4) + numTables(2) + searchRange(2) + entrySelector(2) + rangeShift(2) = 12
            var numTables = ReadUInt16BE(raw, (int)baseOffset + 4);

            // Find the last table record's end to determine the TTF data size
            var maxEnd = baseOffset;
            for (var i = 0; i < numTables; i++)
            {
                var recordPos = (int)baseOffset + 12 + i * 16;
                if (recordPos + 16 > raw.Length)
                    break;

                var tableOffset = ReadUInt32BE(raw, recordPos + 8);
                var tableLength = ReadUInt32BE(raw, recordPos + 12);
                var tableEnd = tableOffset + tableLength;
                if (tableEnd > maxEnd)
                    maxEnd = tableEnd;
            }

            if (maxEnd > (uint)raw.Length)
                maxEnd = (uint)raw.Length;

            var ttfLength = (int)(maxEnd - baseOffset);
            var ttf = new byte[ttfLength];
            Buffer.BlockCopy(raw, (int)baseOffset, ttf, 0, ttfLength);

            // Adjust each table record's offset (subtract baseOffset)
            for (var i = 0; i < numTables; i++)
            {
                var pos = 12 + i * 16;
                if (pos + 12 > ttf.Length)
                    break;

                var tableOffset = ReadUInt32BE(ttf, pos + 8);
                if (tableOffset >= baseOffset)
                {
                    var adjusted = tableOffset - baseOffset;
                    WriteUInt32BE(ttf, pos + 8, adjusted);
                }
            }

            return ttf;
        }
        catch
        {
            return null;
        }
    }

    private static ushort ReadUInt16BE(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32BE(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24)
             | ((uint)data[offset + 1] << 16)
             | ((uint)data[offset + 2] << 8)
             | data[offset + 3];
    }

    private static void WriteUInt32BE(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)((value >> 24) & 0xFF);
        data[offset + 1] = (byte)((value >> 16) & 0xFF);
        data[offset + 2] = (byte)((value >> 8) & 0xFF);
        data[offset + 3] = (byte)(value & 0xFF);
    }
}
