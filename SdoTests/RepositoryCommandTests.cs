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
    public void Constructor_AddsCreateSubcommand()
    {
        var command = new RepositoryCommand(_verboseOption);
        var createCmd = Assert.Single(command.Subcommands, s => s.Name == "create");
        Assert.NotNull(createCmd);
    }

    [Fact]
    public void Constructor_AddsDeleteSubcommand()
    {
        var command = new RepositoryCommand(_verboseOption);
        var deleteCmd = Assert.Single(command.Subcommands, s => s.Name == "delete");
        Assert.NotNull(deleteCmd);
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

    [Fact]
    public void Constructor_RegistersSubcommandsInAlphabeticalOrder()
    {
        var command = new RepositoryCommand(_verboseOption);
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();
        Assert.Equal(new[] { "create", "delete", "list", "show" }, subcommandNames);
    }

    [Fact]
    public void CreateSubcommand_HasNameArgument()
    {
        var command = new RepositoryCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var nameArg = createCmd.Arguments.FirstOrDefault(a => a.Name == "name");
        Assert.NotNull(nameArg);
    }

    [Fact]
    public void CreateSubcommand_HasOptionalDescriptionOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var descOption = createCmd.Options.FirstOrDefault(o => o.Name == "description");
        Assert.NotNull(descOption);
    }

    [Fact]
    public void CreateSubcommand_HasPrivateOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var privateOption = createCmd.Options.FirstOrDefault(o => o.Name == "private");
        Assert.NotNull(privateOption);
    }

    [Fact]
    public void DeleteSubcommand_HasForceOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var deleteCmd = command.Subcommands.First(s => s.Name == "delete");
        var forceOption = deleteCmd.Options.FirstOrDefault(o => o.Name == "force");
        Assert.NotNull(forceOption);
    }

    [Fact]
    public void ListSubcommand_HasTopOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var listCmd = command.Subcommands.First(s => s.Name == "list");
        var topOption = listCmd.Options.FirstOrDefault(o => o.Name == "top");
        Assert.NotNull(topOption);
    }
}

