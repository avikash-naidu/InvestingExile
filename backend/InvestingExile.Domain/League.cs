namespace InvestingExile.Domain;

public class League
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public List<PriceSnapshot> PriceSnapshots { get; set; } = [];
}
