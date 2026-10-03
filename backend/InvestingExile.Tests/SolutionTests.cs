namespace InvestingExile.Tests;

public class SolutionTests
{
    [Fact]
    public void Solution_lists_domain_pipeline_api_and_tests()
    {
        var solutionPath = FindSolutionFile();
        var text = File.ReadAllText(solutionPath);

        string[] projects =
        [
            """InvestingExile.Domain\InvestingExile.Domain.csproj""",
            """InvestingExile.Pipeline\InvestingExile.Pipeline.csproj""",
            """InvestingExile.Api\InvestingExile.Api.csproj""",
            """InvestingExile.Tests\InvestingExile.Tests.csproj"""
        ];

        foreach (var project in projects)
        {
            Assert.Contains(project, text);
        }

        var projectLines = text.Split('\n').Count(line => line.StartsWith("Project(", StringComparison.Ordinal));
        Assert.Equal(projects.Length, projectLines);
    }

    private static string FindSolutionFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "InvestingExile.sln");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("InvestingExile.sln was not found above the test output directory.");
    }
}
