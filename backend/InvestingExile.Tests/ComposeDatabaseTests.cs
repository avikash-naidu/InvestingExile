namespace InvestingExile.Tests;

public class ComposeDatabaseTests
{
    [Fact]
    public void Compose_file_runs_postgres_16_with_database_investingexile()
    {
        var composePath = FindComposeFile();
        var text = File.ReadAllText(composePath);

        Assert.Contains("image: postgres:16", text);
        Assert.Contains("POSTGRES_DB: investingexile", text);
    }

    private static string FindComposeFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "docker-compose.yml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("docker-compose.yml was not found above the test output directory.");
    }
}
