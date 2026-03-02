// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// IAuthenticationService.cs
//
// Interface for authentication services that provide secure token management
// for DevOps platforms.

namespace Sdo.Services
{
    /// <summary>
    /// Interface for authentication services.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets the GitHub token from environment variables.
        /// </summary>
        /// <returns>The GitHub token, or null if not found.</returns>
        Task<string?> GetGitHubTokenAsync();

        /// <summary>
        /// Gets the Azure DevOps PAT from environment variables.
        /// </summary>
        /// <returns>The Azure DevOps PAT, or null if not found.</returns>
        Task<string?> GetAzureDevOpsTokenAsync();
    }
}