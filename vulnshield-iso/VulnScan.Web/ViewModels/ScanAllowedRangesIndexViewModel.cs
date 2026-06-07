using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class ScanAllowedRangesIndexViewModel
{
    public ScanAllowedRange Form { get; set; } = new();

    public IReadOnlyList<ScanAllowedRange> Items { get; set; } = Array.Empty<ScanAllowedRange>();

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; }
}
