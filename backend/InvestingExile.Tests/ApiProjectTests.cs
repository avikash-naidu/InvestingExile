namespace InvestingExile.Tests;

public class ApiProjectTests
{
    [Fact]
    public void Api_is_the_http_read_host()
    {
        var projectPath = FindApiProject();
        var text = File.ReadAllText(projectPath);

        Assert.Contains("Sdk=\"Microsoft.NET.Sdk.Web\"", text);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", text);
        Assert.Contains("InvestingExile.Domain.csproj", text);

        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var sources = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false
                        && path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false)
            .Select(File.ReadAllText)
            .ToArray();

        var combined = string.Join('\n', sources);
        Assert.Contains("WebApplication", combined);
        Assert.Contains("MapGet(\"/items\"", combined);
        Assert.DoesNotContain("weatherforecast", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindApiProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "InvestingExile.Api", "InvestingExile.Api.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("InvestingExile.Api.csproj was not found above the test output directory.");
    }
}
