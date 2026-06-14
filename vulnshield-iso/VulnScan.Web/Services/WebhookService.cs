using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class WebhookService(
    ApplicationDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<WebhookService> logger) : IWebhookService
{
    public async Task NotifyScanCompletedAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.Set<WebhookSetting>()
            .Where(w => w.IsActive && w.Events.Contains(webhookEvent.EventType))
            .ToListAsync(cancellationToken);

        if (settings.Count == 0)
            return;

        var payload = JsonSerializer.Serialize(webhookEvent);
        var tasks = settings.Select(setting => SendAsync(setting, payload, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task SendAsync(WebhookSetting setting, string payload, CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("Webhook");
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(setting.Secret))
            {
                var signature = ComputeHmacSignature(payload, setting.Secret);
                content.Headers.Add("X-Webhook-Signature", signature);
            }

            var response = await client.PostAsync(setting.Url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Webhook {Url} responded with {StatusCode}", setting.Url, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send webhook to {Url}", setting.Url);
        }
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<List<WebhookSetting>> GetSettingsAsync()
    {
        return await dbContext.Set<WebhookSetting>().OrderByDescending(w => w.CreatedAt).ToListAsync();
    }

    public async Task<WebhookSetting?> GetSettingByIdAsync(int id)
    {
        return await dbContext.Set<WebhookSetting>().FindAsync(id);
    }

    public async Task AddSettingAsync(WebhookSetting setting)
    {
        setting.CreatedAt = DateTime.UtcNow;
        dbContext.Set<WebhookSetting>().Add(setting);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateSettingAsync(WebhookSetting setting)
    {
        setting.UpdatedAt = DateTime.UtcNow;
        dbContext.Set<WebhookSetting>().Update(setting);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteSettingAsync(int id)
    {
        var setting = await dbContext.Set<WebhookSetting>().FindAsync(id);
        if (setting is not null)
        {
            dbContext.Set<WebhookSetting>().Remove(setting);
            await dbContext.SaveChangesAsync();
        }
    }
}
