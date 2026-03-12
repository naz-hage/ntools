// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AuthenticationService.cs
//
// Implementation of authentication service for secure token management
// from environment variables and other sources (reusing GitHubRelease authentication).

using GitHubRelease;
using Sdo.Interfaces;

namespace Sdo.Services
{
    /// <summary>
    /// Implementation of authentication service.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        /// <summary>
        /// Gets the GitHub token from environment variables and other sources.
        /// </summary>
        /// <returns>The GitHub token, or null if not found.</returns>
        public Task<string?> GetGitHubTokenAsync()
        {
            try
            {
                var token = Credentials.GetToken();
                return Task.FromResult<string?>(token);
            }
            catch (InvalidOperationException)
            {
                return Task.FromResult<string?>(null);
            }
        }

        /// <summary>
        /// Gets the Azure DevOps PAT from environment variables.
        /// </summary>
        /// <returns>The Azure DevOps PAT, or null if not found.</returns>
        public Task<string?> GetAzureDevOpsTokenAsync()
        {
            var token = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
            return Task.FromResult<string?>(token);
        }
    }
}