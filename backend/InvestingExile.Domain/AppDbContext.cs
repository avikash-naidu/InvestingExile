using Microsoft.EntityFrameworkCore;

namespace InvestingExile.Domain;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<League> Leagues => Set<League>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<League>(entity =>
        {
            entity.Property(league => league.Name).IsRequired();
            entity.HasIndex(league => league.Name).IsUnique();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(item => item.Category).IsRequired();
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Variant).IsRequired();
            entity.Property(item => item.DetailsId).IsRequired();
            entity.HasIndex(item => new { item.Category, item.Name, item.Variant }).IsUnique();
        });

        modelBuilder.Entity<PriceSnapshot>(entity =>
        {
            entity.HasKey(snapshot => new { snapshot.LeagueId, snapshot.ItemId, snapshot.HourBucket });
            entity.Property(snapshot => snapshot.HourBucket)
                .HasConversion(value => TruncateToUtcHour(value), value => value);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_PriceSnapshots_HourBucket",
                "\"HourBucket\" = (date_trunc('hour', \"HourBucket\" AT TIME ZONE 'UTC') AT TIME ZONE 'UTC')"));
            entity.Property(snapshot => snapshot.ChaosValue).HasPrecision(18, 4);
            entity.Property(snapshot => snapshot.DivineValue).HasPrecision(18, 8);
            entity.Property(snapshot => snapshot.Sparkline)
                .HasColumnType("numeric[]")
                .IsRequired();
            entity.HasOne(snapshot => snapshot.League)
                .WithMany(league => league.PriceSnapshots)
                .HasForeignKey(snapshot => snapshot.LeagueId);
            entity.HasOne(snapshot => snapshot.Item)
                .WithMany(item => item.PriceSnapshots)
                .HasForeignKey(snapshot => snapshot.ItemId);
        });
    }

    private static DateTimeOffset TruncateToUtcHour(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }
}
