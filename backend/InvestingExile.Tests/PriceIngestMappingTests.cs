using InvestingExile.Domain;
using InvestingExile.Pipeline;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace InvestingExile.Tests;

public class PriceIngestMappingTests : IAsyncLifetime
{
    private const string LeagueName = "Fixture";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Divine_rate_converts_chaos_into_divine_value()
    {
        await ResetAsync();
        var body = File.ReadAllText(FindFixture());
        var client = new BodyPoeNinjaClient(body, everyType: false);

        await IngestAsync(client);

        await using var db = CreateContext();
        var exalted = await SnapshotForAsync(db, "Exalted Orb");
        Assert.Equal(10m, exalted.ChaosValue);
        Assert.Equal(0.04m, exalted.DivineValue);
    }

    [Fact]
    public async Task Ingest_applies_migrations_after_the_schema_is_dropped()
    {
        await using (var db = CreateContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                """DROP SCHEMA public CASCADE; CREATE SCHEMA public;""");
        }

        var body = File.ReadAllText(FindFixture());
        await using (var db = CreateUnmigratedContext())
        {
            var service = new PriceIngestService(db, new BodyPoeNinjaClient(body, everyType: false));
            await service.IngestAsync(LeagueName);

            var exalted = await SnapshotForAsync(db, "Exalted Orb");
            Assert.Equal(10m, exalted.ChaosValue);
            Assert.Equal(0.04m, exalted.DivineValue);
        }
    }

    [Fact]
    public async Task Chaos_and_divine_orbs_are_seeded_from_a_later_core_when_the_first_omits_them()
    {
        await ResetAsync();
        var body = File.ReadAllText(FindFixture()).Replace("\"category\": \"Currency\"", "\"category\": \"\"", StringComparison.Ordinal);
        var client = new LaterCorePoeNinjaClient(body);

        await IngestAsync(client);

        await using var db = CreateContext();
        var chaosItem = Assert.Single(await db.Items.Where(item => item.Name == "Chaos Orb").ToListAsync());
        Assert.Equal("Currency", chaosItem.Category);
        var chaos = Assert.Single(await db.PriceSnapshots.Where(snapshot => snapshot.ItemId == chaosItem.Id).ToListAsync());
        Assert.Equal(1m, chaos.ChaosValue);
        Assert.Equal(0.004m, chaos.DivineValue);

        var divineItem = Assert.Single(await db.Items.Where(item => item.Name == "Divine Orb").ToListAsync());
        Assert.Equal("Currency", divineItem.Category);
        var divine = Assert.Single(await db.PriceSnapshots.Where(snapshot => snapshot.ItemId == divineItem.Id).ToListAsync());
        Assert.Equal(250m, divine.ChaosValue);
        Assert.Equal(1m, divine.DivineValue);

        Assert.Equal(1, await db.Items.CountAsync(item => item.Name == "Chaos Orb"));
        Assert.Equal(1, await db.Items.CountAsync(item => item.Name == "Divine Orb"));
    }

    [Fact]
    public async Task Chaos_and_divine_orbs_are_seeded_once_across_every_exchange_type()
    {
        await ResetAsync();
        var body = File.ReadAllText(FindFixture());
        var client = new BodyPoeNinjaClient(body, everyType: true);

        await IngestAsync(client);

        Assert.Equal(PriceIngestService.ExchangeTypes.Count, client.Calls);

        await using var db = CreateContext();
        var chaos = await SnapshotForAsync(db, "Chaos Orb");
        Assert.Equal(1m, chaos.ChaosValue);
        Assert.Equal(0.004m, chaos.DivineValue);

        var divine = await SnapshotForAsync(db, "Divine Orb");
        Assert.Equal(250m, divine.ChaosValue);
        Assert.Equal(1m, divine.DivineValue);

        Assert.Equal(1, await db.Items.CountAsync(item => item.Name == "Chaos Orb"));
        Assert.Equal(1, await db.Items.CountAsync(item => item.Name == "Divine Orb"));
    }

    [Fact]
    public async Task Exchange_line_stores_the_icon_and_sparkline_points()
    {
        await ResetAsync();
        const string body = """
            {
              "lines": [
                {
                  "id": "exalted",
                  "primaryValue": 10,
                  "sparkline": { "totalChange": -1.5, "data": [1.0, 2.5, -1.5] }
                }
              ],
              "items": [
                {
                  "id": "exalted",
                  "name": "Exalted Orb",
                  "detailsId": "exalted-orb",
                  "category": "Currency",
                  "image": "/gen/image/exalted.png"
                }
              ]
            }
            """;
        var client = new BodyPoeNinjaClient(body, everyType: false);

        await IngestAsync(client);

        await using var db = CreateContext();
        var exalted = await SnapshotForAsync(db, "Exalted Orb");
        Assert.Equal("/gen/image/exalted.png", exalted.Icon);
        Assert.Equal(new decimal?[] { 1.0m, 2.5m, -1.5m }, exalted.Sparkline);
    }

    [Fact]
    public async Task A_null_sparkline_point_is_kept_in_the_series()
    {
        await ResetAsync();
        const string body = """
            {
              "lines": [
                {
                  "id": "exalted",
                  "primaryValue": 10,
                  "sparkline": { "totalChange": -2.0, "data": [null, 1.5, -2.0] }
                }
              ],
              "items": [
                {
                  "id": "exalted",
                  "name": "Exalted Orb",
                  "detailsId": "exalted-orb",
                  "category": "Currency"
                }
              ]
            }
            """;

        await IngestAsync(new BodyPoeNinjaClient(body, everyType: false));

        await using var db = CreateContext();
        var exalted = await SnapshotForAsync(db, "Exalted Orb");
        Assert.Equal(new decimal?[] { null, 1.5m, -2.0m }, exalted.Sparkline);
    }

    [Fact]
    public async Task A_failed_fetch_writes_nothing()
    {
        await ResetAsync();
        var body = File.ReadAllText(FindFixture());
        var client = new FailingPoeNinjaClient(body);

        await using (var db = CreateContext())
        {
            var service = new PriceIngestService(db, client);
            await Assert.ThrowsAsync<HttpRequestException>(() => service.IngestAsync(LeagueName));
        }

        await using var verify = CreateContext();
        Assert.Empty(await verify.Leagues.ToListAsync());
        Assert.Empty(await verify.Items.ToListAsync());
        Assert.Empty(await verify.PriceSnapshots.ToListAsync());
    }

    private async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "PriceSnapshots", "Items", "Leagues" RESTART IDENTITY CASCADE;""");
    }

    private async Task IngestAsync(IPoeNinjaClient client)
    {
        await using var db = CreateContext();
        var service = new PriceIngestService(db, client);
        await service.IngestAsync(LeagueName);
    }

    private async Task<PriceSnapshot> SnapshotForAsync(AppDbContext db, string name)
    {
        var item = Assert.Single(await db.Items.Where(candidate => candidate.Name == name).ToListAsync());
        return Assert.Single(await db.PriceSnapshots.Where(snapshot => snapshot.ItemId == item.Id).ToListAsync());
    }

    private AppDbContext CreateContext()
    {
        var db = CreateUnmigratedContext();
        db.Database.Migrate();
        return db;
    }

    private AppDbContext CreateUnmigratedContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    private static string FindFixture()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "InvestingExile.Tests",
                "Fixtures",
                "exchange-currency-core.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("exchange-currency-core.json was not found above the test output directory.");
    }

    private sealed class BodyPoeNinjaClient : IPoeNinjaClient
    {
        private readonly string _body;
        private readonly bool _everyType;

        public BodyPoeNinjaClient(string body, bool everyType)
        {
            _body = body;
            _everyType = everyType;
        }

        public int Calls { get; private set; }

        public Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (_everyType || type == "Currency")
            {
                return Task.FromResult(_body);
            }

            return Task.FromResult("""{"lines":[],"items":[]}""");
        }
    }

    private sealed class LaterCorePoeNinjaClient : IPoeNinjaClient
    {
        private readonly string _body;

        public LaterCorePoeNinjaClient(string body)
        {
            _body = body;
        }

        public Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default)
        {
            if (type == "Currency")
            {
                return Task.FromResult(
                    """{"core":{"rates":{"divine":0.004},"items":[]},"lines":[],"items":[]}""");
            }

            if (type == "Fragment")
            {
                return Task.FromResult(_body);
            }

            return Task.FromResult("""{"lines":[],"items":[]}""");
        }
    }

    private sealed class FailingPoeNinjaClient : IPoeNinjaClient
    {
        private readonly string _body;

        public FailingPoeNinjaClient(string body)
        {
            _body = body;
        }

        public Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default)
        {
            if (type == "Fragment")
            {
                throw new HttpRequestException("poe.ninja request failed.");
            }

            if (type == "Currency")
            {
                return Task.FromResult(_body);
            }

            return Task.FromResult("""{"lines":[],"items":[]}""");
        }
    }
}
