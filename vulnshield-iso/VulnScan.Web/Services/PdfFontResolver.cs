using PdfSharpCore.Fonts;

namespace VulnScan.Web.Services;

public sealed class PdfFontResolver : IFontResolver
{
    public const string DefaultFontName = "VulnScanCjk";

    private static readonly string[] FontCandidates =
    [
        @"C:\Windows\Fonts\msjh.ttc",
        @"C:\Windows\Fonts\msjhl.ttc",
        @"C:\Windows\Fonts\kaiu.ttf",
        @"C:\Windows\Fonts\mingliu.ttc",
        @"C:\Windows\Fonts\arial.ttf",
    ];

    string IFontResolver.DefaultFontName => DefaultFontName;

    public byte[]? GetFont(string faceName)
    {
        if (!string.Equals(faceName, DefaultFontName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (var candidate in FontCandidates)
        {
            if (!File.Exists(candidate))
                continue;

            var raw = File.ReadAllBytes(candidate);

            // TTC (TrueType Collection) has a "ttcf" header;
            // PdfSharpCore expects a standalone TTF, so extract the first font.
            if (raw.Length > 16
                && raw[0] == 't' && raw[1] == 't' && raw[2] == 'c' && raw[3] == 'f')
            {
                // TTC header is big-endian:
                //   uint16 majorVersion
                //   uint16 minorVersion
                //   uint32 numFonts
                //   OffsetTable[numFonts] (uint32 each)
                var major = (raw[4] << 8) | raw[5];
                var minor = (raw[6] << 8) | raw[7];
                var numFonts = (raw[8] << 24) | (raw[9] << 16) | (raw[10] << 8) | raw[11];
                if (numFonts > 0 && (major >= 1))
                {
                    var offset = (raw[12] << 24) | (raw[13] << 16) | (raw[14] << 8) | raw[15];
                    if (offset > 0 && offset < raw.Length)
                    {
                        var ttf = new byte[raw.Length - offset];
                        Buffer.BlockCopy(raw, offset, ttf, 0, ttf.Length);
                        return ttf;
                    }
                }
            }

            return raw;
        }

        throw new FileNotFoundException("Cannot locate a usable Windows font for PDF export.");
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(DefaultFontName);
    }
}
