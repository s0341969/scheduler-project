using Microsoft.Extensions.Options;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class AutoImportBackgroundService(
    IOptions<AutoImportOptions> options,
    IServiceScopeFactory scopeFactory,
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
        var autoImportService = scope.ServiceProvider.GetRequiredService<IAutoImportService>();
        var importedCount = await autoImportService.RunOnceAsync(cancellationToken);
        if (importedCount > 0)
        {
            logger.LogInformation("Auto import cycle completed with {ImportedCount} imported files.", importedCount);
        }
    }
}
