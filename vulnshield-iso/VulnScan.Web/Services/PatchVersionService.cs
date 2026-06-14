using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class PatchVersionService(
    ApplicationDbContext dbContext,
    IWebHostEnvironment environment,
    IOptions<VulnScanOptions> options,
    ILogger<PatchVersionService> logger) : IPatchVersionService
{
    private LocalVulnerabilityDb? _db;

    public async Task<List<PatchCheckResult>> CheckVulnerabilitiesAsync(
        int runId, CancellationToken cancellationToken = default)
    {
        var db = await LoadDatabaseAsync(cancellationToken);
        if (db is null || db.Products.Count == 0)
            return [];

        var assetPorts = await dbContext.AssetPorts
            .Where(ap => ap.RunId == runId)
            .ToListAsync(cancellationToken);

        var vulnerabilities = await dbContext.Vulnerabilities
            .Where(v => v.RunId == runId && !string.IsNullOrWhiteSpace(v.DetectedVersion))
            .ToListAsync(cancellationToken);

        var results = new List<PatchCheckResult>();

        foreach (var ap in assetPorts)
        {
            var product = NormalizeName(ap.ServiceProduct);
            var version = ap.ServiceVersion;
            if (string.IsNullOrWhiteSpace(product) || string.IsNullOrWhiteSpace(version))
                continue;

            results.AddRange(CheckProduct(runId, ap.AssetId, product, version, db, $"{ap.ServiceName ?? ""} {ap.ServiceProduct ?? ""}"));
        }

        foreach (var vuln in vulnerabilities)
        {
            var product = NormalizeName(vuln.ServiceName);
            var version = vuln.DetectedVersion;
            if (string.IsNullOrWhiteSpace(product) || string.IsNullOrWhiteSpace(version))
                continue;

            var matched = results.Any(r => r.AssetId == vuln.AssetId && r.ProductName == product);
            if (!matched)
            {
                results.AddRange(CheckProduct(runId, vuln.AssetId, product, version, db, vuln.ServiceName ?? ""));
            }
        }

        foreach (var result in results)
        {
            var exists = await dbContext.Vulnerabilities.AnyAsync(
                v => v.AssetId == result.AssetId
                     && v.RunId == runId
                     && v.PluginId == $"PATCH-{result.MatchedCve.Id}"
                     && v.CVE == result.MatchedCve.Id,
                cancellationToken);

            if (exists)
                continue;

            var asset = await dbContext.Assets.FindAsync([result.AssetId], cancellationToken);
            dbContext.Vulnerabilities.Add(new Vulnerability
            {
                AssetId = result.AssetId,
                RunId = runId,
                IPAddress = asset?.IPAddress,
                CVE = result.MatchedCve.Id,
                PluginId = $"PATCH-{result.MatchedCve.Id}",
                VulnName = $"{result.ProductName} {result.DetectedVersion} - {result.MatchedCve.Id}",
                Severity = SeverityFromCvss(result.MatchedCve.Cvss),
                CVSS = (decimal)result.MatchedCve.Cvss,
                PortNumber = null,
                Protocol = "tcp",
                ServiceName = result.ProductName,
                DetectedVersion = result.DetectedVersion,
                Description = result.MatchedCve.Description,
                Solution = $"請升級 {result.ProductName} 至最新版本以避免 {result.MatchedCve.Id}",
                Status = VulnerabilityStatus.Unprocessed,
                FirstDetectedAt = DateTime.UtcNow,
                LastDetectedAt = DateTime.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return results;
    }

    private static string SeverityFromCvss(double cvss) => cvss switch
    {
        >= 9.0 => "Critical",
        >= 7.0 => "High",
        >= 4.0 => "Medium",
        >= 0.1 => "Low",
        _ => "Info",
    };

    private static List<PatchCheckResult> CheckProduct(
        int runId, int assetId, string product, string version,
        LocalVulnerabilityDb db, string originalName)
    {
        var results = new List<PatchCheckResult>();
        foreach (var prod in db.Products)
        {
            if (prod.NormalizedProductName != product)
                continue;

            foreach (var cve in prod.Cves)
            {
                if (IsVersionAffected(version, cve.AffectedVersionRange))
                {
                    results.Add(new PatchCheckResult
                    {
                        ProductName = prod.Name,
                        DetectedVersion = version,
                        MatchedCve = cve,
                        AssetId = assetId,
                        RunId = runId,
                    });
                }
            }
        }

        return results;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;
        return name.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace("/", "")
            .Replace("_", "");
    }

    internal static bool IsVersionAffected(string detectedVersion, string rangeExpression)
    {
        var parts = rangeExpression.Split("||", StringSplitOptions.TrimEntries);
        return parts.Any(part => MatchSingleRange(detectedVersion, part));
    }

    private static bool MatchSingleRange(string version, string range)
    {
        range = range.Trim();
        if (range.StartsWith('<'))
        {
            var op = range.StartsWith("<=") ? "<=" : "<";
            var target = range[op.Length..].Trim();
            return CompareVersions(version, target) switch
            {
                < 0 when op == "<" => true,
                <= 0 when op == "<=" => true,
                _ => false,
            };
        }

        if (range.StartsWith('>'))
        {
            var op = range.StartsWith(">=") ? ">=" : ">";
            var target = range[op.Length..].Trim();
            return CompareVersions(version, target) switch
            {
                > 0 when op == ">" => true,
                >= 0 when op == ">=" => true,
                _ => false,
            };
        }

        if (range.StartsWith('='))
        {
            var target = range[1..].Trim();
            return CompareVersions(version, target) == 0;
        }

        if (range.Contains(" - "))
        {
            var dashParts = range.Split(" - ", StringSplitOptions.TrimEntries);
            if (dashParts.Length == 2)
            {
                var lower = VersionParse(dashParts[0]);
                var upper = VersionParse(dashParts[1]);
                var ver = VersionParse(version);
                return lower is not null && upper is not null && ver is not null
                    && CompareParsed(ver, lower) >= 0 && CompareParsed(ver, upper) <= 0;
            }
        }

        if (VersionTryParse(range, out _))
        {
            return CompareVersions(version, range) == 0;
        }

        return false;
    }

    private static int CompareVersions(string a, string b)
    {
        var aParsed = VersionParse(a);
        var bParsed = VersionParse(b);
        if (aParsed is null || bParsed is null)
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        return CompareParsed(aParsed, bParsed);
    }

    private static int CompareParsed(int[] a, int[] b)
    {
        var maxLen = Math.Max(a.Length, b.Length);
        for (var i = 0; i < maxLen; i++)
        {
            var av = i < a.Length ? a[i] : 0;
            var bv = i < b.Length ? b[i] : 0;
            if (av != bv)
                return av.CompareTo(bv);
        }
        return 0;
    }

    private static int[]? VersionParse(string version)
    {
        if (!VersionTryParse(version, out var result))
            return null;
        return result;
    }

    private static readonly Regex _versionRegex = new(@"(\d+)(\.(\d+))?(\.(\d+))?(\.(\d+))?", RegexOptions.Compiled);

    private static bool VersionTryParse(string input, out int[] parts)
    {
        parts = [];
        var match = _versionRegex.Match(input);
        if (!match.Success)
            return false;

        var nums = new List<int>();
        for (var i = 1; i <= 7; i += 2)
        {
            if (match.Groups[i].Success && int.TryParse(match.Groups[i].Value, out var n))
                nums.Add(n);
        }

        if (nums.Count == 0)
            return false;

        parts = [.. nums];
        return true;
    }

    private async Task<LocalVulnerabilityDb?> LoadDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_db is not null)
            return _db;

        var dbPath = options.Value.VulnerabilityDbPath;
        if (string.IsNullOrWhiteSpace(dbPath))
            dbPath = "App_Data\\vulnerability-db\\known-vulnerabilities.json";

        if (!Path.IsPathRooted(dbPath))
            dbPath = Path.Combine(environment.ContentRootPath, dbPath);

        if (!File.Exists(dbPath))
        {
            logger.LogWarning("Local vulnerability database not found at {Path}", dbPath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(dbPath, cancellationToken);
            _db = JsonSerializer.Deserialize<LocalVulnerabilityDb>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load vulnerability database from {Path}", dbPath);
        }

        return _db;
    }
}
