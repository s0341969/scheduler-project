using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public sealed class GreenboneImportService(
    ApplicationDbContext dbContext,
    IGreenboneGmpClient greenboneGmpClient,
    IGreenboneSettingsService greenboneSettingsService,
    IScanImportService scanImportService,
    IOptions<VulnScanOptions> options,
    ILogger<GreenboneImportService> logger) : IGreenboneImportService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<GreenboneSyncResult> RunOnceAsync(string triggeredBy, string triggerMode, CancellationToken cancellationToken = default)
    {
        var effectiveOptions = await greenboneSettingsService.GetEffectiveOptionsAsync(cancellationToken);
        if (!effectiveOptions.Enabled)
        {
            return new GreenboneSyncResult();
        }

        IReadOnlyList<GreenboneReportSummary> reports;
        try
        {
            reports = await greenboneGmpClient.GetRecentReportsAsync(effectiveOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Greenbone report listing failed.");
            await WriteSyncLogAsync(effectiveOptions, triggerMode, "Failed", null, null, 0, exception.Message, triggeredBy, null, cancellationToken);
            return new GreenboneSyncResult();
        }

        var importedReports = new List<GreenboneReportSummary>();
        var importRoot = Path.Combine(_options.ResultRootPath, "imports", "greenbone");
        Directory.CreateDirectory(importRoot);

        foreach (var report in reports)
        {
            var marker = $"greenbone://report/{report.ReportId}";
            var alreadyImported = await dbContext.ScanRuns
                .AsNoTracking()
                .AnyAsync(item => item.RawResultPath == marker, cancellationToken);
            if (alreadyImported)
            {
                continue;
            }

            try
            {
                var xml = await greenboneGmpClient.DownloadReportXmlAsync(effectiveOptions, report.ReportId, cancellationToken);
                var filePath = Path.Combine(importRoot, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{report.ReportId}.xml");
                await File.WriteAllTextAsync(filePath, xml, cancellationToken);
                var runId = await scanImportService.ImportGreenboneXmlFileAsync(filePath, report.ReportId, "greenbone-api", cancellationToken);
                await WriteSyncLogAsync(
                    effectiveOptions,
                    triggerMode,
                    "Completed",
                    report.ReportId,
                    report.TaskName,
                    1,
                    "報表同步完成。",
                    triggeredBy,
                    runId,
                    cancellationToken);
                importedReports.Add(report);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Greenbone report import failed for {ReportId}", report.ReportId);
                await WriteSyncLogAsync(
                    effectiveOptions,
                    triggerMode,
                    "Failed",
                    report.ReportId,
                    report.TaskName,
                    0,
                    exception.Message,
                    triggeredBy,
                    null,
                    cancellationToken);
            }
        }

        return new GreenboneSyncResult
        {
            ImportedCount = importedReports.Count,
            ImportedReports = importedReports,
        };
    }

    private async Task WriteSyncLogAsync(
        GreenboneOptions options,
        string triggerMode,
        string status,
        string? reportId,
        string? taskName,
        int importedCount,
        string? message,
        string triggeredBy,
        int? scanRunId,
        CancellationToken cancellationToken)
    {
        dbContext.Set<GreenboneSyncLog>().Add(new GreenboneSyncLog
        {
            TriggerMode = triggerMode,
            Status = status,
            Endpoint = $"{options.Host}:{options.Port}",
            ReportId = reportId,
            TaskName = taskName,
            ImportedCount = importedCount,
            Message = message,
            TriggeredBy = triggeredBy,
            ScanRunId = scanRunId,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
