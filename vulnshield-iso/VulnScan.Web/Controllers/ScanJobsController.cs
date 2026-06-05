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
        return View(await BuildIndexViewModelAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScanJobViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildIndexViewModelAsync(cancellationToken);
            model.Form = form;
            return View("Index", model);
        }

        var job = new ScanJob
        {
            JobName = form.JobName.Trim(),
            TargetRange = form.TargetRange.Trim(),
            ScanType = form.ScanType.Trim(),
            ScanTool = form.ScanTool.Trim(),
            ScanProfile = string.IsNullOrWhiteSpace(form.ScanProfile) ? "Normal" : form.ScanProfile.Trim(),
            ScheduleType = string.IsNullOrWhiteSpace(form.ScheduleType) ? null : form.ScheduleType.Trim(),
            ScheduleTime = form.ScheduleTime,
            CronExpression = string.IsNullOrWhiteSpace(form.CronExpression) ? null : form.CronExpression.Trim(),
            IsEnabled = form.IsEnabled,
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
        try
        {
            await scanJobService.CreateRunAsync(id, User.Identity?.Name ?? "system", cancellationToken);
            TempData["StatusMessage"] = $"已建立掃描任務 #{id} 的執行紀錄。";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var job = await dbContext.ScanJobs.FirstOrDefaultAsync(item => item.JobId == id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        return View(new ScanJobViewModel
        {
            JobId = job.JobId,
            JobName = job.JobName,
            TargetRange = job.TargetRange,
            ScanType = job.ScanType,
            ScanTool = job.ScanTool,
            ScanProfile = job.ScanProfile,
            ScheduleType = job.ScheduleType,
            ScheduleTime = job.ScheduleTime,
            CronExpression = job.CronExpression,
            IsEnabled = job.IsEnabled,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ScanJobViewModel form, CancellationToken cancellationToken)
    {
        if (id != form.JobId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var job = await dbContext.ScanJobs.FirstOrDefaultAsync(item => item.JobId == id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        job.JobName = form.JobName.Trim();
        job.TargetRange = form.TargetRange.Trim();
        job.ScanType = form.ScanType.Trim();
        job.ScanTool = form.ScanTool.Trim();
        job.ScanProfile = string.IsNullOrWhiteSpace(form.ScanProfile) ? "Normal" : form.ScanProfile.Trim();
        job.ScheduleType = string.IsNullOrWhiteSpace(form.ScheduleType) ? null : form.ScheduleType.Trim();
        job.ScheduleTime = form.ScheduleTime;
        job.CronExpression = string.IsNullOrWhiteSpace(form.CronExpression) ? null : form.CronExpression.Trim();
        job.IsEnabled = form.IsEnabled;
        job.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已更新掃描任務：{job.JobName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var job = await dbContext.ScanJobs.FirstOrDefaultAsync(item => item.JobId == id, cancellationToken);
        if (job is null)
        {
            TempData["StatusMessage"] = "掃描任務已不存在。";
            return RedirectToAction(nameof(Index));
        }

        dbContext.ScanJobs.Remove(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已刪除掃描任務：{job.JobName}";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ScanJobsIndexViewModel> BuildIndexViewModelAsync(CancellationToken cancellationToken)
    {
        var nmapStatus = scanJobService.GetNmapInstallationStatus();

        return new ScanJobsIndexViewModel
        {
            Items = await dbContext.ScanJobs
                .AsNoTracking()
                .OrderBy(item => item.JobName)
                .ToListAsync(cancellationToken),
            Nmap = new NmapCheckViewModel
            {
                IsInstalled = nmapStatus.IsInstalled,
                CanStartInstall = OperatingSystem.IsWindows() && !nmapStatus.IsInstalled,
                StatusText = nmapStatus.IsInstalled ? "已就緒" : "缺少 Nmap",
                ResolvedPath = string.IsNullOrWhiteSpace(nmapStatus.ResolvedPath) ? "未找到" : nmapStatus.ResolvedPath,
                Source = string.IsNullOrWhiteSpace(nmapStatus.Source) ? "未判定" : nmapStatus.Source,
                Message = nmapStatus.Message,
            },
        };
    }
}
