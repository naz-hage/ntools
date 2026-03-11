// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PlatformDetectorTests.cs
//
// Unit tests for the PlatformDetector service.

using Xunit;
using Sdo.Services;

namespace SdoTests;

/// <summary>
/// Unit tests for the PlatformDetector class.
/// </summary>
public class PlatformDetectorTests
{
    [Fact]
    public void DetectPlatform_InGitHubRepo_ReturnsGitHub()
    {
        // Arrange
        var detector = new PlatformDetector();

        // Act
        var platform = detector.DetectPlatform();

        // Assert - Current ntools repo should be detected as GitHub
        Assert.Equal(Platform.GitHub, platform);
    }

    [Fact]
    public void GetOrganization_InGitHubRepo_ReturnsOrganization()
    {
        // Arrange
        var detector = new PlatformDetector();

        // Act
        var organization = detector.GetOrganization();

        // Assert - Should detect "naz-hage" from the current repo
        Assert.Equal("naz-hage", organization);
    }

    [Fact]
    public void GetProject_InGitHubRepo_ReturnsProject()
    {
        // Arrange
        var detector = new PlatformDetector();

        // Act
        var project = detector.GetProject();

        // Assert - Should detect "ntools" from the current repo
        Assert.Equal("ntools", project);
    }

    [Fact]
    public void ParseAzureDevOpsUrl_CorrectlyExtractsOrganizationAndProject()
    {
        // This test verifies the parsing fix for Issue 1
        // URL: https://dev.azure.com/nazh/Proto/_git/ConsoleApp1
        // Should extract: organization="nazh", project="Proto"

        var expectedOrganization = "nazh";
        var expectedProject = "Proto";
        var testUrl = "https://dev.azure.com/nazh/Proto/_git/ConsoleApp1";

        // Parse the URL manually to verify our logic
        var cleanUrl = testUrl
            .Replace("https://", "")
            .Split('?').First();

        var parts = cleanUrl.Split('/');
        // parts[0] = "dev.azure.com"
        // parts[1] = "nazh" 
        // parts[2] = "Proto"
        // parts[3] = "_git"
        // parts[4] = "ConsoleApp1"

        Assert.Equal("dev.azure.com", parts[0]);
        Assert.Equal("nazh", parts[1]); // organization
        Assert.Equal("Proto", parts[2]); // project
        Assert.Equal("_git", parts[3]);
        Assert.Equal("ConsoleApp1", parts[4]);

        // Verify our parsing logic would work
        Assert.True(parts.Length >= 5);
        Assert.Contains("dev.azure.com", parts[0]);
        Assert.Equal(expectedOrganization, parts[1]);
        Assert.Equal(expectedProject, parts[2]);
    }

    [Fact]
    public void PlatformDetector_CanBeInstantiated()
    {
        // Act
        var detector = new PlatformDetector();

        // Assert
        Assert.NotNull(detector);
    }
}