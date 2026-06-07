namespace VulnScan.Web.Services;

public interface INucleiService
{
    bool IsInstalled();
    Task<string> RunNucleiAsync(string target, string outputPath, string templateFilter, CancellationToken cancellationToken = default);
    string? GetInstallPath();
}
