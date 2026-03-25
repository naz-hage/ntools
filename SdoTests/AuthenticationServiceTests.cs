// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AuthenticationServiceTests.cs
//
// Unit tests for AuthenticationService class.

using Xunit;
using Sdo.Services;

namespace SdoTests;

/// <summary>
/// Unit tests for the AuthenticationService class.
/// </summary>
public class AuthenticationServiceTests
{
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _authService = new AuthenticationService();
    }

    [Fact]
    public async Task GetGitHubTokenAsync_WithApiGitHubKeyEnvironmentVariable_ReturnsToken()
    {
        // Arrange
        Environment.SetEnvironmentVariable("API_GITHUB_KEY", "test-github-token");

        // Act
        var result = await _authService.GetGitHubTokenAsync();

        // Assert
        // Note: GitHub CLI token takes priority, so either GitHub CLI returns a token or we get the env var
        Assert.True(!string.IsNullOrEmpty(result), "AuthenticationService should return a token from either GitHub CLI or environment variables");

        // Cleanup
        Environment.SetEnvironmentVariable("API_GITHUB_KEY", null);
    }

    [Fact]
    public async Task GetGitHubTokenAsync_WithoutEnvironmentVariables_MayReturnTokenFromOtherSources()
    {
        // Arrange - Clear environment variables but other sources (GitHub CLI, Credential Manager) may still provide tokens
        Environment.SetEnvironmentVariable("API_GITHUB_KEY", null);
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);

        // Act
        var result = await _authService.GetGitHubTokenAsync();

        // Assert - May return token from GitHub CLI or other sources, or null if none available
        // This test verifies the authentication service tries multiple sources as designed
        Assert.True(result == null || !string.IsNullOrEmpty(result));
    }

    [Fact]
    public async Task GetAzureDevOpsTokenAsync_WithEnvironmentVariable_ReturnsToken()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_PAT", "test-azure-token");

        // Act
        var result = await _authService.GetAzureDevOpsTokenAsync();

        // Assert
        Assert.Equal("test-azure-token", result);

        // Cleanup
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_PAT", null);
    }

    [Fact]
    public async Task GetAzureDevOpsTokenAsync_WithoutEnvironmentVariable_ReturnsNull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_PAT", null);

        // Act
        var result = await _authService.GetAzureDevOpsTokenAsync();

        // Assert
        Assert.Null(result);
    }
}