using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class ScanJobsIndexViewModel
{
    public ScanJobViewModel Form { get; set; } = new();

    public IReadOnlyList<Asset> AvailableAssets { get; set; } = Array.Empty<Asset>();

    public IReadOnlyList<ScanJob> Items { get; set; } = Array.Empty<ScanJob>();

    public NmapCheckViewModel? Nmap { get; set; }

    public NmapCheckViewModel? Nuclei { get; set; }

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalCount { get; set; }

    public bool DependencyScannerSupported { get; set; }
}
