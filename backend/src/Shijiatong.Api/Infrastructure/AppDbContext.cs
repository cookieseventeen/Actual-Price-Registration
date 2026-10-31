using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<District> Districts => Set<District>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<CrawlTask> CrawlTasks => Set<CrawlTask>();
    public DbSet<LvrStagingRecord> LvrStagingRecords => Set<LvrStagingRecord>();
    public DbSet<LvrImportRun> LvrImportRuns => Set<LvrImportRun>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<District>(e =>
        {
            e.ToTable("districts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(32);
            e.Property(x => x.AvgUnitPrice).HasPrecision(6, 2);
            e.Property(x => x.ChangePct).HasPrecision(5, 2);
        });

        b.Entity<Transaction>(e =>
        {
            e.ToTable("transactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(32);
            e.Property(x => x.Unit).HasPrecision(6, 2);
            e.Property(x => x.Ping).HasPrecision(7, 2);
            e.HasOne(x => x.District)
                .WithMany(d => d.Transactions)
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.DistrictId);
            e.HasIndex(x => x.Community);
            e.HasIndex(x => x.Date);
        });

        b.Entity<Member>(e =>
        {
            e.ToTable("members");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Provider).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Plan).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Role)
                .HasConversion(
                    v => v == MemberRole.Admin ? "admin" : "member",
                    v => v.Equals("admin", StringComparison.OrdinalIgnoreCase) ? MemberRole.Admin : MemberRole.Member)
                .HasMaxLength(16);
        });

        b.Entity<CrawlTask>(e =>
        {
            e.ToTable("crawl_tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(32);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        });

        b.Entity<LvrStagingRecord>(e =>
        {
            e.ToTable("lvr_staging_records");
            e.HasKey(x => x.SerialNo);
            e.Property(x => x.SerialNo).HasMaxLength(32);
            e.Property(x => x.County).HasMaxLength(4);
            e.Property(x => x.Kind).HasMaxLength(4);
            e.Property(x => x.District).HasMaxLength(16);
            e.Property(x => x.Fields).HasColumnType("jsonb");
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.Property(x => x.FirstSource).HasMaxLength(32);
            e.Property(x => x.LastSource).HasMaxLength(32);
            e.HasIndex(x => x.District);
            e.HasIndex(x => x.LastRunId);
        });

        b.Entity<LvrImportRun>(e =>
        {
            e.ToTable("lvr_import_runs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Source).HasMaxLength(32);
            e.Property(x => x.Url).HasMaxLength(512);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.StartedAt);
        });
    }
}
