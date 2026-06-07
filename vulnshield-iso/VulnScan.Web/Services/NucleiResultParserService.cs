using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class NucleiResultParserService(
    ApplicationDbContext dbContext) : INucleiResultParserService
{
    public async Task<int> ParseAndSaveAsync(int runId, string jsonPath, CancellationToken cancellationToken = default)
    {
        var fileContent = await File.ReadAllTextAsync(jsonPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(fileContent))
            return 0;

        var importedCount = 0;

        if (fileContent.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            using var document = JsonDocument.Parse(fileContent);
            foreach (var element in document.RootElement.EnumerateArray())
            {
                await UpsertFindingAsync(runId, element, cancellationToken);
                importedCount++;
            }
        }
        else
        {
            using var reader = new StringReader(fileContent);
            while (reader.ReadLine() is { } rawLine)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                using var document = JsonDocument.Parse(line);
                await UpsertFindingAsync(runId, document.RootElement, cancellationToken);
                importedCount++;
            }
        }

        return importedCount;
    }

    private async Task UpsertFindingAsync(int runId, JsonElement finding, CancellationToken cancellationToken)
    {
        var host = TryGetString(finding, "host");
        var ip = TryGetString(finding, "ip") ?? ExtractHostOrIp(host);
        var info = finding.TryGetProperty("info", out var infoEl) ? infoEl : default;
        var templateId = TryGetString(finding, "template-id");
        var severity = NormalizeSeverity(TryGetString(info, "severity") ?? TryGetString(finding, "severity") ?? "info");
        var portRaw = TryGetString(finding, "port");
        int? port = int.TryParse(portRaw, out var p) ? p : null;
        var protocol = TryGetString(finding, "type");
        var matchedAt = TryGetString(finding, "matched-at");
        var extractedResults = finding.TryGetProperty("extracted-results", out var extrEl) && extrEl.ValueKind == JsonValueKind.Array
            ? string.Join(Environment.NewLine, extrEl.EnumerateArray().Select(e => e.ToString()))
            : null;
        decimal? cvss = TryGetString(info, "classification") is { } cls
            && JsonDocument.Parse(cls).RootElement.TryGetProperty("cvss-score", out var cvssEl)
            && cvssEl.TryGetDecimal(out var cvssVal) ? cvssVal : null;

        var description = TryGetString(info, "description");
        var remediation = TryGetString(info, "remediation");

        var evidenceParts = new[] { matchedAt, extractedResults }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var vulnName = TryGetString(info, "name")
            ?? templateId
            ?? "Nuclei Finding";

        var asset = await GetOrCreateAssetAsync(ip ?? "unknown-host", cancellationToken);

        var existing = await dbContext.Vulnerabilities.FirstOrDefaultAsync(
            v => v.AssetId == asset.AssetId
                 && v.VulnName == vulnName
                 && v.PluginId == templateId
                 && v.PortNumber == port,
            cancellationToken);

        if (existing is null)
        {
            dbContext.Vulnerabilities.Add(new Vulnerability
            {
                AssetId = asset.AssetId,
                RunId = runId,
                IPAddress = ip,
                PluginId = templateId,
                VulnName = vulnName,
                Severity = severity,
                CVSS = cvss,
                PortNumber = port,
                Protocol = protocol,
                ServiceName = protocol,
                DetectedVersion = null,
                SignatureVersion = ExtractVersion(finding),
                Description = description,
                Solution = remediation,
                Evidence = string.Join(Environment.NewLine, evidenceParts),
                Status = VulnerabilityStatus.Unprocessed,
                FirstDetectedAt = DateTime.UtcNow,
                LastDetectedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.RunId = runId;
            existing.Severity = severity;
            existing.CVSS = cvss ?? existing.CVSS;
            existing.Description = description ?? existing.Description;
            existing.Solution = remediation ?? existing.Solution;
            existing.Evidence = string.Join(Environment.NewLine, evidenceParts);

            if (VulnerabilityStatus.IsClosed(existing.Status))
            {
                existing.Status = VulnerabilityStatus.PendingConfirm;
                existing.ClosedAt = null;
            }
            existing.LastDetectedAt = DateTime.UtcNow;
        }

        asset.LastScanTime = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtractVersion(JsonElement finding)
    {
        var lines = new List<string>();
        if (finding.TryGetProperty("matched-at", out var matched) && matched.ValueKind == JsonValueKind.String)
            lines.Add(matched.GetString()!);
        if (finding.TryGetProperty("template-id", out var tid))
            lines.Add($"template: {tid}");
        if (finding.TryGetProperty("template-url", out var turl))
            lines.Add(turl.GetString()!);

        var result = string.Join(" | ", lines);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;
        return element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static string ExtractHostOrIp(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return "unknown-host";
        var uri = host;
        if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            uri = uri["https://".Length..];
        else if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            uri = uri["http://".Length..];
        var colonPos = uri.IndexOf(':');
        return colonPos > 0 ? uri[..colonPos] : uri;
    }

    private static string NormalizeSeverity(string? raw)
    {
        return raw?.ToLowerInvariant() switch
        {
            "critical" => "Critical",
            "high" => "High",
            "medium" => "Medium",
            "low" => "Low",
            "info" => "Info",
            "unknown" => "Info",
            null => "Info",
            _ => raw,
        };
    }

    private async Task<Asset> GetOrCreateAssetAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.IPAddress == ipAddress, cancellationToken);
        if (asset is not null)
            return asset;

        asset = new Asset
        {
            AssetName = ipAddress,
            IPAddress = ipAddress,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return asset;
    }
}
