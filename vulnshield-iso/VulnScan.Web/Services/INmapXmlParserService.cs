namespace VulnScan.Web.Services;

public interface INmapXmlParserService
{
    Task ParseAndSaveAsync(int runId, string xmlPath, CancellationToken cancellationToken = default);
}
