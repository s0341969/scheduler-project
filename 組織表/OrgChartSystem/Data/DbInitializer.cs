using Microsoft.EntityFrameworkCore;
using OrgChartSystem.Models;

namespace OrgChartSystem.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        await EnsureSchemaUpdatesAsync(db);

        if (!await db.OrgChartSettings.AnyAsync())
        {
            db.OrgChartSettings.Add(new OrgChartSetting
            {
                Id = 1,
                DisplayMode = "person",
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await db.OrgNodes.AnyAsync())
        {
            SeedDefaultNodes(db);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureSchemaUpdatesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS OrgChartSnapshots (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Reason TEXT NOT NULL,
                DataJson TEXT NOT NULL,
                Actor TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS AuditLogs (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Action TEXT NOT NULL,
                NodeId INTEGER NULL,
                Detail TEXT NOT NULL,
                Actor TEXT NOT NULL,
                Role TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """);
    }

    private static void SeedDefaultNodes(AppDbContext db)
    {
        var root = new OrgNode
        {
            Code = "CEO",
            DepartmentName = "總經理室",
            PersonName = "王小明",
            Title = "總經理",
            Email = "ceo@example.com",
            Phone = "02-2345-1000",
            SortOrder = 0
        };

        var hr = new OrgNode
        {
            Code = "HR",
            DepartmentName = "人力資源部",
            PersonName = "陳怡君",
            Title = "協理",
            Email = "hr@example.com",
            Phone = "02-2345-1100",
            Parent = root,
            SortOrder = 0
        };

        var it = new OrgNode
        {
            Code = "IT",
            DepartmentName = "資訊部",
            PersonName = "林政宏",
            Title = "經理",
            Email = "it@example.com",
            Phone = "02-2345-1200",
            Parent = root,
            SortOrder = 1
        };

        var fin = new OrgNode
        {
            Code = "FIN",
            DepartmentName = "財務部",
            PersonName = "黃雅筑",
            Title = "經理",
            Email = "fin@example.com",
            Phone = "02-2345-1300",
            Parent = root,
            SortOrder = 2
        };

        var dev = new OrgNode
        {
            Code = "DEV",
            DepartmentName = "系統開發組",
            PersonName = "張子軒",
            Title = "副理",
            Email = "dev@example.com",
            Phone = "02-2345-1210",
            Parent = it,
            SortOrder = 0
        };

        var ops = new OrgNode
        {
            Code = "OPS",
            DepartmentName = "維運組",
            PersonName = "李佩珊",
            Title = "副理",
            Email = "ops@example.com",
            Phone = "02-2345-1220",
            Parent = it,
            SortOrder = 1
        };

        db.OrgNodes.AddRange(root, hr, it, fin, dev, ops);
    }
}
