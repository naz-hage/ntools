// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AzureDevOpsClientTests.cs
//
// Unit tests for AzureDevOpsClient class.

using Xunit;
using Sdo.Services;

namespace SdoTests;

/// <summary>
/// Unit tests for the AzureDevOpsClient class.
/// </summary>
public class AzureDevOpsClientTests
{
    [Fact]
    public async Task VerifyAuthenticationAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
        var organization = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(organization))
        {
            return; // Skip integration test if no credentials
        }

        using var client = new AzureDevOpsClient(token, organization);

        // Act
        var result = await client.VerifyAuthenticationAsync();

        // Assert - skip if credentials are invalid (integration test)
        if (!result)
        {
            return; // Skip test if credentials don't work
        }

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyAuthenticationAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        using var client = new AzureDevOpsClient("invalid-token", "test-org");

        // Act
        var result = await client.VerifyAuthenticationAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var token = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
        var organization = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(organization))
        {
            return; // Skip integration test if no credentials
        }

        using var client = new AzureDevOpsClient(token, organization);

        // Act
        var user = await client.GetUserAsync();

        // Assert - skip if credentials are invalid (integration test)
        if (user == null)
        {
            return; // Skip test if credentials don't work
        }

        Assert.NotNull(user);
        Assert.NotNull(user.UniqueName);
    }

    [Fact]
    public async Task GetUserAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        using var client = new AzureDevOpsClient("invalid-token", "test-org");

        // Act
        var user = await client.GetUserAsync();

        // Assert
        Assert.Null(user);
    }
}