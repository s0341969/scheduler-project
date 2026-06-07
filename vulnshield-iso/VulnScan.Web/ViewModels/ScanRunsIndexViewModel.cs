using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class ScanRunsIndexViewModel
{
    public IReadOnlyList<ScanRun> Items { get; set; } = Array.Empty<ScanRun>();

    public string? SearchTerm { get; set; }

    public string? StatusFilter { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; }
}
