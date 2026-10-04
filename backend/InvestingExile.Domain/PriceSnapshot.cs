namespace InvestingExile.Domain;

public class PriceSnapshot
{
    public int LeagueId { get; set; }

    public League League { get; set; } = null!;

    public int ItemId { get; set; }

    public Item Item { get; set; } = null!;

    public DateTimeOffset HourBucket { get; set; }

    public decimal ChaosValue { get; set; }

    public decimal? DivineValue { get; set; }

    public int? ListingCount { get; set; }
}
