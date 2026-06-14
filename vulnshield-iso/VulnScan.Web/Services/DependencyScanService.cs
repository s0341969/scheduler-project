using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class DependencyScanService(
    ApplicationDbContext dbContext,
    IOptions<VulnScanOptions> options,
    ILogger<DependencyScanService> logger) : IDependencyScanService
{
    public bool IsSupported()
    {
        if (!OperatingSystem.IsWindows())
            return false;
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };
            proc.Start();
            var version = proc.StandardOutput.ReadLine();
            proc.WaitForExit(2000);
            return !string.IsNullOrWhiteSpace(version);
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> RunScanAsync(int runId, string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(targetDirectory))
            throw new InvalidOperationException($"目標目錄不存在: {targetDirectory}");

        var totalFindings = 0;

        if (HasProjectFile(targetDirectory, "*.csproj"))
        {
            totalFindings += await ScanDotNetAsync(runId, targetDirectory, cancellationToken);
        }

        if (HasProjectFile(targetDirectory, "package.json"))
        {
            totalFindings += await ScanNpmAsync(runId, targetDirectory, cancellationToken);
        }

        if (HasProjectFile(targetDirectory, "requirements.txt") || HasProjectFile(targetDirectory, "Pipfile"))
        {
            totalFindings += await ScanPythonAsync(runId, targetDirectory, cancellationToken);
        }

        return totalFindings;
    }

    private async Task<int> ScanDotNetAsync(int runId, string directory, CancellationToken cancellationToken)
    {
        try
        {
            var slnFiles = Directory.GetFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly);
            var projFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);

            if (slnFiles.Length == 0 && projFiles.Length == 0)
                return 0;

            var target = slnFiles.Length > 0 ? slnFiles[0] : projFiles[0];
            var projectDir = Path.GetDirectoryName(target) ?? directory;

            var result = await RunProcessAsync("dotnet", $"list \"{projectDir}\" package --vulnerable --include-transitive 2>&1", cancellationToken);

            if (string.IsNullOrWhiteSpace(result))
                return 0;

            return await ParseDotNetResultAsync(runId, result, directory, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "dotnet list package --vulnerable failed for {Dir}", directory);
            return 0;
        }
    }

    private async Task<int> ParseDotNetResultAsync(int runId, string output, string directory, CancellationToken cancellationToken)
    {
        var asset = await GetOrCreateDirectoryAssetAsync(directory, cancellationToken);
        var count = 0;
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentProject = string.Empty;

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("Project ", StringComparison.Ordinal))
            {
                currentProject = line.Trim();
                continue;
            }

            var match = Regex.Match(line.Trim(), @">\s*(\S+)\s+(\S+)\s+(\S+)\s+(.+)$");
            if (!match.Success)
                continue;

            var packageName = match.Groups[2].Value;
            var resolvedVersion = match.Groups[3].Value;
            var severityText = match.Groups[1].Value;
            var advisoryUrl = match.Groups[4].Value.Trim();
            var cveId = ExtractCveFromUrl(advisoryUrl);

            var vulnName = $"{packageName} ({resolvedVersion})";
            var exists = await dbContext.Vulnerabilities.AnyAsync(
                v => v.AssetId == asset.AssetId && v.RunId == runId && v.PluginId == $"DEP-{packageName}", cancellationToken);
            if (exists)
                continue;

            dbContext.Vulnerabilities.Add(new Vulnerability
            {
                AssetId = asset.AssetId,
                RunId = runId,
                IPAddress = asset.IPAddress,
                CVE = cveId,
                PluginId = $"DEP-{packageName}",
                VulnName = vulnName,
                Severity = NormalizeSeverity(severityText),
                PortNumber = null,
                Protocol = "dependency",
                ServiceName = ".NET",
                DetectedVersion = resolvedVersion,
                Description = $"{packageName} ({resolvedVersion}) 存在已知弱點: {advisoryUrl}",
                Solution = $"請升級 {packageName} 至修補版本",
                Status = VulnerabilityStatus.Unprocessed,
                FirstDetectedAt = DateTime.UtcNow,
                LastDetectedAt = DateTime.UtcNow,
            });
            count++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return count;
    }

    private async Task<int> ScanNpmAsync(int runId, string directory, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunProcessAsync("npm", $"audit --json 2>&1", cancellationToken, directory);
            if (string.IsNullOrWhiteSpace(result))
                return 0;

            return await ParseNpmResultAsync(runId, result, directory, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "npm audit failed for {Dir}", directory);
            return 0;
        }
    }

    private async Task<int> ParseNpmResultAsync(int runId, string output, string directory, CancellationToken cancellationToken)
    {
        var asset = await GetOrCreateDirectoryAssetAsync(directory, cancellationToken);
        var count = 0;

        try
        {
            using var doc = JsonDocument.Parse(output);
            if (!doc.RootElement.TryGetProperty("vulnerabilities", out var vulns))
                return 0;

            foreach (var vuln in vulns.EnumerateObject())
            {
                var packageName = vuln.Name;
                var vulnInfo = vuln.Value;
                var severity = TryGetString(vulnInfo, "severity") ?? "high";
                var via = vulnInfo.TryGetProperty("via", out var viaEl) && viaEl.ValueKind == JsonValueKind.Array
                    ? viaEl.EnumerateArray().FirstOrDefault()
                    : default;
                var cveId = via.ValueKind == JsonValueKind.Object
                    ? TryGetString(via, "cve") ?? TryGetString(via, "name")
                    : null;

                var exists = await dbContext.Vulnerabilities.AnyAsync(
                    v => v.AssetId == asset.AssetId && v.RunId == runId && v.PluginId == $"NPM-{packageName}", cancellationToken);
                if (exists)
                    continue;

                dbContext.Vulnerabilities.Add(new Vulnerability
                {
                    AssetId = asset.AssetId,
                    RunId = runId,
                    IPAddress = asset.IPAddress,
                    CVE = cveId,
                    PluginId = $"NPM-{packageName}",
                    VulnName = $"{packageName} (npm)",
                    Severity = NormalizeSeverity(severity),
                    PortNumber = null,
                    Protocol = "dependency",
                    ServiceName = "npm",
                    Description = TryGetString(vulnInfo, "overview") ?? cveId ?? $"npm 套件 {packageName} 存在弱點",
                    Solution = TryGetString(vulnInfo, "fixAvailable") == "true"
                        ? $"請升級 {packageName} 至最新版本"
                        : $"無法自動修復 {packageName}，請手動檢視",
                    Status = VulnerabilityStatus.Unprocessed,
                    FirstDetectedAt = DateTime.UtcNow,
                    LastDetectedAt = DateTime.UtcNow,
                });
                count++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse npm audit JSON output");
        }

        return count;
    }

    private async Task<int> ScanPythonAsync(int runId, string directory, CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunProcessAsync("pip-audit", "--json 2>&1", cancellationToken, directory);
            if (string.IsNullOrWhiteSpace(result))
                return 0;

            return await ParsePythonResultAsync(runId, result, directory, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "pip-audit failed for {Dir} (expected if not installed)", directory);
            return 0;
        }
    }

    private async Task<int> ParsePythonResultAsync(int runId, string output, string directory, CancellationToken cancellationToken)
    {
        var asset = await GetOrCreateDirectoryAssetAsync(directory, cancellationToken);
        var count = 0;

        try
        {
            using var doc = JsonDocument.Parse(output);
            if (!doc.RootElement.TryGetProperty("dependencies", out var deps))
                return 0;

            foreach (var dep in deps.EnumerateArray())
            {
                var packageName = TryGetString(dep, "name") ?? "unknown";
                var version = TryGetString(dep, "version") ?? "?";
                if (!dep.TryGetProperty("vulnerabilities", out var vulnList) || vulnList.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var vuln in vulnList.EnumerateArray())
                {
                    var cveId = TryGetString(vuln, "id") ?? TryGetString(vuln, "aliases") ?? "unknown";
                    var desc = TryGetString(vuln, "description") ?? $"{packageName} {version} 存在弱點";

                    var exists = await dbContext.Vulnerabilities.AnyAsync(
                        v => v.AssetId == asset.AssetId && v.RunId == runId && v.PluginId == $"PY-{cveId}", cancellationToken);
                    if (exists)
                        continue;

                    dbContext.Vulnerabilities.Add(new Vulnerability
                    {
                        AssetId = asset.AssetId,
                        RunId = runId,
                        IPAddress = asset.IPAddress,
                        CVE = cveId,
                        PluginId = $"PY-{cveId}",
                        VulnName = $"{packageName} {version}",
                        Severity = "High",
                        PortNumber = null,
                        Protocol = "dependency",
                        ServiceName = "Python",
                        DetectedVersion = version,
                        Description = desc,
                        Solution = $"請升級 {packageName} 至修補版本",
                        Status = VulnerabilityStatus.Unprocessed,
                        FirstDetectedAt = DateTime.UtcNow,
                        LastDetectedAt = DateTime.UtcNow,
                    });
                    count++;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse pip-audit JSON output");
        }

        return count;
    }

    private async Task<string?> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken, string? workingDir = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDir ?? string.Empty,
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        process.Start();

        var outputTask = ConsumeReaderAsync(process.StandardOutput, outputBuilder, cancellationToken);
        var errorTask = ConsumeReaderAsync(process.StandardError, outputBuilder, cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            await Task.WhenAll(outputTask, errorTask);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return null;
        }

        return outputBuilder.ToString();
    }

    private static async Task ConsumeReaderAsync(StreamReader reader, System.Text.StringBuilder builder, CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
                builder.AppendLine(line);
        }
        catch (OperationCanceledException) { }
    }

    private static bool HasProjectFile(string directory, string pattern)
    {
        return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).Length > 0;
    }

    private static string? ExtractCveFromUrl(string url)
    {
        var match = Regex.Match(url, @"(CVE-\d{4}-\d{4,})", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static string NormalizeSeverity(string? raw) => raw?.ToLowerInvariant() switch
    {
        "critical" or "moderate" => "Critical",
        "high" or "important" => "High",
        "medium" or "moderate" => "Medium",
        "low" => "Low",
        _ => "Info",
    };

    private static string? TryGetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private async Task<Asset> GetOrCreateDirectoryAssetAsync(string directory, CancellationToken cancellationToken)
    {
        var ipAddress = $"dep-{directory.Replace(":", "").Replace("\\", "-").Replace("/", "-")}";
        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.IPAddress == ipAddress, cancellationToken);
        if (asset is not null)
            return asset;

        var dirName = new DirectoryInfo(directory).Name;
        asset = new Asset
        {
            AssetName = $"相依性掃描 - {dirName}",
            IPAddress = ipAddress,
            AssetType = "DependencyScan",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return asset;
    }
}
