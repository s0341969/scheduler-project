using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class ScanImportService(
    ApplicationDbContext dbContext,
    IAuditLogService auditLogService,
    IOptions<VulnScanOptions> options) : IScanImportService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<int> ImportNucleiJsonAsync(Stream inputStream, string fileName, string userAccount, CancellationToken cancellationToken = default)
    {
        var savedPath = await SaveImportFileAsync(inputStream, "nuclei", fileName, cancellationToken);
        var run = await CreateImportRunAsync("Nuclei", savedPath, userAccount, cancellationToken);

        var fileContent = await File.ReadAllTextAsync(savedPath, cancellationToken);
        var importedCount = 0;

        if (fileContent.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            using var document = JsonDocument.Parse(fileContent);
            foreach (var element in document.RootElement.EnumerateArray())
            {
                await UpsertNucleiFindingAsync(run.RunId, element, userAccount, cancellationToken);
                importedCount += 1;
            }
        }
        else
        {
            using var reader = new StringReader(fileContent);
            while (reader.ReadLine() is { } rawLine)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(line);
                await UpsertNucleiFindingAsync(run.RunId, document.RootElement, userAccount, cancellationToken);
                importedCount += 1;
            }
        }

        run.Status = "Completed";
        run.TotalVulnerabilities = importedCount;
        run.EndTime = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync("NucleiImported", "ScanRun", run.RunId, $"匯入 {importedCount} 筆結果：{fileName}", userAccount, null, cancellationToken);
        return run.RunId;
    }

    public async Task<int> ImportNessusAsync(Stream inputStream, string fileName, string userAccount, CancellationToken cancellationToken = default)
    {
        var savedPath = await SaveImportFileAsync(inputStream, "nessus", fileName, cancellationToken);
        var run = await CreateImportRunAsync("Nessus", savedPath, userAccount, cancellationToken);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var importedCount = extension switch
        {
            ".csv" => await ImportNessusCsvAsync(run.RunId, savedPath, userAccount, cancellationToken),
            ".xml" => await ImportNessusXmlAsync(run.RunId, savedPath, userAccount, cancellationToken),
            _ => throw new InvalidOperationException("Nessus 只支援 CSV 或 XML 匯入。"),
        };

        run.Status = "Completed";
        run.TotalVulnerabilities = importedCount;
        run.EndTime = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync("NessusImported", "ScanRun", run.RunId, $"匯入 {importedCount} 筆結果：{fileName}", userAccount, null, cancellationToken);
        return run.RunId;
    }

    private async Task<string> SaveImportFileAsync(Stream inputStream, string subFolder, string fileName, CancellationToken cancellationToken)
    {
        var importRoot = Path.Combine(_options.ResultRootPath, "imports", subFolder);
        Directory.CreateDirectory(importRoot);
        var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Path.GetFileName(fileName)}";
        var savedPath = Path.Combine(importRoot, safeName);

        await using var outputStream = File.Create(savedPath);
        await inputStream.CopyToAsync(outputStream, cancellationToken);
        return savedPath;
    }

    private async Task<ScanRun> CreateImportRunAsync(string scanTool, string rawResultPath, string userAccount, CancellationToken cancellationToken)
    {
        var job = new ScanJob
        {
            JobName = $"{scanTool} Import {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            TargetRange = rawResultPath,
            ScanType = "Import",
            ScanTool = scanTool,
            ScanProfile = "Imported",
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
            Status = "Running",
            RawResultPath = rawResultPath,
            CreatedBy = userAccount,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.ScanRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    private async Task<int> ImportNessusCsvAsync(int runId, string filePath, string userAccount, CancellationToken cancellationToken)
    {
        var importedCount = 0;
        using var parser = new TextFieldParser(filePath, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            return 0;
        }

        var headers = parser.ReadFields() ?? Array.Empty<string>();
        var headerMap = headers
            .Select((value, index) => new { value, index })
            .ToDictionary(item => item.value.Trim(), item => item.index, StringComparer.OrdinalIgnoreCase);

        while (!parser.EndOfData)
        {
            var row = parser.ReadFields();
            if (row is null || row.Length == 0)
            {
                continue;
            }

            await UpsertImportedVulnerabilityAsync(
                runId,
                userAccount,
                GetValue(headerMap, row, "Host"),
                GetValue(headerMap, row, "Name"),
                GetValue(headerMap, row, "Risk"),
                GetValue(headerMap, row, "Plugin ID"),
                GetValue(headerMap, row, "CVE"),
                ParseNullableDecimal(GetValue(headerMap, row, "CVSS")),
                ParseNullableInt(GetValue(headerMap, row, "Port")),
                GetValue(headerMap, row, "Protocol"),
                GetValue(headerMap, row, "Description"),
                GetValue(headerMap, row, "Solution"),
                GetValue(headerMap, row, "Plugin Output"),
                cancellationToken);

            importedCount += 1;
        }

        return importedCount;
    }

    private async Task<int> ImportNessusXmlAsync(int runId, string filePath, string userAccount, CancellationToken cancellationToken)
    {
        var document = XDocument.Load(filePath);
        var importedCount = 0;

        foreach (var reportHost in document.Descendants("ReportHost"))
        {
            var hostName = reportHost.Attribute("name")?.Value ?? string.Empty;
            foreach (var reportItem in reportHost.Descendants("ReportItem"))
            {
                var cves = string.Join(",", reportItem.Elements("cve").Select(item => item.Value).Where(item => !string.IsNullOrWhiteSpace(item)));
                await UpsertImportedVulnerabilityAsync(
                    runId,
                    userAccount,
                    hostName,
                    reportItem.Attribute("pluginName")?.Value ?? "Nessus Finding",
                    reportItem.Element("risk_factor")?.Value ?? reportItem.Attribute("severity")?.Value,
                    reportItem.Attribute("pluginID")?.Value,
                    cves,
                    ParseNullableDecimal(reportItem.Element("cvss_base_score")?.Value),
                    ParseNullableInt(reportItem.Attribute("port")?.Value),
                    reportItem.Attribute("protocol")?.Value,
                    reportItem.Element("description")?.Value,
                    reportItem.Element("solution")?.Value,
                    reportItem.Element("plugin_output")?.Value,
                    cancellationToken);

                importedCount += 1;
            }
        }

        return importedCount;
    }

    private async Task UpsertNucleiFindingAsync(int runId, JsonElement finding, string userAccount, CancellationToken cancellationToken)
    {
        var host = TryGetString(finding, "host");
        var info = finding.TryGetProperty("info", out var infoElement) ? infoElement : default;
        var extractedResults = finding.TryGetProperty("extracted-results", out var extractedResultsElement) && extractedResultsElement.ValueKind == JsonValueKind.Array
            ? string.Join(Environment.NewLine, extractedResultsElement.EnumerateArray().Select(item => item.ToString()))
            : null;

        var evidenceParts = new[]
        {
            TryGetString(finding, "matched-at"),
            extractedResults,
        }.Where(item => !string.IsNullOrWhiteSpace(item));

        await UpsertImportedVulnerabilityAsync(
            runId,
            userAccount,
            ExtractHostOrIp(host),
            TryGetString(info, "name") ?? TryGetString(finding, "template-id") ?? "Nuclei Finding",
            TryGetString(info, "severity"),
            TryGetString(finding, "template-id"),
            null,
            null,
            ParseNullableInt(TryGetString(finding, "port")),
            TryGetString(finding, "type"),
            TryGetString(info, "description"),
            TryGetString(info, "remediation"),
            string.Join(Environment.NewLine, evidenceParts),
            cancellationToken);
    }

    private async Task UpsertImportedVulnerabilityAsync(
        int runId,
        string userAccount,
        string? ipAddress,
        string? vulnName,
        string? severity,
        string? pluginId,
        string? cve,
        decimal? cvss,
        int? portNumber,
        string? protocol,
        string? description,
        string? solution,
        string? evidence,
        CancellationToken cancellationToken)
    {
        var normalizedAddress = string.IsNullOrWhiteSpace(ipAddress) ? "unknown-host" : ipAddress.Trim();
        var asset = await GetOrCreateAssetAsync(normalizedAddress, cancellationToken);

        var existing = await dbContext.Vulnerabilities.FirstOrDefaultAsync(
            item => item.AssetId == asset.AssetId &&
                    item.VulnName == (vulnName ?? "Imported Finding") &&
                    item.PluginId == pluginId &&
                    item.PortNumber == portNumber,
            cancellationToken);

        if (existing is null)
        {
            dbContext.Vulnerabilities.Add(new Vulnerability
            {
                AssetId = asset.AssetId,
                RunId = runId,
                IPAddress = normalizedAddress,
                CVE = string.IsNullOrWhiteSpace(cve) ? null : cve.Trim(),
                PluginId = string.IsNullOrWhiteSpace(pluginId) ? null : pluginId.Trim(),
                VulnName = string.IsNullOrWhiteSpace(vulnName) ? "Imported Finding" : vulnName.Trim(),
                Severity = NormalizeSeverity(severity),
                CVSS = cvss,
                PortNumber = portNumber,
                Protocol = string.IsNullOrWhiteSpace(protocol) ? null : protocol.Trim(),
                Description = description,
                Solution = solution,
                Evidence = evidence,
                Status = "未處理",
                FirstDetectedAt = DateTime.UtcNow,
                LastDetectedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.RunId = runId;
            existing.Severity = NormalizeSeverity(severity);
            existing.CVSS = cvss;
            existing.CVE = string.IsNullOrWhiteSpace(cve) ? existing.CVE : cve.Trim();
            existing.Protocol = string.IsNullOrWhiteSpace(protocol) ? existing.Protocol : protocol.Trim();
            existing.Description = string.IsNullOrWhiteSpace(description) ? existing.Description : description;
            existing.Solution = string.IsNullOrWhiteSpace(solution) ? existing.Solution : solution;
            existing.Evidence = string.IsNullOrWhiteSpace(evidence) ? existing.Evidence : evidence;
            existing.LastDetectedAt = DateTime.UtcNow;
            if (string.Equals(existing.Status, "已關閉", StringComparison.OrdinalIgnoreCase))
            {
                existing.Status = "待確認";
                existing.ClosedAt = null;
            }
        }

        asset.LastScanTime = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Asset> GetOrCreateAssetAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(item => item.IPAddress == ipAddress, cancellationToken);
        if (asset is not null)
        {
            return asset;
        }

        asset = new Asset
        {
            AssetName = ipAddress,
            IPAddress = ipAddress,
            AssetType = "Imported",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return asset;
    }

    private static string NormalizeSeverity(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "critical" or "4" => "Critical",
            "high" or "3" => "High",
            "medium" or "moderate" or "2" => "Medium",
            "low" or "1" => "Low",
            "info" or "informational" or "0" => "Info",
            _ => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim(),
        };
    }

    private static string? GetValue(IReadOnlyDictionary<string, int> headerMap, IReadOnlyList<string> row, string header)
    {
        return headerMap.TryGetValue(header, out var index) && index < row.Count ? row[index] : null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.ValueKind != JsonValueKind.Undefined &&
               element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False
            ? property.ToString()
            : null;
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) ? parsedValue : null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue) ? parsedValue : null;
    }

    private static string ExtractHostOrIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown-host";
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        return value.Trim();
    }
}
