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
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var db = new AppDbContext(options);
        db.Database.Migrate();
        return db;
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
