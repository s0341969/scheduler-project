using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            dbContext.Users.AddRange(
                new User
                {
                    Account = "admin",
                    UserName = "系統管理員",
                    Email = "admin@local",
                    RoleName = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Account = "secmgr",
                    UserName = "資安主管",
                    Email = "security@local",
                    RoleName = "SecurityManager",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Account = "scanner",
                    UserName = "弱掃分析員",
                    Email = "scanner@local",
                    RoleName = "Scanner",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Account = "viewer",
                    UserName = "稽核檢視者",
                    Email = "viewer@local",
                    RoleName = "Viewer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
        }

        if (!await dbContext.ScanAllowedRanges.AnyAsync(cancellationToken))
        {
            dbContext.ScanAllowedRanges.Add(new ScanAllowedRange
            {
                RangeName = "內網預設",
                Cidr = "10.0.0.0/8",
                Description = "預設允許 10.x.x.x 內網掃描範圍",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
