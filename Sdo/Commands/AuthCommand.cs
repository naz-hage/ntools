// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AuthCommand.cs
//
// This file contains the AuthCommand class for verifying authentication
// with GitHub and Azure DevOps platforms based on detected Git remote.

using System.CommandLine;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command for verifying authentication with the detected platform.
    /// </summary>
    public class AuthCommand : Command
    {
        private readonly PlatformDetector _platformDetector;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthCommand"/> class.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        public AuthCommand(Option<bool> verboseOption) : base("auth", "Verify authentication with GitHub or Azure DevOps")
        {
            _platformDetector = new PlatformDetector();

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
                    Console.WriteLine("✗ Unsupported platform detected");
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Authentication verification failed: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine(ex.StackTrace);
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
                Console.WriteLine("✗ GitHub authentication failed - check your token");
                if (verbose)
                {
                    Console.WriteLine("No API_GITHUB_KEY environment variable, GitHub CLI auth, or Windows Credential Manager token found");
                }
                return 1;
            }

            using var client = new GitHubClient(token);
            var isAuthenticated = await client.VerifyAuthenticationAsync();
            if (!isAuthenticated)
            {
                Console.WriteLine("✗ GitHub authentication failed - invalid token");
                return 1;
            }

            if (verbose)
            {
                var user = await client.GetUserAsync();
                if (user != null)
                {
                    Console.WriteLine($"✓ Authenticated as GitHub user: {user.Login}");
                }
            }

            Console.WriteLine("✓ GitHub authentication successful");
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
                Console.WriteLine("✗ Azure DevOps authentication failed - check your token");
                if (verbose)
                {
                    Console.WriteLine("No AZURE_DEVOPS_PAT environment variable found");
                }
                return 1;
            }

            if (verbose)
            {
                Console.WriteLine($"✓ Found AZURE_DEVOPS_PAT (length: {token.Length})");
            }

            var organization = _platformDetector.GetOrganization();
            if (string.IsNullOrEmpty(organization))
            {
                Console.WriteLine("✗ Azure DevOps authentication failed - could not determine organization");
                return 1;
            }

            // Allow overriding organization via environment variable
            var overrideOrg = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG");
            if (!string.IsNullOrEmpty(overrideOrg))
            {
                organization = overrideOrg;
                if (verbose)
                {
                    Console.WriteLine($"✓ Organization overridden via AZURE_DEVOPS_ORG: {organization}");
                }
            }

            if (verbose)
            {
                Console.WriteLine($"✓ Using organization: {organization}");
            }

            using var client = new AzureDevOpsClient(token, organization);
            
            if (verbose)
            {
                // Show token access permissions
                var scopes = await client.GetTokenScopesAsync();
                if (scopes != null && scopes.Count > 0)
                {
                    Console.WriteLine("\n  Token Permissions:");
                    foreach (var kvp in scopes)
                    {
                        Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            
            var isAuthenticated = await client.VerifyAuthenticationAsync();
            if (!isAuthenticated)
            {
                Console.WriteLine("✗ Azure DevOps authentication failed - invalid token or organization");
                return 1;
            }

            if (verbose)
            {
                var user = await client.GetUserAsync();
                if (user != null)
                {
                    Console.WriteLine($"✓ Authenticated as Azure DevOps user: {user.DisplayName}");
                }
            }

            Console.WriteLine("✓ Azure DevOps authentication successful");
            return 0;
        }
    }
}