namespace InvestingExile.Tests;

public class PipelineProjectTests
{
    [Fact]
    public void Pipeline_is_a_console_app_with_the_ingest_command()
    {
        var projectPath = FindPipelineProject();
        var text = File.ReadAllText(projectPath);

        Assert.Contains("Sdk=\"Microsoft.NET.Sdk\"", text);
        Assert.DoesNotContain("Microsoft.NET.Sdk.Web", text);
        Assert.Contains("<OutputType>Exe</OutputType>", text);
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", text);
        Assert.Contains("InvestingExile.Domain.csproj", text);

        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var sources = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false
                        && path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false)
            .Select(File.ReadAllText)
            .ToArray();

        var combined = string.Join('\n', sources);
        Assert.Contains("class Program", combined);
        Assert.Contains("poe.ninja", combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpClient", combined);
        Assert.Contains("--league", combined);

        // The ingest command and patch-note ingest stay separate.
        Assert.DoesNotContain("PatchChange", combined);
    }

    private static string FindPipelineProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "InvestingExile.Pipeline", "InvestingExile.Pipeline.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("InvestingExile.Pipeline.csproj was not found above the test output directory.");
    }
}
