using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class AssetsIndexViewModel
{
    public AssetViewModel Form { get; set; } = new();

    public IReadOnlyList<Asset> Items { get; set; } = Array.Empty<Asset>();

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; }
}
