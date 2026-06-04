using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Models;

namespace VulnScan.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<ScanAllowedRange> ScanAllowedRanges => Set<ScanAllowedRange>();

    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();

    public DbSet<ScanRun> ScanRuns => Set<ScanRun>();

    public DbSet<AssetPort> AssetPorts => Set<AssetPort>();

    public DbSet<Vulnerability> Vulnerabilities => Set<Vulnerability>();

    public DbSet<VulnerabilityAction> VulnerabilityActions => Set<VulnerabilityAction>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ReportExport> ReportExports => Set<ReportExport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(item => item.Account)
            .IsUnique();

        modelBuilder.Entity<Asset>()
            .HasIndex(item => item.IPAddress);

        modelBuilder.Entity<ScanAllowedRange>()
            .HasIndex(item => item.Cidr);

        modelBuilder.Entity<ScanRun>()
            .HasOne(item => item.ScanJob)
            .WithMany(item => item.ScanRuns)
            .HasForeignKey(item => item.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssetPort>()
            .HasOne(item => item.Asset)
            .WithMany(item => item.AssetPorts)
            .HasForeignKey(item => item.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vulnerability>()
            .HasOne(item => item.Asset)
            .WithMany(item => item.Vulnerabilities)
            .HasForeignKey(item => item.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VulnerabilityAction>()
            .HasOne(item => item.Vulnerability)
            .WithMany(item => item.Actions)
            .HasForeignKey(item => item.VulnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
