using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface IGreenboneGmpClient
{
    Task<IReadOnlyList<GreenboneReportSummary>> GetRecentReportsAsync(CancellationToken cancellationToken = default);

    Task<string> DownloadReportXmlAsync(string reportId, CancellationToken cancellationToken = default);
}
