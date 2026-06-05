namespace VulnScan.Web.Services;

public interface IScanImportService
{
    Task<int> ImportNucleiJsonAsync(Stream inputStream, string fileName, string userAccount, CancellationToken cancellationToken = default);

    Task<int> ImportNessusAsync(Stream inputStream, string fileName, string userAccount, CancellationToken cancellationToken = default);

    Task<int> ImportNucleiJsonFileAsync(string filePath, string userAccount, CancellationToken cancellationToken = default);

    Task<int> ImportNessusFileAsync(string filePath, string userAccount, CancellationToken cancellationToken = default);

    Task<int> ImportGreenboneXmlFileAsync(string filePath, string reportId, string userAccount, CancellationToken cancellationToken = default);
}
