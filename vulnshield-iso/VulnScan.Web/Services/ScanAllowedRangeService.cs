using System.Net;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Services;

public sealed class ScanAllowedRangeService(ApplicationDbContext dbContext, IAuditLogService auditLogService) : IScanAllowedRangeService
{
    public async Task<bool> IsTargetAllowedAsync(string target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            await auditLogService.WriteAsync("ScanDenied", "Target", null, "Target 為空白。", null, null, cancellationToken);
            return false;
        }

        if (!IPAddress.TryParse(target, out var targetIp))
        {
            await auditLogService.WriteAsync("ScanDenied", "Target", null, $"Target `{target}` 不是合法 IP。", null, null, cancellationToken);
            return false;
        }

        var ranges = await dbContext.ScanAllowedRanges
            .AsNoTracking()
            .Where(item => item.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var range in ranges)
        {
            if (IpRangeMatcher.Contains(range.Cidr, targetIp))
            {
                return true;
            }
        }

        await auditLogService.WriteAsync("ScanDenied", "Target", null, $"Target `{target}` 不在白名單。", null, null, cancellationToken);
        return false;
    }
}
