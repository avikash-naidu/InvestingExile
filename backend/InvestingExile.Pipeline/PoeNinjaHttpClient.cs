namespace InvestingExile.Pipeline;

/// <summary>
/// Live poe.ninja currency-exchange client. Targets the 2026 economy API; the
/// old poe.ninja/api/data/* endpoints are gone. See reference.md.
/// </summary>
public sealed class PoeNinjaHttpClient : IPoeNinjaClient
{
    private const string ExchangeUrl =
        "https://poe.ninja/poe1/api/economy/exchange/current/overview";

    private readonly HttpClient _http;

    public PoeNinjaHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default)
    {
        var url = $"{ExchangeUrl}?league={Uri.EscapeDataString(league)}&type={Uri.EscapeDataString(type)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
