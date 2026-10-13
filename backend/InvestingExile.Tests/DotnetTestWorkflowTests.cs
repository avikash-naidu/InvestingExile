namespace InvestingExile.Tests;

public class DotnetTestWorkflowTests
{
    [Fact]
    public void Push_runs_dotnet_test_from_backend()
    {
        var workflowPath = FindOnlyWorkflow();
        var text = File.ReadAllText(workflowPath);

        Assert.Contains("push:", text, StringComparison.Ordinal);
        Assert.Contains("working-directory: backend", text, StringComparison.Ordinal);
        Assert.Contains("dotnet test", text, StringComparison.Ordinal);
        Assert.DoesNotContain("continue-on-error:", text, StringComparison.Ordinal);
    }

    private static string FindOnlyWorkflow()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var workflows = Path.Combine(directory.FullName, ".github", "workflows");
            if (Directory.Exists(workflows))
            {
                var files = Directory.GetFiles(workflows, "*.yml");
                Assert.Single(files);
                return files[0];
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(".github/workflows was not found above the test output directory.");
    }
}
