namespace InvestingExile.Tests;

public class XunitProjectTests
{
    [Fact]
    public void Xunit_project_has_no_double_run_fixture()
    {
        var projectPath = FindTestsProject();
        var text = File.ReadAllText(projectPath);

        Assert.Contains("<IsTestProject>true</IsTestProject>", text);
        Assert.Contains("Include=\"xunit\"", text);
        Assert.Contains("Include=\"Microsoft.NET.Test.Sdk\"", text);
        Assert.Contains("Include=\"xunit.runner.visualstudio\"", text);
        Assert.DoesNotContain("Testcontainers", text);

        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var files = Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false
                        && path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) == false);

        Assert.DoesNotContain(files, path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindTestsProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "InvestingExile.Tests", "InvestingExile.Tests.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("InvestingExile.Tests.csproj was not found above the test output directory.");
    }
}
