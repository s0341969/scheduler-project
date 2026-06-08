using PdfSharpCore.Fonts;

namespace VulnScan.Web.Services;

public sealed class PdfFontResolver : IFontResolver
{
    public const string DefaultFontName = "VulnScanCjk";

    private static readonly string[] FontCandidates =
    [
        @"C:\Windows\Fonts\kaiu.ttf",
        @"C:\Windows\Fonts\msjh.ttc",
        @"C:\Windows\Fonts\msjhl.ttc",
        @"C:\Windows\Fonts\mingliu.ttc",
        @"C:\Windows\Fonts\arial.ttf",
    ];

    string IFontResolver.DefaultFontName => DefaultFontName;

    public byte[]? GetFont(string faceName)
    {
        if (!string.Equals(faceName, DefaultFontName, StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (var candidate in FontCandidates)
        {
            if (!File.Exists(candidate))
                continue;

            var raw = File.ReadAllBytes(candidate);

            if (IsTtc(raw))
            {
                var ttf = ExtractFirstTtfFromTtc(raw);
                if (ttf is not null)
                    return ttf;

                continue;
            }

            return raw;
        }

        throw new FileNotFoundException("Cannot locate a usable Windows font for PDF export.");
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(DefaultFontName);
    }

    private static bool IsTtc(byte[] data)
    {
        return data.Length > 4
            && data[0] == 't' && data[1] == 't' && data[2] == 'c' && data[3] == 'f';
    }

    private static byte[]? ExtractFirstTtfFromTtc(byte[] raw)
    {
        // TTC header (big-endian):
        //   4 bytes: tag "ttcf"
        //   2 bytes: majorVersion
        //   2 bytes: minorVersion
        //   4 bytes: numFonts
        //   numFonts * 4 bytes: offsetTable[]
        //   (version 2) 12 bytes: DSIG info
        if (raw.Length < 16)
            return null;

        var numFonts = ReadUInt32BE(raw, 8);
        if (numFonts < 1)
            return null;

        var baseOffset = ReadUInt32BE(raw, 12);
        if (baseOffset < 12 || baseOffset >= raw.Length)
            return null;

        // Copy the entire font data (including shared tables) starting at baseOffset
        var ttfLength = (int)(raw.Length - baseOffset);
        var ttf = new byte[ttfLength];
        Buffer.BlockCopy(raw, (int)baseOffset, ttf, 0, ttfLength);

        // Adjust each table record's offset (subtract baseOffset)
        // TTF offset subtable: sfVersion(4) + numTables(2) + searchRange(2) + entrySelector(2) + rangeShift(2) = 12 bytes
        var numTables = ReadUInt16BE(ttf, 4);
        var recordStart = 12;

        for (var i = 0; i < numTables; i++)
        {
            var pos = recordStart + i * 16; // 16 bytes per record
            if (pos + 12 > ttf.Length)
                break;

            // Table record: tag(4) + checksum(4) + offset(4) + length(4)
            var tableOffset = ReadUInt32BE(ttf, pos + 8);
            if (tableOffset >= baseOffset)
            {
                var adjusted = tableOffset - baseOffset;
                // Write back adjusted offset (big-endian)
                ttf[pos + 8] = (byte)((adjusted >> 24) & 0xFF);
                ttf[pos + 9] = (byte)((adjusted >> 16) & 0xFF);
                ttf[pos + 10] = (byte)((adjusted >> 8) & 0xFF);
                ttf[pos + 11] = (byte)(adjusted & 0xFF);
            }
        }

        return ttf;
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
}
