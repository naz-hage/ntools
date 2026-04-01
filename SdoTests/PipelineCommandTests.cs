// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PipelineCommandTests.cs
//
// Unit tests for PipelineCommand - pipeline/workflow management operations.

using System.CommandLine;
using Xunit;
using Sdo.Commands;

namespace SdoTests;

/// <summary>
/// Unit tests for PipelineCommand covering all subcommands and options.
/// </summary>
public class PipelineCommandTests
{
    private readonly Option<bool> _verboseOption;

    public PipelineCommandTests()
    {
        _verboseOption = new Option<bool>("--verbose");
    }

    #region Command Structure Tests

    [Fact]
    public void Constructor_CreatesCommandWithCorrectName()
    {
        var command = new PipelineCommand(_verboseOption);
        Assert.Equal("pipeline", command.Name);
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectDescription()
    {
        var command = new PipelineCommand(_verboseOption);
        Assert.NotNull(command.Description);
        Assert.Contains("Pipeline", command.Description);
    }

    [Fact]
    public void Constructor_Has9Subcommands()
    {
        var command = new PipelineCommand(_verboseOption);
        Assert.Equal(9, command.Subcommands.Count);
    }

    [Fact]
    public void Constructor_RegistersSubcommandsInAlphabeticalOrder()
    {
        var command = new PipelineCommand(_verboseOption);
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();
        var expected = new[] { "create", "delete", "lastbuild", "list", "logs", "run", "show", "status", "update" };
        Assert.Equal(expected, subcommandNames);
    }

    #endregion

    #region Individual Subcommand Tests

    [Fact]
    public void Constructor_AddsCreateSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var createCmd = Assert.Single(command.Subcommands, s => s.Name == "create");
        Assert.NotNull(createCmd);
    }

    [Fact]
    public void Constructor_AddsDeleteSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var deleteCmd = Assert.Single(command.Subcommands, s => s.Name == "delete");
        Assert.NotNull(deleteCmd);
    }

    [Fact]
    public void Constructor_AddsLastbuildSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var lastbuildCmd = Assert.Single(command.Subcommands, s => s.Name == "lastbuild");
        Assert.NotNull(lastbuildCmd);
    }

    [Fact]
    public void Constructor_AddsListSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");
        Assert.NotNull(listCmd);
    }

    [Fact]
    public void Constructor_AddsLogsSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var logsCmd = Assert.Single(command.Subcommands, s => s.Name == "logs");
        Assert.NotNull(logsCmd);
    }

    [Fact]
    public void Constructor_AddsRunSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var runCmd = Assert.Single(command.Subcommands, s => s.Name == "run");
        Assert.NotNull(runCmd);
    }

    [Fact]
    public void Constructor_AddsShowSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");
        Assert.NotNull(showCmd);
    }

    [Fact]
    public void Constructor_AddsStatusSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var statusCmd = Assert.Single(command.Subcommands, s => s.Name == "status");
        Assert.NotNull(statusCmd);
    }

    [Fact]
    public void Constructor_AddsUpdateSubcommand()
    {
        var command = new PipelineCommand(_verboseOption);
        var updateCmd = Assert.Single(command.Subcommands, s => s.Name == "update");
        Assert.NotNull(updateCmd);
    }

    #endregion

    #region Subcommand Options Tests

    [Fact]
    public void CreateSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        Assert.NotEmpty(createCmd.Options);
    }

    [Fact]
    public void CreateSubcommand_HasFileArgument()
    {
        var command = new PipelineCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        Assert.NotEmpty(createCmd.Arguments);
    }

    [Fact]
    public void CreateCommand_CanBeCalledWithFilePath()
    {
        var command = new PipelineCommand(_verboseOption);
        var createCmd = command.Subcommands.First(s => s.Name == "create");
        var fileArg = createCmd.Arguments.FirstOrDefault(a => a.Name.Contains("file"));
        Assert.NotNull(fileArg);
    }

    [Fact]
    public void DeleteSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var deleteCmd = command.Subcommands.First(s => s.Name == "delete");
        // Should have force option and verbose option
        Assert.True(deleteCmd.Options.Count >= 2);
    }

    [Fact]
    public void DeleteSubcommand_HasPipelineIdArgument()
    {
        var command = new PipelineCommand(_verboseOption);
        var deleteCmd = command.Subcommands.First(s => s.Name == "delete");
        Assert.NotEmpty(deleteCmd.Arguments);
    }

    [Fact]
    public void DeleteCommand_HasForceOption()
    {
        var command = new PipelineCommand(_verboseOption);
        var deleteCmd = command.Subcommands.First(s => s.Name == "delete");
        // Verify force option exists (can be checked by name containing "force")
        var hasForceOption = deleteCmd.Options.Any(o => o.Name.Contains("force"));
        Assert.True(hasForceOption);
    }

    [Fact]
    public void LastbuildSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var lastbuildCmd = command.Subcommands.First(s => s.Name == "lastbuild");
        Assert.NotEmpty(lastbuildCmd.Options);
    }

    [Fact]
    public void LastbuildSubcommand_HasPipelineNameArgument()
    {
        var command = new PipelineCommand(_verboseOption);
        var lastbuildCmd = command.Subcommands.First(s => s.Name == "lastbuild");
        Assert.NotEmpty(lastbuildCmd.Arguments);
    }

    [Fact]
    public void ListSubcommand_HasMultipleOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var listCmd = command.Subcommands.First(s => s.Name == "list");
        // List should have: verbose, repo, and all options
        Assert.True(listCmd.Options.Count >= 3);
    }

    [Fact]
    public void ListCommand_NoRequiredArguments()
    {
        var command = new PipelineCommand(_verboseOption);
        var listCmd = command.Subcommands.First(s => s.Name == "list");
        // List command should have no required arguments
        var requiredArgs = listCmd.Arguments.Where(a => a.Arity.MinimumNumberOfValues > 0).ToList();
        Assert.Empty(requiredArgs);
    }

    [Fact]
    public void ListCommand_CanBeCalledWithoutOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var listCmd = command.Subcommands.First(s => s.Name == "list");
        Assert.NotNull(listCmd);
        Assert.NotEmpty(listCmd.Options);
    }

    [Fact]
    public void LogsSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var logsCmd = command.Subcommands.First(s => s.Name == "logs");
        Assert.NotEmpty(logsCmd.Options);
    }

    [Fact]
    public void RunSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var runCmd = command.Subcommands.First(s => s.Name == "run");
        // Should have branch option and verbose option
        Assert.True(runCmd.Options.Count >= 2);
    }

    [Fact]
    public void ShowSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var showCmd = command.Subcommands.First(s => s.Name == "show");
        Assert.NotEmpty(showCmd.Options);
    }

    [Fact]
    public void ShowCommand_NoArguments()
    {
        var command = new PipelineCommand(_verboseOption);
        var showCmd = command.Subcommands.First(s => s.Name == "show");
        // Show command should have no required arguments
        var requiredArgs = showCmd.Arguments.Where(a => a.Arity.MinimumNumberOfValues > 0).ToList();
        Assert.Empty(requiredArgs);
    }

    [Fact]
    public void StatusSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var statusCmd = command.Subcommands.First(s => s.Name == "status");
        Assert.NotEmpty(statusCmd.Options);
    }

    [Fact]
    public void UpdateSubcommand_HasOptions()
    {
        var command = new PipelineCommand(_verboseOption);
        var updateCmd = command.Subcommands.First(s => s.Name == "update");
        Assert.NotEmpty(updateCmd.Options);
    }

    #endregion
}
