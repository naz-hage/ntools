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
    public void PlatformDetector_CanBeInstantiated()
    {
        // Act
        var detector = new PlatformDetector();

        // Assert
        Assert.NotNull(detector);
    }
}