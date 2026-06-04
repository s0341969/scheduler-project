using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class ScanAllowedRangesIndexViewModel
{
    public ScanAllowedRange Form { get; set; } = new();

    public IReadOnlyList<ScanAllowedRange> Items { get; set; } = Array.Empty<ScanAllowedRange>();
}
