using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public interface IWebhookService
{
    Task NotifyScanCompletedAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<List<WebhookSetting>> GetSettingsAsync();
    Task<WebhookSetting?> GetSettingByIdAsync(int id);
    Task AddSettingAsync(WebhookSetting setting);
    Task UpdateSettingAsync(WebhookSetting setting);
    Task DeleteSettingAsync(int id);
}
