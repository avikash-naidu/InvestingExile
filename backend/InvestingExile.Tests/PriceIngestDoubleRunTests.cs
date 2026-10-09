using InvestingExile.Domain;
using InvestingExile.Pipeline;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace InvestingExile.Tests;

public class PriceIngestDoubleRunTests : IAsyncLifetime
{
    private const string LeagueName = "Fixture";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Second_run_in_the_same_hour_updates_the_snapshot()
    {
        var started = TruncateToUtcHour(DateTimeOffset.UtcNow);
        var fixture = File.ReadAllText(FindFixture());
        var client = new SequencePoeNinjaClient(fixture);

        await IngestAsync(client);
        await IngestAsync(client);

        var finished = TruncateToUtcHour(DateTimeOffset.UtcNow);
        Assert.Equal(started, finished);

        await using var db = CreateContext();
        var league = Assert.Single(await db.Leagues.ToListAsync());
        Assert.Equal(LeagueName, league.Name);

        var item = Assert.Single(await db.Items.ToListAsync());
        Assert.Equal("Currency", item.Category);
        Assert.Equal("Exalted Orb", item.Name);
        Assert.Equal("", item.Variant);
        Assert.Equal("exalted-orb", item.DetailsId);

        var snapshot = Assert.Single(await db.PriceSnapshots.ToListAsync());
        Assert.Equal(league.Id, snapshot.LeagueId);
        Assert.Equal(item.Id, snapshot.ItemId);
        Assert.Equal(started, snapshot.HourBucket);
        Assert.Equal(25m, snapshot.ChaosValue);
        Assert.Null(snapshot.DivineValue);
        Assert.Null(snapshot.ListingCount);
    }

    [Fact]
    public async Task Two_runs_at_once_on_an_empty_schema_both_finish_with_one_row_per_key()
    {
        await using (var db = CreateContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                """DROP SCHEMA public CASCADE; CREATE SCHEMA public;""");
        }

        var started = TruncateToUtcHour(DateTimeOffset.UtcNow);
        var client = new FetchGate(File.ReadAllText(FindFixture()), runs: 2);

        await Task.WhenAll(
            IngestAsync(client, CreateUnmigratedContext),
            IngestAsync(client, CreateUnmigratedContext));

        await AssertOneRowPerKeyAsync(started);
    }

    [Fact]
    public async Task Two_runs_at_once_on_a_current_schema_both_finish_with_one_row_per_key()
    {
        var started = TruncateToUtcHour(DateTimeOffset.UtcNow);
        var client = new FetchGate(File.ReadAllText(FindFixture()), runs: 2);

        await Task.WhenAll(
            IngestAsync(client, CreateContext),
            IngestAsync(client, CreateContext));

        await AssertOneRowPerKeyAsync(started);
    }

    private async Task AssertOneRowPerKeyAsync(DateTimeOffset started)
    {
        var finished = TruncateToUtcHour(DateTimeOffset.UtcNow);
        Assert.Equal(started, finished);

        await using var db = CreateContext();
        var league = Assert.Single(await db.Leagues.ToListAsync());
        Assert.Equal(LeagueName, league.Name);

        var item = Assert.Single(await db.Items.ToListAsync());
        Assert.Equal("Currency", item.Category);
        Assert.Equal("Exalted Orb", item.Name);
        Assert.Equal("", item.Variant);

        var snapshot = Assert.Single(await db.PriceSnapshots.ToListAsync());
        Assert.Equal(league.Id, snapshot.LeagueId);
        Assert.Equal(item.Id, snapshot.ItemId);
        Assert.Equal(started, snapshot.HourBucket);
        Assert.Equal(10m, snapshot.ChaosValue);
    }

    private Task IngestAsync(IPoeNinjaClient client) => IngestAsync(client, CreateContext);

    private static async Task IngestAsync(IPoeNinjaClient client, Func<AppDbContext> createContext)
    {
        await using var db = createContext();
        var service = new PriceIngestService(db, client);
        await service.IngestAsync(LeagueName);
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
                "currency-overview.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("currency-overview.json was not found above the test output directory.");
    }

    private static DateTimeOffset TruncateToUtcHour(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    private sealed class SequencePoeNinjaClient : IPoeNinjaClient
    {
        private readonly string _firstBody;
        private readonly string _secondBody;
        private int _currencyCalls;

        public SequencePoeNinjaClient(string fixture)
        {
            _firstBody = fixture;
            _secondBody = fixture.Replace("\"primaryValue\": 10", "\"primaryValue\": 25", StringComparison.Ordinal);
        }

        public Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default)
        {
            if (type != "Currency")
            {
                return Task.FromResult("""{"lines":[],"items":[]}""");
            }

            var body = _currencyCalls == 0 ? _firstBody : _secondBody;
            _currencyCalls++;
            return Task.FromResult(body);
        }
    }

    /// <summary>
    /// Returns the fixture for Currency and an empty overview for every other
    /// type. On the last exchange type it waits until every run has reached it,
    /// so the runs hit the database together.
    /// </summary>
    private sealed class FetchGate : IPoeNinjaClient
    {
        private readonly string _fixture;
        private readonly int _runs;
        private readonly TaskCompletionSource _allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public FetchGate(string fixture, int runs)
        {
            _fixture = fixture;
            _runs = runs;
        }

        public async Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default)
        {
            if (type == PriceIngestService.ExchangeTypes[^1])
            {
                if (Interlocked.Increment(ref _arrived) == _runs)
                {
                    _allArrived.SetResult();
                }

                await _allArrived.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }

            return type == "Currency" ? _fixture : """{"lines":[],"items":[]}""";
        }
    }
}
