namespace VulnScan.Web.Models;

public sealed class VulnScanOptions
{
    public const string SectionName = "VulnScan";

    public string ResultRootPath { get; set; } = @"C:\VulnScan\Results";

    public string NmapPath { get; set; } = "nmap";

    public string NmapDownloadPageUrl { get; set; } = "https://nmap.org/download.html";

    public string NmapInstallerBaseUrl { get; set; } = "https://nmap.org/dist/";

    public string NucleiPath { get; set; } = "nuclei";

    public string InstallerCachePath { get; set; } = "App_Data\\Installers";

    public int MaxConcurrentScans { get; set; } = 2;

    public bool AllowExternalTargets { get; set; }
}
