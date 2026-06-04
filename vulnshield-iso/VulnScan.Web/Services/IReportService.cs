namespace VulnScan.Web.Services;

public interface IReportService
{
    Task<string> ExportVulnerabilityExcelAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);

    Task<string> ExportHighRiskExcelAsync(CancellationToken cancellationToken = default);

    Task<string> ExportIso27001PdfAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}
