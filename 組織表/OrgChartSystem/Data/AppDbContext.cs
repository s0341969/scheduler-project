using Microsoft.EntityFrameworkCore;
using OrgChartSystem.Models;

namespace OrgChartSystem.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OrgNode> OrgNodes => Set<OrgNode>();

    public DbSet<OrgChartSetting> OrgChartSettings => Set<OrgChartSetting>();

    public DbSet<OrgChartSnapshot> OrgChartSnapshots => Set<OrgChartSnapshot>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrgNode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.DepartmentName).HasMaxLength(128);
            entity.Property(x => x.PersonName).HasMaxLength(64);
            entity.Property(x => x.Title).HasMaxLength(64);
            entity.Property(x => x.Email).HasMaxLength(128);
            entity.Property(x => x.Phone).HasMaxLength(32);

            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParentId, x.SortOrder });
        });

        modelBuilder.Entity<OrgChartSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayMode).HasMaxLength(16);
        });

        modelBuilder.Entity<OrgChartSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(128);
            entity.Property(x => x.Actor).HasMaxLength(64);
            entity.Property(x => x.DataJson).HasColumnType("TEXT");
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(64);
            entity.Property(x => x.Actor).HasMaxLength(64);
            entity.Property(x => x.Role).HasMaxLength(32);
            entity.Property(x => x.Detail).HasMaxLength(2048);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.NodeId);
        });
    }
}
