// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PRCommand.cs
//
// Command handler for pull request management operations.
// Supports both GitHub pull requests and Azure DevOps pull requests.

using System.CommandLine;
using Nbuild;
using Nbuild.Helpers;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for pull request management operations.
    /// </summary>
    public class PullRequestCommand : System.CommandLine.Command
    {
        private readonly PlatformService _platformDetector;

        /// <summary>
        /// Initializes a new instance of the PullRequestCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public PullRequestCommand(Option<bool> verboseOption) : base("pr", "Pull request operations")
        {
            _platformDetector = new PlatformService();

            // Add subcommands (in alphabetical order)
            AddCreateCommand(verboseOption);
            AddListCommand(verboseOption);
            AddShowCommand(verboseOption);
            AddStatusCommand(verboseOption);
            AddUpdateCommand(verboseOption);
        }

        private void AddCreateCommand(Option<bool> verboseOption)
        {
            var createCommand = new System.CommandLine.Command("create", "Create a pull request from markdown file");
            var fileOption = new Option<string>("--file", new[] { "-f" }) { Description = "Path to markdown file containing PR details" };
            var workItemOption = new Option<int>("--work-item") { Description = "Work item ID to link to the pull request" };
            var draftOption = new Option<bool>("--draft") { Description = "Create as draft pull request" };
            var dryRunOption = new Option<bool>("--dry-run") { Description = "Parse and preview PR creation without creating it" };

            createCommand.Add(fileOption);
            createCommand.Add(workItemOption);
            createCommand.Add(draftOption);
            createCommand.Add(dryRunOption);
            createCommand.Add(verboseOption);

            createCommand.SetAction(async (parseResult) =>
            {
                var file = parseResult.GetValue(fileOption);
                var workItem = parseResult.GetValue(workItemOption);
                var draft = parseResult.GetValue(draftOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await CreatePullRequest(file!, workItem, draft, dryRun, verbose);
            });

            Subcommands.Add(createCommand);
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new System.CommandLine.Command("ls", "List pull requests in the current repository");
            var statusOption = new Option<string>("--status") { Description = "Filter PRs by status (default: active)" };
            statusOption.DefaultValueFactory = _ => "active";
            var topOption = new Option<int>("--top") { Description = "Maximum number of PRs to show (default: 10)" };
            topOption.DefaultValueFactory = _ => 10;

            listCommand.Add(statusOption);
            listCommand.Add(topOption);
            listCommand.Add(verboseOption);

            listCommand.SetAction(async (parseResult) =>
            {
                var status = parseResult.GetValue(statusOption);
                var top = parseResult.GetValue(topOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await ListPullRequests(status, top, verbose);
            });

            Subcommands.Add(listCommand);
        }

        private void AddShowCommand(Option<bool> verboseOption)
        {
            var showCommand = new System.CommandLine.Command("show", "Show detailed information about a pull request");
            var prIdArgument = new Argument<int>("pr-id") { Description = "Pull request number/ID" };

            showCommand.Add(prIdArgument);
            showCommand.Add(verboseOption);

            showCommand.SetAction(async (parseResult) =>
            {
                var prId = parseResult.GetValue(prIdArgument);
                var verbose = parseResult.GetValue(verboseOption);
                return await ShowPullRequest(prId, verbose);
            });

            Subcommands.Add(showCommand);
        }

        private void AddStatusCommand(Option<bool> verboseOption)
        {
            var statusCommand = new System.CommandLine.Command("status", "Show status of a pull request");
            var prIdArgument = new Argument<int>("pr-id") { Description = "Pull request number/ID" };

            statusCommand.Add(prIdArgument);
            statusCommand.Add(verboseOption);

            statusCommand.SetAction(async (parseResult) =>
            {
                var prId = parseResult.GetValue(prIdArgument);
                var verbose = parseResult.GetValue(verboseOption);
                return await ShowPullRequest(prId, verbose);
            });

            Subcommands.Add(statusCommand);
        }

        private void AddUpdateCommand(Option<bool> verboseOption)
        {
            var updateCommand = new System.CommandLine.Command("update", "Update an existing pull request");
            var prIdOption = new Option<int>("--pr-id") { Description = "Pull request ID to update" };
            var fileOption = new Option<string?>("--file", new[] { "-f" }) { Description = "Path to markdown file with updated PR details" };
            var titleOption = new Option<string?>("--title", new[] { "-t" }) { Description = "New title for the pull request" };
            var statusOption = new Option<string?>("--status") { Description = "New status for the pull request" };

            updateCommand.Add(prIdOption);
            updateCommand.Add(fileOption);
            updateCommand.Add(titleOption);
            updateCommand.Add(statusOption);
            updateCommand.Add(verboseOption);

            updateCommand.SetAction(async (parseResult) =>
            {
                var prId = parseResult.GetValue(prIdOption);
                var file = parseResult.GetValue(fileOption);
                var title = parseResult.GetValue(titleOption);
                var status = parseResult.GetValue(statusOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await UpdatePullRequest(prId, title, status, verbose);
            });

            Subcommands.Add(updateCommand);
        }

        private async Task<int> CreatePullRequest(string file, int workItem, bool draft, bool dryRun, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Creating pull request...");

                if (!System.IO.File.Exists(file))
                {
                    ConsoleHelper.WriteError($"X Error: File not found: {file}");
                    return 1;
                }

                // Read the markdown file
                var content = System.IO.File.ReadAllText(file);
                var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Extract title (first line or # format)
                string? title = null;
                string? body = null;
                int bodyStartIndex = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("# "))
                    {
                        title = lines[i].Substring(2).Trim();
                        bodyStartIndex = i + 1;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(title) && lines.Length > 0)
                {
                    title = lines[0];
                    bodyStartIndex = 1;
                }

                if (bodyStartIndex < lines.Length)
                {
                    body = string.Join(Environment.NewLine, lines.Skip(bodyStartIndex)).Trim();
                }

                if (string.IsNullOrEmpty(title))
                {
                    ConsoleHelper.WriteError("X Error: Could not extract PR title from file");
                    return 1;
                }

                // If dry-run, just show what would be created and exit
                if (dryRun)
                {
                    Console.WriteLine($"PR would be created with:");
                    Console.WriteLine($"  Title: {title}");
                    Console.WriteLine($"  Work Item: {workItem}");
                    Console.WriteLine($"  Draft: {draft}");
                    if (!string.IsNullOrEmpty(body))
                        Console.WriteLine($"  Description: {body.Substring(0, Math.Min(100, body.Length))}...");
                    return 0;
                }

                var pat = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("AZURE_DEVOPS_EXT_PAT");
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Set GITHUB_TOKEN or AZURE_DEVOPS_EXT_PAT environment variable.");
                    return 1;
                }

                var platform = _platformDetector.DetectPlatform();
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    using var client = new GitHubClient(pat);
                    try
                    {
                        var pr = await client.CreatePullRequestAsync(repoInfo.Owner, repoInfo.Repo, title,
                            "feature", "main", body, draft);

                        if (pr == null)
                        {
                            ConsoleHelper.WriteError("X Error: Failed to create pull request");
                            return 1;
                        }

                        ConsoleHelper.WriteLine($"✓ Successfully created pull request #{pr.Number}", ConsoleColor.Green);
                        if (verbose) ConsoleHelper.WriteLine($"  URL: {pr.Url}");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteError($"X Error: {ex.Message}");
                        return 1;
                    }
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    var repoInfo = _platformDetector.GetRepositoryInfo();

                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || repoInfo == null || string.IsNullOrEmpty(repoInfo.Repo))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine Azure DevOps project/repository information.");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var pr = await client.CreatePullRequestAsync(project, repoInfo.Repo, title,
                        "refs/heads/feature", "refs/heads/main", body);

                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? "Failed to create pull request"}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"✓ Successfully created pull request #{pr.Number}", ConsoleColor.Green);
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteError("X Error: Unknown platform.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ListPullRequests(string? status, int top, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Listing pull requests...");

                var pat = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("AZURE_DEVOPS_EXT_PAT");
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Set GITHUB_TOKEN or AZURE_DEVOPS_EXT_PAT environment variable.");
                    return 1;
                }

                var platform = _platformDetector.DetectPlatform();
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    using var client = new GitHubClient(pat);
                    var prs = await client.ListPullRequestsAsync(repoInfo.Owner, repoInfo.Repo, status ?? "open", 30, top);

                    if (prs == null)
                    {
                        ConsoleHelper.WriteError("X Error: Failed to list pull requests");
                        return 1;
                    }

                    if (prs.Count == 0)
                    {
                        Console.WriteLine($"No pull requests found ({status ?? "open"} status)");
                        return 0;
                    }

                    Console.WriteLine($"Pull Requests in {repoInfo.Owner}/{repoInfo.Repo} ({prs.Count} total):");
                    foreach (var pr in prs)
                    {
                        Console.WriteLine($"  #{pr.Number}: {pr.Title}");
                        Console.WriteLine($"    Status: {pr.Status}, Author: {pr.Author}");
                    }
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    var repoInfo = _platformDetector.GetRepositoryInfo();

                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || repoInfo == null || string.IsNullOrEmpty(repoInfo.Repo))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine Azure DevOps project/repository information.");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var prs = await client.ListPullRequestsAsync(project, repoInfo.Repo, status ?? "active", top);

                    if (prs == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? "Failed to list pull requests"}");
                        return 1;
                    }

                    if (prs.Count == 0)
                    {
                        Console.WriteLine($"No pull requests found ({status ?? "active"} status)");
                        return 0;
                    }

                    Console.WriteLine($"Pull Requests in {project}/{repoInfo.Repo} ({prs.Count} total):");
                    foreach (var pr in prs)
                    {
                        Console.WriteLine($"  #{pr.Number}: {pr.Title}");
                        Console.WriteLine($"    Status: {pr.Status}, Author: {pr.Author}");
                    }
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteError("X Error: Unknown platform.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ShowPullRequest(int prId, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine($"Retrieving pull request #{prId}...");

                var pat = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("AZURE_DEVOPS_EXT_PAT");
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Set GITHUB_TOKEN or AZURE_DEVOPS_EXT_PAT environment variable.");
                    return 1;
                }

                var platform = _platformDetector.DetectPlatform();
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    using var client = new GitHubClient(pat);
                    var pr = await client.GetPullRequestAsync(repoInfo.Owner, repoInfo.Repo, prId);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: Pull request #{prId} not found");
                        return 1;
                    }

                    Console.WriteLine($"Pull Request #{pr.Number}: {pr.Title}");
                    Console.WriteLine($"  Author: {pr.Author}");
                    Console.WriteLine($"  Status: {pr.Status}");
                    Console.WriteLine($"  Branches: {pr.SourceBranch} → {pr.TargetBranch}");
                    Console.WriteLine($"  Created: {pr.CreatedAt}");
                    Console.WriteLine($"  Updated: {pr.UpdatedAt}");
                    if (!string.IsNullOrEmpty(pr.Description))
                    {
                        Console.WriteLine($"  Description: {pr.Description}");
                    }
                    Console.WriteLine($"  URL: {pr.Url}");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    var repoInfo = _platformDetector.GetRepositoryInfo();

                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || repoInfo == null || string.IsNullOrEmpty(repoInfo.Repo))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine Azure DevOps project/repository information.");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var pr = await client.GetPullRequestAsync(project, repoInfo.Repo, prId);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? $"Pull request #{prId} not found"}");
                        return 1;
                    }

                    Console.WriteLine($"Pull Request #{pr.Number}: {pr.Title}");
                    Console.WriteLine($"  Author: {pr.Author}");
                    Console.WriteLine($"  Status: {pr.Status}");
                    Console.WriteLine($"  Branches: {pr.SourceBranch} → {pr.TargetBranch}");
                    Console.WriteLine($"  Created: {pr.CreatedAt}");
                    Console.WriteLine($"  Updated: {pr.UpdatedAt}");
                    if (!string.IsNullOrEmpty(pr.Description))
                    {
                        Console.WriteLine($"  Description: {pr.Description}");
                    }
                    Console.WriteLine($"  URL: {pr.Url}");
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteError("X Error: Unknown platform.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> UpdatePullRequest(int prId, string? title, string? status, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine($"Updating pull request #{prId}...");

                var pat = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("AZURE_DEVOPS_EXT_PAT");
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Set GITHUB_TOKEN or AZURE_DEVOPS_EXT_PAT environment variable.");
                    return 1;
                }

                var platform = _platformDetector.DetectPlatform();
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    using var client = new GitHubClient(pat);
                    var pr = await client.UpdatePullRequestAsync(repoInfo.Owner, repoInfo.Repo, prId, title, null, status);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: Failed to update pull request #{prId}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"✓ Successfully updated pull request #{pr.Number}", ConsoleColor.Green);
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    var repoInfo = _platformDetector.GetRepositoryInfo();

                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project) || repoInfo == null || string.IsNullOrEmpty(repoInfo.Repo))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine Azure DevOps project/repository information.");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var pr = await client.UpdatePullRequestAsync(project, repoInfo.Repo, prId, title, null, status);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? "Failed to update pull request"}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"✓ Successfully updated pull request #{pr.Number}", ConsoleColor.Green);
                    return 0;
                }
                else
                {
                    ConsoleHelper.WriteError("X Error: Unknown platform.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error: {ex.Message}");
                return 1;
            }
        }
    }
}
