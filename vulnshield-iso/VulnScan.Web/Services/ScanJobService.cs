using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class ScanJobService(
    ApplicationDbContext dbContext,
    IBackgroundJobClient backgroundJobClient,
    IScanAllowedRangeService scanAllowedRangeService,
    INmapService nmapService,
    INmapXmlParserService nmapXmlParserService,
    INucleiService nucleiService,
    INucleiResultParserService nucleiResultParserService,
    IAuditLogService auditLogService,
    IWebhookService webhookService,
    IPatchVersionService patchVersionService,
    IDependencyScanService dependencyScanService,
    IOptions<VulnScanOptions> options) : IScanJobService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<int> GetRunningScanCountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ScanRuns
            .CountAsync(item => item.Status == "Pending" || item.Status == "Running", cancellationToken);
    }

    private async Task EnsureConcurrencyLimitAsync(CancellationToken cancellationToken = default)
    {
        var running = await GetRunningScanCountAsync(cancellationToken);
        if (running >= _options.MaxConcurrentScans)
        {
            throw new InvalidOperationException($"目前已達到最大並行掃描數限制（{_options.MaxConcurrentScans}），請等待執行中的掃描完成後再試。");
        }
    }

    public NmapInstallationStatus GetNmapInstallationStatus() => nmapService.GetInstallationStatus();

    public bool IsNucleiInstalled() => nucleiService.IsInstalled();

    public async Task<int> CreateRunAsync(int jobId, string userAccount, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ScanJobs.FirstOrDefaultAsync(item => item.JobId == jobId, cancellationToken)
                  ?? throw new InvalidOperationException($"找不到掃描任務 {jobId}。");

        if (!await scanAllowedRangeService.IsTargetAllowedAsync(job.TargetRange, cancellationToken))
        {
            throw new InvalidOperationException($"Target `{job.TargetRange}` 不在白名單內。");
        }

        ValidateExecutionPrerequisites(job);
        await EnsureConcurrencyLimitAsync(cancellationToken);

        var run = new ScanRun
        {
            JobId = job.JobId,
            StartTime = DateTime.UtcNow,
            Status = "Pending",
            CreatedBy = userAccount,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.ScanRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        backgroundJobClient.Enqueue<IScanJobService>(service => service.RunScanAsync(run.RunId, CancellationToken.None));
        await auditLogService.WriteAsync("ScanRunCreated", "ScanRun", run.RunId, $"建立掃描執行紀錄 JobID={job.JobId}", userAccount, null, cancellationToken);

        return run.RunId;
    }

    public async Task RunScanAsync(int runId, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ScanRuns.Include(item => item.ScanJob).FirstOrDefaultAsync(item => item.RunId == runId, cancellationToken)
                  ?? throw new InvalidOperationException($"找不到 ScanRun {runId}。");
        var job = run.ScanJob ?? throw new InvalidOperationException($"ScanRun {runId} 缺少 ScanJob。");

        try
        {
            run.Status = "Running";
            run.StartTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (string.Equals(job.ScanTool, "DependencyScanner", StringComparison.OrdinalIgnoreCase))
            {
                await RunDependencyScanAsync(run, job, cancellationToken);
            }
            else if (string.Equals(job.ScanTool, "Nuclei", StringComparison.OrdinalIgnoreCase))
            {
                await RunNucleiScanAsync(run, job, cancellationToken);
            }
            else
            {
                await RunNmapScanAsync(run, job, cancellationToken);
            }

            if (_options.EnablePatchVersionCheck && run.Status != "Failed")
            {
                await patchVersionService.CheckVulnerabilitiesAsync(run.RunId, cancellationToken);
            }

            run.Status = run.Status == "Running" ? "Completed" : run.Status;
            run.EndTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            run.Status = "Failed";
            run.EndTime = DateTime.UtcNow;
            run.ErrorMessage = exception.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync("ScanRunFailed", "ScanRun", run.RunId, exception.Message, run.CreatedBy, null, cancellationToken);
        }
        finally
        {
            await NotifyScanCompletedAsync(run, job, cancellationToken);
        }
    }

    public async Task<int> CreateDependencyScanRunAsync(string targetDirectory, string userAccount, CancellationToken cancellationToken = default)
    {
        if (!dependencyScanService.IsSupported())
            throw new InvalidOperationException("相依性掃描需要 .NET SDK 支援（dotnet CLI）。");

        var job = new ScanJob
        {
            JobName = $"相依性掃描 - {new DirectoryInfo(targetDirectory).Name}",
            TargetRange = targetDirectory,
            ScanType = "DependencyScan",
            ScanTool = "DependencyScanner",
            ScanProfile = "DependencyScan",
            IsEnabled = false,
            CreatedBy = userAccount,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.ScanJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        var run = new ScanRun
        {
            JobId = job.JobId,
            StartTime = DateTime.UtcNow,
            Status = "Pending",
            CreatedBy = userAccount,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.ScanRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        backgroundJobClient.Enqueue<IScanJobService>(service => service.RunDependencyScanAsync(run.RunId, CancellationToken.None));
        await auditLogService.WriteAsync("DependencyScanCreated", "ScanRun", run.RunId, $"建立相依性掃描: {targetDirectory}", userAccount, null, cancellationToken);

        return run.RunId;
    }

    public async Task RunDependencyScanAsync(int runId, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.ScanRuns.Include(item => item.ScanJob).FirstOrDefaultAsync(item => item.RunId == runId, cancellationToken)
                  ?? throw new InvalidOperationException($"找不到 ScanRun {runId}。");
        var job = run.ScanJob ?? throw new InvalidOperationException($"ScanRun {runId} 缺少 ScanJob。");

        try
        {
            run.Status = "Running";
            run.StartTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            await RunDependencyScanAsync(run, job, cancellationToken);

            run.Status = "Completed";
            run.EndTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            run.Status = "Failed";
            run.EndTime = DateTime.UtcNow;
            run.ErrorMessage = exception.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RunNmapScanAsync(ScanRun run, ScanJob job, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(_options.ResultRootPath, $"run-{run.RunId}.xml");
        var xmlPath = await nmapService.RunNmapAsync(job.TargetRange, outputPath, job.ScanProfile ?? "Standard", cancellationToken);
        await nmapXmlParserService.ParseAndSaveAsync(run.RunId, xmlPath, cancellationToken);
        run.RawResultPath = xmlPath;
    }

    private async Task RunNucleiScanAsync(ScanRun run, ScanJob job, CancellationToken cancellationToken)
    {
        var profile = ScanProfileDefinition.Get(job.ScanProfile ?? "All");
        var outputPath = Path.Combine(_options.ResultRootPath, $"nuclei-run-{run.RunId}.json");
        var jsonPath = await nucleiService.RunNucleiAsync(
            job.TargetRange, outputPath, job.ScanProfile ?? "All",
            profile?.CliFlag, profile?.CliValue, cancellationToken);
        var importedCount = await nucleiResultParserService.ParseAndSaveAsync(run.RunId, jsonPath, cancellationToken);
        run.RawResultPath = jsonPath;
        run.TotalVulnerabilities = importedCount;
    }

    private async Task RunDependencyScanAsync(ScanRun run, ScanJob job, CancellationToken cancellationToken)
    {
        if (!dependencyScanService.IsSupported())
        {
            run.Status = "Failed";
            run.ErrorMessage = "相依性掃描需要 .NET SDK 支援（dotnet CLI）。";
            run.EndTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var importedCount = await dependencyScanService.RunScanAsync(run.RunId, job.TargetRange, cancellationToken);
        run.TotalVulnerabilities = importedCount;
    }

    private void ValidateExecutionPrerequisites(ScanJob job)
    {
        if (string.Equals(job.ScanTool, "DependencyScanner", StringComparison.OrdinalIgnoreCase))
        {
            if (dependencyScanService.IsSupported())
                return;

            throw new InvalidOperationException("相依性掃描需要 .NET SDK。請確認 dotnet CLI 可在系統 PATH 中使用。");
        }

        if (string.Equals(job.ScanTool, "Nuclei", StringComparison.OrdinalIgnoreCase))
        {
            if (nucleiService.IsInstalled())
                return;

            var jobName = string.IsNullOrWhiteSpace(job.JobName) ? $"Job {job.JobId}" : job.JobName;
            throw new InvalidOperationException(
                $"無法執行掃描任務「{jobName}」。找不到 nuclei.exe。請先在系統 PATH 中安裝 Nuclei 或設定 VulnScan:NucleiPath，確認後再重新執行。");
        }

        // NULL/Empty → 視為 Nmap（既有任務的預設值）
        if (string.IsNullOrWhiteSpace(job.ScanTool)
            || string.Equals(job.ScanTool, "Nmap", StringComparison.OrdinalIgnoreCase))
        {
            var status = nmapService.GetInstallationStatus();
            if (status.IsInstalled)
                return;

            var name = string.IsNullOrWhiteSpace(job.JobName) ? $"Job {job.JobId}" : job.JobName;
            throw new InvalidOperationException(
                $"無法執行掃描任務「{name}」。原因：{status.Message} 請先安裝 Nmap，或在系統設定將 `VulnScan:NmapPath` 指到有效的 nmap.exe，確認後再重新執行。");
        }
    }

    private async Task NotifyScanCompletedAsync(ScanRun run, ScanJob job, CancellationToken cancellationToken)
    {
        var webhookEvent = new WebhookEvent
        {
            EventType = "ScanCompleted",
            RunId = run.RunId,
            JobId = job.JobId,
            JobName = job.JobName ?? string.Empty,
            TargetRange = job.TargetRange,
            ScanTool = job.ScanTool,
            Status = run.Status,
            ErrorMessage = run.Status == "Failed" ? run.ErrorMessage : null,
            TotalVulnerabilities = run.TotalVulnerabilities,
            RunUrl = $"/ScanRuns/Index?jobId={job.JobId}",
        };

        await webhookService.NotifyScanCompletedAsync(webhookEvent, cancellationToken);
    }
}
