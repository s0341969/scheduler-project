namespace VulnScan.Web.Services;

public sealed class NmapInstallerResult
{
    public bool Started { get; init; }

    public string Message { get; init; } = string.Empty;

    public string InstallerPath { get; init; } = string.Empty;

    public string DownloadUrl { get; init; } = string.Empty;
}
