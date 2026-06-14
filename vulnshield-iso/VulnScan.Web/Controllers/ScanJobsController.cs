using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

    [Authorize]
public sealed class ScanJobsController(
    ApplicationDbContext dbContext,
    IScanJobService scanJobService,
    IScanScheduleService scanScheduleService) : Controller
{
    private const int PageSize = 20;

    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken, int page = 1)
    {
        return View(await BuildIndexViewModelAsync(search, cancellationToken, page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScanJobViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildIndexViewModelAsync(null, cancellationToken);
            model.Form = form;
            return View("Index", model);
        }

        var scanType = form.ScanType.Trim();
        var isAllScan = string.Equals(scanType, "AllScan", StringComparison.OrdinalIgnoreCase);
        var job = new ScanJob
        {
            JobName = form.JobName.Trim(),
            TargetRange = ResolveTargetRange(form),
            ScanType = scanType,
            ScanTool = isAllScan ? "All" : form.ScanTool.Trim(),
            ScanProfile = isAllScan ? "Deep" : string.IsNullOrWhiteSpace(form.ScanProfile) ? "Normal" : form.ScanProfile.Trim(),
            ScheduleType = string.IsNullOrWhiteSpace(form.ScheduleType) ? null : form.ScheduleType.Trim(),
            ScheduleTime = form.ScheduleTime,
            CronExpression = string.IsNullOrWhiteSpace(form.CronExpression) ? null : form.CronExpression.Trim(),
            IsEnabled = form.IsEnabled,
            CreatedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.ScanJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncScanJobAssetsAsync(job.JobId, form.SelectedAssetIds, cancellationToken);

        if (!string.IsNullOrWhiteSpace(job.CronExpression) && job.IsEnabled)
        {
            await scanScheduleService.AddOrUpdateJobAsync(job.JobId, job.CronExpression, cancellationToken);
        }

        TempData["StatusMessage"] = $"已建立掃描任務：{job.JobName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunNow(int id, CancellationToken cancellationToken)
    {
        try
        {
            var runId = await scanJobService.CreateRunAsync(id, User.Identity?.Name ?? "system", cancellationToken);
            TempData["StatusMessage"] = $"已建立掃描任務 #{id} 的執行紀錄 (Run #{runId})。";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunDependencyScan(string targetDirectory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            TempData["ErrorMessage"] = "請指定要掃描的目錄。";
            return RedirectToAction(nameof(Index));
        }

        if (!Directory.Exists(targetDirectory))
        {
            TempData["ErrorMessage"] = $"目錄不存在：{targetDirectory}";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var runId = await scanJobService.CreateDependencyScanRunAsync(targetDirectory, User.Identity?.Name ?? "system", cancellationToken);
            TempData["StatusMessage"] = $"已建立相依性掃描 (Run #{runId})，目錄：{targetDirectory}";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RunningScans(CancellationToken cancellationToken)
    {
        var runs = await dbContext.ScanRuns
            .AsNoTracking()
            .Include(item => item.ScanJob)
            .Where(item => item.Status == "Pending" || item.Status == "Running")
            .OrderByDescending(item => item.StartTime)
            .Select(item => new
            {
                item.RunId,
                JobName = item.ScanJob != null ? item.ScanJob.JobName : $"Job {item.JobId}",
                item.Status,
                StartedAt = item.StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            })
            .ToListAsync(cancellationToken);

        return Json(runs);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var job = await dbContext.ScanJobs
            .Include(item => item.ScanJobAssets)
            .FirstOrDefaultAsync(item => item.JobId == id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        return View(new ScanJobViewModel
        {
            JobId = job.JobId,
            JobName = job.JobName,
            TargetRange = job.TargetRange,
            SelectedAssetIds = job.ScanJobAssets.Select(sja => sja.AssetId).ToList(),
            ScanType = job.ScanType,
            ScanTool = string.IsNullOrWhiteSpace(job.ScanTool) ? "Nmap" : job.ScanTool,
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
            var availableAssets = await dbContext.Assets.Where(a => a.IsActive).OrderBy(a => a.AssetName).ToListAsync(cancellationToken);
            ViewBag.AvailableAssets = availableAssets;
            return View(form);
        }

        var job = await dbContext.ScanJobs
            .Include(item => item.ScanJobAssets)
            .FirstOrDefaultAsync(item => item.JobId == id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var editScanType = form.ScanType.Trim();
        var editIsAllScan = string.Equals(editScanType, "AllScan", StringComparison.OrdinalIgnoreCase);
        job.JobName = form.JobName.Trim();
        job.TargetRange = ResolveTargetRange(form);
        job.ScanType = editScanType;
        job.ScanTool = editIsAllScan ? "All" : form.ScanTool.Trim();
        job.ScanProfile = editIsAllScan ? "Deep" : string.IsNullOrWhiteSpace(form.ScanProfile) ? "Normal" : form.ScanProfile.Trim();
        job.ScheduleType = string.IsNullOrWhiteSpace(form.ScheduleType) ? null : form.ScheduleType.Trim();
        job.ScheduleTime = form.ScheduleTime;
        job.CronExpression = string.IsNullOrWhiteSpace(form.CronExpression) ? null : form.CronExpression.Trim();
        job.IsEnabled = form.IsEnabled;
        job.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncScanJobAssetsAsync(job.JobId, form.SelectedAssetIds, cancellationToken);

        if (!string.IsNullOrWhiteSpace(job.CronExpression))
        {
            if (job.IsEnabled)
            {
                await scanScheduleService.RemoveJobAsync(job.JobId, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(form.CronExpression) && form.IsEnabled)
            {
                await scanScheduleService.AddOrUpdateJobAsync(job.JobId, form.CronExpression, cancellationToken);
            }
        }
        else if (!string.IsNullOrWhiteSpace(form.CronExpression) && form.IsEnabled)
        {
            await scanScheduleService.AddOrUpdateJobAsync(job.JobId, form.CronExpression, cancellationToken);
        }

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

        if (!string.IsNullOrWhiteSpace(job.CronExpression) && job.IsEnabled)
        {
            await scanScheduleService.RemoveJobAsync(job.JobId, cancellationToken);
        }

        dbContext.ScanJobs.Remove(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已刪除掃描任務：{job.JobName}";
        return RedirectToAction(nameof(Index));
    }

    private static string ResolveTargetRange(ScanJobViewModel form)
    {
        return form.SelectedAssetIds.Count > 0 ? string.Empty : form.TargetRange.Trim();
    }

    private async Task SyncScanJobAssetsAsync(int jobId, List<int> selectedAssetIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ScanJobAssets
            .Where(sja => sja.ScanJobId == jobId)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(sja => !selectedAssetIds.Contains(sja.AssetId)).ToList();
        var existingAssetIds = existing.Select(sja => sja.AssetId).ToHashSet();
        var toAdd = selectedAssetIds.Where(id => !existingAssetIds.Contains(id))
            .Select(assetId => new ScanJobAsset { ScanJobId = jobId, AssetId = assetId })
            .ToList();

        dbContext.ScanJobAssets.RemoveRange(toRemove);
        dbContext.ScanJobAssets.AddRange(toAdd);

        if (toRemove.Count > 0 || toAdd.Count > 0)
        {
            // Recompute TargetRange from linked assets
            var job = await dbContext.ScanJobs
                .Include(j => j.ScanJobAssets)
                .ThenInclude(sja => sja.Asset)
                .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
            if (job is not null)
            {
                var ips = job.ScanJobAssets
                    .Select(sja => sja.Asset?.IPAddress)
                    .Where(ip => !string.IsNullOrWhiteSpace(ip))
                    .Distinct()
                    .ToList();
                job.TargetRange = ips.Count > 0 ? string.Join(" ", ips) : "-";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ScanJobsIndexViewModel> BuildIndexViewModelAsync(string? search, CancellationToken cancellationToken, int page = 1)
    {
        var nmapStatus = scanJobService.GetNmapInstallationStatus();
        var nucleiStatus = scanJobService.IsNucleiInstalled();
        var dependencyScanSupported = false;
        try
        {
            using var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };
            proc.Start();
            var version = proc.StandardOutput.ReadLine();
            proc.WaitForExit(2000);
            dependencyScanSupported = !string.IsNullOrWhiteSpace(version);
        }
        catch { }
        var query = dbContext.ScanJobs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.JobName.Contains(term) ||
                item.TargetRange.Contains(term) ||
                (item.ScanType != null && item.ScanType.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        return new ScanJobsIndexViewModel
        {
            DependencyScannerSupported = dependencyScanSupported,
            AvailableAssets = await dbContext.Assets
                .Where(a => a.IsActive)
                .OrderBy(a => a.AssetName)
                .ToListAsync(cancellationToken),
            Items = await query
                .OrderBy(item => item.JobName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
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
            Nuclei = new NmapCheckViewModel
            {
                IsInstalled = nucleiStatus,
                CanStartInstall = false,
                StatusText = nucleiStatus ? "已就緒" : "缺少 Nuclei",
                ResolvedPath = nucleiStatus ? "nuclei" : "未找到",
                Source = nucleiStatus ? "PATH" : "未安裝",
                Message = nucleiStatus ? "可用" : "nuclei.exe 未安裝於系統 PATH 中，無法執行 Nuclei 掃描任務",
            },
            SearchTerm = search,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
        };
    }
}
