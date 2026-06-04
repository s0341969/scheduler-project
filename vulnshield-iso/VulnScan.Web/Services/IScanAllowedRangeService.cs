namespace VulnScan.Web.Services;

public interface IScanAllowedRangeService
{
    Task<bool> IsTargetAllowedAsync(string target, CancellationToken cancellationToken = default);
}
