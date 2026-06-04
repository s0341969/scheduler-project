using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class AssetsIndexViewModel
{
    public AssetViewModel Form { get; set; } = new();

    public IReadOnlyList<Asset> Items { get; set; } = Array.Empty<Asset>();
}
