namespace InvestingExile.Domain;

// Temporary stand-in until issue #4 adds the EF Core model.
public class PriceSnapshot
{
    public int LeagueId { get; set; }

    public int ItemId { get; set; }

    public DateTimeOffset HourBucket { get; set; }

    public decimal ChaosValue { get; set; }

    public decimal? DivineValue { get; set; }

    public int? ListingCount { get; set; }
}
