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
        var workflows = RepoFiles.FindAboveTestOutput(Path.Combine(".github", "workflows"));
        var files = Directory.GetFiles(workflows, "*.yml");
        Assert.Single(files);
        return files[0];
    }
}
