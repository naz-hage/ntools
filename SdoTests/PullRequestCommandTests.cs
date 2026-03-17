using System.CommandLine;
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
        var lsCmd = Assert.Single(command.Subcommands, s => s.Name == "ls");
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
        Assert.Equal(new[] { "create", "ls", "show", "status", "update" }, subcommandNames);
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
        var lsCmd = command.Subcommands.First(s => s.Name == "ls");
        var statusOption = lsCmd.Options.FirstOrDefault(o => o.Name.Contains("status"));
        Assert.NotNull(statusOption);
    }

    [Fact]
    public void LsSubcommand_HasTopOption()
    {
        var command = new PullRequestCommand(_verboseOption);
        var lsCmd = command.Subcommands.First(s => s.Name == "ls");
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
}

