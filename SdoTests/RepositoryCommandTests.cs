using System.CommandLine;
using Xunit;
using Sdo.Commands;

namespace SdoTests;

public class RepositoryCommandTests
{
    private readonly Option<bool> _verboseOption;

    public RepositoryCommandTests()
    {
        _verboseOption = new Option<bool>("--verbose");
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectName()
    {
        var command = new RepositoryCommand(_verboseOption);
        Assert.Equal("repo", command.Name);
    }

    [Fact]
    public void Constructor_AddsListSubcommand()
    {
        var command = new RepositoryCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");
        Assert.NotNull(listCmd);
    }

    [Fact]
    public void Constructor_AddsShowSubcommand()
    {
        var command = new RepositoryCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");
        Assert.NotNull(showCmd);
    }
}
