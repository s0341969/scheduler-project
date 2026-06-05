namespace VulnScan.Web.Services;

public sealed class NmapInstallationStatus
{
    public bool IsInstalled { get; init; }

    public string ResolvedPath { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
