using System.Reflection;
using InvestingExile.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace InvestingExile.Tests;

public class PriceModelTests
{
    [Fact]
    public void Price_model_keys_items_by_category_name_and_variant_and_snapshots_by_league_item_and_hour()
    {
        using var context = CreateContext();

        var league = context.Model.FindEntityType(typeof(League));
        var item = context.Model.FindEntityType(typeof(Item));
        var snapshot = context.Model.FindEntityType(typeof(PriceSnapshot));

        Assert.NotNull(league);
        Assert.NotNull(item);
        Assert.NotNull(snapshot);
        var entityTypes = context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToArray();
        Assert.Equal(3, entityTypes.Length);
        Assert.Contains(typeof(League), entityTypes);
        Assert.Contains(typeof(Item), entityTypes);
        Assert.Contains(typeof(PriceSnapshot), entityTypes);

        Assert.Equal(nameof(League.Id), league!.FindPrimaryKey()!.Properties.Single().Name);
        var leagueName = Assert.Single(league.GetIndexes(), index => index.IsUnique);
        Assert.Equal([nameof(League.Name)], leagueName.Properties.Select(property => property.Name).ToArray());

        Assert.Equal(nameof(Item.Id), item!.FindPrimaryKey()!.Properties.Single().Name);
        var itemIdentity = Assert.Single(item.GetIndexes(), index => index.IsUnique);
        Assert.Equal(
            [nameof(Item.Category), nameof(Item.Name), nameof(Item.Variant)],
            itemIdentity.Properties.Select(property => property.Name).ToArray());
        Assert.NotNull(item.FindProperty(nameof(Item.DetailsId)));
        Assert.DoesNotContain(
            item.GetIndexes(),
            index => index.Properties.Any(property => property.Name == nameof(Item.DetailsId)));

        Assert.Equal(
            [nameof(PriceSnapshot.LeagueId), nameof(PriceSnapshot.ItemId), nameof(PriceSnapshot.HourBucket)],
            snapshot!.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray());
        var hourConverter = snapshot.FindProperty(nameof(PriceSnapshot.HourBucket))!.GetValueConverter();
        Assert.NotNull(hourConverter);
        var storedHour = (DateTimeOffset)hourConverter!.ConvertToProvider(
            new DateTimeOffset(2026, 10, 4, 22, 37, 15, TimeSpan.FromHours(13)))!;
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero), storedHour);
        Assert.False(snapshot.FindProperty(nameof(PriceSnapshot.ChaosValue))!.IsNullable);
        Assert.True(snapshot.FindProperty(nameof(PriceSnapshot.DivineValue))!.IsNullable);
        Assert.True(snapshot.FindProperty(nameof(PriceSnapshot.ListingCount))!.IsNullable);
    }

    [Fact]
    public void One_migration_creates_league_item_and_price_snapshot_tables()
    {
        var migrationTypes = typeof(AppDbContext).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Migration)) && type.IsAbstract == false)
            .ToArray();
        var migrationType = Assert.Single(migrationTypes);
        var migration = (Migration)Activator.CreateInstance(migrationType)!;
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(Migration)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, [builder]);

        var tables = builder.Operations.OfType<CreateTableOperation>().ToArray();
        Assert.Equal(
            ["Items", "Leagues", "PriceSnapshots"],
            tables.Select(table => table.Name).OrderBy(name => name).ToArray());

        var leagues = tables.Single(table => table.Name == "Leagues");
        Assert.Equal([nameof(League.Id)], leagues.PrimaryKey!.Columns);
        Assert.Contains(leagues.Columns, column => column.Name == nameof(League.Name));

        var items = tables.Single(table => table.Name == "Items");
        Assert.Equal([nameof(Item.Id)], items.PrimaryKey!.Columns);
        Assert.Contains(items.Columns, column => column.Name == nameof(Item.DetailsId));

        var snapshots = tables.Single(table => table.Name == "PriceSnapshots");
        Assert.Equal(
            [nameof(PriceSnapshot.LeagueId), nameof(PriceSnapshot.ItemId), nameof(PriceSnapshot.HourBucket)],
            snapshots.PrimaryKey!.Columns);
        Assert.Contains(snapshots.Columns, column => column.Name == nameof(PriceSnapshot.ChaosValue));
        Assert.Contains(snapshots.Columns, column => column.Name == nameof(PriceSnapshot.DivineValue));
        Assert.Contains(snapshots.Columns, column => column.Name == nameof(PriceSnapshot.ListingCount));
        Assert.Contains(
            snapshots.CheckConstraints,
            constraint => constraint.Sql.Contains("date_trunc('hour'", StringComparison.Ordinal));

        var uniqueIndexes = builder.Operations.OfType<CreateIndexOperation>().Where(index => index.IsUnique).ToArray();
        Assert.Contains(uniqueIndexes, index => index.Table == "Leagues" && index.Columns.SequenceEqual([nameof(League.Name)]));
        Assert.Contains(
            uniqueIndexes,
            index => index.Table == "Items" && index.Columns.SequenceEqual([nameof(Item.Category), nameof(Item.Name), nameof(Item.Variant)]));
        Assert.DoesNotContain(uniqueIndexes, index => index.Columns.Contains(nameof(Item.DetailsId)));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=investingexile;Username=investingexile;Password=investingexile")
            .Options;
        return new AppDbContext(options);
    }
}
