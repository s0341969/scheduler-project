using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public sealed class AutoImportService(
    ApplicationDbContext dbContext,
    IWebHostEnvironment environment,
    IScanImportService scanImportService,
    IOptions<AutoImportOptions> options,
    ILogger<AutoImportService> logger) : IAutoImportService
{
    private readonly AutoImportOptions _options = options.Value;

    public Task EnsureFoldersAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ResolvePath(_options.NucleiDropPath));
        Directory.CreateDirectory(ResolvePath(_options.NessusDropPath));
        Directory.CreateDirectory(ResolvePath(_options.ProcessedPath));
        Directory.CreateDirectory(ResolvePath(_options.FailedPath));
        return Task.CompletedTask;
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return 0;
        }

        await EnsureFoldersAsync(cancellationToken);

        var totalImported = 0;
        totalImported += await ProcessDirectoryAsync(
            ResolvePath(_options.NucleiDropPath),
            ResolvePath(_options.ProcessedPath),
            ResolvePath(_options.FailedPath),
            scanImportService.ImportNucleiJsonFileAsync,
            cancellationToken);
        totalImported += await ProcessDirectoryAsync(
            ResolvePath(_options.NessusDropPath),
            ResolvePath(_options.ProcessedPath),
            ResolvePath(_options.FailedPath),
            scanImportService.ImportNessusFileAsync,
            cancellationToken);
        return totalImported;
    }

    public async Task<AutoImportIndexViewModel> BuildDashboardAsync(CancellationToken cancellationToken = default)
    {
        await EnsureFoldersAsync(cancellationToken);

        var processedRoot = ResolvePath(_options.ProcessedPath);
        var failedRoot = ResolvePath(_options.FailedPath);

        return new AutoImportIndexViewModel
        {
            Enabled = _options.Enabled,
            PollIntervalSeconds = _options.PollIntervalSeconds,
            Sources =
            [
                BuildSource("Nuclei", ResolvePath(_options.NucleiDropPath), ".json, .jsonl"),
                BuildSource("Nessus", ResolvePath(_options.NessusDropPath), ".csv, .xml"),
            ],
            RecentRuns = await dbContext.ScanRuns
                .AsNoTracking()
                .Include(item => item.ScanJob)
                .Where(item => item.CreatedBy == "auto-import")
                .OrderByDescending(item => item.CreatedAt)
                .Take(12)
                .Select(item => new AutoImportRunViewModel
                {
                    RunId = item.RunId,
                    ScanTool = item.ScanJob != null ? item.ScanJob.ScanTool : "Import",
                    FileName = item.RawResultPath ?? "-",
                    Status = item.Status,
                    TotalVulnerabilities = item.TotalVulnerabilities,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                })
                .ToListAsync(cancellationToken),
            RecentProcessedFiles = GetRecentFiles(processedRoot),
            RecentFailedFiles = GetRecentFiles(failedRoot),
        };
    }

    private async Task<int> ProcessDirectoryAsync(
        string sourcePath,
        string processedRootPath,
        string failedRootPath,
        Func<string, string, CancellationToken, Task<int>> importHandler,
        CancellationToken cancellationToken)
    {
        var importedCount = 0;
        foreach (var filePath in Directory.EnumerateFiles(sourcePath).OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".processing", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var workingPath = filePath + ".processing";
            try
            {
                File.Move(filePath, workingPath, overwrite: false);
            }
            catch (IOException)
            {
                continue;
            }

            try
            {
                await importHandler(workingPath, "auto-import", cancellationToken);
                MoveToDatedFolder(workingPath, processedRootPath, fileName);
                importedCount += 1;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Auto import failed for {FileName}", fileName);
                MoveToDatedFolder(workingPath, failedRootPath, fileName);
            }
        }

        return importedCount;
    }

    private static void MoveToDatedFolder(string sourcePath, string rootPath, string originalFileName)
    {
        var targetDirectory = Path.Combine(rootPath, DateTime.UtcNow.ToString("yyyyMMdd"));
        Directory.CreateDirectory(targetDirectory);
        File.Move(sourcePath, Path.Combine(targetDirectory, originalFileName), overwrite: true);
    }

    private AutoImportSourceViewModel BuildSource(string sourceName, string incomingPath, string sampleExtensions)
    {
        var pendingFileCount = Directory.Exists(incomingPath)
            ? Directory.EnumerateFiles(incomingPath)
                .Count(filePath => !Path.GetFileName(filePath).EndsWith(".processing", StringComparison.OrdinalIgnoreCase))
            : 0;

        return new AutoImportSourceViewModel
        {
            SourceName = sourceName,
            IncomingPath = incomingPath,
            PendingFileCount = pendingFileCount,
            SampleExtensions = sampleExtensions,
        };
    }

    private static IReadOnlyList<AutoImportFileViewModel> GetRecentFiles(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            return Array.Empty<AutoImportFileViewModel>();
        }

        return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .Take(10)
            .Select(info => new AutoImportFileViewModel
            {
                FileName = info.Name,
                FullPath = info.FullName,
                LastWriteTime = info.LastWriteTime,
            })
            .ToList();
    }

    private string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path);
}
