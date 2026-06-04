namespace VulnScan.Web.Services;

public interface INmapService
{
    Task<string> RunNmapAsync(string target, string outputPath, string scanProfile, CancellationToken cancellationToken = default);
}
