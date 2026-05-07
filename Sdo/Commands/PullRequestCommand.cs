// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PRCommand.cs
//
// Command handler for pull request management operations.
// Supports both GitHub pull requests and Azure DevOps pull requests.

using System;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using Nbuild.Helpers;
using NbuildTasks;
using Sdo.Interfaces;
using Sdo.Services;
using Sdo.Mapping;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for pull request management operations.
    /// </summary>
    public class PullRequestCommand : System.CommandLine.Command
    {
        private readonly PlatformService _platformDetector;
        private readonly Sdo.Mapping.IMappingGenerator _mappingGenerator;
        private readonly Sdo.Mapping.IMappingPresenter _mappingPresenter;

        /// <summary>
        /// Initializes a new instance of the PullRequestCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public PullRequestCommand(Option<bool> verboseOption, Sdo.Mapping.IMappingGenerator? mappingGenerator = null, Sdo.Mapping.IMappingPresenter? mappingPresenter = null) : base("pr", "Pull request operations")
        {
            _platformDetector = new PlatformService();
            _mappingGenerator = mappingGenerator ?? new Sdo.Mapping.MappingGenerator();
            _mappingPresenter = mappingPresenter ?? new Sdo.Mapping.ConsoleMappingPresenter();

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
            var fileOption = new Option<string?>("--file", new[] { "-f" }) 
            { 
                Description = "Path to markdown file containing PR details (auto-detected as .temp/<work-item-id>-pr-message.md if not provided)" 
            };
            var workItemOption = new Option<int?>("--work-item") 
            { 
                Description = "Work item ID to link to the pull request (auto-detected from branch name if not provided)" 
            };
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
                return await CreatePullRequest(file, workItem, draft, dryRun, verbose);
            });

            Subcommands.Add(createCommand);
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new System.CommandLine.Command("list", "List pull requests in the current repository");
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
                return await UpdatePullRequest(prId, file, title, status, verbose);
            });

            Subcommands.Add(updateCommand);
        }

        private async Task<int> CreatePullRequest(string? file, int? workItem, bool draft, bool dryRun, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Creating pull request...");

                // Auto-detect work item ID from branch name if not provided
                if (!workItem.HasValue || workItem <= 0)
                {
                    var currentBranch = GetCurrentBranch();
                    workItem = ExtractWorkItemIdFromBranch(currentBranch);
                    
                    if (!workItem.HasValue || workItem <= 0)
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine work item ID");
                        Console.WriteLine($"\nCurrent branch: '{currentBranch}'");
                        Console.WriteLine("\nExpected branch name format: <number>-<description>");
                        Console.WriteLine("  - Must start with digits (e.g., 244)");
                        Console.WriteLine("  - Followed by a hyphen (-)");
                        Console.WriteLine("  - Then any description (e.g., issue, feature-name)");
                        Console.WriteLine("\nValid examples:");
                        Console.WriteLine("  ✓ 244-issue");
                        Console.WriteLine("  ✓ 123-feature-name");
                        Console.WriteLine("  ✓ 999-my-feature");
                        Console.WriteLine("\nInvalid examples:");
                        Console.WriteLine("  ✗ issue-244 (number not at start)");
                        Console.WriteLine("  ✗ feature (no hyphen or number)");
                        Console.WriteLine("\nAlternatively, provide work item ID explicitly:");
                        Console.WriteLine("  sdo pr create --work-item <id>");
                        return 1;
                    }
                    
                    if (verbose)
                    {
                        Console.WriteLine($"[VERBOSE] Auto-detected work item ID '{workItem}' from branch '{currentBranch}'");
                    }
                }

                // Auto-detect file path if not provided
                if (string.IsNullOrEmpty(file))
                {
                    file = ConstructDefaultFilePath(workItem.Value);
                    
                    if (verbose)
                    {
                        Console.WriteLine($"[VERBOSE] Auto-detected file path: {file}");
                    }
                }

                // Validate file exists
                if (!System.IO.File.Exists(file))
                {
                    ConsoleHelper.WriteError($"X Error: File not found: {file}");
                    Console.WriteLine("\nExpected file path format: .temp/<work-item-id>-pr-message.md");
                    Console.WriteLine($"Example for work item {workItem}: .temp/{workItem}-pr-message.md");
                    Console.WriteLine("\nIf you want to auto-detect the file path:");
                    Console.WriteLine("  - Create the file at the expected location above, OR");
                    Console.WriteLine("  - Provide file path explicitly with -f or --file option");
                    Console.WriteLine($"\nExample:");
                    Console.WriteLine($"  sdo pr create -f .temp/{workItem}-pr-message.md");
                    return 1;
                }
                // Read the markdown file
                var content = System.IO.File.ReadAllText(file);
                // Split by multiple line ending formats (Windows \r\n, Unix \n, old Mac \r)
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

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

                // Add work item ID to title (remove existing brackets and add work item number)
                if (workItem.HasValue && workItem.Value > 0)
                {
                    // Remove existing [PBI-XXX] or similar bracketed prefixes
                    var titleWithoutBrackets = System.Text.RegularExpressions.Regex.Replace(title, @"^\s*\[[^\]]*\]\s*", "");
                    title = $"{workItem}: {titleWithoutBrackets}";
                }

                // Truncate title to 256 characters (GitHub API limit)
                if (title.Length > 256)
                {
                    title = title.Substring(0, 253) + "...";
                }

                // If dry-run, just show what would be created and exit
                if (dryRun)
                {
                    Console.WriteLine($"PR would be created with:");
                    Console.WriteLine($"  Title: {title}");
                    Console.WriteLine($"  Work Item: {(workItem.HasValue ? workItem.ToString() : "Not specified")}");
                    Console.WriteLine($"  Draft: {draft}");
                    if (!string.IsNullOrEmpty(body))
                        Console.WriteLine($"  Description: {body.Substring(0, Math.Min(100, body.Length))}...");
                    if (verbose)
                    {
                        var repoInfo = _platformDetector.GetRepositoryInfo();
                        if (repoInfo != null && !string.IsNullOrEmpty(repoInfo.Owner) && !string.IsNullOrEmpty(repoInfo.Repo))
                        {
                            _mappingPresenter.Present(_mappingGenerator.PrCreateGitHub(repoInfo.Owner, repoInfo.Repo, title, file, GetCurrentBranch(), "main", draft));
                        }
                    }
                    return 0;
                }

                var platform = _platformDetector.DetectPlatform();

                var pat = await GetAuthenticationTokenAsync(platform);

                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                    return 1;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine("✓ Authentication token retrieved successfully", ConsoleColor.Green);
                }

                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        _mappingPresenter.Present(_mappingGenerator.PrCreateGitHub(repoInfo.Owner, repoInfo.Repo, title, file, GetCurrentBranch(), "main", draft));
                    }

                    using var client = new GitHubClient(pat);
                    var currentBranch = GetCurrentBranch();
                    
                    // Check if the current branch exists on the remote before attempting to create PR
                    if (!BranchExistsOnRemote(currentBranch))
                    {
                        ConsoleHelper.WriteError($"X Error: Branch '{currentBranch}' does not exist on remote 'origin'");
                        Console.WriteLine("\nTo fix this, push your branch first:");
                        Console.WriteLine($"  git push -u origin {currentBranch}");
                        Console.WriteLine("\nThen retry the PR creation command.");
                        return 1;
                    }
                    
                    try
                    {
                        var pr = await client.CreatePullRequestAsync(repoInfo.Owner, repoInfo.Repo, title,
                            currentBranch, "main", body, draft);

                        if (pr == null)
                        {
                            ConsoleHelper.WriteError("X Error: Failed to create pull request");
                            return 1;
                        }

                        Console.WriteLine("✓ Pull request created successfully!");
                        Console.WriteLine($"URL: {pr.Url}");
                        if (verbose)
                        {
                            Console.WriteLine("Platform: GitHub");
                        }
                        return 0;
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Attempt to extract concise GitHub API error message from JSON payload
                        try
                        {
                            var msg = ex.Message;
                            var jsonStart = msg.IndexOf('{');
                            if (jsonStart >= 0)
                            {
                                var json = msg.Substring(jsonStart);
                                using var doc = JsonDocument.Parse(json);
                                var root = doc.RootElement;
                                string main = root.GetProperty("message").GetString() ?? "GitHub API error";
                                string detail = string.Empty;
                                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                                {
                                    var first = errors[0];
                                    if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("message", out var fm))
                                    {
                                        detail = fm.GetString() ?? string.Empty;
                                    }
                                }

                                if (!string.IsNullOrEmpty(detail))
                                    ConsoleHelper.WriteLine($"X Error: {detail}", ConsoleColor.Red);
                                else
                                    ConsoleHelper.WriteLine($"X Error: {main}", ConsoleColor.Red);

                                return 1;
                            }
                        }
                        catch
                        {
                            // Fall through to generic message
                        }

                        ConsoleHelper.WriteError($"X Error: {ex.Message}");
                        return 1;
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

                    // Show mapping when verbose
                    if (verbose)
                    {
                        var mapping = _mappingGenerator.PrCreateAzure(organization, project, repoInfo.Repo, title, $"refs/heads/{GetCurrentBranch()}", "refs/heads/main", false, body);
                        _mappingPresenter.Present(mapping);
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var currentBranch = GetCurrentBranch();
                    
                    // Check if the current branch exists on the remote before attempting to create PR
                    if (!BranchExistsOnRemote(currentBranch))
                    {
                        ConsoleHelper.WriteError($"X Error: Branch '{currentBranch}' does not exist on remote 'origin'");
                        Console.WriteLine("\nTo fix this, push your branch first:");
                        Console.WriteLine($"  git push -u origin {currentBranch}");
                        Console.WriteLine("\nThen retry the PR creation command.");
                        return 1;
                    }
                    
                    var pr = await client.CreatePullRequestAsync(project, repoInfo.Repo, title,
                        $"refs/heads/{currentBranch}", "refs/heads/main", body);

                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? "Failed to create pull request"}");
                        return 1;
                    }

                    Console.WriteLine("✓ Pull request created successfully!");
                    Console.WriteLine($"URL: {pr.Url}");
                    // If a work item ID was provided, attempt to link it to the created PR
                    if (workItem.HasValue && workItem.Value > 0)
                    {
                        var linked = await client.LinkWorkItemAsync(workItem.Value, pr.Number, repoInfo.Repo);
                        if (!linked)
                        {
                            ConsoleHelper.WriteError($"Warning: Failed to link work item #{workItem} to PR {pr.Number}: {client.LastError}");
                        }
                        else
                        {
                            Console.WriteLine($"Linked work item #{workItem} to PR {pr.Number}");
                        }
                    }
                    if (verbose)
                    {
                        Console.WriteLine("Platform: Azure DevOps");
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

        

        private async Task<int> ListPullRequests(string? status, int top, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Listing pull requests...");

                var platform = _platformDetector.DetectPlatform();
                var pat = await GetAuthenticationTokenAsync(platform);
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                    return 1;
                }
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    using var client = new GitHubClient(pat);
                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        var mapping = _mappingGenerator.PrListGitHub(repoInfo.Owner, repoInfo.Repo, status ?? "open", top);
                        _mappingPresenter.Present(mapping);
                    }
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

                    // Display header in Python-compatible format
                    Console.WriteLine($"Active Pull Requests ({prs.Count} found):");
                    Console.WriteLine(new string('-', 70));

                    foreach (var pr in prs)
                    {
                        Console.WriteLine($" #  {pr.Number} | {pr.Title}");
                        Console.WriteLine($"    Author: {pr.Author} | Status: {pr.Status}");
                        Console.WriteLine($"    URL: {pr.Url}");
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
                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        var mapping = _mappingGenerator.PrListAzure(organization, project, repoInfo.Repo, status ?? "active", top);
                        _mappingPresenter.Present(mapping);
                    }
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

                    // Display header in consistent format
                    Console.WriteLine($"Active Pull Requests ({prs.Count} found):");
                    Console.WriteLine(new string('-', 70));

                    foreach (var pr in prs)
                    {
                        Console.WriteLine($" #  {pr.Number} | {pr.Title}");
                        Console.WriteLine($"    Author: {pr.Author} | Status: {pr.Status}");
                        if (!string.IsNullOrEmpty(pr.Url))
                        {
                            Console.WriteLine($"    URL: {pr.Url}");
                        }
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
                // Validate that PR ID was provided
                if (prId <= 0)
                {
                    ConsoleHelper.WriteError("X Error: PR ID is required. Use the positional argument or --pr-id option");
                    Console.WriteLine("\nExample:");
                    Console.WriteLine("  sdo pr show 123");
                    Console.WriteLine("  sdo pr show --pr-id 123");
                    return 1;
                }

                if (verbose) Console.WriteLine($"Retrieving pull request #{prId}...");

                var platform = _platformDetector.DetectPlatform();
                var pat = await GetAuthenticationTokenAsync(platform);
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                    return 1;
                }
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        var mapping = $"gh pr view -R {repoInfo.Owner}/{repoInfo.Repo} {prId}";
                        _mappingPresenter.Present(mapping);
                    }

                    using var client = new GitHubClient(pat);
                    var pr = await client.GetPullRequestAsync(repoInfo.Owner, repoInfo.Repo, prId);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: Pull request #{prId} not found");
                        return 1;
                    }

                    // Display PR information in Python-style format
                    var statusUpper = pr.Status?.ToUpper() ?? "UNKNOWN";
                    Console.WriteLine($"PR #{pr.Number}: {statusUpper}");
                    Console.WriteLine($"Title: {pr.Title}");
                    Console.WriteLine($"Author: {pr.Author}");
                    Console.WriteLine($"Branch: {pr.SourceBranch} -> {pr.TargetBranch}");

                    // Fetch and display CI/CD checks
                    if (!string.IsNullOrEmpty(pr.HeadSha))
                    {
                        Console.WriteLine();
                        var checks = await client.GetCheckRunsAsync(repoInfo.Owner, repoInfo.Repo, pr.HeadSha);
                        if (checks != null && checks.Count > 0)
                        {
                            Console.WriteLine("CI/CD Checks:");
                            foreach (var (name, status, duration, url) in checks)
                            {
                                Console.WriteLine($"{name,-20} {status,-10} {duration,-8} {url}");
                            }
                        }
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

                    // Show mapping when verbose
                    if (verbose)
                    {
                        var mapping = _mappingGenerator.PrShowAzure(organization, project, repoInfo.Repo, prId);
                        _mappingPresenter.Present(mapping);
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var pr = await client.GetPullRequestAsync(project, repoInfo.Repo, prId);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? $"Pull request #{prId} not found"}");
                        return 1;
                    }

                    // Display PR information in consistent format
                    var statusUpper = pr.Status?.ToUpper() ?? "UNKNOWN";
                    Console.WriteLine($"PR #{pr.Number}: {statusUpper}");
                    Console.WriteLine($"Title: {pr.Title}");
                    Console.WriteLine($"Author: {pr.Author}");
                    Console.WriteLine($"Branch: {pr.SourceBranch} -> {pr.TargetBranch}");

                    // Fetch and display linked work items (if any)
                    try
                    {
                        var linked = await client.GetPullRequestWorkItemsAsync(project, repoInfo.Repo, prId);
                        if (linked != null && linked.Count > 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Linked Work Items:");
                            foreach (var wi in linked)
                            {
                                Console.WriteLine($"  - #{wi.Id} {wi.Type ?? ""}: {wi.Title}");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Non-fatal: do not block showing PR if fetching work items fails
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

        private async Task<int> UpdatePullRequest(int prId, string? file, string? title, string? status, bool verbose)
        {
            try
            {
                // Validate that PR ID was provided
                if (prId <= 0)
                {
                    ConsoleHelper.WriteError("X Error: PR ID is required. Use --pr-id <number>");
                    Console.WriteLine("\nExample:");
                    Console.WriteLine("  sdo pr update --pr-id 123 -f ./pr-message.md");
                    Console.WriteLine("  sdo pr update --pr-id 123 --title \"New Title\"");
                    return 1;
                }

                if (verbose) Console.WriteLine($"Updating pull request #{prId}...");

                string? body = null;
                // If a file was provided, parse title/body from the markdown file
                if (!string.IsNullOrEmpty(file))
                {
                    if (!System.IO.File.Exists(file))
                    {
                        ConsoleHelper.WriteError($"X Error: File not found: {file}");
                        return 1;
                    }

                    var content = System.IO.File.ReadAllText(file);
                    var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    string? titleFromFile = null;
                    int bodyStartIndex = 0;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("# "))
                        {
                            titleFromFile = lines[i].Substring(2).Trim();
                            bodyStartIndex = i + 1;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(titleFromFile) && lines.Length > 0)
                    {
                        titleFromFile = lines[0];
                        bodyStartIndex = 1;
                    }

                    if (bodyStartIndex < lines.Length)
                    {
                        body = string.Join(Environment.NewLine, lines.Skip(bodyStartIndex)).Trim();
                    }

                    // CLI title overrides file title
                    if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(titleFromFile))
                    {
                        title = titleFromFile;
                    }
                }

                var platform = _platformDetector.DetectPlatform();
                var pat = await GetAuthenticationTokenAsync(platform);
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                    return 1;
                }
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                    {
                        ConsoleHelper.WriteError("X Error: Could not determine repository information.");
                        return 1;
                    }

                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        var parts = new System.Collections.Generic.List<string>();
                        parts.Add($"gh pr edit -R {repoInfo.Owner}/{repoInfo.Repo} {prId}");
                        if (!string.IsNullOrEmpty(title)) parts.Add($"--title \"{title}\"");
                        if (!string.IsNullOrEmpty(body)) parts.Add($"--body \"{body}\"");
                        if (!string.IsNullOrEmpty(status)) parts.Add($"--state {status}");
                        var mapping = string.Join(" ", parts);
                        _mappingPresenter.Present(mapping);
                    }

                    using var client = new GitHubClient(pat);
                    var pr = await client.UpdatePullRequestAsync(repoInfo.Owner, repoInfo.Repo, prId, title, body, status);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? "Failed to update pull request"}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"✓ Successfully updated pull request #{pr.Number}", ConsoleColor.Green);
                    ConsoleHelper.WriteLine($"PR URL: {pr.Url}");
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

                    // Show mapping when verbose
                    if (verbose)
                    {
                        var mapping = _mappingGenerator.PrUpdateAzure(organization, project, repoInfo.Repo, prId, title, status, body);
                        _mappingPresenter.Present(mapping);
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var pr = await client.UpdatePullRequestAsync(project, repoInfo.Repo, prId, title, body, status);
                    if (pr == null)
                    {
                        ConsoleHelper.WriteError($"X Error: {client.LastError ?? "Failed to update pull request"}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"✓ Successfully updated pull request #{pr.Number}", ConsoleColor.Green);
                    ConsoleHelper.WriteLine($"PR URL: {pr.Url}");
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

        private async Task<string?> GetAuthenticationTokenAsync(Platform platform)
        {
            var auth = new AuthenticationService();
            if (platform == Platform.GitHub)
            {
                return await auth.GetGitHubTokenAsync();
            }
            else if (platform == Platform.AzureDevOps)
            {
                return await auth.GetAzureDevOpsTokenAsync();
            }
            return null;
        }

        private string GetCurrentBranch()
        {
            try
            {
                var gitWrapper = new GitWrapper();
                var branch = gitWrapper.Branch;
                if (!string.IsNullOrEmpty(branch))
                {
                    return branch;
                }
            }
            catch (Exception)
            {
                // If git command fails, fall back to default
            }

            return "main"; // Default fallback
        }

        private bool BranchExistsOnRemote(string branchName)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Use ArgumentList to safely pass arguments (prevents command injection)
                processInfo.ArgumentList.Add("ls-remote");
                processInfo.ArgumentList.Add("--exit-code");
                processInfo.ArgumentList.Add("--heads");
                processInfo.ArgumentList.Add("origin");
                processInfo.ArgumentList.Add(branchName);

                // Disable credential prompts to prevent hanging on authentication
                processInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        const int gitTimeoutMilliseconds = 10000; // 10-second timeout
                        if (!process.WaitForExit(gitTimeoutMilliseconds))
                        {
                            try
                            {
                                process.Kill(entireProcessTree: true);
                            }
                            catch
                            {
                                // Process may have already exited
                            }

                            return true; // Timeout - assume branch exists to avoid false negatives
                        }

                        return process.ExitCode == 0;
                    }
                }
            }
            catch (Exception)
            {
                // If git command fails, assume branch exists to avoid false negatives
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts the work item ID from the branch name.
        /// Expected format: <number>-<description> (e.g., 244-issue)
        /// </summary>
        /// <param name="branchName">The git branch name.</param>
        /// <returns>The work item ID, or null if not found.</returns>
        private int? ExtractWorkItemIdFromBranch(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(branchName, @"^(\d+)-");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                return id;
            }

            return null;
        }

        /// <summary>
        /// Constructs the default file path for a PR markdown file based on the work item ID.
        /// Default format: .temp/<work-item-id>-pr-message.md
        /// </summary>
        /// <param name="workItemId">The work item ID.</param>
        /// <returns>The default file path.</returns>
        private string ConstructDefaultFilePath(int workItemId)
        {
            return System.IO.Path.Combine(".temp", $"{workItemId}-pr-message.md");
        }
    }
}

