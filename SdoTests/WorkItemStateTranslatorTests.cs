// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItemStateTranslatorTests.cs
//
// Unit tests for the WorkItemStateTranslator class.
// Tests state parsing, GitHub translation, and Azure DevOps translation.

using System;
using Xunit;
using Sdo.Models;

namespace SdoTests;

/// <summary>
/// Unit tests for the WorkItemStateTranslator class.
/// Tests the state parsing, platform translation, and validation logic.
/// </summary>
public class WorkItemStateTranslatorTests
{
    #region ParseState Tests

    [Fact]
    public void ParseState_WithValidNewState_ReturnsNewEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("new");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.New, result);
    }

    [Fact]
    public void ParseState_WithValidNewStateUppercase_ReturnsNewEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("NEW");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.New, result);
    }

    [Fact]
    public void ParseState_WithValidNewStateMixedCase_ReturnsNewEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("New");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.New, result);
    }

    [Fact]
    public void ParseState_WithValidApprovedState_ReturnsApprovedEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("approved");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Approved, result);
    }

    [Fact]
    public void ParseState_WithValidCommittedState_ReturnsCommittedEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("committed");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Committed, result);
    }

    [Fact]
    public void ParseState_WithValidDoneState_ReturnsDoneEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("done");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Done, result);
    }

    [Fact]
    public void ParseState_WithValidClosedState_ReturnsDoneEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("closed");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Done, result);
    }

    [Fact]
    public void ParseState_WithValidToDoState_ReturnsToDoEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("to do");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.ToDo, result);
    }

    [Fact]
    public void ParseState_WithValidToDoStateNoSpace_ReturnsToDoEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("todo");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.ToDo, result);
    }

    [Fact]
    public void ParseState_WithValidInProgressState_ReturnsInProgressEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("in progress");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.InProgress, result);
    }

    [Fact]
    public void ParseState_WithValidInProgressStateNoSpace_ReturnsInProgressEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("inprogress");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.InProgress, result);
    }

    [Fact]
    public void ParseState_WithNull_ReturnsNull()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseState_WithEmptyString_ReturnsNull()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseState_WithWhitespace_ReturnsNull()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseState_WithInvalidState_ReturnsNull()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("invalid");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseState_WithStateContainingLeadingWhitespace_ReturnsValidEnum()
    {
        // Act
        var result = WorkItemStateTranslator.ParseState("  done  ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Done, result);
    }

    #endregion

    #region ToGitHubState Tests

    [Fact]
    public void ToGitHubState_WithNewState_ReturnsOpen()
    {
        // Act
        var result = WorkItemStateTranslator.ToGitHubState(WorkItemState.New);

        // Assert
        Assert.Equal("open", result);
    }

    [Fact]
    public void ToGitHubState_WithApprovedState_ReturnsOpen()
    {
        // Act
        var result = WorkItemStateTranslator.ToGitHubState(WorkItemState.Approved);

        // Assert
        Assert.Equal("open", result);
    }

    [Fact]
    public void ToGitHubState_WithCommittedState_ReturnsOpen()
    {
        // Act
        var result = WorkItemStateTranslator.ToGitHubState(WorkItemState.Committed);

        // Assert
        Assert.Equal("open", result);
    }

    [Fact]
    public void ToGitHubState_WithToDoState_ReturnsOpen()
    {
        // Act
        var result = WorkItemStateTranslator.ToGitHubState(WorkItemState.ToDo);

        // Assert
        Assert.Equal("open", result);
    }

    [Fact]
    public void ToGitHubState_WithInProgressState_ReturnsOpen()
    {
        // Act
        var result = WorkItemStateTranslator.ToGitHubState(WorkItemState.InProgress);

        // Assert
        Assert.Equal("open", result);
    }

    [Fact]
    public void ToGitHubState_WithDoneState_ReturnsClosed()
    {
        // Act
        var result = WorkItemStateTranslator.ToGitHubState(WorkItemState.Done);

        // Assert
        Assert.Equal("closed", result);
    }

    #endregion

    #region ToAzureDevOpsState Tests

    [Fact]
    public void ToAzureDevOpsState_WithNewState_ReturnsNew()
    {
        // Act
        var result = WorkItemStateTranslator.ToAzureDevOpsState(WorkItemState.New);

        // Assert
        Assert.Equal("New", result);
    }

    [Fact]
    public void ToAzureDevOpsState_WithApprovedState_ReturnsApproved()
    {
        // Act
        var result = WorkItemStateTranslator.ToAzureDevOpsState(WorkItemState.Approved);

        // Assert
        Assert.Equal("Approved", result);
    }

    [Fact]
    public void ToAzureDevOpsState_WithCommittedState_ReturnsCommitted()
    {
        // Act
        var result = WorkItemStateTranslator.ToAzureDevOpsState(WorkItemState.Committed);

        // Assert
        Assert.Equal("Committed", result);
    }

    [Fact]
    public void ToAzureDevOpsState_WithDoneState_ReturnsDone()
    {
        // Act
        var result = WorkItemStateTranslator.ToAzureDevOpsState(WorkItemState.Done);

        // Assert
        Assert.Equal("Done", result);
    }

    [Fact]
    public void ToAzureDevOpsState_WithToDoState_ReturnsToDoWithSpace()
    {
        // Act
        var result = WorkItemStateTranslator.ToAzureDevOpsState(WorkItemState.ToDo);

        // Assert
        Assert.Equal("To Do", result);
    }

    [Fact]
    public void ToAzureDevOpsState_WithInProgressState_ReturnsInProgressWithSpace()
    {
        // Act
        var result = WorkItemStateTranslator.ToAzureDevOpsState(WorkItemState.InProgress);

        // Assert
        Assert.Equal("In Progress", result);
    }

    #endregion

    #region Round-trip Conversion Tests

    [Theory]
    [InlineData("new", WorkItemState.New)]
    [InlineData("approved", WorkItemState.Approved)]
    [InlineData("committed", WorkItemState.Committed)]
    [InlineData("done", WorkItemState.Done)]
    [InlineData("to do", WorkItemState.ToDo)]
    [InlineData("in progress", WorkItemState.InProgress)]
    public void RoundTrip_ParseStateThenToAzureDevOps_PreservesState(string userInput, WorkItemState expected)
    {
        // Act
        var parsed = WorkItemStateTranslator.ParseState(userInput);
        Assert.NotNull(parsed);
        var adoState = WorkItemStateTranslator.ToAzureDevOpsState(parsed.Value);

        // Assert
        Assert.Equal(expected, parsed.Value);
        var expectedAdo = WorkItemStateTranslator.ToAzureDevOpsState(expected);
        Assert.Equal(expectedAdo, adoState);
    }

    [Theory]
    [InlineData("new", "open")]
    [InlineData("approved", "open")]
    [InlineData("committed", "open")]
    [InlineData("done", "closed")]
    [InlineData("to do", "open")]
    [InlineData("in progress", "open")]
    public void RoundTrip_ParseStateThenToGitHub_ProducesCorrectGitHubState(string userInput, string expectedGitHub)
    {
        // Act
        var parsed = WorkItemStateTranslator.ParseState(userInput);
        Assert.NotNull(parsed);
        var gitHubState = WorkItemStateTranslator.ToGitHubState(parsed.Value);

        // Assert
        Assert.Equal(expectedGitHub, gitHubState);
    }

    #endregion

    #region GetValidStatesForHelp Tests

    [Fact]
    public void GetValidStatesForHelp_ReturnsFormattedStateList()
    {
        // Act
        var result = WorkItemStateTranslator.GetValidStatesForHelp();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("New", result);
        Assert.Contains("Approved", result);
        Assert.Contains("Committed", result);
        Assert.Contains("Done", result);
        Assert.Contains("To Do", result);
        Assert.Contains("In Progress", result);
    }

    [Fact]
    public void GetValidStatesForHelp_ContainsAllSixStates()
    {
        // Act
        var result = WorkItemStateTranslator.GetValidStatesForHelp();
        var states = result.Split(", ");

        // Assert
        Assert.Equal(6, states.Length);
    }

    #endregion

    #region Case Insensitivity Tests

    [Theory]
    [InlineData("DONE")]
    [InlineData("Done")]
    [InlineData("dONe")]
    public void ParseState_WithDifferentCasings_AllReturnDoneState(string input)
    {
        // Act
        var result = WorkItemStateTranslator.ParseState(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Done, result);
    }

    [Theory]
    [InlineData("TO DO")]
    [InlineData("To Do")]
    [InlineData("TO do")]
    [InlineData("TODO")]
    [InlineData("Todo")]
    public void ParseState_WithDifferentCasingsForToDo_AllReturnToDoState(string input)
    {
        // Act
        var result = WorkItemStateTranslator.ParseState(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.ToDo, result);
    }

    [Theory]
    [InlineData("IN PROGRESS")]
    [InlineData("In Progress")]
    [InlineData("IN progress")]
    [InlineData("INPROGRESS")]
    [InlineData("InProgress")]
    public void ParseState_WithDifferentCasingsForInProgress_AllReturnInProgressState(string input)
    {
        // Act
        var result = WorkItemStateTranslator.ParseState(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.InProgress, result);
    }

    #endregion

    #region Closed as Done Alias Tests

    [Fact]
    public void ParseState_WithClosedAlias_ReturnsDoneState()
    {
        // Act & Assert
        var result = WorkItemStateTranslator.ParseState("closed");
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Done, result);
    }

    [Fact]
    public void ParseState_WithClosedAliasUppercase_ReturnsDoneState()
    {
        // Act & Assert
        var result = WorkItemStateTranslator.ParseState("CLOSED");
        Assert.NotNull(result);
        Assert.Equal(WorkItemState.Done, result);
    }

    #endregion
}
