using System.Text.Json;
using InvestingExile.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestingExile.Pipeline;

/// <summary>
/// Reads poe.ninja currency-exchange prices for one league across every
/// exchange type and upserts League, Item, and PriceSnapshot rows for the
/// current UTC hour. Re-running in the same hour updates the snapshot in place;
/// it never inserts a second item or snapshot.
/// </summary>
public sealed class PriceIngestService
{
    // poe.ninja's PoE1 currency-exchange types. There is no types endpoint, so
    // this mirrors the enum in poe.ninja's API docs and needs a bump when a
    // patch adds or retires a mechanic type (AllflameEmber, Tattoo, Runegraft,
    // and the like are league-specific). Gear, uniques, gems, and maps are a
    // different, individually-priced endpoint and are out of scope here.
    public static readonly IReadOnlyList<string> ExchangeTypes = new[]
    {
        "Currency", "Fragment", "Runegraft", "AllflameEmber", "Tattoo", "Omen",
        "DjinnCoin", "Ducat", "EnshroudingCrystal", "DivinationCard", "Artifact",
        "Oil", "DeliriumOrb", "Scarab", "Astrolabe", "Fossil", "Resonator", "Essence",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly AppDbContext _db;
    private readonly IPoeNinjaClient _client;

    public PriceIngestService(AppDbContext db, IPoeNinjaClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task IngestAsync(string league, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(league))
        {
            throw new ArgumentException("League is required.", nameof(league));
        }

        var hourBucket = TruncateToUtcHour(DateTimeOffset.UtcNow);

        // Fetch everything before touching the database so a failed request
        // cannot leave a half-written set of snapshots.
        var rows = new List<IngestRow>();
        var coreSeeded = false;

        foreach (var type in ExchangeTypes)
        {
            var json = await _client.GetExchangeOverviewAsync(league, type, cancellationToken);
            ParseExchange(type, json, rows, ref coreSeeded);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var leagueEntity = await UpsertLeagueAsync(league, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await UpsertItemsAndSnapshotsAsync(leagueEntity.Id, hourBucket, rows, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<League> UpsertLeagueAsync(string name, CancellationToken cancellationToken)
    {
        var league = await _db.Leagues.FirstOrDefaultAsync(l => l.Name == name, cancellationToken);
        if (league is null)
        {
            league = new League { Name = name };
            _db.Leagues.Add(league);
        }

        return league;
    }

    private async Task UpsertItemsAndSnapshotsAsync(
        int leagueId,
        DateTimeOffset hourBucket,
        IReadOnlyCollection<IngestRow> rows,
        CancellationToken cancellationToken)
    {
        var existingItems = await _db.Items.ToDictionaryAsync(
            item => (item.Category, item.Name, item.Variant),
            cancellationToken);

        foreach (var row in rows)
        {
            var key = (row.Category, row.Name, row.Variant);
            if (!existingItems.TryGetValue(key, out var item))
            {
                item = new Item
                {
                    Category = row.Category,
                    Name = row.Name,
                    Variant = row.Variant,
                };
                _db.Items.Add(item);
                existingItems[key] = item;
            }

            item.DetailsId = row.DetailsId;
        }

        // Items need their generated ids before snapshots can reference them.
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var row in rows)
        {
            var item = existingItems[(row.Category, row.Name, row.Variant)];
            var snapshot = await _db.PriceSnapshots.FindAsync(
                new object[] { leagueId, item.Id, hourBucket }, cancellationToken);

            if (snapshot is null)
            {
                snapshot = new PriceSnapshot
                {
                    LeagueId = leagueId,
                    ItemId = item.Id,
                    HourBucket = hourBucket,
                };
                _db.PriceSnapshots.Add(snapshot);
            }

            snapshot.ChaosValue = row.ChaosValue;
            snapshot.DivineValue = row.DivineValue;
            snapshot.Icon = row.Icon;
            snapshot.Sparkline = row.Sparkline;

            // The exchange exposes trade volume, not a listing count. Left null
            // for A1; the liquidity signal is revisited in slice B (rec a).
            snapshot.ListingCount = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ParseExchange(string type, string json, List<IngestRow> rows, ref bool coreSeeded)
    {
        var overview = JsonSerializer.Deserialize<ExchangeOverview>(json, JsonOptions)
            ?? new ExchangeOverview();

        // divine per chaos. primaryValue is denominated in chaos (core.primary).
        var divineRate = overview.Core?.Rates?.Divine;

        var metaById = new Dictionary<string, ItemMeta>(StringComparer.Ordinal);
        foreach (var meta in overview.Items)
        {
            if (!string.IsNullOrEmpty(meta.Id))
            {
                metaById[meta.Id] = meta;
            }
        }

        // Chaos Orb and Divine Orb live in core.items, not in any type's lines.
        // Seed them once (chaos is the unit; divine is 1/rate chaos).
        if (!coreSeeded && overview.Core is { } core)
        {
            foreach (var coreItem in core.Items)
            {
                if (coreItem.Id == "chaos")
                {
                    rows.Add(MakeRow(coreItem, type, chaosValue: 1m, divineRate, sparkline: null));
                }
                else if (coreItem.Id == "divine" && divineRate is > 0m)
                {
                    rows.Add(MakeRow(coreItem, type, chaosValue: 1m / divineRate.Value, divineRate, sparkline: null));
                }
            }

            coreSeeded = true;
        }

        foreach (var line in overview.Lines)
        {
            if (string.IsNullOrEmpty(line.Id))
            {
                continue;
            }

            metaById.TryGetValue(line.Id, out var meta);
            rows.Add(MakeRow(meta, type, line.PrimaryValue, divineRate, line.Sparkline?.Data, fallbackId: line.Id));
        }
    }

    private static IngestRow MakeRow(
        ItemMeta? meta,
        string type,
        decimal chaosValue,
        decimal? divineRate,
        IReadOnlyList<decimal>? sparkline,
        string? fallbackId = null)
    {
        var id = meta?.Id ?? fallbackId ?? "";
        return new IngestRow(
            // Prefer poe.ninja's own category (for example Catalysts under the
            // Currency type); fall back to the exchange type queried.
            Category: string.IsNullOrEmpty(meta?.Category) ? type : meta!.Category!,
            Name: string.IsNullOrEmpty(meta?.Name) ? id : meta!.Name!,
            Variant: "",
            DetailsId: string.IsNullOrEmpty(meta?.DetailsId) ? id : meta!.DetailsId!,
            Icon: string.IsNullOrEmpty(meta?.Image) ? null : meta!.Image,
            ChaosValue: chaosValue,
            DivineValue: divineRate is > 0m ? chaosValue * divineRate.Value : null,
            Sparkline: sparkline is null ? [] : sparkline.ToArray());
    }

    private static DateTimeOffset TruncateToUtcHour(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    private sealed record IngestRow(
        string Category,
        string Name,
        string Variant,
        string DetailsId,
        string? Icon,
        decimal ChaosValue,
        decimal? DivineValue,
        decimal[] Sparkline);

    private sealed class ExchangeOverview
    {
        public Core? Core { get; set; }

        public List<ExchangeLine> Lines { get; set; } = new();

        public List<ItemMeta> Items { get; set; } = new();
    }

    private sealed class Core
    {
        public List<ItemMeta> Items { get; set; } = new();

        public Rates? Rates { get; set; }

        public string? Primary { get; set; }

        public string? Secondary { get; set; }
    }

    private sealed class Rates
    {
        public decimal? Divine { get; set; }
    }

    private sealed class ExchangeLine
    {
        public string? Id { get; set; }

        public decimal PrimaryValue { get; set; }

        public SparklineBody? Sparkline { get; set; }
    }

    private sealed class SparklineBody
    {
        public List<decimal>? Data { get; set; }
    }

    private sealed class ItemMeta
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? DetailsId { get; set; }

        public string? Category { get; set; }

        public string? Image { get; set; }
    }
}
