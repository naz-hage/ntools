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
    public void Constructor_AddsCloneSubcommandWithUrlAndPathOptions()
    {
        var command = new RepositoryCommand(_verboseOption, dryRunOption: new Option<bool>("--dry-run"));
        var cloneCmd = Assert.Single(command.Subcommands, s => s.Name == "clone");

        Assert.NotNull(cloneCmd.Options.FirstOrDefault(o => o.Name == "--url"));
        Assert.NotNull(cloneCmd.Options.FirstOrDefault(o => o.Name == "--path"));
    }

    [Fact]
    public void Constructor_AddsTagSubcommandWithNestedCommands()
    {
        var command = new RepositoryCommand(_verboseOption, dryRunOption: new Option<bool>("--dry-run"));
        var tagCmd = Assert.Single(command.Subcommands, s => s.Name == "tag");
        var nestedNames = tagCmd.Subcommands.Select(s => s.Name).ToList();

        Assert.Equal(new[] { "auto", "delete", "push-auto", "set" }, nestedNames);
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
    public void Constructor_AddsInfoSubcommand()
    {
        var command = new RepositoryCommand(_verboseOption);
        var infoCmd = Assert.Single(command.Subcommands, s => s.Name == "info");
        Assert.NotNull(infoCmd);
    }

    [Fact]
    public void Constructor_RegistersSubcommandsInAlphabeticalOrder()
    {
        var command = new RepositoryCommand(_verboseOption);
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();
        Assert.Equal(new[] { "clone", "create", "delete", "info", "list", "tag" }, subcommandNames);
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
        var descOption = createCmd.Options.FirstOrDefault(o =>
            o.Name.Contains("description"));
        Assert.NotNull(descOption);
    }

    [Fact]
    public void CreateSubcommand_HasPrivateOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var privateOption = createCmd.Options.FirstOrDefault(o =>
            o.Name.Contains("private"));
        Assert.NotNull(privateOption);
    }

    [Fact]
    public void DeleteSubcommand_HasForceOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var deleteCmd = command.Subcommands.First(s => s.Name == "delete");
        var forceOption = deleteCmd.Options.FirstOrDefault(o =>
            o.Name.Contains("force"));
        Assert.NotNull(forceOption);
    }

    [Fact]
    public void ListSubcommand_HasTopOption()
    {
        var command = new RepositoryCommand(_verboseOption);
        var listCmd = command.Subcommands.First(s => s.Name == "list");
        var topOption = listCmd.Options.FirstOrDefault(o =>
            o.Name.Contains("top"));
        Assert.NotNull(topOption);
    }
}

