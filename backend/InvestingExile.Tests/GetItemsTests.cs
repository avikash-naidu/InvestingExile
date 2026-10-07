using System.Net;
using System.Net.Http.Json;
using InvestingExile.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace InvestingExile.Tests;

public class GetItemsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:InvestingExile", _postgres.GetConnectionString());
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Get_items_returns_each_items_latest_snapshot_and_does_not_cache()
    {
        var earlier = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var latest = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

        await using (var db = CreateContext())
        {
            var oldLeague = new League { Name = "Old" };
            var newLeague = new League { Name = "New" };
            var exalted = new Item
            {
                Category = "Currency",
                Name = "Exalted Orb",
                Variant = "",
                DetailsId = "exalted-orb",
            };
            var mirror = new Item
            {
                Category = "Currency",
                Name = "Mirror of Kalandra",
                Variant = "foil",
                DetailsId = "mirror-of-kalandra",
            };
            var unsold = new Item
            {
                Category = "Currency",
                Name = "Portal Scroll",
                Variant = "",
                DetailsId = "portal-scroll",
            };
            db.AddRange(oldLeague, newLeague, exalted, mirror, unsold);
            await db.SaveChangesAsync();

            db.PriceSnapshots.AddRange(
                new PriceSnapshot
                {
                    LeagueId = oldLeague.Id,
                    ItemId = exalted.Id,
                    HourBucket = earlier,
                    ChaosValue = 10m,
                    DivineValue = 0.01m,
                    ListingCount = 3,
                    Icon = "/gen/image/old.png",
                    Sparkline = [1m],
                },
                new PriceSnapshot
                {
                    LeagueId = newLeague.Id,
                    ItemId = exalted.Id,
                    HourBucket = latest,
                    ChaosValue = 25m,
                    DivineValue = 0.08m,
                    ListingCount = 9,
                    Icon = "/gen/image/exalted.png",
                    Sparkline = [4.5m, -2m, 0m],
                },
                new PriceSnapshot
                {
                    LeagueId = newLeague.Id,
                    ItemId = mirror.Id,
                    HourBucket = latest,
                    ChaosValue = 3m,
                    DivineValue = null,
                    ListingCount = null,
                    Sparkline = [],
                });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);

        var items = await response.Content.ReadFromJsonAsync<List<ItemSnapshotBody>>();
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);

        var exaltedBody = Assert.Single(items, item => item.Name == "Exalted Orb");
        Assert.Equal("Currency", exaltedBody.Category);
        Assert.Equal("", exaltedBody.Variant);
        Assert.Equal("/gen/image/exalted.png", exaltedBody.Icon);
        Assert.Equal(25m, exaltedBody.ChaosValue);
        Assert.Equal(0.08m, exaltedBody.DivineValue);
        Assert.Equal(9, exaltedBody.ListingCount);
        Assert.Equal([4.5m, -2m, 0m], exaltedBody.Sparkline);
        Assert.Equal(latest, exaltedBody.SnapshotHour);

        var mirrorBody = Assert.Single(items, item => item.Name == "Mirror of Kalandra");
        Assert.Equal("foil", mirrorBody.Variant);
        Assert.Null(mirrorBody.Icon);
        Assert.Equal(3m, mirrorBody.ChaosValue);
        Assert.Null(mirrorBody.DivineValue);
        Assert.Null(mirrorBody.ListingCount);
        Assert.Empty(mirrorBody.Sparkline);
        Assert.Equal(latest, mirrorBody.SnapshotHour);

        Assert.DoesNotContain(items, item => item.Name == "Portal Scroll");
        Assert.DoesNotContain(items, item => item.ChaosValue == 10m);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.Migrate();
        return db;
    }

    private sealed record ItemSnapshotBody(
        string Name,
        string Category,
        string Variant,
        string? Icon,
        decimal ChaosValue,
        decimal? DivineValue,
        int? ListingCount,
        List<decimal> Sparkline,
        DateTimeOffset SnapshotHour);
}
