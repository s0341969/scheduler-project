using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;

namespace VulnScan.Web.Tests.Services;

public sealed class ScanAllowedRangeServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IOptions<VulnScanOptions> CreateOptions(bool allowExternal = false)
    {
        return Options.Create(new VulnScanOptions { AllowExternalTargets = allowExternal });
    }

    [Fact]
    public async Task IsTargetAllowedAsync_AllowExternal_ReturnsTrue()
    {
        var db = CreateDbContext();
        var audit = new Mock<IAuditLogService>();
        var service = new ScanAllowedRangeService(db, audit.Object, CreateOptions(true));

        var result = await service.IsTargetAllowedAsync("8.8.8.8");

        Assert.True(result);
    }

    [Fact]
    public async Task IsTargetAllowedAsync_InternalRange_ReturnsTrue()
    {
        var db = CreateDbContext();
        db.ScanAllowedRanges.Add(new ScanAllowedRange
        {
            RangeName = "Internal",
            Cidr = "192.168.1.0/24",
            Description = "Test",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var audit = new Mock<IAuditLogService>();
        var service = new ScanAllowedRangeService(db, audit.Object, CreateOptions());

        var result = await service.IsTargetAllowedAsync("192.168.1.100");

        Assert.True(result);
    }

    [Fact]
    public async Task IsTargetAllowedAsync_ExternalRange_ReturnsFalse()
    {
        var db = CreateDbContext();
        db.ScanAllowedRanges.Add(new ScanAllowedRange
        {
            RangeName = "Internal",
            Cidr = "10.0.0.0/8",
            Description = "Test",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var audit = new Mock<IAuditLogService>();
        var service = new ScanAllowedRangeService(db, audit.Object, CreateOptions());

        var result = await service.IsTargetAllowedAsync("8.8.8.8");

        Assert.False(result);
    }
}
