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
    IAuditLogService auditLogService,
    IOptions<VulnScanOptions> options) : IScanJobService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<int> CreateRunAsync(int jobId, string userAccount, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ScanJobs.FirstOrDefaultAsync(item => item.JobId == jobId, cancellationToken)
                  ?? throw new InvalidOperationException($"找不到掃描任務 {jobId}。");

        if (!await scanAllowedRangeService.IsTargetAllowedAsync(job.TargetRange, cancellationToken))
        {
            throw new InvalidOperationException($"Target `{job.TargetRange}` 不在白名單內。");
        }

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

            var outputPath = Path.Combine(_options.ResultRootPath, $"run-{run.RunId}.xml");
            var xmlPath = await nmapService.RunNmapAsync(job.TargetRange, outputPath, job.ScanProfile ?? "Normal", cancellationToken);
            await nmapXmlParserService.ParseAndSaveAsync(run.RunId, xmlPath, cancellationToken);

            run.Status = "Completed";
            run.EndTime = DateTime.UtcNow;
            run.RawResultPath = xmlPath;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            run.Status = "Failed";
            run.EndTime = DateTime.UtcNow;
            run.ErrorMessage = exception.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync("ScanRunFailed", "ScanRun", run.RunId, exception.Message, run.CreatedBy, null, cancellationToken);
            throw;
        }
    }
}
