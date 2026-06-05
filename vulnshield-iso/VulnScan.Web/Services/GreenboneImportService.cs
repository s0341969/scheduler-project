using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public sealed class GreenboneImportService(
    ApplicationDbContext dbContext,
    IGreenboneGmpClient greenboneGmpClient,
    IScanImportService scanImportService,
    IOptions<VulnScanOptions> options,
    ILogger<GreenboneImportService> logger) : IGreenboneImportService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<GreenboneSyncResult> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var reports = await greenboneGmpClient.GetRecentReportsAsync(cancellationToken);
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
                var xml = await greenboneGmpClient.DownloadReportXmlAsync(report.ReportId, cancellationToken);
                var filePath = Path.Combine(importRoot, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{report.ReportId}.xml");
                await File.WriteAllTextAsync(filePath, xml, cancellationToken);
                await scanImportService.ImportGreenboneXmlFileAsync(filePath, report.ReportId, "greenbone-api", cancellationToken);
                importedReports.Add(report);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Greenbone report import failed for {ReportId}", report.ReportId);
            }
        }

        return new GreenboneSyncResult
        {
            ImportedCount = importedReports.Count,
            ImportedReports = importedReports,
        };
    }
}
