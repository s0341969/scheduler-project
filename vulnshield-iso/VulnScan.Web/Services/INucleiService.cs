namespace VulnScan.Web.Services;

public interface INucleiService
{
    bool IsInstalled();
    Task<string> RunNucleiAsync(string target, string outputPath, string templateOrTag, string? cliFlag = null, string? cliValue = null, CancellationToken cancellationToken = default);
    string? GetInstallPath();
}
