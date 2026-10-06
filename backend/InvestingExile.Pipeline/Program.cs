using InvestingExile.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestingExile.Pipeline;

// Price ingest command (issue #5):
//   dotnet run --project InvestingExile.Pipeline -- --league "<name>"
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var league = ParseLeague(args);
        if (league is null)
        {
            Console.Error.WriteLine(
                "Usage: dotnet run --project InvestingExile.Pipeline -- --league \"<name>\"");
            return 1;
        }

        var connectionString = Environment.GetEnvironmentVariable("INVESTINGEXILE_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=investingexile;Username=investingexile;Password=investingexile";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var db = new AppDbContext(options);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("InvestingExile/1.0");

        var service = new PriceIngestService(db, new PoeNinjaHttpClient(http));
        await service.IngestAsync(league);

        Console.WriteLine($"Ingested prices for league '{league}' at the current UTC hour.");
        return 0;
    }

    private static string? ParseLeague(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--league")
            {
                return string.IsNullOrWhiteSpace(args[i + 1]) ? null : args[i + 1];
            }
        }

        return null;
    }
}
