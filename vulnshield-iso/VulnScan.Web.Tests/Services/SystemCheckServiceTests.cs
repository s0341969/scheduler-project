using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Tests.Services;

public sealed class SystemCheckServiceTests
{
    private static ApplicationDbContext CreateSqliteDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsNucleiStatus()
    {
        using var db = CreateSqliteDbContext();
        var config = new ConfigurationBuilder().Build();
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var nmap = new Mock<INmapService>();
        nmap.Setup(n => n.GetInstallationStatus())
            .Returns(new NmapInstallationStatus { IsInstalled = true, ResolvedPath = "nmap.exe" });

        var nuclei = new Mock<INucleiService>();
        nuclei.Setup(n => n.IsInstalled()).Returns(true);
        nuclei.Setup(n => n.GetInstallPath()).Returns("nuclei.exe");

        var greenbone = new Mock<IGreenboneSettingsService>();
        greenbone.Setup(g => g.GetEffectiveOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GreenboneOptions());

        var service = new SystemCheckService(config, env.Object, db, nmap.Object, nuclei.Object, greenbone.Object);

        var result = await service.GetStatusAsync();

        Assert.NotNull(result);
        Assert.True(result.Nmap.IsInstalled);
        Assert.True(result.Nuclei.IsInstalled);
    }

    [Fact]
    public async Task GetStatusAsync_DetectsNucleiNotInstalled()
    {
        using var db = CreateSqliteDbContext();
        var config = new ConfigurationBuilder().Build();
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        var nmap = new Mock<INmapService>();
        nmap.Setup(n => n.GetInstallationStatus())
            .Returns(new NmapInstallationStatus { IsInstalled = false });

        var nuclei = new Mock<INucleiService>();
        nuclei.Setup(n => n.IsInstalled()).Returns(false);

        var greenbone = new Mock<IGreenboneSettingsService>();
        greenbone.Setup(g => g.GetEffectiveOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GreenboneOptions());

        var service = new SystemCheckService(config, env.Object, db, nmap.Object, nuclei.Object, greenbone.Object);

        var result = await service.GetStatusAsync();

        Assert.NotNull(result);
        Assert.False(result.Nuclei.IsInstalled);
    }
}
