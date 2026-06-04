namespace VulnScan.Web.Models;

public sealed class VulnScanOptions
{
    public const string SectionName = "VulnScan";

    public string ResultRootPath { get; set; } = @"C:\VulnScan\Results";

    public string NmapPath { get; set; } = "nmap";

    public int MaxConcurrentScans { get; set; } = 2;

    public bool AllowExternalTargets { get; set; }
}
