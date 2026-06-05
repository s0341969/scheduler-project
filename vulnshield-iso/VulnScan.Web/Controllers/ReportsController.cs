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
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-30);
        var model = new ReportsIndexViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            VulnerabilityCount = await dbContext.Vulnerabilities.CountAsync(cancellationToken),
            HighRiskCount = await dbContext.Vulnerabilities.CountAsync(item => item.Severity == "Critical" || item.Severity == "High", cancellationToken),
            ExportCount = await dbContext.ReportExports.CountAsync(cancellationToken),
            RecentExports = await dbContext.ReportExports
                .AsNoTracking()
                .OrderByDescending(item => item.ExportedAt)
                .Take(10)
                .ToListAsync(cancellationToken),
            RecentFindings = await dbContext.Vulnerabilities
                .AsNoTracking()
                .Include(item => item.Asset)
                .OrderByDescending(item => item.LastDetectedAt)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportPdf(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            TempData["StatusMessage"] = "PDF 報表日期區間不正確：結束日期不得早於開始日期。";
            return RedirectToAction(nameof(Index));
        }

        var filePath = await reportService.ExportIso27001PdfAsync(startDate, endDate.AddDays(1).AddTicks(-1), cancellationToken);
        TempData["StatusMessage"] = $"PDF 報表已產出：{filePath}";
        return RedirectToAction(nameof(Index));
    }
}
