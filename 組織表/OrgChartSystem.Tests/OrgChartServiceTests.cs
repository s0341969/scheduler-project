using Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrgChartSystem.Contracts;
using OrgChartSystem.Data;
using OrgChartSystem.Models;
using OrgChartSystem.Services;

namespace OrgChartSystem.Tests;

public class OrgChartServiceTests
{
    [Fact]
    public async Task Reposition_SameParent_ShouldReorder()
    {
        await using var scope = await CreateServiceScopeAsync();
        var service = scope.Service;
        var db = scope.Db;

        var root = new OrgNode { DepartmentName = "Root", SortOrder = 0 };
        var a = new OrgNode { DepartmentName = "A", Parent = root, SortOrder = 0 };
        var b = new OrgNode { DepartmentName = "B", Parent = root, SortOrder = 1 };
        var c = new OrgNode { DepartmentName = "C", Parent = root, SortOrder = 2 };

        db.OrgNodes.AddRange(root, a, b, c);
        await db.SaveChangesAsync();

        var moved = await service.RepositionNodeAsync(c.Id, root.Id, 0, "tester", "editor");

        Assert.True(moved);

        var siblings = await db.OrgNodes
            .Where(x => x.ParentId == root.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.DepartmentName)
            .ToListAsync();

        Assert.Equal(["C", "A", "B"], siblings);
    }

    [Fact]
    public async Task Reposition_ToDescendant_ShouldThrow()
    {
        await using var scope = await CreateServiceScopeAsync();
        var service = scope.Service;
        var db = scope.Db;

        var root = new OrgNode { DepartmentName = "Root", SortOrder = 0 };
        var child = new OrgNode { DepartmentName = "Child", Parent = root, SortOrder = 0 };

        db.OrgNodes.AddRange(root, child);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RepositionNodeAsync(root.Id, child.Id, 0, "tester", "editor"));
    }

    [Fact]
    public async Task Search_ShouldFindByDepartment()
    {
        await using var scope = await CreateServiceScopeAsync();
        var service = scope.Service;
        var db = scope.Db;

        db.OrgNodes.AddRange(
            new OrgNode { DepartmentName = "資訊部", PersonName = "王小明", SortOrder = 0 },
            new OrgNode { DepartmentName = "財務部", PersonName = "陳小美", SortOrder = 1 });
        await db.SaveChangesAsync();

        var result = await service.SearchAsync("資訊", 10);

        Assert.Single(result);
        Assert.Equal("資訊部", result[0].DepartmentName);
    }

    [Fact]
    public async Task PreviewImport_ShouldReturnCounts()
    {
        await using var scope = await CreateServiceScopeAsync();
        var service = scope.Service;
        var db = scope.Db;

        db.OrgNodes.Add(new OrgNode { DepartmentName = "A", SortOrder = 0 });
        await db.SaveChangesAsync();

        var preview = await service.PreviewImportAsync(new ImportOrgChartRequest
        {
            DisplayMode = "person",
            Nodes =
            [
                new ImportOrgNodeRequest
                {
                    DepartmentName = "Root",
                    Children =
                    [
                        new ImportOrgNodeRequest { DepartmentName = "Child" }
                    ]
                }
            ]
        });

        Assert.Equal(1, preview.CurrentNodeCount);
        Assert.Equal(2, preview.IncomingNodeCount);
        Assert.Equal(1, preview.RootCount);
        Assert.Equal(2, preview.MaxDepth);
    }

    private static async Task<TestScope> CreateServiceScopeAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=orgchart.test.db",
                ["Backup:Directory"] = "test-backups"
            })
            .Build();

        var service = new OrgChartService(db, config);
        return new TestScope(service, db, connection);
    }

    private sealed class TestScope(OrgChartService service, AppDbContext db, SqliteConnection connection) : IAsyncDisposable
    {
        public OrgChartService Service { get; } = service;

        public AppDbContext Db { get; } = db;

        public SqliteConnection Connection { get; } = connection;

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}

