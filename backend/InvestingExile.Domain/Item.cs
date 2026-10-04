namespace InvestingExile.Domain;

// Temporary stand-in until issue #4 adds the EF Core model.
public class Item
{
    public string Category { get; set; } = "";

    public string Name { get; set; } = "";

    public string Variant { get; set; } = "";

    public string DetailsId { get; set; } = "";
}
