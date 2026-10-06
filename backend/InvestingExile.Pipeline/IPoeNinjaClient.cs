namespace InvestingExile.Pipeline;

/// <summary>
/// Fetches raw poe.ninja currency-exchange JSON for one type. The seam that
/// lets the double-run test (issue #6) feed a saved fixture body instead of
/// calling the live API.
/// </summary>
public interface IPoeNinjaClient
{
    Task<string> GetExchangeOverviewAsync(string league, string type, CancellationToken cancellationToken = default);
}
