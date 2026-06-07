namespace VulnScan.Web.Services;

public interface INucleiResultParserService
{
    Task<int> ParseAndSaveAsync(int runId, string jsonPath, CancellationToken cancellationToken = default);
}
