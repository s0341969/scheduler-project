using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;

namespace VulnScan.Web.Tests.Services;

public sealed class WebhookServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddSettingAsync_PersistsSetting()
    {
        var db = CreateDbContext();
        var httpFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(db, httpFactory.Object, logger.Object);

        var setting = new WebhookSetting
        {
            Url = "https://example.com/webhook",
            Events = "ScanCompleted",
            IsActive = true,
        };

        await service.AddSettingAsync(setting);

        var saved = await db.Set<WebhookSetting>().FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("https://example.com/webhook", saved!.Url);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsOrderedByCreatedAt()
    {
        var db = CreateDbContext();
        var httpFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(db, httpFactory.Object, logger.Object);

        db.Set<WebhookSetting>().AddRange(
            new WebhookSetting { Url = "http://a.com", Events = "ScanCompleted", IsActive = true, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new WebhookSetting { Url = "http://b.com", Events = "ScanCompleted", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var settings = await service.GetSettingsAsync();

        Assert.Equal(2, settings.Count);
        Assert.Equal("http://b.com", settings[0].Url);
    }

    [Fact]
    public async Task DeleteSettingAsync_RemovesSetting()
    {
        var db = CreateDbContext();
        var httpFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(db, httpFactory.Object, logger.Object);

        var setting = new WebhookSetting
        {
            Url = "https://example.com/webhook",
            Events = "ScanCompleted",
            IsActive = true,
        };
        db.Set<WebhookSetting>().Add(setting);
        await db.SaveChangesAsync();

        await service.DeleteSettingAsync(setting.Id);

        var count = await db.Set<WebhookSetting>().CountAsync();
        Assert.Equal(0, count);
    }
}
