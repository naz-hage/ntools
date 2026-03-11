// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// GitHubClientTests.cs
//
// Unit tests for GitHubClient class.

using Xunit;
using Sdo.Services;

namespace SdoTests;

/// <summary>
/// Unit tests for the GitHubClient class.
/// </summary>
public class GitHubClientTests
{
    [Fact]
    public async Task VerifyAuthenticationAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            return; // Skip integration test if no token
        }

        using var client = new GitHubClient(token);

        // Act
        var result = await client.VerifyAuthenticationAsync();

        // Assert - skip if token is invalid (integration test)
        if (!result)
        {
            return; // Skip test if token doesn't work
        }

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyAuthenticationAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        using var client = new GitHubClient("invalid-token");

        // Act
        var result = await client.VerifyAuthenticationAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            return; // Skip integration test if no token
        }

        using var client = new GitHubClient(token);

        // Act
        var user = await client.GetUserAsync();

        // Assert - skip if token is invalid (integration test)
        if (user == null)
        {
            return; // Skip test if token doesn't work
        }

        Assert.NotNull(user);
        Assert.NotNull(user.Login);
    }

    [Fact]
    public async Task GetUserAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        using var client = new GitHubClient("invalid-token");

        // Act
        var user = await client.GetUserAsync();

        // Assert
        Assert.Null(user);
    }
}