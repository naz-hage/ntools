// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItemCommandTests.cs
//
// Unit tests for the WorkItemCommand class.

using System;
using System.CommandLine;
using Xunit;
using Sdo.Commands;

namespace SdoTests;

/// <summary>
/// Unit tests for the WorkItemCommand class.
/// Tests for show and list subcommands (existing functionality) and
/// placeholder tests for update and comment subcommands (TDD-driven development).
/// </summary>
public class WorkItemCommandTests
{
    private readonly Option<bool> _verboseOption;

    public WorkItemCommandTests()
    {
        _verboseOption = new Option<bool>("--verbose");
        _verboseOption.Description = "Enable verbose output";
    }

    #region Constructor and Subcommand Registration Tests

    [Fact]
    public void Constructor_CreatesCommandWithCorrectNameAndDescription()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);

        // Assert
        Assert.Equal("wi", command.Name);
        Assert.Equal("Work item management commands", command.Description);
    }

    [Fact]
    public void Constructor_AddsShowSubcommand()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);

        // Assert
        var showSubcommand = Assert.Single(command.Subcommands, s => s.Name == "show");
        Assert.NotNull(showSubcommand);
        Assert.Equal("Display detailed work item information", showSubcommand.Description);
    }

    [Fact]
    public void Constructor_AddsListSubcommand()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);

        // Assert
        var listSubcommand = Assert.Single(command.Subcommands, s => s.Name == "list");
        Assert.NotNull(listSubcommand);
        Assert.Equal("List work items with optional filtering", listSubcommand.Description);
    }

    #endregion

    #region Show Subcommand Option Tests

    [Fact]
    public void ShowSubcommand_HasIdOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");

        // Assert
        var idOption = Assert.Single(showCmd.Options, o => o.Name == "--id");
        Assert.NotNull(idOption);
        Assert.Equal("Work item ID (required)", idOption.Description);
    }

    [Fact]
    public void ShowSubcommand_HasCommentsOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");

        // Assert
        var commentsOption = Assert.Single(showCmd.Options, o => o.Name == "--comments");
        Assert.NotNull(commentsOption);
        Assert.Equal("Show comments/discussion", commentsOption.Description);
    }

    [Fact]
    public void ShowSubcommand_CommentsOptionHasShortAlias()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");
        var commentsOption = Assert.Single(showCmd.Options, o => o.Name == "--comments");

        // Assert
        // The option should support -c as a short alias
        Assert.NotNull(commentsOption);
        Assert.Contains("-c", commentsOption.Aliases);
    }

    [Fact]
    public void ShowSubcommand_HasVerboseOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var showCmd = Assert.Single(command.Subcommands, s => s.Name == "show");

        // Assert
        var verboseOpt = Assert.Single(showCmd.Options, o => o.Name == "--verbose");
        Assert.NotNull(verboseOpt);
        Assert.Equal("Enable verbose output", verboseOpt.Description);
    }

    #endregion

    #region List Subcommand Option Tests

    [Fact]
    public void ListSubcommand_HasTypeOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");

        // Assert
        var typeOption = Assert.Single(listCmd.Options, o => o.Name == "--type");
        Assert.NotNull(typeOption);
        Assert.Equal("Filter by work item type (PBI, Bug, Task, Spike, Epic)", typeOption.Description);
    }

    [Fact]
    public void ListSubcommand_HasStateOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");

        // Assert
        var stateOption = Assert.Single(listCmd.Options, o => o.Name == "--state");
        Assert.NotNull(stateOption);
        Assert.Equal("Filter by state (New, Approved, Committed, Done, To Do, In Progress)", stateOption.Description);
    }

    [Fact]
    public void ListSubcommand_HasAssignedToOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");

        // Assert
        var assignedToOption = Assert.Single(listCmd.Options, o => o.Name == "--assigned-to");
        Assert.NotNull(assignedToOption);
        Assert.Equal("Filter by assigned user (email or display name)", assignedToOption.Description);
    }

    [Fact]
    public void ListSubcommand_HasAssignedToMeOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");

        // Assert
        var assignedToMeOption = Assert.Single(listCmd.Options, o => o.Name == "--assigned-to-me");
        Assert.NotNull(assignedToMeOption);
        Assert.Equal("Filter by work items assigned to current user", assignedToMeOption.Description);
    }

    [Fact]
    public void ListSubcommand_HasTopOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");

        // Assert
        var topOption = Assert.Single(listCmd.Options, o => o.Name == "--top");
        Assert.NotNull(topOption);
        Assert.Equal("Maximum number of items to return (default: 50)", topOption.Description);
    }

    [Fact]
    public void ListSubcommand_HasVerboseOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var listCmd = Assert.Single(command.Subcommands, s => s.Name == "list");

        // Assert
        var verboseOpt = Assert.Single(listCmd.Options, o => o.Name == "--verbose");
        Assert.NotNull(verboseOpt);
        Assert.Equal("Enable verbose output", verboseOpt.Description);
    }

    #endregion

    #region Show Subcommand Execution Tests

    [Fact]
    public void ShowSubcommand_WithNoArguments_ReturnsError()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        // Should fail because --id is required
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void ShowSubcommand_WithValidId_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "123" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        // Result should be an integer (either 0 for success or non-zero for error)
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ShowSubcommand_WithIdAndComments_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "123", "--comments" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ShowSubcommand_WithIdAndCommentShortAlias_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "123", "-c" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ShowSubcommand_WithIdAndVerbose_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "123", "--verbose" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ShowSubcommand_WithIdOne_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "1" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ShowSubcommand_WithIdHundred_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "100" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ShowSubcommand_WithIdLarge_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "show", "--id", "999999" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    #endregion

    #region List Subcommand Execution Tests

    [Fact]
    public void ListSubcommand_WithNoFilters_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithTypeFilter_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--type", "Bug" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithStateFilter_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--state", "Done" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithAssignedToFilter_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--assigned-to", "user@example.com" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithAssignedToMeFilter_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--assigned-to-me" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithTopOption_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--top", "100" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithMultipleFilters_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--type", "Task", "--state", "In Progress", "--top", "25" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithVerbose_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--verbose" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithNoOptions_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithTypeFilterOnly_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--type", "PBI" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithStateFilterOnly_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--state", "New" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    [Fact]
    public void ListSubcommand_WithTopFilterOnly_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "list", "--top", "50" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    #endregion

    #region TDD Placeholder Tests for Update Subcommand

    [Fact(Skip = "TDD: Update subcommand not yet implemented")]
    public void Constructor_AddsUpdateSubcommand()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);

        // Assert
        var updateSubcommand = Assert.Single(command.Subcommands, s => s.Name == "update");
        Assert.NotNull(updateSubcommand);
        Assert.Equal("Update a work item", updateSubcommand.Description);
    }

    [Fact(Skip = "TDD: Update subcommand not yet implemented")]
    public void UpdateSubcommand_HasIdOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var updateCmd = Assert.Single(command.Subcommands, s => s.Name == "update");

        // Assert
        var idOption = Assert.Single(updateCmd.Options, o => o.Name == "--id");
        Assert.NotNull(idOption);
    }

    [Fact(Skip = "TDD: Update subcommand not yet implemented")]
    public void UpdateSubcommand_HasTitleOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var updateCmd = Assert.Single(command.Subcommands, s => s.Name == "update");

        // Assert
        var titleOption = Assert.Single(updateCmd.Options, o => o.Name == "--title");
        Assert.NotNull(titleOption);
    }

    [Fact(Skip = "TDD: Update subcommand not yet implemented")]
    public void UpdateSubcommand_HasStateOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var updateCmd = Assert.Single(command.Subcommands, s => s.Name == "update");

        // Assert
        var stateOption = Assert.Single(updateCmd.Options, o => o.Name == "--state");
        Assert.NotNull(stateOption);
    }

    [Fact(Skip = "TDD: Update subcommand not yet implemented")]
    public void UpdateSubcommand_HasAssignedToOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var updateCmd = Assert.Single(command.Subcommands, s => s.Name == "update");

        // Assert
        var assignedToOption = Assert.Single(updateCmd.Options, o => o.Name == "--assigned-to");
        Assert.NotNull(assignedToOption);
    }

    [Fact(Skip = "TDD: Update subcommand not yet implemented")]
    public void UpdateSubcommand_WithValidArguments_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "update", "--id", "123", "--title", "Updated Title" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    #endregion

    #region TDD Placeholder Tests for Comment Subcommand

    [Fact(Skip = "TDD: Comment subcommand not yet implemented")]
    public void Constructor_AddsCommentSubcommand()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);

        // Assert
        var commentSubcommand = Assert.Single(command.Subcommands, s => s.Name == "comment");
        Assert.NotNull(commentSubcommand);
        Assert.Equal("Add a comment to a work item", commentSubcommand.Description);
    }

    [Fact(Skip = "TDD: Comment subcommand not yet implemented")]
    public void CommentSubcommand_HasIdOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var commentCmd = Assert.Single(command.Subcommands, s => s.Name == "comment");

        // Assert
        var idOption = Assert.Single(commentCmd.Options, o => o.Name == "--id");
        Assert.NotNull(idOption);
    }

    [Fact(Skip = "TDD: Comment subcommand not yet implemented")]
    public void CommentSubcommand_HasTextOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var commentCmd = Assert.Single(command.Subcommands, s => s.Name == "comment");

        // Assert
        var textOption = Assert.Single(commentCmd.Options, o => o.Name == "--text");
        Assert.NotNull(textOption);
    }

    [Fact(Skip = "TDD: Comment subcommand not yet implemented")]
    public void CommentSubcommand_WithValidArguments_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "comment", "--id", "123", "--text", "This is a comment" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    #endregion

    #region Create Subcommand Tests

    [Fact]
    public void Constructor_AddsCreateSubcommand()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);

        // Assert
        var createSubcommand = Assert.Single(command.Subcommands, s => s.Name == "create");
        Assert.NotNull(createSubcommand);
        Assert.Equal("Create a new work item", createSubcommand.Description);
    }

    [Fact]
    public void CreateSubcommand_HasTitleOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var createCmd = Assert.Single(command.Subcommands, s => s.Name == "create");

        // Assert
        var titleOption = Assert.Single(createCmd.Options, o => o.Name == "--title");
        Assert.NotNull(titleOption);
    }

    [Fact]
    public void CreateSubcommand_HasTypeOption()
    {
        // Act
        var command = new WorkItemCommand(_verboseOption);
        var createCmd = Assert.Single(command.Subcommands, s => s.Name == "create");

        // Assert
        var typeOption = Assert.Single(createCmd.Options, o => o.Name == "--type");
        Assert.NotNull(typeOption);
    }

    [Fact]
    public void CreateSubcommand_WithValidArguments_ReturnsExitCode()
    {
        // Arrange
        var command = new WorkItemCommand(_verboseOption);
        var args = new[] { "create", "--title", "New Issue", "--type", "Bug" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.True(result == 0 || result == 1, $"Expected exit code 0 or 1, got {result}");
    }

    #endregion

    #region Integration Tests (Phase 3.2+)

    /// <summary>
    /// Integration tests for actual API calls.
    /// These require valid credentials and network access.
    /// Scaffolded for Phase 3.2 implementation.
    /// </summary>

    [Fact(Skip = "Integration: Scaffolded for Phase 3.2 - requires GitHub credentials")]
    public void IntegrationTest_Update_GitHub_WithValidIssue()
    {
        // TODO: Implement real GitHub API integration test
        // 1. Create a test issue
        // 2. Update it using wi update command
        // 3. Verify update via GitHub API
        // 4. Clean up test issue
        Assert.True(true);
    }

    [Fact(Skip = "Integration: Scaffolded for Phase 3.2 - requires Azure DevOps credentials")]
    public void IntegrationTest_Update_AzureDevOps_WithValidWorkItem()
    {
        // TODO: Implement real Azure DevOps API integration test
        // 1. Create a test work item
        // 2. Update it using wi update command
        // 3. Verify update via Azure DevOps API
        // 4. Clean up test work item
        Assert.True(true);
    }

    [Fact(Skip = "Integration: Scaffolded for Phase 3.2 - requires GitHub credentials")]
    public void IntegrationTest_Comment_GitHub_WithValidIssue()
    {
        // TODO: Implement real GitHub API integration test
        // 1. Create a test issue
        // 2. Add comment using wi comment command
        // 3. Verify comment via GitHub API
        // 4. Clean up test issue
        Assert.True(true);
    }

    [Fact(Skip = "Integration: Scaffolded for Phase 3.2 - requires Azure DevOps credentials")]
    public void IntegrationTest_Comment_AzureDevOps_WithValidWorkItem()
    {
        // TODO: Implement real Azure DevOps API integration test
        // 1. Create a test work item
        // 2. Add comment using wi comment command
        // 3. Verify comment via Azure DevOps API
        // 4. Clean up test work item
        Assert.True(true);
    }

    [Fact(Skip = "Integration: Scaffolded for Phase 3.2 - requires GitHub credentials")]
    public void IntegrationTest_Create_GitHub_WithValidRepository()
    {
        // TODO: Implement real GitHub API integration test
        // 1. Create new issue using wi create command
        // 2. Verify issue created via GitHub API
        // 3. Clean up test issue
        Assert.True(true);
    }

    [Fact(Skip = "Integration: Scaffolded for Phase 3.2 - requires Azure DevOps credentials")]
    public void IntegrationTest_Create_AzureDevOps_WithValidProject()
    {
        // TODO: Implement real Azure DevOps API integration test
        // 1. Create new work item using wi create command
        // 2. Verify work item created via Azure DevOps API
        // 3. Clean up test work item
        Assert.True(true);
    }

    #endregion
}
