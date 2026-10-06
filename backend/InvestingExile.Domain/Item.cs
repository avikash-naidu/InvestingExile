namespace InvestingExile.Domain;

public class Item
{
    public int Id { get; set; }

    public string Category { get; set; } = "";

    public string Name { get; set; } = "";

    public string Variant { get; set; } = "";

    public string DetailsId { get; set; } = "";

    public List<PriceSnapshot> PriceSnapshots { get; set; } = [];
}
