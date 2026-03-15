// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// RepositoryCommand.cs
//
// Command handler for repository management operations.
// Supports both GitHub repositories and Azure DevOps Git repositories.

using System.CommandLine;
using Nbuild;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for repository management operations.
    /// </summary>
    public class RepositoryCommand : System.CommandLine.Command
    {
        private readonly PlatformService _platformDetector;

        /// <summary>
        /// Initializes a new instance of the RepositoryCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public RepositoryCommand(Option<bool> verboseOption) : base("repo", "Repository management commands")
        {
            _platformDetector = new PlatformService();

            // Add subcommands (in alphabetical order)
            AddListCommand(verboseOption);
            AddShowCommand(verboseOption);
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new System.CommandLine.Command("list", "List repositories");
            var topOption = new Option<int>("--top") { Description = "Return top N repositories" };

            listCommand.Add(topOption);
            listCommand.Add(verboseOption);

            listCommand.SetAction(async (parseResult) =>
            {
                var top = parseResult.GetValue(topOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await ListRepositories(top, verbose);
            });

            Subcommands.Add(listCommand);
        }

        private void AddShowCommand(Option<bool> verboseOption)
        {
            var showCommand = new System.CommandLine.Command("show", "Display repository details");
            var nameOption = new Option<string>("--name") { Description = "Repository name" };

            showCommand.Add(nameOption);
            showCommand.Add(verboseOption);

            showCommand.SetAction(async (parseResult) =>
            {
                var name = parseResult.GetValue(nameOption);
                var verbose = parseResult.GetValue(verboseOption);
                if (string.IsNullOrEmpty(name))
                {
                    ConsoleHelper.WriteError("X Repository name is required");
                    return 1;
                }
                return await ShowRepository(name, verbose);
            });

            Subcommands.Add(showCommand);
        }

        private async Task<int> ListRepositories(int top, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Listing repositories...");

                var platform = _platformDetector.DetectPlatform();

                if (platform == Platform.GitHub)
                {
                    using var client = new GitHubClient();
                    var repos = await client.ListRepositoriesAsync(null, "all", 30, top);
                    if (repos == null) return 1;
                    Console.WriteLine($"Found {repos.Count} repositories");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteError("X Unable to determine Azure DevOps organization");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat!, organization);
                    var projects = await client.ListProjectsAsync(1);
                    if (projects == null || projects.Count == 0) return 1;
                    var repos = await client.ListRepositoriesAsync(projects[0].Id!, top);
                    if (repos == null) return 1;
                    Console.WriteLine($"Found {repos.Count} repositories");
                    return 0;
                }

                ConsoleHelper.WriteError("X Unsupported platform");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ShowRepository(string name, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine($"Retrieving repository '{name}'...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository owner");
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    using var client = new GitHubClient();
                    var repo = await client.GetRepositoryAsync(repoInfo.Owner, name);
                    if (repo == null)
                    {
                        ConsoleHelper.WriteError("X Repository not found");
                        return 1;
                    }
                    Console.WriteLine($"Name: {repo.Name}");
                    Console.WriteLine($"URL: {repo.Url}");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteError("X AZURE_DEVOPS_PAT environment variable not set");
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteError("X Could not determine Azure DevOps organization");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization);
                    var repo = await client.GetRepositoryAsync(repoInfo.Repo ?? name, name);
                    if (repo == null)
                    {
                        ConsoleHelper.WriteError("X Repository not found");
                        return 1;
                    }
                    Console.WriteLine($"Name: {repo.Name}");
                    Console.WriteLine($"URL: {repo.Url}");
                    return 0;
                }

                ConsoleHelper.WriteError("X Unsupported platform");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error: {ex.Message}");
                return 1;
            }
        }
    }
}
