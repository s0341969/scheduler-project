using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Services;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class ReportsController(ApplicationDbContext dbContext, IReportService reportService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new ReportsIndexViewModel
        {
            VulnerabilityCount = await dbContext.Vulnerabilities.CountAsync(cancellationToken),
            HighRiskCount = await dbContext.Vulnerabilities.CountAsync(item => item.Severity == "Critical" || item.Severity == "High", cancellationToken),
            ExportCount = await dbContext.ReportExports.CountAsync(cancellationToken),
            RecentExports = await dbContext.ReportExports
                .AsNoTracking()
                .OrderByDescending(item => item.ExportedAt)
                .Take(10)
                .ToListAsync(cancellationToken),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportHighRisk(CancellationToken cancellationToken)
    {
        var filePath = await reportService.ExportHighRiskExcelAsync(cancellationToken);
        TempData["StatusMessage"] = $"高風險弱點報表已產出：{filePath}";
        return RedirectToAction(nameof(Index));
    }
}
