namespace VulnScan.Web.Models;

public sealed class AutoImportOptions
{
    public const string SectionName = "AutoImport";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 60;

    public string NucleiDropPath { get; set; } = "App_Data\\AutoImport\\Nuclei\\incoming";

    public string NessusDropPath { get; set; } = "App_Data\\AutoImport\\Nessus\\incoming";

    public string ProcessedPath { get; set; } = "App_Data\\AutoImport\\processed";

    public string FailedPath { get; set; } = "App_Data\\AutoImport\\failed";
}
