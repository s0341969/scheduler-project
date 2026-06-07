using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class VulnerabilitiesController(
    ApplicationDbContext dbContext,
    IVulnerabilityService vulnerabilityService,
    IScanImportService scanImportService,
    IOptions<VulnScanOptions> options) : Controller
{
    private const int PageSize = 20;
    private readonly VulnScanOptions _options = options.Value;

    public async Task<IActionResult> Index(string? search, string? severity, string? status, CancellationToken cancellationToken, int page = 1)
    {
        IQueryable<Vulnerability> query = dbContext.Vulnerabilities
            .AsNoTracking()
            .Include(item => item.Asset);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.VulnName.Contains(term) ||
                (item.IPAddress != null && item.IPAddress.Contains(term)) ||
                (item.CVE != null && item.CVE.Contains(term)) ||
                (item.PluginId != null && item.PluginId.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(severity) && severity != "All")
        {
            query = query.Where(item => item.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(item => item.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        return View(new VulnerabilitiesIndexViewModel
        {
            Items = await query
                .OrderByDescending(item => item.LastDetectedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(cancellationToken),
            SearchTerm = search,
            SeverityFilter = severity,
            StatusFilter = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vulnerability = await dbContext.Vulnerabilities
            .AsNoTracking()
            .Include(item => item.Asset)
            .FirstOrDefaultAsync(item => item.VulnId == id, cancellationToken);

        if (vulnerability is null)
        {
            return NotFound();
        }

        var actions = await dbContext.VulnerabilityActions
            .AsNoTracking()
            .Where(item => item.VulnId == id)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(new VulnerabilityDetailsViewModel
        {
            Vulnerability = vulnerability,
            WorkflowForm = new VulnerabilityWorkflowFormViewModel
            {
                VulnId = vulnerability.VulnId,
                OwnerUser = vulnerability.OwnerUser,
                Status = vulnerability.Status,
                DueDate = vulnerability.DueDate,
            },
            Actions = actions,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWorkflow(VulnerabilityWorkflowFormViewModel form, IFormFile? attachmentFile, CancellationToken cancellationToken)
    {
        var attachmentPath = await SaveAttachmentIfNeededAsync(attachmentFile, cancellationToken);
        await vulnerabilityService.UpdateStatusAsync(
            form.VulnId,
            form.Status,
            form.Note ?? string.Empty,
            User.Identity?.Name ?? "system",
            form.OwnerUser,
            form.DueDate,
            attachmentPath,
            cancellationToken);

        TempData["StatusMessage"] = $"已更新弱點 #{form.VulnId} 的改善追蹤。";
        return RedirectToAction(nameof(Details), new { id = form.VulnId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportNuclei(IFormFile importFile, CancellationToken cancellationToken)
    {
        if (importFile is null || importFile.Length == 0)
        {
            TempData["StatusMessage"] = "請選擇 Nuclei JSON 檔案。";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = importFile.OpenReadStream();
        var runId = await scanImportService.ImportNucleiJsonAsync(stream, importFile.FileName, User.Identity?.Name ?? "system", cancellationToken);
        TempData["StatusMessage"] = $"Nuclei 匯入完成，已建立 ScanRun #{runId}。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportNessus(IFormFile importFile, CancellationToken cancellationToken)
    {
        if (importFile is null || importFile.Length == 0)
        {
            TempData["StatusMessage"] = "請選擇 Nessus CSV 或 XML 檔案。";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = importFile.OpenReadStream();
        var runId = await scanImportService.ImportNessusAsync(stream, importFile.FileName, User.Identity?.Name ?? "system", cancellationToken);
        TempData["StatusMessage"] = $"Nessus 匯入完成，已建立 ScanRun #{runId}。";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> SaveAttachmentIfNeededAsync(IFormFile? attachmentFile, CancellationToken cancellationToken)
    {
        if (attachmentFile is null || attachmentFile.Length == 0)
        {
            return null;
        }

        var attachmentRoot = Path.Combine(_options.ResultRootPath, "attachments");
        Directory.CreateDirectory(attachmentRoot);
        var safePath = Path.Combine(attachmentRoot, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Path.GetFileName(attachmentFile.FileName)}");

        await using var outputStream = System.IO.File.Create(safePath);
        await attachmentFile.CopyToAsync(outputStream, cancellationToken);
        return safePath;
    }
}
