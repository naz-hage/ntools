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
        /// Gets the GitHub token from GitHub CLI first, then environment variables and other sources.
        /// </summary>
        /// <returns>The GitHub token, or null if not found.</returns>
        public Task<string?> GetGitHubTokenAsync()
        {
            try
            {
                // Try GitHub CLI first (gh auth token)
                var token = GetGitHubCliToken();
                if (!string.IsNullOrEmpty(token))
                {
                    return Task.FromResult<string?>(token);
                }
            }
            catch
            {
                // Fall through to other methods
            }

            try
            {
                // Fall back to GitHubRelease credentials (GITHUB_TOKEN, GitHub CLI auth, Windows Credential Manager)
                var token = Credentials.GetToken();
                return Task.FromResult<string?>(token);
            }
            catch (InvalidOperationException)
            {
                return Task.FromResult<string?>(null);
            }
        }

        /// <summary>
        /// Gets the token from GitHub CLI (gh auth token).
        /// </summary>
        private static string? GetGitHubCliToken()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "auth token",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            return output;
                        }
                    }
                }
            }
            catch
            {
                // gh command not found or failed
            }

            return null;
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