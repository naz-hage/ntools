using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Sdo.Commands;

namespace SdoTests;

public class PullRequestCommandTests
{
    private readonly Option<bool> _verboseOption;

    public PullRequestCommandTests()
    {
        _verboseOption = new Option<bool>("--verbose");
    }

    /// <summary>
    /// Captures console output for testing error messages
    /// </summary>
    private class ConsoleOutputCapture : IDisposable
    {
        private readonly StringWriter _stringWriter;
        private readonly TextWriter _originalOut;

        public ConsoleOutputCapture()
        {
            _stringWriter = new StringWriter();
            _originalOut = Console.Out;
            Console.SetOut(_stringWriter);
        }

        public string GetOutput() => _stringWriter.ToString();

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            _stringWriter?.Dispose();
        }
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectName()
    {
        var command = new PullRequestCommand(_verboseOption);
        Assert.Equal("pr", command.Name);
    }

    [Fact]
    public void Constructor_AddsCreateSubcommand()
    {
        var command = new PullRequestCommand(_verboseOption);
        var createCmd = Assert.Single(command.Subcommands, s => s.Name == "create");
        Assert.NotNull(createCmd);
    }

    [Fact]
    public void Constructor_AddsLsSubcommand()
    {
        var command = new PullRequestCommand(_verboseOption);
        var lsCmd = Assert.Single(command.Subcommands, s => s.Name == "list");
        Assert.NotNull(lsCmd);
    }

    [Fact]
    public void Constructor_AddsShowSubcommand()
    {
        var command = new PullRequestCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");
        Assert.NotNull(showCmd);
    }

    [Fact]
    public void Constructor_AddsStatusSubcommand()
    {
        var command = new PullRequestCommand(_verboseOption);
        var statusCmd = Assert.Single(command.Subcommands, s => s.Name == "status");
        Assert.NotNull(statusCmd);
    }

    [Fact]
    public void Constructor_AddsUpdateSubcommand()
    {
        var command = new PullRequestCommand(_verboseOption);
        var updateCmd = Assert.Single(command.Subcommands, s => s.Name == "update");
        Assert.NotNull(updateCmd);
    }

    [Fact]
    public void Constructor_RegistersSubcommandsInAlphabeticalOrder()
    {
        var command = new PullRequestCommand(_verboseOption);
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();
        Assert.Equal(new[] { "create", "list", "show", "status", "update" }, subcommandNames);
    }

    [Fact]
    public void CreateSubcommand_HasFileOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var fileOption = createCmd.Options.FirstOrDefault(o => o.Name.Contains("file"));
        Assert.NotNull(fileOption);
    }

    [Fact]
    public void CreateSubcommand_HasWorkItemOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var workItemOption = createCmd.Options.FirstOrDefault(o => o.Name.Contains("work-item"));
        Assert.NotNull(workItemOption);
    }

    [Fact]
    public void CreateSubcommand_HasDraftOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var draftOption = createCmd.Options.FirstOrDefault(o => o.Name.Contains("draft"));
        Assert.NotNull(draftOption);
    }

    [Fact]
    public void CreateSubcommand_HasDryRunOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var dryRunOption = createCmd.Options.FirstOrDefault(o => o.Name.Contains("dry-run"));
        Assert.NotNull(dryRunOption);
    }

    [Fact]
    public void LsSubcommand_HasStatusOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var lsCmd = command.Subcommands.First(s => s.Name == "list");
        var statusOption = lsCmd.Options.FirstOrDefault(o => o.Name.Contains("status"));
        Assert.NotNull(statusOption);
    }

    [Fact]
    public void LsSubcommand_HasTopOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var lsCmd = command.Subcommands.First(s => s.Name == "list");
        var topOption = lsCmd.Options.FirstOrDefault(o => o.Name.Contains("top"));
        Assert.NotNull(topOption);
    }

    [Fact]
    public void ShowSubcommand_HasPrIdArgument()
    {
        var command = new PullRequestCommand(_verboseOption);
        var showCmd = command.Subcommands.First(s => s.Name == "show");
        var prIdArg = showCmd.Arguments.FirstOrDefault(a => a.Name == "pr-id");
        Assert.NotNull(prIdArg);
    }

    [Fact]
    public void StatusSubcommand_HasPrNumberArgument()
    {
        var command = new PullRequestCommand(_verboseOption);
        var statusCmd = command.Subcommands.First(s => s.Name == "status");
        var prArg = statusCmd.Arguments.FirstOrDefault(a => a.Name == "pr-id");
        Assert.NotNull(prArg);
    }

    [Fact]
    public void UpdateSubcommand_HasPrIdOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var updateCmd = command.Subcommands.First(s => s.Name == "update");
        var prIdOption = updateCmd.Options.FirstOrDefault(o => o.Name.Contains("pr-id"));
        Assert.NotNull(prIdOption);
    }

    [Fact]
    public void UpdateSubcommand_HasFileOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var updateCmd = command.Subcommands.First(s => s.Name == "update");
        var fileOption = updateCmd.Options.FirstOrDefault(o => o.Name.Contains("file"));
        Assert.NotNull(fileOption);
    }

    [Fact]
    public void UpdateSubcommand_HasTitleOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var updateCmd = command.Subcommands.First(s => s.Name == "update");
        var titleOption = updateCmd.Options.FirstOrDefault(o => o.Name.Contains("title"));
        Assert.NotNull(titleOption);
    }

    [Fact]
    public void UpdateSubcommand_HasStatusOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var updateCmd = command.Subcommands.First(s => s.Name == "update");
        var statusOption = updateCmd.Options.FirstOrDefault(o => o.Name.Contains("status"));
        Assert.NotNull(statusOption);
    }

    // Error Handling Tests for PR ID Validation

    [Fact]
    public async Task UpdatePullRequest_WithZeroPrId_ReturnsErrorCode()
    {
        using (new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("UpdatePullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { 0, null, null, null, false })!;
            var result = await task;

            // Should return error code 1
            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task UpdatePullRequest_WithNegativePrId_ReturnsErrorCode()
    {
        using (new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("UpdatePullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { -1, null, null, null, false })!;
            var result = await task;

            // Should return error code 1
            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task UpdatePullRequest_WithZeroPrId_DisplaysHelpfulErrorMessage()
    {
        using (var capture = new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("UpdatePullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { 0, null, null, null, false })!;
            await task;

            var output = capture.GetOutput();
            
            // Verify error message contains key information
            Assert.Contains("PR ID is required", output);
            Assert.Contains("--pr-id", output);
            Assert.Contains("Example:", output);
            Assert.Contains("sdo pr update --pr-id 123", output);
        }
    }

    [Fact]
    public async Task ShowPullRequest_WithZeroPrId_ReturnsErrorCode()
    {
        using (new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("ShowPullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { 0, false })!;
            var result = await task;

            // Should return error code 1
            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task ShowPullRequest_WithNegativePrId_ReturnsErrorCode()
    {
        using (new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("ShowPullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { -1, false })!;
            var result = await task;

            // Should return error code 1
            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task ShowPullRequest_WithZeroPrId_DisplaysHelpfulErrorMessage()
    {
        using (var capture = new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("ShowPullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { 0, false })!;
            await task;

            var output = capture.GetOutput();
            
            // Verify error message contains key information
            Assert.Contains("PR ID is required", output);
            Assert.Contains("positional argument", output);
            Assert.Contains("--pr-id", output);
            Assert.Contains("Example:", output);
            Assert.Contains("sdo pr show 123", output);
        }
    }

    [Fact]
    public async Task UpdatePullRequest_WithZeroPrId_ShowsExamples()
    {
        using (var capture = new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("UpdatePullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { 0, null, null, null, false })!;
            await task;

            var output = capture.GetOutput();
            
            // Verify both examples are shown
            Assert.Contains("sdo pr update --pr-id 123 -f ./pr-message.md", output);
            Assert.Contains("sdo pr update --pr-id 123 --title", output);
        }
    }

    [Fact]
    public async Task ShowPullRequest_WithZeroPrId_ShowsExamples()
    {
        using (var capture = new ConsoleOutputCapture())
        {
            var command = new PullRequestCommand(_verboseOption);
            var method = typeof(PullRequestCommand).GetMethod("ShowPullRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<int>)method.Invoke(command, new object[] { 0, false })!;
            await task;

            var output = capture.GetOutput();
            
            // Verify both examples are shown
            Assert.Contains("sdo pr show 123", output);
            Assert.Contains("sdo pr show --pr-id 123", output);
        }
    }
}

