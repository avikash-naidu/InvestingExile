namespace InvestingExile.Tests;

public class ComposeDatabaseTests
{
    [Fact]
    public void Compose_file_runs_postgres_16_with_database_investingexile()
    {
        var composePath = RepoFiles.FindAboveTestOutput("docker-compose.yml");
        var text = File.ReadAllText(composePath);

        Assert.Contains("image: postgres:16", text);
        Assert.Contains("POSTGRES_DB: investingexile", text);
    }
}
