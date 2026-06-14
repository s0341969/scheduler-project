using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public interface IPatchVersionService
{
    Task<List<PatchCheckResult>> CheckVulnerabilitiesAsync(
        int runId,
        CancellationToken cancellationToken = default);
}
