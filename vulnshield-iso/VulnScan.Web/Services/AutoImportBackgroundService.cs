using Microsoft.Extensions.Options;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class AutoImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment environment,
    IOptions<AutoImportOptions> options,
    ILogger<AutoImportBackgroundService> logger) : BackgroundService
{
    private readonly AutoImportOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Auto import is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDropFoldersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Auto import cycle failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessDropFoldersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var importService = scope.ServiceProvider.GetRequiredService<IScanImportService>();

        var nucleiDropPath = ResolvePath(_options.NucleiDropPath);
        var nessusDropPath = ResolvePath(_options.NessusDropPath);
        var processedPath = ResolvePath(_options.ProcessedPath);
        var failedPath = ResolvePath(_options.FailedPath);

        Directory.CreateDirectory(nucleiDropPath);
        Directory.CreateDirectory(nessusDropPath);
        Directory.CreateDirectory(processedPath);
        Directory.CreateDirectory(failedPath);

        await ProcessDirectoryAsync(nucleiDropPath, processedPath, failedPath, importService.ImportNucleiJsonFileAsync, cancellationToken);
        await ProcessDirectoryAsync(nessusDropPath, processedPath, failedPath, importService.ImportNessusFileAsync, cancellationToken);
    }

    private async Task ProcessDirectoryAsync(
        string sourcePath,
        string processedRootPath,
        string failedRootPath,
        Func<string, string, CancellationToken, Task<int>> importHandler,
        CancellationToken cancellationToken)
    {
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
                var processedDirectory = Path.Combine(processedRootPath, DateTime.UtcNow.ToString("yyyyMMdd"));
                Directory.CreateDirectory(processedDirectory);
                File.Move(workingPath, Path.Combine(processedDirectory, fileName), overwrite: true);
                logger.LogInformation("Auto imported file {FileName}.", fileName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to auto import file {FileName}.", fileName);
                var failedDirectory = Path.Combine(failedRootPath, DateTime.UtcNow.ToString("yyyyMMdd"));
                Directory.CreateDirectory(failedDirectory);
                File.Move(workingPath, Path.Combine(failedDirectory, fileName), overwrite: true);
            }
        }
    }

    private string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path);
}
