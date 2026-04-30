// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AuthCommand.cs
//
// This file contains the AuthCommand class for verifying authentication
// with GitHub and Azure DevOps platforms based on detected Git remote.

using System.CommandLine;
using Nbuild.Helpers;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command for verifying authentication with the detected platform.
    /// </summary>
    public class AuthCommand : Command
    {
        private readonly PlatformService _platformDetector;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthCommand"/> class.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        public AuthCommand(Option<bool> verboseOption) : base("auth", "Verify authentication with GitHub or Azure DevOps")
        {
            _platformDetector = new PlatformService();

            // Add subcommands
            var ghCommand = new Command("gh", "Verify GitHub authentication");
            ghCommand.Options.Add(verboseOption);
            ghCommand.SetAction(async (parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                return await VerifyGitHubAuth(verbose);
            });

            var azdoCommand = new Command("azdo", "Verify Azure DevOps authentication");
            azdoCommand.Options.Add(verboseOption);
            azdoCommand.SetAction(async (parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                return await VerifyAzureDevOpsAuth(verbose);
            });

            // Keep the default behavior for backward compatibility
            Options.Add(verboseOption);
            SetAction(async (parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                return await HandleAuthCommand(verbose);
            });

            Subcommands.Add(ghCommand);
            Subcommands.Add(azdoCommand);
        }

        /// <summary>
        /// Handles the auth command.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> HandleAuthCommand(bool verbose)
        {
            try
            {
                if (verbose)
                {
                    Console.WriteLine("Detecting platform from Git repository...");
                }

                // Detect platform
                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    Console.WriteLine($"Detected platform: {platform}");                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    Console.WriteLine($"✓ Organization: {organization ?? "null"}");
                    Console.WriteLine($"✓ Project: {project ?? "null"}");                }

                // Verify authentication based on platform
                if (platform == Platform.GitHub)
                {
                    return await VerifyGitHubAuth(verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    return await VerifyAzureDevOpsAuth(verbose);
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                ConsoleHelper.WriteLine(ex.Message, ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Authentication verification failed: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine(ex.StackTrace!, ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Verifies GitHub authentication.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        internal async Task<int> VerifyGitHubAuth(bool verbose)
        {
            var auth = new AuthenticationService();
            var token = await auth.GetGitHubTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                ConsoleHelper.WriteLine("X GitHub authentication failed - check your token", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine("No API_GITHUB_KEY environment variable, GitHub CLI auth, or Windows Credential Manager token found", ConsoleColor.Red);
                }
                return 1;
            }

            using var client = new GitHubClient(token);
            var isAuthenticated = await client.VerifyAuthenticationAsync();
            if (!isAuthenticated)
            {
                ConsoleHelper.WriteLine("X GitHub authentication failed - invalid token", ConsoleColor.Red);
                return 1;
            }

            if (verbose)
            {
                var user = await client.GetUserAsync();
                if (user != null)
                {
                    ConsoleHelper.WriteLine($"✓ Authenticated as GitHub user: {user.Login}", ConsoleColor.Green);
                }
            }

            ConsoleHelper.WriteLine("✓ GitHub authentication successful", ConsoleColor.Green);
            return 0;
        }

        /// <summary>
        /// Verifies Azure DevOps authentication.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        internal async Task<int> VerifyAzureDevOpsAuth(bool verbose)
        {
            var auth = new AuthenticationService();
            var token = await auth.GetAzureDevOpsTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                ConsoleHelper.WriteLine("X Azure DevOps authentication failed - check your token", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine("No AZURE_DEVOPS_PAT environment variable found", ConsoleColor.Red);
                }
                return 1;
            }

            if (verbose)
            {
                ConsoleHelper.WriteLine($"✓ Found AZURE_DEVOPS_PAT (length: {token.Length})", ConsoleColor.Green);
            }

            var organization = _platformDetector.GetOrganization();
            if (string.IsNullOrEmpty(organization))
            {
                ConsoleHelper.WriteLine("X Azure DevOps authentication failed - could not determine organization", ConsoleColor.Red);
                return 1;
            }

            // Allow overriding organization via environment variable
            var overrideOrg = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG");
            if (!string.IsNullOrEmpty(overrideOrg))
            {
                organization = overrideOrg;
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"✓ Organization overridden via AZURE_DEVOPS_ORG: {organization}", ConsoleColor.Green);
                }
            }

            if (verbose)
            {
                ConsoleHelper.WriteLine($"✓ Using organization: {organization}", ConsoleColor.Green);
            }

            var project = _platformDetector.GetProject();
            using var client = new AzureDevOpsClient(token, organization, project);
            
            if (verbose)
            {
                // Show token access permissions
                var scopes = await client.GetTokenScopesAsync();
                if (scopes != null && scopes.Count > 0)
                {
                    ConsoleHelper.WriteLine("\n  Token Permissions:", ConsoleColor.Green);
                    foreach (var kvp in scopes)
                    {
                        ConsoleHelper.WriteLine($"    {kvp.Key}: {kvp.Value}", ConsoleColor.Green);
                    }
                }
            }
            
            var isAuthenticated = await client.VerifyAuthenticationAsync(verbose);
            if (!isAuthenticated)
            {
                ConsoleHelper.WriteLine("X Azure DevOps authentication failed - invalid token or organization", ConsoleColor.Red);
                return 1;
            }

            if (verbose)
            {
                var user = await client.GetUserAsync();
                if (user != null)
                {
                    ConsoleHelper.WriteLine($"✓ Authenticated as Azure DevOps user: {user.DisplayName}", ConsoleColor.Green);
                }
            }

            ConsoleHelper.WriteLine("✓ Azure DevOps authentication successful", ConsoleColor.Green);
            return 0;
        }
    }
}