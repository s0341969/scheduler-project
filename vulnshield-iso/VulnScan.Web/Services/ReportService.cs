using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class ReportService(
    ApplicationDbContext dbContext,
    IAuditLogService auditLogService,
    IOptions<VulnScanOptions> options) : IReportService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<string> ExportVulnerabilityExcelAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var filePath = BuildReportPath("vulnerability-export");
        var items = await dbContext.Vulnerabilities
            .AsNoTracking()
            .Where(item => item.FirstDetectedAt >= start && item.FirstDetectedAt <= end)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Vulnerabilities");
        worksheet.Cell(1, 1).Value = "VulnID";
        worksheet.Cell(1, 2).Value = "AssetID";
        worksheet.Cell(1, 3).Value = "IPAddress";
        worksheet.Cell(1, 4).Value = "VulnName";
        worksheet.Cell(1, 5).Value = "Severity";
        worksheet.Cell(1, 6).Value = "CVSS";
        worksheet.Cell(1, 7).Value = "DetectedVersion";
        worksheet.Cell(1, 8).Value = "Status";

        for (var index = 0; index < items.Count; index += 1)
        {
            var row = index + 2;
            worksheet.Cell(row, 1).Value = items[index].VulnId;
            worksheet.Cell(row, 2).Value = items[index].AssetId;
            worksheet.Cell(row, 3).Value = items[index].IPAddress;
            worksheet.Cell(row, 4).Value = items[index].VulnName;
            worksheet.Cell(row, 5).Value = items[index].Severity;
            worksheet.Cell(row, 6).Value = items[index].CVSS;
            worksheet.Cell(row, 7).Value = items[index].DetectedVersion ?? items[index].ServiceName;
            worksheet.Cell(row, 8).Value = items[index].Status;
        }

        workbook.SaveAs(filePath);
        await SaveExportRecordAsync("弱點掃描總表", "Excel", filePath, cancellationToken);
        return filePath;
    }

    public async Task<string> ExportHighRiskExcelAsync(CancellationToken cancellationToken = default)
    {
        var filePath = BuildReportPath("high-risk-export");
        var items = await dbContext.Vulnerabilities
            .AsNoTracking()
            .Where(item => item.Severity == "Critical" || item.Severity == "High")
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("HighRisk");
        worksheet.Cell(1, 1).Value = "IPAddress";
        worksheet.Cell(1, 2).Value = "VulnName";
        worksheet.Cell(1, 3).Value = "Severity";
        worksheet.Cell(1, 4).Value = "DetectedVersion";
        worksheet.Cell(1, 5).Value = "DueDate";

        for (var index = 0; index < items.Count; index += 1)
        {
            var row = index + 2;
            worksheet.Cell(row, 1).Value = items[index].IPAddress;
            worksheet.Cell(row, 2).Value = items[index].VulnName;
            worksheet.Cell(row, 3).Value = items[index].Severity;
            worksheet.Cell(row, 4).Value = items[index].DetectedVersion ?? items[index].ServiceName;
            worksheet.Cell(row, 5).Value = items[index].DueDate?.ToString("yyyy-MM-dd");
        }

        workbook.SaveAs(filePath);
        await SaveExportRecordAsync("高風險弱點清單", "Excel", filePath, cancellationToken);
        return filePath;
    }

    public Task<string> ExportIso27001PdfAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("V1 尚未實作 PDF 匯出，依規格屬後續階段。");
    }

    private string BuildReportPath(string namePrefix)
    {
        var reportRoot = Path.Combine(_options.ResultRootPath, "reports");
        Directory.CreateDirectory(reportRoot);
        return Path.Combine(reportRoot, $"{namePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    private async Task SaveExportRecordAsync(string reportName, string reportType, string filePath, CancellationToken cancellationToken)
    {
        dbContext.ReportExports.Add(new ReportExport
        {
            ReportName = reportName,
            ReportType = reportType,
            FilePath = filePath,
            ExportedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync("ReportExported", "ReportExport", null, $"{reportName} => {filePath}", null, null, cancellationToken);
    }
}
