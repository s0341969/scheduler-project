using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class DashboardController(ApplicationDbContext dbContext) : Controller
{
    [AllowAnonymous]
    public IActionResult Landing() => View();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var model = new DashboardViewModel
        {
            AssetCount = await dbContext.Assets.CountAsync(item => item.IsActive, cancellationToken),
            HighRiskVulnerabilityCount = await dbContext.Vulnerabilities.CountAsync(
                item => item.Status != "已關閉" &&
                        (item.Severity == "Critical" || item.Severity == "High" || (item.CVSS != null && item.CVSS >= 7m)),
                cancellationToken),
            OpenVulnerabilityCount = await dbContext.Vulnerabilities.CountAsync(item => item.Status != "已關閉", cancellationToken),
            OverdueVulnerabilityCount = await dbContext.Vulnerabilities.CountAsync(item => item.DueDate != null && item.DueDate < DateOnly.FromDateTime(now) && item.Status != "已關閉", cancellationToken),
            AllowedRangeCount = await dbContext.ScanAllowedRanges.CountAsync(item => item.IsEnabled, cancellationToken),
            ScanJobCount = await dbContext.ScanJobs.CountAsync(item => item.IsEnabled, cancellationToken),
            RunningScanCount = await dbContext.ScanRuns.CountAsync(item => item.Status == "Pending" || item.Status == "Running", cancellationToken),
            LastScanTime = await dbContext.ScanRuns.OrderByDescending(item => item.EndTime).Select(item => item.EndTime).FirstOrDefaultAsync(cancellationToken),
            RecentRuns = await dbContext.ScanRuns
                .AsNoTracking()
                .Include(item => item.ScanJob)
                .OrderByDescending(item => item.CreatedAt)
                .Take(8)
                .Select(item => new DashboardRecentRunViewModel
                {
                    RunId = item.RunId,
                    JobName = item.ScanJob != null ? item.ScanJob.JobName : $"Job {item.JobId}",
                    TargetRange = item.ScanJob != null ? item.ScanJob.TargetRange : "-",
                    Status = item.Status,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                })
                .ToListAsync(cancellationToken),
            RecentVulnerabilities = await dbContext.Vulnerabilities
                .AsNoTracking()
                .Include(item => item.Asset)
                .OrderByDescending(item => item.LastDetectedAt)
                .Take(8)
                .Select(item => new DashboardRecentVulnerabilityViewModel
                {
                    VulnId = item.VulnId,
                    AssetName = item.Asset != null ? item.Asset.AssetName : (item.IPAddress ?? "-"),
                    VulnName = item.VulnName,
                    Severity = item.Severity ?? "Unknown",
                    Status = item.Status,
                })
                .ToListAsync(cancellationToken),
        };

        return View(model);
    }
}
