using InvestingExile.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestingExile.Api;

public static class LatestItemSnapshots
{
    public static async Task<IReadOnlyList<ItemSnapshot>> ListAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var snapshots = await db.PriceSnapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Item)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(snapshot => snapshot.ItemId)
            .Select(group => group
                .OrderByDescending(snapshot => snapshot.HourBucket)
                .ThenByDescending(snapshot => snapshot.LeagueId)
                .First())
            .Select(snapshot => new ItemSnapshot(
                snapshot.Item.Name,
                snapshot.Item.Category,
                snapshot.Item.Variant,
                snapshot.Icon,
                snapshot.ChaosValue,
                snapshot.DivineValue,
                snapshot.ListingCount,
                snapshot.Sparkline,
                snapshot.HourBucket))
            .ToArray();
    }
}

public sealed record ItemSnapshot(
    string Name,
    string Category,
    string Variant,
    string? Icon,
    decimal ChaosValue,
    decimal? DivineValue,
    int? ListingCount,
    decimal[] Sparkline,
    DateTimeOffset SnapshotHour);
