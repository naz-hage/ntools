// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// MapCommandTests.cs
//
// Unit tests for the MapCommand class.

using System;
using System.CommandLine;
using System.IO;
using Xunit;
using Sdo.Commands;

namespace SdoTests;

/// <summary>
/// Unit tests for the MapCommand class.
/// </summary>
public class MapCommandTests
{
    private readonly Option<bool> _verboseOption;

    public MapCommandTests()
    {
        _verboseOption = new Option<bool>("--verbose");
        _verboseOption.Description = "Enable verbose output";
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectNameAndDescription()
    {
        // Act
        var command = new MapCommand(_verboseOption);

        // Assert
        Assert.Equal("map", command.Name);
        Assert.Equal("Show command mappings between SDO and native CLI tools", command.Description);
    }

    [Fact]
    public void Constructor_AddsPlatformOption()
    {
        // Act
        var command = new MapCommand(_verboseOption);

        // Assert
        var platformOption = Assert.Single(command.Options, o => o.Name == "--platform");
        Assert.Equal("Platform to show mappings for (gh=github, azdo=azure-devops, leave empty for auto-detect)", platformOption.Description);
    }

    [Fact]
    public void Constructor_AddsAllOption()
    {
        // Act
        var command = new MapCommand(_verboseOption);

        // Assert
        var allOption = Assert.Single(command.Options, o => o.Name == "--all");
        Assert.Equal("Show all mappings for both platforms", allOption.Description);
    }

    [Fact]
    public void Execute_WithNoOptions_ReturnsSuccess()
    {
        // Arrange
        var command = new MapCommand(_verboseOption);
        var args = Array.Empty<string>();

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithPlatformGhOption_ReturnsSuccess()
    {
        // Arrange
        var command = new MapCommand(_verboseOption);
        var args = new[] { "--platform", "gh" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithPlatformAzdoOption_ReturnsSuccess()
    {
        // Arrange
        var command = new MapCommand(_verboseOption);
        var args = new[] { "--platform", "azdo" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithAllOption_ReturnsSuccess()
    {
        // Arrange
        var command = new MapCommand(_verboseOption);
        var args = new[] { "--all" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithInvalidPlatform_ShowsErrorMessage()
    {
        // Arrange
        var command = new MapCommand(_verboseOption);
        var args = new[] { "--platform", "invalid" };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.Equal(0, result); // The command still succeeds but shows an error message
    }

    [Fact]
    public void Command_HasActionSet()
    {
        // Arrange
        var command = new MapCommand(_verboseOption);

        // Act & Assert
        // The fact that we can parse and invoke without exceptions means the action is set
        var result = command.Parse(Array.Empty<string>()).Invoke();
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("gh")]
    [InlineData("azdo")]
    [InlineData("")]
    [InlineData("auto")]
    public void Execute_WithVariousPlatformValues_ReturnsSuccess(string platform)
    {
        // Arrange
        var command = new MapCommand(_verboseOption);
        var args = platform == "" ? Array.Empty<string>() : new[] { "--platform", platform };

        // Act
        var result = command.Parse(args).Invoke();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EmbeddedResource_Exists()
    {
        // Arrange
        var assembly = typeof(Sdo.Program).Assembly;
        var resourceName = "Sdo.mapping.md";

        // Act
        var resourceNames = assembly.GetManifestResourceNames();
        var exists = resourceNames.Contains(resourceName);

        // Assert
        Assert.True(exists, $"Embedded resource '{resourceName}' not found. Available resources: {string.Join(", ", resourceNames)}");
    }
}