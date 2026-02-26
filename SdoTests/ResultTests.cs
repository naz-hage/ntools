// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// ResultTests.cs
//
// Unit tests for the Result pattern classes.

using Xunit;
using Sdo.Services;

namespace SdoTests;

/// <summary>
/// Unit tests for the Result pattern classes.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Result_Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Result_Failure_CreatesFailedResult()
    {
        // Arrange
        const string errorMessage = "Test error";
        const int exitCode = 42;

        // Act
        var result = Result.Failure(errorMessage, exitCode);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        Assert.Equal(exitCode, result.ExitCode);
    }

    [Fact]
    public void Result_Failure_DefaultExitCode_IsOne()
    {
        // Act
        var result = Result.Failure("Test error");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Test error", result.Error);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void ResultT_Success_CreatesSuccessfulResultWithValue()
    {
        // Arrange
        const string testValue = "test value";

        // Act
        var result = Result<string>.Success(testValue);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(testValue, result.Value);
    }

    [Fact]
    public void ResultT_Failure_CreatesFailedResult()
    {
        // Arrange
        const string errorMessage = "Test error";
        const int exitCode = 42;

        // Act
        var result = Result<string>.Failure(errorMessage, exitCode);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
        Assert.Equal(exitCode, result.ExitCode);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ResultT_Failure_DefaultExitCode_IsOne()
    {
        // Act
        var result = Result<int>.Failure("Test error");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Test error", result.Error);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(0, result.Value); // default value for int
    }
}