using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class ScanAllowedRangeService(
    ApplicationDbContext dbContext,
    IAuditLogService auditLogService,
    IOptions<VulnScanOptions> options) : IScanAllowedRangeService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<bool> IsTargetAllowedAsync(string target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            await auditLogService.WriteAsync("ScanDenied", "Target", null, "Target 為空白。", null, null, cancellationToken);
            return false;
        }

        if (_options.AllowExternalTargets)
        {
            return true;
        }

        var ips = target.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ips.Length == 0)
        {
            await auditLogService.WriteAsync("ScanDenied", "Target", null, "Target 為空白。", null, null, cancellationToken);
            return false;
        }

        var parsedIps = new List<IPAddress>(ips.Length);
        foreach (var ip in ips)
        {
            if (!IPAddress.TryParse(ip, out var parsed))
            {
                await auditLogService.WriteAsync("ScanDenied", "Target", null, $"Target `{ip}` 不是合法 IP。", null, null, cancellationToken);
                return false;
            }
            parsedIps.Add(parsed);
        }

        var ranges = await dbContext.ScanAllowedRanges
            .AsNoTracking()
            .Where(item => item.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var targetIp in parsedIps)
        {
            var allowed = false;
            foreach (var range in ranges)
            {
                if (IpRangeMatcher.Contains(range.Cidr, targetIp))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
            {
                await auditLogService.WriteAsync("ScanDenied", "Target", null, $"Target `{targetIp}` 不在白名單。", null, null, cancellationToken);
                return false;
            }
        }

        return true;
    }
}
