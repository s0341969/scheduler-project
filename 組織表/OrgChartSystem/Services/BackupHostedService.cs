using OrgChartSystem.Services;

namespace OrgChartSystem.Services;

public class BackupHostedService(IServiceProvider serviceProvider, IConfiguration config, ILogger<BackupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = bool.TryParse(config["Backup:AutoEnabled"], out var parsedEnabled) && parsedEnabled;
        if (!enabled)
        {
            return;
        }

        var intervalHours = 24;
        if (int.TryParse(config["Backup:AutoIntervalHours"], out var configuredHours))
        {
            intervalHours = Math.Clamp(configuredHours, 1, 168);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<OrgChartService>();
                await service.CreateDatabaseBackupAsync("排程自動備份", "system", "editor");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "自動備份執行失敗");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
