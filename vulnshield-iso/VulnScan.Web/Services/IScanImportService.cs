namespace VulnScan.Web.Services;

public interface IScanImportService
{
    Task<int> ImportNucleiJsonAsync(Stream inputStream, string fileName, string userAccount, CancellationToken cancellationToken = default);

    Task<int> ImportNessusAsync(Stream inputStream, string fileName, string userAccount, CancellationToken cancellationToken = default);
}
