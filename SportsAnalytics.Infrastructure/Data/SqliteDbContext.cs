using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;

namespace SportsAnalytics.Infrastructure.Data;

/// <summary>
/// DbContext الرئيسي للبيانات المنظمة (SQLite).
/// يحتوي على كل الجداول الأساسية مع العلاقات والـ Indexes.
/// </summary>
public class SqliteDbContext : DbContext
{
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchStatistics> MatchStatistics => Set<MatchStatistics>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Team ──
        modelBuilder.Entity<Team>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.Country).HasMaxLength(100);
            e.Property(t => t.League).HasMaxLength(100);
            e.HasIndex(t => t.Name);
        });

        // ── Player ──
        modelBuilder.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(150);
            e.Property(p => p.Position).HasMaxLength(10);
            e.HasOne(p => p.Team)
             .WithMany()
             .HasForeignKey(p => p.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.TeamId);
        });

        // ── Match ──
        modelBuilder.Entity<Match>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.League).HasMaxLength(100);
            e.Property(m => m.Season).HasMaxLength(20);

            // علاقة المضيف
            e.HasOne(m => m.HomeTeam)
             .WithMany()
             .HasForeignKey(m => m.HomeTeamId)
             .OnDelete(DeleteBehavior.Restrict);

            // علاقة الضيف
            e.HasOne(m => m.AwayTeam)
             .WithMany()
             .HasForeignKey(m => m.AwayTeamId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(m => m.MatchDate);
            e.HasIndex(m => new { m.HomeTeamId, m.AwayTeamId });
        });

        // ── MatchStatistics ──
        modelBuilder.Entity<MatchStatistics>(e =>
        {
            e.HasKey(ms => ms.Id);
            e.Property(ms => ms.DataSource).HasMaxLength(100);
            e.HasOne(ms => ms.Match)
             .WithMany()
             .HasForeignKey(ms => ms.MatchId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(ms => ms.MatchId).IsUnique();
        });

        // ── Prediction ──
        modelBuilder.Entity<Prediction>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.ModelVersion).HasMaxLength(50);
            e.HasOne(p => p.Match)
             .WithMany()
             .HasForeignKey(p => p.MatchId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.MatchId);
            e.HasIndex(p => p.CreatedAt);
        });
    }
}
