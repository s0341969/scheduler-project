using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class ScanJobsController(ApplicationDbContext dbContext, IScanJobService scanJobService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new ScanJobsIndexViewModel
        {
            Items = await dbContext.ScanJobs
                .AsNoTracking()
                .OrderBy(item => item.JobName)
                .ToListAsync(cancellationToken),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScanJobViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new ScanJobsIndexViewModel
            {
                Form = form,
                Items = await dbContext.ScanJobs.AsNoTracking().OrderBy(item => item.JobName).ToListAsync(cancellationToken),
            });
        }

        var job = new ScanJob
        {
            JobName = form.JobName.Trim(),
            TargetRange = form.TargetRange.Trim(),
            ScanType = form.ScanType.Trim(),
            ScanTool = form.ScanTool.Trim(),
            ScanProfile = string.IsNullOrWhiteSpace(form.ScanProfile) ? "Normal" : form.ScanProfile.Trim(),
            IsEnabled = true,
            CreatedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.ScanJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已建立掃描任務：{job.JobName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunNow(int id, CancellationToken cancellationToken)
    {
        await scanJobService.CreateRunAsync(id, User.Identity?.Name ?? "system", cancellationToken);
        TempData["StatusMessage"] = $"已建立掃描任務 #{id} 的執行紀錄。";
        return RedirectToAction(nameof(Index));
    }
}
