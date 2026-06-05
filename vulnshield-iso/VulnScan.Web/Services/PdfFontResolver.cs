using PdfSharpCore.Fonts;

namespace VulnScan.Web.Services;

public sealed class PdfFontResolver : IFontResolver
{
    public const string DefaultFontName = "VulnScanCjk";

    string IFontResolver.DefaultFontName => DefaultFontName;

    public byte[]? GetFont(string faceName)
    {
        if (!string.Equals(faceName, DefaultFontName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candidates = new[]
        {
            @"C:\Windows\Fonts\msjh.ttc",
            @"C:\Windows\Fonts\mingliu.ttc",
            @"C:\Windows\Fonts\arial.ttf",
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return File.ReadAllBytes(candidate);
            }
        }

        throw new FileNotFoundException("Cannot locate a usable Windows font for PDF export.");
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(DefaultFontName);
    }
}
