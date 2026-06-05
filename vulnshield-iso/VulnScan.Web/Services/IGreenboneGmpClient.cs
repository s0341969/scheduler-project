using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface IGreenboneGmpClient
{
    Task<IReadOnlyList<GreenboneReportSummary>> GetRecentReportsAsync(GreenboneOptions options, CancellationToken cancellationToken = default);

    Task<string> DownloadReportXmlAsync(GreenboneOptions options, string reportId, CancellationToken cancellationToken = default);
}
