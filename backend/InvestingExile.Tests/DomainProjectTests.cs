namespace InvestingExile.Tests;

public class DomainProjectTests
{
    [Fact]
    public void Domain_is_a_class_library_with_temporary_price_types()
    {
        var projectPath = FindDomainProject();
        var text = File.ReadAllText(projectPath);

        Assert.Contains("Sdk=\"Microsoft.NET.Sdk\"", text);
        Assert.DoesNotContain("Microsoft.NET.Sdk.Web", text);
        Assert.DoesNotContain("<OutputType>", text);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", text);

        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var sources = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false
                        && path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false)
            .Select(File.ReadAllText)
            .ToArray();

        var combined = string.Join('\n', sources);
        Assert.Contains("class League", combined);
        Assert.Contains("class Item", combined);
        Assert.Contains("class PriceSnapshot", combined);
        Assert.DoesNotContain("class AppDbContext", combined);
    }

    private static string FindDomainProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "InvestingExile.Domain", "InvestingExile.Domain.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("InvestingExile.Domain.csproj was not found above the test output directory.");
    }
}
