// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItemCommand.cs
//
// This file contains the WorkItemCommand class for managing work items
// across GitHub Issues and Azure DevOps work items.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.CommandLine;
using Nbuild.Helpers;
using Sdo.Interfaces;
using Sdo.Services;
using Sdo.Models;
using Sdo.Mapping;
using Sdo.Utilities;
using NbuildTasks;

namespace Sdo.Commands
{
    /// <summary>
    /// Command for managing work items across GitHub and Azure DevOps.
    /// </summary>
    public class WorkItemCommand : Command
    {
        private readonly PlatformService _platformDetector;
        private readonly Sdo.Mapping.IMappingGenerator _mappingGenerator;
        private readonly Sdo.Mapping.IMappingPresenter _mappingPresenter;
        private readonly ConfigurationManager _configManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkItemCommand"/> class.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        public WorkItemCommand(Option<bool> verboseOption, Sdo.Mapping.IMappingGenerator? mappingGenerator = null, Sdo.Mapping.IMappingPresenter? mappingPresenter = null) : base("wi", "Work item management commands")
        {
            _platformDetector = new PlatformService();
            _mappingGenerator = mappingGenerator ?? new Sdo.Mapping.MappingGenerator();
            _mappingPresenter = mappingPresenter ?? new Sdo.Mapping.ConsoleMappingPresenter();
            _configManager = new ConfigurationManager();

            // Add subcommands (in alphabetical order for help display)
            AddCloseCommand(verboseOption);
            AddCommentCommand(verboseOption);
            AddCreateCommand(verboseOption);
            AddListCommand(verboseOption);
            AddShowCommand(verboseOption);
            AddStartCommand(verboseOption);
            AddUpdateCommand(verboseOption);
        }

        /// <summary>
        /// Adds the 'close' subcommand to close work items and clean up branches.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddCloseCommand(Option<bool> verboseOption)
        {
            var closeCommand = new Command("close", "Close a work item and clean up feature branch");

            var idArgument = new Argument<int?>("id") 
            { 
                Description = "Work item ID (optional; auto-detected from branch name if not on main)",
                Arity = ArgumentArity.ZeroOrOne
            };

            closeCommand.Add(idArgument);
            closeCommand.Add(verboseOption);

            closeCommand.SetAction(async (parseResult) =>
            {
                var id = parseResult.GetValue(idArgument);
                var verbose = parseResult.GetValue(verboseOption);

                return await CloseWorkItem(id, verbose);
            });

            Subcommands.Add(closeCommand);
        }

        /// <summary>
        /// Adds the 'show' subcommand to display work item details.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddShowCommand(Option<bool> verboseOption)
        {
            var showCommand = new Command("show", "Display detailed work item information");

            var idOption = new Option<int?>("--id") { Description = "Work item ID (optional; auto-detected from branch name if not on main)" };
            var commentsOption = new Option<bool>("--comments") { Description = "Show comments/discussion", Aliases = { "-c" } };

            showCommand.Add(idOption);
            showCommand.Add(commentsOption);
            showCommand.Add(verboseOption);

            showCommand.SetAction(async (parseResult) =>
            {
                var id = parseResult.GetValue(idOption);
                var includeComments = parseResult.GetValue(commentsOption);
                var verbose = parseResult.GetValue(verboseOption);

                return await ShowWorkItem(id, includeComments, verbose);
            });

            Subcommands.Add(showCommand);
        }

        /// <summary>
        /// Adds the 'list' subcommand to list work items with filtering.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new Command("list", "List work items with optional filtering");

            var typeOption = new Option<string?>("--type") { Description = "Filter by work item type (PBI, Bug, Task, Spike, Epic)" };
            var stateOption = new Option<string?>("--state") { Description = "Filter by state (New, Approved, Committed, Done, To Do, In Progress)" };
            var assignedToOption = new Option<string?>("--assigned-to") { Description = "Filter by assigned user (email or display name)" };
            var assignedToMeOption = new Option<bool>("--assigned-to-me") { Description = "Filter by work items assigned to current user" };
            var areaOption = new Option<string?>("--area") { Description = "Filter by area path (Azure DevOps only). Example: 'Project\\Area\\SubArea'" };
            var iterationOption = new Option<string?>("--iteration") { Description = "Filter by iteration (Azure DevOps only). Example: 'Project\\Sprint 1'" };
            var topOption = new Option<int>("--top") { Description = "Maximum number of items to return (default: 50)" };
            var configOption = new Option<string?>("--config") { Description = "Path to sdo-config.yaml file (optional)" };

            listCommand.Add(typeOption);
            listCommand.Add(stateOption);
            listCommand.Add(assignedToOption);
            listCommand.Add(assignedToMeOption);
            listCommand.Add(areaOption);
            listCommand.Add(iterationOption);
            listCommand.Add(topOption);
            listCommand.Add(configOption);
            listCommand.Add(verboseOption);

            listCommand.SetAction(async (parseResult) =>
            {
                var type = parseResult.GetValue(typeOption);
                var state = parseResult.GetValue(stateOption);
                var assignedTo = parseResult.GetValue(assignedToOption);
                var assignedToMe = parseResult.GetValue(assignedToMeOption);
                var area = parseResult.GetValue(areaOption);
                var iteration = parseResult.GetValue(iterationOption);
                var top = parseResult.GetValue(topOption);
                var config = parseResult.GetValue(configOption);
                var verbose = parseResult.GetValue(verboseOption);

                return await ListWorkItems(type, state, assignedTo, assignedToMe, area, iteration, top, config, verbose);
            });

            Subcommands.Add(listCommand);
        }

        /// <summary>
        /// Adds the 'update' subcommand to modify work item properties.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddUpdateCommand(Option<bool> verboseOption)
        {
            var updateCommand = new Command("update", "Update work item properties");

            var idOption = new Option<int>("--id") { Description = "Work item ID (required)" };
            var titleOption = new Option<string?>("--title") { Description = "Update work item title" };
            var stateOption = new Option<string?>("--state") { Description = "Update work item state" };
            var assigneeOption = new Option<string?>("--assignee") { Description = "Update assigned user" };
            var descriptionOption = new Option<string?>("--description") { Description = "Update work item description" };

            updateCommand.Add(idOption);
            updateCommand.Add(titleOption);
            updateCommand.Add(stateOption);
            updateCommand.Add(assigneeOption);
            updateCommand.Add(descriptionOption);
            updateCommand.Add(verboseOption);

            updateCommand.SetAction(async (parseResult) =>
            {
                var id = parseResult.GetValue(idOption);
                var title = parseResult.GetValue(titleOption);
                var state = parseResult.GetValue(stateOption);
                var assignee = parseResult.GetValue(assigneeOption);
                var description = parseResult.GetValue(descriptionOption);
                var verbose = parseResult.GetValue(verboseOption);

                return await UpdateWorkItem(id, title, state, assignee, description, verbose);
            });

            Subcommands.Add(updateCommand);
        }

        /// <summary>
        /// Adds the 'start' subcommand to initialize a work item branch.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddStartCommand(Option<bool> verboseOption)
        {
            var startCommand = new Command("start", "Start work on a work item by creating a feature branch and PR template");

            var idArgument = new Argument<int?>("id") 
            { 
                Description = "Work item ID (optional; auto-detected from branch name if not on main)",
                Arity = ArgumentArity.ZeroOrOne
            };

            startCommand.Add(idArgument);
            startCommand.Add(verboseOption);

            startCommand.SetAction(async (parseResult) =>
            {
                var id = parseResult.GetValue(idArgument);
                var verbose = parseResult.GetValue(verboseOption);

                return await StartWorkItem(id, verbose);
            });

            Subcommands.Add(startCommand);
        }

        /// <summary>
        /// Adds the 'comment' subcommand to add comments to work items.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddCommentCommand(Option<bool> verboseOption)
        {
            var commentCommand = new Command("comment", "Add comment to a work item");

            var idOption = new Option<int>("--id") { Description = "Work item ID (required)" };
            var messageOption = new Option<string>("--message") { Description = "Comment message (required)" };

            commentCommand.Add(idOption);
            commentCommand.Add(messageOption);
            commentCommand.Add(verboseOption);

            commentCommand.SetAction(async (parseResult) =>
            {
                var id = parseResult.GetValue(idOption);
                var message = parseResult.GetValue(messageOption);
                var verbose = parseResult.GetValue(verboseOption);

                return await AddComment(id, message!, verbose);
            });

            Subcommands.Add(commentCommand);
        }

        /// <summary>
        /// Adds the 'create' subcommand to create a new work item.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddCreateCommand(Option<bool> verboseOption)
        {
            var createCommand = new Command("create", "Create a new work item");
            // Only allow markdown-file-driven creation; all fields must be in the file
            var filePathOption = new Option<string?>("--file-path", new[] { "-f" }) 
            { 
                Description = "Path to markdown file containing work item details (auto-detected as .temp/wi.md if not provided)" 
            };
            var dryRunOption = new Option<bool>("--dry-run") { Description = "Parse and preview work item creation without creating it" };

            createCommand.Add(filePathOption);
            createCommand.Add(dryRunOption);
            createCommand.Add(verboseOption);

            createCommand.SetAction(async (parseResult) =>
            {
                var filePath = parseResult.GetValue(filePathOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                var verbose = parseResult.GetValue(verboseOption);

                // Title/type/description/assignee must come from the markdown file
                return await CreateWorkItem(null, null, null, null, verbose, filePath, dryRun);
            });

            Subcommands.Add(createCommand);
        }

        /// <summary>
        /// Handles displaying work item details.
        /// </summary>
        /// <param name="id">The work item ID (optional; auto-detected from branch if not provided).</param>
        /// <param name="includeComments">Whether to include comments/discussion.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> ShowWorkItem(int? id, bool includeComments, bool verbose)
        {
            try
            {
                // Auto-detect work item ID from branch name if not provided
                if (!id.HasValue)
                {
                    id = TryAutoDetectWorkItemIdFromBranch(verbose);
                    if (!id.HasValue)
                    {
                        return 1;
                    }
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detecting platform and retrieving work item {id}...", ConsoleColor.Green);
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    // Build and display the equivalent external command mapping for 'show'
                    string mappingCmd = string.Empty;
                    if (platform == Platform.GitHub)
                    {
                        var repoInfo = _platformDetector.GetRepositoryInfo();
                        if (repoInfo != null && !string.IsNullOrEmpty(repoInfo.Owner) && !string.IsNullOrEmpty(repoInfo.Repo))
                        {
                            mappingCmd = _mappingGenerator.IssueShowGitHub(repoInfo.Owner, repoInfo.Repo, (int)id!);
                        }
                    }
                    else if (platform == Platform.AzureDevOps)
                    {
                        var org = _platformDetector.GetOrganization();
                        var project = _platformDetector.GetProject();
                        if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(project))
                        {
                            mappingCmd = _mappingGenerator.WorkItemShowAzure(org, project, (int)id!);
                        }
                    }

                    if (!string.IsNullOrEmpty(mappingCmd))
                    {
                        _mappingPresenter.Present(mappingCmd);
                    }

                    ConsoleHelper.WriteLine($"Detected platform: {platform}", ConsoleColor.Green);
                }

                if (platform == Platform.GitHub)
                {
                    return await ShowGitHubIssue((int)id!, includeComments, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    return await ShowAzureDevOpsWorkItem((int)id!, includeComments, verbose);
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                ConsoleHelper.WriteLine($"X {ex.Message}", ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Displays GitHub issue details.
        /// </summary>
        private async Task<int> ShowGitHubIssue(int issueNumber, bool includeComments, bool verbose)
        {
            try
            {
                var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                    return 1;
                }

                var client = new GitHubClient(token);
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (string.IsNullOrEmpty(repoInfo?.Owner) || string.IsNullOrEmpty(repoInfo?.Repo))
                {
                    ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                    return 1;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Fetching GitHub issue: {repoInfo.Owner}/{repoInfo.Repo}#{issueNumber}", ConsoleColor.Green);
                }

                var issue = await client.GetIssueAsync(repoInfo.Owner, repoInfo.Repo, issueNumber);

                if (issue == null)
                {
                    ConsoleHelper.WriteLine($"X Issue #{issueNumber} not found", ConsoleColor.Red);
                    return 1;
                }

                DisplayIssue(issue, includeComments);
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Failed to fetch GitHub issue: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        /// <summary>
        /// Displays Azure DevOps work item details.
        /// </summary>
        private async Task<int> ShowAzureDevOpsWorkItem(int workItemId, bool includeComments, bool verbose)
        {
            try
            {
                var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                    return 1;
                }

                var organization = _platformDetector.GetOrganization();
                if (string.IsNullOrEmpty(organization))
                {
                    ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization from Git remote", ConsoleColor.Red);
                    return 1;
                }

                var client = new AzureDevOpsClient(pat, organization);

                if (verbose)
                {
                    Console.WriteLine($"Fetching Azure DevOps work item: {organization}#{workItemId}");
                }

                var workItem = await client.GetWorkItemAsync(workItemId);

                if (workItem == null)
                {
                    ConsoleHelper.WriteLine($"X Work item #{workItemId} not found", ConsoleColor.Red);
                    return 1;
                }

                DisplayWorkItem(workItem, includeComments);
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Failed to fetch Azure DevOps work item: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        /// <summary>
        /// Handles listing work items with filtering.
        /// </summary>
        private async Task<int> ListWorkItems(string? type, string? state, string? assignedTo, 
            bool assignedToMe, string? areaPath, string? iteration, int top, string? configPath, bool verbose)
        {
            // Load configuration defaults from sdo-config.yaml (if not provided via CLI)
            var configLoaded = _configManager.Load(configPath);
            if (!configLoaded)
            {
                // Display config errors to user
                foreach (var error in _configManager.GetErrors())
                {
                    ConsoleHelper.WriteError($"Config error: {error}");
                }
                return 1;
            }
            
            // Display config file location
            if (_configManager.LoadedConfigPath != null)
            {
                Console.WriteLine($"Config: {_configManager.LoadedConfigPath}");
            }
            
            // Apply configuration defaults only if CLI values were not provided
            if (string.IsNullOrEmpty(type))
            {
                type = _configManager.GetValue("commands:wi:list:type");
            }
            if (string.IsNullOrEmpty(state))
            {
                state = _configManager.GetValue("commands:wi:list:state");
            }
            if (string.IsNullOrEmpty(assignedTo))
            {
                assignedTo = _configManager.GetValue("commands:wi:list:assigned_to");
            }
            if (string.IsNullOrEmpty(areaPath))
            {
                areaPath = _configManager.GetValue("commands:wi:list:area_path");
            }
            if (string.IsNullOrEmpty(iteration))
            {
                iteration = _configManager.GetValue("commands:wi:list:iteration");
            }
            if (top <= 0)
            {
                var topConfig = _configManager.GetInt("commands:wi:list:top", 0);
                if (topConfig > 0) top = topConfig;
            }

            try
            {
                if (verbose)
                {
                    Console.WriteLine("Detecting platform and listing work items...");
                }

                var platform = _platformDetector.DetectPlatform();
                // Build and display equivalent external command mapping (gh / az) when verbose
                string mappingCmd = string.Empty;
                if (platform == Platform.GitHub)
                {
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    var repoPart = repoInfo != null && !string.IsNullOrEmpty(repoInfo.Owner) && !string.IsNullOrEmpty(repoInfo.Repo)
                        ? $"--repo {repoInfo.Owner}/{repoInfo.Repo}"
                        : string.Empty;

                    string ghState = "open";
                    if (!string.IsNullOrEmpty(state))
                    {
                        var parsed = WorkItemStateTranslator.ParseState(state);
                        ghState = parsed.HasValue ? WorkItemStateTranslator.ToGitHubState(parsed.Value).ToLower() : state.ToLower();
                    }

                    if (repoInfo != null && !string.IsNullOrEmpty(repoInfo.Owner) && !string.IsNullOrEmpty(repoInfo.Repo))
                    {
                        mappingCmd = _mappingGenerator.IssueListGitHub(repoInfo.Owner, repoInfo.Repo, ghState, top);
                    }
                    else
                    {
                        mappingCmd = string.Empty;
                    }
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var org = _platformDetector.GetOrganization() ?? "(organization)";
                    var project = _platformDetector.GetProject() ?? "(project)";
                    var wiql = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [System.CreatedDate], [System.ChangedDate] FROM WorkItems ORDER BY [System.ChangedDate] DESC";
                    mappingCmd = _mappingGenerator.BoardsQueryAzure(org, project, wiql, top);
                }

                if (verbose && !string.IsNullOrEmpty(mappingCmd))
                {
                    ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                }

                if (verbose)
                {
                    Console.WriteLine($"Detected platform: {platform}");
                    if (!string.IsNullOrEmpty(type)) Console.WriteLine($"Type filter: {type}");
                    if (!string.IsNullOrEmpty(state)) Console.WriteLine($"State filter: {state}");
                    if (assignedToMe) Console.WriteLine("Assigned to me filter: enabled");
                }

                if (platform == Platform.GitHub)
                {
                    if (!string.IsNullOrEmpty(areaPath))
                    {
                        ConsoleHelper.WriteLine("X --area-path is only supported for Azure DevOps", ConsoleColor.Yellow);
                    }
                    if (!string.IsNullOrEmpty(iteration))
                    {
                        ConsoleHelper.WriteLine("X --iteration is only supported for Azure DevOps", ConsoleColor.Yellow);
                    }
                    return await ListGitHubIssues(type, state, assignedTo, assignedToMe, top, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    return await ListAzureDevOpsWorkItems(type, state, assignedTo, assignedToMe, areaPath, iteration, top, verbose);
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                ConsoleHelper.WriteLine($"X {ex.Message}", ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Lists GitHub issues with filtering.
        /// </summary>
        private async Task<int> ListGitHubIssues(string? type, string? state, string? assignedTo,
            bool assignedToMe, int top, bool verbose)
        {
            try
            {
                var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                    return 1;
                }

                var client = new GitHubClient(token);
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (string.IsNullOrEmpty(repoInfo?.Owner) || string.IsNullOrEmpty(repoInfo?.Repo))
                {
                    ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                    return 1;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Fetching GitHub issues: {repoInfo.Owner}/{repoInfo.Repo}", ConsoleColor.Green);
                }

                // Determine page size: use larger page when filtering to ensure sufficient filtered results
                // If --top is specified, fetch at least 100 items to ensure enough after filtering
                int pageSize = Math.Max(100, top);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Fetching with perPage={pageSize} (filtering results to {top})", ConsoleColor.Green);
                }

                // Determine which GitHub API state to request (open/closed/all)
                string ghApiState = "open";
                if (!string.IsNullOrEmpty(state))
                {
                    var parsed = WorkItemStateTranslator.ParseState(state);
                    if (parsed.HasValue)
                    {
                        ghApiState = WorkItemStateTranslator.ToGitHubState(parsed.Value).ToLower();
                    }
                    else
                    {
                        // If user provided an API-valid value like 'closed' or 'all', use it
                        ghApiState = state.ToLower();
                    }
                }

                var issues = await client.ListIssuesAsync(repoInfo.Owner, repoInfo.Repo, pageSize, ghApiState, top);

                if (issues == null)
                {
                    if (verbose)
                    {
                        ConsoleHelper.WriteLine("API returned null", ConsoleColor.Red);
                    }
                    ConsoleHelper.WriteLine("No issues found", ConsoleColor.Red);
                    return 0;
                }

                // Filter issues by state
                if (!string.IsNullOrEmpty(state))
                {
                    // Map user-provided state (e.g., Done) to GitHub state (open/closed)
                    var parsedState = WorkItemStateTranslator.ParseState(state);
                    if (parsedState.HasValue)
                    {
                        var ghState = WorkItemStateTranslator.ToGitHubState(parsedState.Value).ToLower();
                        issues = issues.Where(i => (i.State ?? "").ToLower() == ghState).ToList();
                    }
                    else
                    {
                        // Fallback to literal comparison: support comma-separated states
                        var requestedStates = state.Split(',').Select(s => s.Trim().ToLower()).ToList();
                        issues = issues.Where(i => requestedStates.Contains((i.State ?? "").ToLower())).ToList();
                    }
                }
                else
                {
                    // Default: exclude closed issues
                    issues = issues.Where(i => (i.State ?? "").ToLower() != "closed").ToList();
                }

                // Filter by type if specified (for GitHub, type could be used with labels in future)
                if (!string.IsNullOrEmpty(type))
                {
                    // For now, type filter on GitHub is a no-op as GitHub doesn't have issue types like Azure DevOps
                    // This is here for consistency and future enhancement when labels are used as types
                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Note: --type filter not fully implemented for GitHub issues", ConsoleColor.Yellow);
                    }
                }

                // Filter by assigned-to if specified
                if (!string.IsNullOrEmpty(assignedTo))
                {
                    issues = issues.Where(i => !string.IsNullOrEmpty(i.Assignee?.Login) &&
                        i.Assignee.Login.Equals(assignedTo, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filter by assigned-to-me if specified
                if (assignedToMe)
                {
                    var currentUser = await client.GetUserAsync();
                    if (string.IsNullOrEmpty(currentUser?.Login))
                    {
                        ConsoleHelper.WriteLine("X Error: Could not determine current GitHub user. Make sure your authentication token is valid.", ConsoleColor.Red);
                        if (!string.IsNullOrEmpty(client.LastError))
                        {
                            ConsoleHelper.WriteLine($"  Details: {client.LastError}", ConsoleColor.Red);
                        }
                        return 1;
                    }
                    issues = issues.Where(i => !string.IsNullOrEmpty(i.Assignee?.Login) &&
                        i.Assignee.Login.Equals(currentUser.Login, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Limit to requested number
                if (top > 0 && issues.Count > top)
                {
                    issues = issues.Take(top).ToList();
                }
                // If there are no issues after filtering, report and exit
                if (issues == null || !issues.Any())
                {
                    Console.WriteLine("No issues found");
                    return 0;
                }

                // Display results
                DisplayIssuesList(issues);
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Failed to list GitHub issues: {ex.Message}", ConsoleColor.Red);
                return 1;
            }

        }
        /// <summary>
        /// Lists Azure DevOps work items with filtering.
        /// </summary>
        private async Task<int> ListAzureDevOpsWorkItems(string? type, string? state, string? assignedTo,
            bool assignedToMe, string? areaPath, string? iteration, int top, bool verbose)
        {
            try
            {
                var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                    return 1;
                }

                var organization = _platformDetector.GetOrganization();
                if (string.IsNullOrEmpty(organization))
                {
                    ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization from Git remote", ConsoleColor.Red);
                    return 1;
                }

                var project = _platformDetector.GetProject();
                var client = new AzureDevOpsClient(pat, organization, project);

                if (verbose)
                {
                    Console.WriteLine($"Fetching Azure DevOps work items: {organization}");
                    if (!string.IsNullOrEmpty(project))
                    {
                        Console.WriteLine($"  Project: {project}");
                    }
                }

                // Verify project access first (informational only, don't fail if check fails)
                if (verbose && !string.IsNullOrEmpty(project))
                {
                    Console.WriteLine("  Verifying project access...");
                    var projectAccessOk = await client.VerifyProjectAccessAsync();
                    if (!projectAccessOk)
                    {
                        Console.WriteLine($"  ⚠ Project access verification inconclusive: {client.LastError}");
                    }
                    else
                    {
                        Console.WriteLine("  ✓ Project access OK");
                    }
                }

                var workItems = await client.ListWorkItemsAsync(top, areaPath, iteration, verbose);

                if (workItems == null)
                {
                    if (verbose)
                    {
                        ConsoleHelper.WriteLine("X Failed to retrieve work items from Azure DevOps", ConsoleColor.Red);
                        if (!string.IsNullOrEmpty(client.LastError))
                        {
                            ConsoleHelper.WriteLine($"  Error: {client.LastError}", ConsoleColor.Red);
                            
                            // Provide specific guidance for common errors
                            if (client.LastError.Contains("Unauthorized"))
                            {
                                ConsoleHelper.WriteLine("  ", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("  This typically means:", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("    1. The AZURE_DEVOPS_PAT token doesn't have 'Work Item Query' permissions", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("    2. The PAT scope doesn't include 'vso.work_read'", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("    3. The project has work items disabled", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("  ", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("  Solution: Create a PAT with these scopes:", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("    - Work Item > Read", ConsoleColor.Red);
                                ConsoleHelper.WriteLine("    - Code > Read", ConsoleColor.Red);
                            }
                        }
                        else
                        {
                            ConsoleHelper.WriteLine("  Possible causes:", ConsoleColor.Red);
                            ConsoleHelper.WriteLine("    - AZURE_DEVOPS_PAT token permissions are insufficient", ConsoleColor.Red);
                            ConsoleHelper.WriteLine("    - Work items feature is disabled in the project", ConsoleColor.Red);
                            ConsoleHelper.WriteLine($"  Organization: {organization}", ConsoleColor.Red);
                            ConsoleHelper.WriteLine($"  Project: {project ?? "(not specified)"}", ConsoleColor.Red);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No work items found");
                    }
                    return 0;
                }

                // Filter by state
                if (!string.IsNullOrEmpty(state))
                {
                    // Support comma-separated states: "To Do,In Progress" → match any of them
                    var requestedStates = state.Split(',').Select(s => s.Trim().ToLower()).ToList();
                    workItems = workItems.Where(w => requestedStates.Contains(w.State?.ToLower() ?? "")).ToList();
                }
                else
                {
                    // Default: exclude done and closed items
                    workItems = workItems.Where(w => w.State?.ToLower() is not ("done" or "closed")).ToList();
                }

                // Filter by type if specified
                if (!string.IsNullOrEmpty(type))
                {
                    var normalizedType = NormalizeWorkItemType(type);
                    workItems = workItems.Where(w => NormalizeWorkItemType(w.Type ?? "").Equals(normalizedType, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filter by assigned-to if specified
                if (!string.IsNullOrEmpty(assignedTo))
                {
                    workItems = workItems.Where(w => !string.IsNullOrEmpty(w.AssignedTo) && 
                        w.AssignedTo.Equals(assignedTo, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filter by assigned-to-me if specified
                if (assignedToMe)
                {
                    var currentUser = await GetCurrentAzureDevOpsUserAsync(pat, organization);
                    if (string.IsNullOrEmpty(currentUser))
                    {
                        ConsoleHelper.WriteLine("X Error: Could not determine current Azure DevOps user. Make sure your authentication token is valid.", ConsoleColor.Red);
                        return 1;
                    }
                    workItems = workItems.Where(w => !string.IsNullOrEmpty(w.AssignedTo) && 
                        w.AssignedTo.Equals(currentUser, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Limit to requested number
                if (top > 0 && workItems.Count > top)
                {
                    workItems = workItems.Take(top).ToList();
                }

                if (!workItems.Any())
                {
                    Console.WriteLine("No work items found");
                    return 0;
                }

                DisplayWorkItemsList(workItems);
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Failed to list Azure DevOps work items: {ex.Message}", ConsoleColor.Red);
                return 1;
            }
        }

        /// <summary>
        /// Handles updating work item properties.
        /// </summary>
        /// <param name="id">The work item ID.</param>
        /// <param name="title">New title (optional).</param>
        /// <param name="state">New state (optional).</param>
        /// <param name="assignee">New assignee (optional).</param>
        /// <param name="description">New description (optional).</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> UpdateWorkItem(int id, string? title, string? state, string? assignee, string? description, bool verbose)
        {
            try
            {
                if (id <= 0)
                {
                    ConsoleHelper.WriteLine("X Work item ID must be positive", ConsoleColor.Red);
                    return 1;
                }

                if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(state) &&
                    string.IsNullOrEmpty(assignee) && string.IsNullOrEmpty(description))
                {
                    ConsoleHelper.WriteLine("X At least one property must be specified for update (--title, --state, --assignee, --description)", ConsoleColor.Red);
                    return 1;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detecting platform and updating work item {id}...", ConsoleColor.Yellow);
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detected platform: {platform}", ConsoleColor.Yellow);
                }

                if (platform == Platform.GitHub)
                {
                    // Get repository information
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || repoInfo.Owner == null || repoInfo.Repo == null)
                    {
                        ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                        return 1;
                    }

                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    using var client = new GitHubClient(token);

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Updating GitHub issue #{id} in {repoInfo.Owner}/{repoInfo.Repo}...", ConsoleColor.Yellow);
                    }

                    // Translate state to GitHub API format
                    string? ghState = null;
                    if (!string.IsNullOrEmpty(state))
                    {
                        var parsedState = WorkItemStateTranslator.ParseState(state);
                        if (parsedState.HasValue)
                        {
                            ghState = WorkItemStateTranslator.ToGitHubState(parsedState.Value);
                        }
                        else
                        {
                            ConsoleHelper.WriteLine($"X Invalid state '{state}'. Valid states: {WorkItemStateTranslator.GetValidStatesForHelp()}", ConsoleColor.Red);
                            return 1;
                        }
                    }

                    if (verbose)
                    {
                        var mappingCmd = _mappingGenerator.IssueUpdateGitHub(repoInfo.Owner!, repoInfo.Repo!, id, title, ghState, description, assignee);
                        if (!string.IsNullOrEmpty(mappingCmd)) _mappingPresenter.Present(mappingCmd);
                    }

                    var result = await client.UpdateIssueAsync(repoInfo.Owner!, repoInfo.Repo!, id, title, ghState, description, assignee);

                    if (result != null)
                    {
                        if (!string.IsNullOrEmpty(ghState))
                        {
                            ConsoleHelper.WriteLine($"✓ GitHub issue #{id} updated successfully to state: {ghState}", ConsoleColor.Green);
                        }
                        else
                        {
                            ConsoleHelper.WriteLine($"✓ GitHub issue #{id} updated successfully", ConsoleColor.Green);
                        }
                        if (verbose)
                        {
                            ConsoleHelper.WriteLine($"  Title: {result.Title}", ConsoleColor.Yellow);
                            ConsoleHelper.WriteLine($"  State: {result.State}", ConsoleColor.Yellow);
                        }
                        return 0;
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"X Failed to update GitHub issue #{id}", ConsoleColor.Red);
                        if (!string.IsNullOrEmpty(ghState))
                        {
                            ConsoleHelper.WriteLine($"  Supported GitHub states: open, closed", ConsoleColor.Gray);
                        }
                        return 1;
                    }
                }
                else if (platform == Platform.AzureDevOps)
                {
                    // Get Azure DevOps configuration
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization", ConsoleColor.Red);
                        return 1;
                    }

                    var project = _platformDetector.GetProject();

                    using var client = new AzureDevOpsClient(pat, organization, project);

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Updating Azure DevOps work item {id} in {organization}...", ConsoleColor.Yellow);
                    }

                    // Translate state to Azure DevOps format
                    string? adoState = null;
                    if (!string.IsNullOrEmpty(state))
                    {
                        var parsedState = WorkItemStateTranslator.ParseState(state);
                        if (parsedState.HasValue)
                        {
                            adoState = WorkItemStateTranslator.ToAzureDevOpsState(parsedState.Value);
                        }
                        else
                        {
                            ConsoleHelper.WriteLine($"X Invalid state '{state}'. Valid states: {WorkItemStateTranslator.GetValidStatesForHelp()}", ConsoleColor.Red);
                            return 1;
                        }
                    }

                    if (verbose)
                    {
                        var mappingCmd = _mappingGenerator.WorkItemUpdateAzure(organization, project ?? string.Empty, id, title, adoState, assignee, description);
                        if (!string.IsNullOrEmpty(mappingCmd)) _mappingPresenter.Present(mappingCmd);
                    }

                    var result = await client.UpdateWorkItemAsync(id, title, adoState, assignee, description);

                    if (result != null)
                    {
                        if (!string.IsNullOrEmpty(adoState))
                        {
                            ConsoleHelper.WriteLine($"✓ Work item {id} updated successfully to state: {adoState}", ConsoleColor.Green);
                        }
                        else
                        {
                            ConsoleHelper.WriteLine($"✓ Work item {id} updated successfully", ConsoleColor.Green);
                        }
                        if (verbose)
                        {
                            ConsoleHelper.WriteLine($"  Title: {result.Title}", ConsoleColor.Yellow);
                            ConsoleHelper.WriteLine($"  State: {result.State}", ConsoleColor.Yellow);
                        }
                        return 0;
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"X Failed to update work item {id}", ConsoleColor.Red);
                        if (!string.IsNullOrEmpty(adoState))
                        {
                            ConsoleHelper.WriteLine($"  Supported states: {WorkItemStateTranslator.GetValidStatesForHelp()}", ConsoleColor.Gray);
                        }
                        return 1;
                    }
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                ConsoleHelper.WriteLine($"X {ex.Message}", ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Handles adding a comment to a work item.
        /// </summary>
        /// <param name="id">The work item ID.</param>
        /// <param name="message">The comment message.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> AddComment(int id, string message, bool verbose)
        {
            try
            {
                if (id <= 0)
                {
                    ConsoleHelper.WriteLine("X Work item ID must be positive", ConsoleColor.Red);
                    return 1;
                }

                if (string.IsNullOrEmpty(message))
                {
                    ConsoleHelper.WriteLine("X Comment message cannot be empty", ConsoleColor.Red);
                    return 1;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detecting platform and adding comment to work item {id}...", ConsoleColor.Gray);
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detected platform: {platform}", ConsoleColor.Gray);
                }

                if (platform == Platform.GitHub)
                {
                    // Get repository information
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || repoInfo.Owner == null || repoInfo.Repo == null)
                    {
                        ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                        return 1;
                    }

                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    using var client = new GitHubClient(token);

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Adding comment to GitHub issue #{id} in {repoInfo.Owner}/{repoInfo.Repo}...", ConsoleColor.Gray);
                    }

                    var success = await client.AddCommentAsync(repoInfo.Owner!, repoInfo.Repo!, id, message);

                    if (success)
                    {
                        ConsoleHelper.WriteLine($"✓ Comment added to GitHub issue #{id}", ConsoleColor.Green);
                        return 0;
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"X Failed to add comment to GitHub issue #{id}", ConsoleColor.Red);
                        return 1;
                    }
                }
                else if (platform == Platform.AzureDevOps)
                {
                    // Get Azure DevOps configuration
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization", ConsoleColor.Red);
                        return 1;
                    }

                    var project = _platformDetector.GetProject();

                    using var client = new AzureDevOpsClient(pat, organization, project);

                    if (verbose)
                    {
                        Console.WriteLine($"Adding comment to Azure DevOps work item {id} in {organization}...");
                    }

                    var success = await client.AddCommentAsync(id, message);

                    if (success)
                    {
                        Console.WriteLine($"✓ Comment added to work item {id}");
                        return 0;
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"X Failed to add comment to work item {id}", ConsoleColor.Red);
                        if (!string.IsNullOrEmpty(client.LastError))
                        {
                            ConsoleHelper.WriteLine($"  Error: {client.LastError}", ConsoleColor.Red);
                        }
                        return 1;
                    }
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                ConsoleHelper.WriteLine($"X {ex.Message}", ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Handles creating a new work item.
        /// </summary>
        /// <param name="title">The work item title.</param>
        /// <param name="type">The work item type.</param>
        /// <param name="description">The work item description (optional).</param>
        /// <param name="assignee">The assignee (optional).</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> CreateWorkItem(string? title, string? type, string? description, string? assignee, bool verbose, string? filePath = null, bool dryRun = false)
        {
            try
            {
                // Auto-detect file path if not provided
                string? sourceFilePath = filePath;
                if (string.IsNullOrEmpty(sourceFilePath))
                {
                    sourceFilePath = Path.Combine(".temp", "wi.md");
                    
                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"[VERBOSE] Auto-detected file path: {sourceFilePath}", ConsoleColor.Gray);
                    }
                }

                // Validate file exists
                if (!File.Exists(sourceFilePath))
                {
                    ConsoleHelper.WriteLine($"X Error: File not found: {sourceFilePath}", ConsoleColor.Red);
                    Console.WriteLine("\nExpected file path format: .temp/wi.md");
                    Console.WriteLine("If you want to use a different file path:");
                    Console.WriteLine($"  sdo wi create -f <file-path>");
                    return 1;
                }

                // If a markdown file was provided, parse it and override values where present
                List<string>? acceptanceCriteria = null;
                WorkItemParseResult? parsed = null;
                if (!string.IsNullOrEmpty(sourceFilePath))
                {
                    parsed = ParseWorkItemFromMarkdown(sourceFilePath);
                    if (parsed == null)
                    {
                        ConsoleHelper.WriteLine($"X Failed to parse markdown file: {sourceFilePath}", ConsoleColor.Red);
                        return 1;
                    }

                    if (!string.IsNullOrEmpty(parsed.Title)) title = parsed.Title;
                    if (!string.IsNullOrEmpty(parsed.Description)) description = parsed.Description;
                    if (string.IsNullOrEmpty(type) && parsed.Metadata != null && parsed.Metadata.TryGetValue("work_item_type", out var mt)) type = mt;
                    if (!string.IsNullOrEmpty(parsed.Metadata?.GetValueOrDefault("assignee"))) assignee = parsed.Metadata.GetValueOrDefault("assignee");
                    acceptanceCriteria = parsed.AcceptanceCriteria;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detecting platform and creating new work item...", ConsoleColor.Gray);
                }

                var platform = _platformDetector.DetectPlatform();

                // Allow the markdown file to override detected platform via metadata 'target' or 'platform'
                if (parsed?.Metadata != null)
                {
                    string? target = null;
                    if (parsed.Metadata.TryGetValue("target", out var t1) && !string.IsNullOrEmpty(t1)) target = t1;
                    else if (parsed.Metadata.TryGetValue("platform", out var t2) && !string.IsNullOrEmpty(t2)) target = t2;

                    if (!string.IsNullOrEmpty(target))
                    {
                        var tl = target.ToLowerInvariant();
                        if (tl.Contains("azdo") || tl.Contains("azure") || tl.Contains("devops")) platform = Platform.AzureDevOps;
                        else if (tl.Contains("github") || tl.Contains("gh") || tl.Contains("git")) platform = Platform.GitHub;
                    }
                }

                if (verbose)
                {
                    Console.WriteLine($"Detected platform: {platform}");
                }

                // Validate required fields after parsing markdown
                if (string.IsNullOrEmpty(title))
                {
                    ConsoleHelper.WriteLine("X Title is required for creating a work item (provide via markdown file)", ConsoleColor.Red);
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    // Get repository information
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || repoInfo.Owner == null || repoInfo.Repo == null)
                    {
                        ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                        return 1;
                    }

                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    using var client = new GitHubClient(token);

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Creating GitHub issue in {repoInfo.Owner}/{repoInfo.Repo}...", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Description: {description ?? "(none)"}", ConsoleColor.Gray);
                    }

                    // Show external mapping command when verbose (helpful for users migrating from CLI)
                    if (verbose)
                    {
                        var mappingCmd = !string.IsNullOrEmpty(sourceFilePath)
                            ? _mappingGenerator.IssueCreateGitHub(repoInfo.Owner, repoInfo.Repo, title, sourceFilePath, true)
                            : _mappingGenerator.IssueCreateGitHub(repoInfo.Owner, repoInfo.Repo, title, description ?? string.Empty, false);

                        if (!string.IsNullOrEmpty(mappingCmd))
                            _mappingPresenter.Present(mappingCmd);
                    }

                    // Structured dry-run preview similar to Python CLI
                    var labelsRaw = parsed?.Metadata?.GetValueOrDefault("labels");
                    var labels = !string.IsNullOrEmpty(labelsRaw)
                        ? labelsRaw.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray()
                        : Array.Empty<string>();

                    if (dryRun)
                    {
                        ConsoleHelper.WriteLine("[dry-run] Would create GitHub issue with:", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Repository: {repoInfo.Owner}/{repoInfo.Repo}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Labels: {(labels.Any() ? string.Join(", ", labels) : "(none)")}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine("  Body:", ConsoleColor.Gray);
                        if (!string.IsNullOrEmpty(description))
                        {
                            Console.WriteLine(description);
                        }
                        else
                        {
                            Console.WriteLine();
                        }

                        if (acceptanceCriteria != null && acceptanceCriteria.Any())
                        {
                            Console.WriteLine();
                            ConsoleHelper.WriteLine("## Acceptance Criteria", ConsoleColor.Gray);
                            foreach (var ac in acceptanceCriteria)
                            {
                                ConsoleHelper.WriteLine($"- [ ] {ac}", ConsoleColor.Gray);
                            }
                        }

                        return 0;
                    }

                    // Build issue body with acceptance criteria appended (GitHub uses markdown)
                    var issueBody = description ?? string.Empty;
                    if (acceptanceCriteria != null && acceptanceCriteria.Any())
                    {
                        var acMarkdown = string.Join("\n", acceptanceCriteria.Select(ac => $"- [ ] {ac}"));
                        if (!string.IsNullOrEmpty(issueBody))
                            issueBody += $"\n\n## Acceptance Criteria\n{acMarkdown}";
                        else
                            issueBody = $"## Acceptance Criteria\n{acMarkdown}";
                    }

                    // Create the GitHub issue
                    var createdIssue = await client.CreateIssueAsync(repoInfo.Owner!, repoInfo.Repo!, title, issueBody, labels.Any() ? labels : null);

                    if (createdIssue != null)
                    {
                        ConsoleHelper.WriteLine($"✓ Created GitHub issue #{createdIssue.Number}: {title}", ConsoleColor.Green);
                        if (!string.IsNullOrEmpty(createdIssue.HtmlUrl))
                            ConsoleHelper.WriteLine($"   URL: {createdIssue.HtmlUrl}", ConsoleColor.Green);
                        
                        // Rename file if it was auto-detected from default path
                        if (string.IsNullOrEmpty(filePath) && sourceFilePath != null)
                        {
                            string newFileName = Path.Combine(".temp", $"{createdIssue.Number}-issue.md");
                            try
                            {
                                if (File.Exists(sourceFilePath))
                                {
                                    File.Move(sourceFilePath, newFileName, overwrite: true);
                                    if (verbose)
                                        ConsoleHelper.WriteLine($"✓ Renamed file from {sourceFilePath} to {newFileName}", ConsoleColor.Gray);
                                }
                            }
                            catch (Exception ex)
                            {
                                ConsoleHelper.WriteLine($"⚠ Warning: Failed to rename file: {ex.Message}", ConsoleColor.Yellow);
                                // Don't fail the operation if renaming fails
                            }
                        }
                        
                        // Display next steps
                        Console.WriteLine();
                        ConsoleHelper.WriteLine("Next steps:", ConsoleColor.Cyan);
                        ConsoleHelper.WriteLine($"  1. Run: sdo wi start {createdIssue.Number}", ConsoleColor.Cyan);
                        ConsoleHelper.WriteLine($"  2. Implement changes for issue {createdIssue.Number}", ConsoleColor.Cyan);
                        ConsoleHelper.WriteLine($"  3. Edit .temp/{createdIssue.Number}-pr-message.md with PR details (optional)", ConsoleColor.Cyan);
                        ConsoleHelper.WriteLine($"  4. Run: sdo pr create", ConsoleColor.Cyan);
                        
                        return 0;
                    }

                    ConsoleHelper.WriteLine("X Failed to create GitHub issue", ConsoleColor.Red);
                    return 1;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    // Get Azure DevOps configuration
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    
                    // Allow markdown metadata to override the detected organization
                    if (parsed?.Metadata != null && parsed.Metadata.TryGetValue("organization", out var metadataOrg) && !string.IsNullOrEmpty(metadataOrg))
                    {
                        organization = metadataOrg;
                        if (verbose)
                        {
                            ConsoleHelper.WriteLine($"[VERBOSE] Organization overridden from metadata: {organization}", ConsoleColor.Gray);
                        }
                    }
                    
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization", ConsoleColor.Red);
                        return 1;
                    }

                    var project = _platformDetector.GetProject();
                    
                    // Allow markdown metadata to override the detected project
                    if (parsed?.Metadata != null && parsed.Metadata.TryGetValue("project", out var metadataProject) && !string.IsNullOrEmpty(metadataProject))
                    {
                        project = metadataProject;
                        if (verbose)
                        {
                            ConsoleHelper.WriteLine($"[VERBOSE] Project overridden from metadata: {project}", ConsoleColor.Gray);
                        }
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Creating Azure DevOps work item in {organization}/{project ?? "default"}...", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Type: {type} (normalized: {NormalizeWorkItemType(type ?? "PBI")})", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Description: {description ?? "(none)"}", ConsoleColor.Gray);
                        if (!string.IsNullOrEmpty(assignee)) ConsoleHelper.WriteLine($"  Assignee: {assignee}", ConsoleColor.Gray);
                    }

                    // Normalize work item type: convert abbreviations to full names
                    var normalizedType = NormalizeWorkItemType(type ?? "PBI");

                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        var mappingCmd = $"az boards work-item create --title \"{title}\" --type \"{normalizedType}\" --org \"{organization}\"";
                        if (!string.IsNullOrEmpty(project)) mappingCmd += $" --project \"{project}\"";
                        if (!string.IsNullOrEmpty(assignee)) mappingCmd += $" --assigned-to \"{assignee}\"";
                        ConsoleHelper.WriteLine(mappingCmd, ConsoleColor.Yellow);
                    }

                    // Use new AzureDevOpsClient.CreateWorkItemAsync implementation
                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Creating Azure DevOps work item in {organization}/{project ?? "default"}...", ConsoleColor.Gray);
                    }

                    // Provide area/iteration/parent if available in parsed metadata
                    string? area = parsed?.Metadata?.GetValueOrDefault("area") ?? parsed?.Metadata?.GetValueOrDefault("area_path");
                    string? iteration = parsed?.Metadata?.GetValueOrDefault("iteration") ?? parsed?.Metadata?.GetValueOrDefault("iteration_path");
                    string? parent = parsed?.Metadata?.GetValueOrDefault("parent");

                    if (verbose && parsed?.Metadata != null)
                    {
                        ConsoleHelper.WriteLine("[VERBOSE] Parsed metadata:", ConsoleColor.Gray);
                        foreach (var kvp in parsed.Metadata)
                        {
                            ConsoleHelper.WriteLine($"  {kvp.Key} = {kvp.Value}", ConsoleColor.Gray);
                        }
                    }

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"[VERBOSE] parent ID value: {parent ?? "(null)"}", ConsoleColor.Gray);
                    }

                    var result = await client.CreateWorkItemAsync(project ?? string.Empty, normalizedType, title, description ?? string.Empty, acceptanceCriteria, assignee, area, iteration, parent, dryRun, verbose);

                    if (result != null)
                    {
                        // Dry-run preview returns a dictionary with 'dry_run' key
                        if (result.TryGetValue("dry_run", out var dr) && dr is bool drb && drb)
                        {
                            ConsoleHelper.WriteLine("[dry-run] Azure DevOps preview complete", ConsoleColor.Gray);
                            return 0;
                        }

                        if (result.TryGetValue("id", out var idObj))
                        {
                            ConsoleHelper.WriteLine($"✓ Work item {idObj} created successfully", ConsoleColor.Green);
                            if (result.TryGetValue("url", out var urlObj) && urlObj != null)
                            {
                                ConsoleHelper.WriteLine($"  URL: {urlObj}", ConsoleColor.Yellow);
                            }
                            
                            // Rename file if it was auto-detected from default path
                            if (string.IsNullOrEmpty(filePath) && sourceFilePath != null && idObj is int workItemId)
                            {
                                // Determine suffix based on work item type
                                string typeSuffix = normalizedType.ToLowerInvariant() switch
                                {
                                    "task" => "task",
                                    "pbi" => "pbi",
                                    "product backlog item" => "pbi",
                                    "bug" => "bug",
                                    "epic" => "epic",
                                    "feature" => "feature",
                                    _ => normalizedType.ToLowerInvariant()
                                };
                                
                                string newFileName = Path.Combine(".temp", $"{workItemId}-{typeSuffix}.md");
                                try
                                {
                                    if (File.Exists(sourceFilePath))
                                    {
                                        File.Move(sourceFilePath, newFileName, overwrite: true);
                                        if (verbose)
                                            ConsoleHelper.WriteLine($"✓ Renamed file from {sourceFilePath} to {newFileName}", ConsoleColor.Gray);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleHelper.WriteLine($"⚠ Warning: Failed to rename file: {ex.Message}", ConsoleColor.Yellow);
                                    // Don't fail the operation if renaming fails
                                }
                            }
                            
                            // Display context-aware next steps
                            Console.WriteLine();
                            ConsoleHelper.WriteLine("Next steps:", ConsoleColor.Cyan);
                            
                            // Check if this is a Task or Bug type - these are implementation items
                            bool isImplementationItem = normalizedType.Equals("Task", StringComparison.OrdinalIgnoreCase) || 
                                                      normalizedType.Equals("Bug", StringComparison.OrdinalIgnoreCase);
                            
                            if (isImplementationItem)
                            {
                                // Task/Bug workflow - start working immediately
                                ConsoleHelper.WriteLine($"  1. Run: sdo wi start {idObj}", ConsoleColor.Cyan);
                                ConsoleHelper.WriteLine($"  2. Implement changes for work item {idObj}", ConsoleColor.Cyan);
                                ConsoleHelper.WriteLine($"  3. Edit .temp/{idObj}-pr-message.md with PR details (optional)", ConsoleColor.Cyan);
                                ConsoleHelper.WriteLine($"  4. Run: sdo pr create", ConsoleColor.Cyan);
                            }
                            else
                            {
                                // PBI/Feature/Epic workflow - create child items first
                                string nextItemType = normalizedType.Equals("Feature", StringComparison.OrdinalIgnoreCase) || 
                                                     normalizedType.Equals("Epic", StringComparison.OrdinalIgnoreCase) 
                                    ? "PBI" : "Task";
                                string tempFileName = nextItemType.Equals("PBI", StringComparison.OrdinalIgnoreCase) 
                                    ? ".temp/pbi.md" : ".temp/wi.md";
                                
                                ConsoleHelper.WriteLine($"  1. Create a {nextItemType} as a child item", ConsoleColor.Cyan);
                                ConsoleHelper.WriteLine($"  2. Create {nextItemType} markdown in {tempFileName}", ConsoleColor.Cyan);
                                ConsoleHelper.WriteLine($"  3. Run: sdo wi create", ConsoleColor.Cyan);
                                ConsoleHelper.WriteLine($"  4. Then follow the workflow for the {nextItemType}", ConsoleColor.Cyan);
                            }
                            
                            return 0;
                        }
                    }

                    ConsoleHelper.WriteLine("X Failed to create work item", ConsoleColor.Red);
                    if (!string.IsNullOrEmpty(client.LastError))
                    {
                        ConsoleHelper.WriteLine($"  Error: {client.LastError}", ConsoleColor.Red);
                    }
                    return 1;
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                ConsoleHelper.WriteLine($"X {ex.Message}", ConsoleColor.Red);
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Displays a GitHub issue in formatted output.
        /// </summary>
        private void DisplayIssue(GitHubIssue issue, bool includeComments)
        {
            Console.WriteLine();
            Console.WriteLine($"Issue #{issue.Number}");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine($"Title:       {issue.Title}");
            Console.WriteLine($"State:       {issue.State}");
            Console.WriteLine($"Created:     {issue.CreatedAt:O}");
            Console.WriteLine($"Updated:     {issue.UpdatedAt:O}");
            
            // Display Assigned To field
            Console.WriteLine($"Assigned To: {issue.Assignee?.Login ?? "Not assigned"}");

            if (!string.IsNullOrEmpty(issue.Body))
            {
                Console.WriteLine();
                Console.WriteLine("Description:");
                Console.WriteLine(issue.Body);
            }

            if (includeComments && issue.Comments > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Comments: {issue.Comments}");
            }

            Console.WriteLine();
            Console.WriteLine($"URL: {issue.HtmlUrl}");
        }

        /// <summary>
        /// Displays an Azure DevOps work item in formatted output.
        /// </summary>
        private void DisplayWorkItem(AzureDevOpsWorkItem workItem, bool includeComments)
        {
            Console.WriteLine();
            Console.WriteLine($"Work Item #{workItem.Id}");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine($"Title:       {workItem.Title}");
            Console.WriteLine($"Type:        {workItem.Type}");
            if (workItem.ParentId.HasValue)
                Console.WriteLine($"Parent ID:   {workItem.ParentId}");
            Console.WriteLine($"State:       {workItem.State}");
            Console.WriteLine($"Created:     {workItem.CreatedDate:O}");
            Console.WriteLine($"Updated:     {workItem.ChangedDate:O}");
            
            // Display Area, Iteration, and Assigned To fields
            Console.WriteLine($"Area:        {workItem.Area ?? "Not assigned"}");
            Console.WriteLine($"Iteration:   {workItem.Sprint ?? "Not assigned"}");
            Console.WriteLine($"Assigned To: {workItem.AssignedTo ?? "Not assigned"}");

            if (!string.IsNullOrEmpty(workItem.Description))
            {
                Console.WriteLine();
                Console.WriteLine("Description:");
                // Strip HTML tags and format for display
                var cleanedDescription = StripHtmlTags(workItem.Description);
                Console.WriteLine(cleanedDescription);
            }

            // Display acceptance criteria if present
            if (!string.IsNullOrEmpty(workItem.AcceptanceCriteria))
            {
                Console.WriteLine();
                Console.WriteLine("Acceptance Criteria:");
                var cleanedCriteria = StripHtmlTags(workItem.AcceptanceCriteria);
                Console.WriteLine(cleanedCriteria);
            }

            // Validate acceptance criteria for PBI and Epic types
            var isPBIOrEpic = workItem.Type != null && 
                (workItem.Type.Equals("PBI", StringComparison.OrdinalIgnoreCase) || 
                 workItem.Type.Equals("Epic", StringComparison.OrdinalIgnoreCase));
            
            if (isPBIOrEpic && string.IsNullOrEmpty(workItem.AcceptanceCriteria))
            {
                Console.WriteLine();
                ConsoleHelper.WriteLine("⚠ WARNING: PBI/Epic work items should have acceptance criteria defined", ConsoleColor.Yellow);
            }

            if (includeComments && workItem.CommentCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Comments: {workItem.CommentCount}");
            }

            Console.WriteLine();
            // Convert API URL to browsable format
            // From: https://dev.azure.com/org/proj/project/_apis/wit/workItems/123
            // To:   https://dev.azure.com/org/proj/project/_workitems/edit/123
            string displayUrl = workItem.Url ?? string.Empty;
            if (!string.IsNullOrEmpty(displayUrl) && displayUrl.Contains("_apis/wit/workItems/"))
            {
                displayUrl = displayUrl.Replace("/_apis/wit/workItems/", "/_workitems/edit/");
            }
            Console.WriteLine($"URL: {displayUrl}");
        }

        /// <summary>
        /// Strips HTML tags from text and formats it for display.
        /// </summary>
        private string StripHtmlTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Replace common HTML tags with text equivalents
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<h[1-6]>(.*?)</h[1-6]>", "$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<h[1-6]>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</h[1-6]>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?ul>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?ol>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<li>(.*?)</li>", "• $1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?li>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>\s*", "\n");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?p>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?strong>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?em>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</?[^>]+>", ""); // Remove any remaining tags

            // Clean up whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+\n", "\n");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s+", "\n");
            text = text.Trim();

            return text;
        }

        /// <summary>
        /// Displays a list of GitHub issues in table format.
        /// </summary>
        private void DisplayIssuesList(IEnumerable<GitHubIssue> issues)
        {
            var issueList = issues.ToList();

            Console.WriteLine($"gh Issues ({issueList.Count} found):");

            // Define column widths for GitHub issues
            const int numberWidth = 7;
            const int titleWidth = 60;
            const int stateWidth = 12;
            const int labelsWidth = 30;
            const int assigneeWidth = 15;
            
            int totalWidth = numberWidth + titleWidth + stateWidth + labelsWidth + assigneeWidth + 5; // +5 for separators

            // Print header separator
            Console.WriteLine(new string('-', totalWidth));

            // Print column headers
            Console.Write("#".PadRight(numberWidth));
            Console.Write("Title".PadRight(titleWidth));
            Console.Write("State".PadRight(stateWidth));
            Console.Write("Labels".PadRight(labelsWidth));
            Console.Write("Assignee".PadRight(assigneeWidth));
            Console.WriteLine();

            // Print header separator
            Console.WriteLine(new string('-', totalWidth));

            // Print data rows
            foreach (var issue in issueList)
            {
                // Truncate title if too long
                string title = issue.Title ?? "";
                string displayTitle = title.Length > titleWidth - 3
                    ? title.Substring(0, titleWidth - 4) + "..."
                    : title;

                string labels = (issue.Labels != null && issue.Labels.Any())
                    ? string.Join(", ", issue.Labels.Take(3).Select(l => l.Name))
                    : "";
                if (labels.Length > labelsWidth - 3)
                {
                    labels = labels.Substring(0, labelsWidth - 4) + "...";
                }

                string assignee = issue.Assignee?.Login ?? "Unassigned";

                Console.Write(issue.Number.ToString().PadRight(numberWidth));
                Console.Write(displayTitle.PadRight(titleWidth));
                Console.Write((issue.State ?? "").ToUpper().PadRight(stateWidth));
                Console.Write(labels.PadRight(labelsWidth));
                Console.Write(assignee.PadRight(assigneeWidth));
                Console.WriteLine();
            }

            // Print footer separator
            Console.WriteLine(new string('-', totalWidth));

            // Print summary by state
            Console.WriteLine("\n Summary:");
            var grouped = issueList.GroupBy(i => i.State ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key);

            foreach (var group in grouped)
            {
                Console.WriteLine($"  {group.Key.ToUpper()}: {group.Count()}");
            }
        }

        /// <summary>
        /// Displays a list of Azure DevOps work items in table format.
        /// </summary>
        private void DisplayWorkItemsList(IEnumerable<AzureDevOpsWorkItem> workItems)
        {
            var itemList = workItems.ToList();

            Console.WriteLine($"azdo Work Items ({itemList.Count} found):");

            // Define column widths
            const int idWidth = 7;
            const int typeWidth = 21;
            const int titleWidth = 35;
            const int stateWidth = 12;
            const int sprintWidth = 20;
            const int assignedToWidth = 15;
            
            int totalWidth = idWidth + typeWidth + titleWidth + stateWidth + sprintWidth + assignedToWidth + 6; // +6 for separators

            // Print header separator
            Console.WriteLine(new string('-', totalWidth));

            // Print column headers
            Console.Write("ID".PadRight(idWidth));
            Console.Write("Type".PadRight(typeWidth));
            Console.Write("Title".PadRight(titleWidth));
            Console.Write("State".PadRight(stateWidth));
            Console.Write("Sprint".PadRight(sprintWidth));
            Console.Write("Assigned To".PadRight(assignedToWidth));
            Console.WriteLine();

            // Print header separator
            Console.WriteLine(new string('-', totalWidth));

            // Print data rows
            foreach (var item in itemList)
            {
                // Truncate title if too long
                string title = item.Title ?? "";
                string displayTitle = title.Length > titleWidth - 3
                    ? title.Substring(0, titleWidth - 4) + "..."
                    : title;

                string sprint = string.IsNullOrEmpty(item.Sprint) ? "Proto" : item.Sprint;
                string assignedTo = item.AssignedTo ?? "Unassigned";

                Console.Write(item.Id.ToString().PadRight(idWidth));
                Console.Write((item.Type ?? "").PadRight(typeWidth));
                Console.Write(displayTitle.PadRight(titleWidth));
                Console.Write((item.State ?? "").PadRight(stateWidth));
                Console.Write(sprint.PadRight(sprintWidth));
                Console.Write(assignedTo.PadRight(assignedToWidth));
                Console.WriteLine();
            }

            // Print footer separator
            Console.WriteLine(new string('-', totalWidth));

            // Print summary by type
            Console.WriteLine("\n Summary:");
            var grouped = itemList.GroupBy(w => w.Type ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key);

            foreach (var group in grouped)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()}");
            }
        }



        private WorkItemParseResult? ParseWorkItemFromMarkdown(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                // Use the robust MarkdownParser utility class
                var parseResult = Utilities.MarkdownParser.ParseFile(filePath, verbose: false);
                
                if (!parseResult.Success)
                {
                    if (parseResult.Errors.Any())
                    {
                        foreach (var error in parseResult.Errors)
                        {
                            ConsoleHelper.WriteLine($"X Markdown parse error (line {error.LineNumber}): {error.Message}", ConsoleColor.Red);
                        }
                    }
                    return null;
                }

                // Map MarkdownParser result to WorkItemParseResult
                var result = new WorkItemParseResult
                {
                    Title = StripTypePrefix(parseResult.Title),
                    Description = parseResult.Description,
                    AcceptanceCriteria = parseResult.AcceptanceCriteria
                };

                // Normalize and copy metadata from MarkdownParser
                foreach (var kvp in parseResult.Metadata)
                {
                    var normalizedKey = kvp.Key.ToLowerInvariant().Replace(" ", "_");
                    result.Metadata[normalizedKey] = kvp.Value;
                }

                return result;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Failed to parse markdown file: {ex.Message}", ConsoleColor.Red);
                return null;
            }
        }

        /// <summary>
        /// Strips type prefixes from work item titles.
        /// Converts "Bug-001: Title" to "Title"
        /// </summary>
        private string StripTypePrefix(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;
            
            // Pattern: Type-Number: Title
            // Examples: Bug-001: ..., Epic-001: ..., Task-001: ..., PBI-001: ..., etc.
            var match = System.Text.RegularExpressions.Regex.Match(title, @"^(Bug|Epic|Task|PBI|Feature|Story)-\d+:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[2].Value; // Return everything after the colon and space
            }
            
            return title; // Return unchanged if no prefix found
        }

        /// <summary>
        /// Normalizes work item type names to support aliases.
        /// Maps abbreviations like "PBI" to their full names like "Product Backlog Item".
        /// </summary>
        private string NormalizeWorkItemType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return "Product Backlog Item"; // Default

            // Convert to lowercase for comparison, but return proper case
            var lowerType = type.ToLower().Trim();
            return lowerType switch
            {
                "pbi" => "Product Backlog Item",
                "product backlog item" => "Product Backlog Item",
                "task" => "Task",
                "bug" => "Bug",
                "epic" => "Epic",
                "spike" => "Spike",
                "feature" => "Feature",
                "user story" => "User Story",
                // Default: return as-is
                _ => type
            };
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

        private async Task<string?> GetCurrentAzureDevOpsUserAsync(string pat, string organization)
        {
            try
            {
                using var client = new AzureDevOpsClient(pat, organization);
                var user = await client.GetUserAsync();
                return user?.DisplayName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Handles starting work on a work item by creating a feature branch and PR template.
        /// Validates all conditions first before making any changes to the repository.
        /// </summary>
        /// <param name="id">The work item ID.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> StartWorkItem(int? idParam, bool verbose)
        {
            try
            {
                // Auto-detect work item ID from branch name if not provided
                int? detectedId = idParam;
                if (!detectedId.HasValue)
                {
                    detectedId = TryAutoDetectWorkItemIdFromBranch(verbose);
                    if (!detectedId.HasValue)
                    {
                        return 1;
                    }
                }

                int id = (int)detectedId!;

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Starting work on work item {id}...", ConsoleColor.Green);
                }

                // ===== VALIDATION PHASE (no repository changes) =====
                if (verbose)
                {
                    ConsoleHelper.WriteLine("\n=== VALIDATION PHASE ===", ConsoleColor.Cyan);
                }

                // Validate: ID must be positive
                if (id <= 0)
                {
                    ConsoleHelper.WriteLine("X Work item ID must be positive", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Work item ID is valid", ConsoleColor.Yellow);

                // Validate: Detect platform
                if (verbose) ConsoleHelper.WriteLine("\nValidating: Platform Detection", ConsoleColor.Cyan);
                var platform = _platformDetector.DetectPlatform();
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"✓ Detected platform: {platform}", ConsoleColor.Yellow);
                }

                // Validate: Platform-specific prerequisites
                string? workItemTitle = null;
                if (platform == Platform.GitHub)
                {
                    if (verbose) ConsoleHelper.WriteLine("\nValidating: GitHub Prerequisites", ConsoleColor.Cyan);
                    
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || repoInfo.Owner == null || repoInfo.Repo == null)
                    {
                        ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ Repository: {repoInfo.Owner}/{repoInfo.Repo}", ConsoleColor.Yellow);

                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine("✓ GitHub authentication token found", ConsoleColor.Yellow);

                    using var client = new GitHubClient(token);
                    var issue = await client.GetIssueAsync(repoInfo.Owner!, repoInfo.Repo!, id);
                    if (issue == null)
                    {
                        ConsoleHelper.WriteLine($"X GitHub issue #{id} not found", ConsoleColor.Red);
                        return 1;
                    }
                    workItemTitle = issue.Title;
                    if (verbose) ConsoleHelper.WriteLine($"✓ Issue found: {workItemTitle}", ConsoleColor.Yellow);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose) ConsoleHelper.WriteLine("\nValidating: Azure DevOps Prerequisites", ConsoleColor.Cyan);
                    
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine("✓ Azure DevOps authentication token found", ConsoleColor.Yellow);

                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ Organization: {organization}", ConsoleColor.Yellow);

                    var project = _platformDetector.GetProject();
                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var workItem = await client.GetWorkItemAsync(id);
                    if (workItem == null)
                    {
                        ConsoleHelper.WriteLine($"X Work item {id} not found", ConsoleColor.Red);
                        return 1;
                    }
                    workItemTitle = workItem.Title;
                    if (verbose) ConsoleHelper.WriteLine($"✓ Work item found: {workItemTitle}", ConsoleColor.Yellow);
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }

                // Validate: Work item has title
                if (string.IsNullOrEmpty(workItemTitle))
                {
                    ConsoleHelper.WriteLine($"X Work item {id} has no title", ConsoleColor.Red);
                    return 1;
                }

                // Validate: Create branch name
                if (verbose) ConsoleHelper.WriteLine("\nValidating: Branch Name", ConsoleColor.Cyan);
                var branchName = CreateBranchName(id, workItemTitle);
                if (verbose) ConsoleHelper.WriteLine($"✓ Branch name valid: {branchName}", ConsoleColor.Yellow);

                // Validate: PR template exists
                if (verbose) ConsoleHelper.WriteLine("\nValidating: PR Template", ConsoleColor.Cyan);
                string prTemplatePath = GetPullRequestTemplatePath(platform);
                if (string.IsNullOrEmpty(prTemplatePath))
                {
                    ConsoleHelper.WriteLine($"X Unable to determine PR template path for platform {platform}", ConsoleColor.Red);
                    return 1;
                }
                if (!File.Exists(prTemplatePath))
                {
                    ConsoleHelper.WriteLine($"X Pull request template not found at: {prTemplatePath}", ConsoleColor.Red);
                    ConsoleHelper.WriteLine($"", ConsoleColor.Red);
                    ConsoleHelper.WriteLine($"Expected one of:", ConsoleColor.Red);
                    ConsoleHelper.WriteLine($"  - .github/PULL_REQUEST_TEMPLATE/pull_request_template.md (GitHub)", ConsoleColor.Red);
                    ConsoleHelper.WriteLine($"  - .azuredevops/PULL_REQUEST_TEMPLATE.md (Azure DevOps)", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine($"✓ PR template found: {prTemplatePath}", ConsoleColor.Yellow);

                // Validate: .temp directory can be created/accessed
                if (verbose) ConsoleHelper.WriteLine("\nValidating: .temp Directory", ConsoleColor.Cyan);
                var tempDir = ".temp";
                try
                {
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ .temp directory accessible", ConsoleColor.Yellow);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteLine($"X Cannot access .temp directory: {ex.Message}", ConsoleColor.Red);
                    return 1;
                }

                // Validate: Git is available and main branch exists
                if (verbose) ConsoleHelper.WriteLine("\nValidating: Git Environment", ConsoleColor.Cyan);
                var gitWrapper = new GitWrapper(verbose: verbose);
                
                if (!gitWrapper.BranchExists("main"))
                {
                    ConsoleHelper.WriteLine("X Main branch not found in repository", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Main branch exists", ConsoleColor.Yellow);

                // ===== EXECUTION PHASE (all validations passed) =====
                if (verbose)
                {
                    ConsoleHelper.WriteLine("\n=== EXECUTION PHASE ===", ConsoleColor.Cyan);
                }

                // Switch to main branch
                if (verbose) ConsoleHelper.WriteLine("\nStep 1: Switching to main branch", ConsoleColor.Cyan);
                if (!gitWrapper.CheckoutBranch("main"))
                {
                    ConsoleHelper.WriteLine("X Failed to switch to main branch", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Switched to main branch", ConsoleColor.Yellow);

                // Git sync (pull latest)
                if (verbose) ConsoleHelper.WriteLine("\nStep 2: Syncing with remote", ConsoleColor.Cyan);
                if (!gitWrapper.PullWithRebase())
                {
                    ConsoleHelper.WriteLine("⚠ Git pull encountered an issue, but continuing...", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Repository synchronized", ConsoleColor.Yellow);

                // Create feature branch
                if (verbose) ConsoleHelper.WriteLine("\nStep 3: Creating feature branch", ConsoleColor.Cyan);
                if (!gitWrapper.CheckoutBranch(branchName, create: true))
                {
                    ConsoleHelper.WriteLine($"X Failed to create branch {branchName}", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine($"✓ Created feature branch: {branchName}", ConsoleColor.Yellow);

                // Copy PR template
                if (verbose) ConsoleHelper.WriteLine("\nStep 4: Copying PR template", ConsoleColor.Cyan);
                string prMessageFile = Path.Combine(tempDir, $"{id}-pr-message.md");
                try
                {
                    File.Copy(prTemplatePath, prMessageFile, overwrite: true);
                    if (verbose) ConsoleHelper.WriteLine($"✓ Copied PR template to {prMessageFile}", ConsoleColor.Yellow);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteLine($"X Failed to copy PR template: {ex.Message}", ConsoleColor.Red);
                    return 1;
                }

                // Success!
                Console.WriteLine();
                ConsoleHelper.WriteLine($"✓ Work item {id} ready for development", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"  Branch: {branchName}", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"  PR Message: {prMessageFile}", ConsoleColor.Green);
                Console.WriteLine();
                ConsoleHelper.WriteLine("Next steps:", ConsoleColor.Cyan);
                ConsoleHelper.WriteLine($"  1. Implement changes for work item {id}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLine($"  2. Commit your changes to the {branchName} branch", ConsoleColor.Cyan);
                ConsoleHelper.WriteLine($"  3. Edit {prMessageFile} with PR details (optional)", ConsoleColor.Cyan);
                ConsoleHelper.WriteLine($"  4. Run: sdo pr create", ConsoleColor.Cyan);

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Closes a work item and cleans up the feature branch.
        /// </summary>
        /// <param name="idParam">The work item ID (optional; auto-detected from branch name).</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>0 on success, 1 on failure.</returns>
        private async Task<int> CloseWorkItem(int? idParam, bool verbose)
        {
            try
            {
                // Get current branch before auto-detection (we'll need it for deletion)
                var gitWrapper = new GitWrapper(verbose: verbose);
                var currentBranchBeforeClose = gitWrapper.Branch;

                // Auto-detect work item ID from branch name if not provided
                int? detectedId = idParam;
                if (!detectedId.HasValue)
                {
                    detectedId = TryAutoDetectWorkItemIdFromBranch(verbose);
                    if (!detectedId.HasValue)
                    {
                        return 1;
                    }
                }

                int id = (int)detectedId!;

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Closing work item {id}...", ConsoleColor.Green);
                }

                // ===== VALIDATION PHASE (no repository changes) =====
                if (verbose)
                {
                    ConsoleHelper.WriteLine("\n=== VALIDATION PHASE ===", ConsoleColor.Cyan);
                }

                // Validate: ID must be positive
                if (id <= 0)
                {
                    ConsoleHelper.WriteLine("X Work item ID must be positive", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Work item ID is valid", ConsoleColor.Yellow);

                // Validate: Detect platform
                if (verbose) ConsoleHelper.WriteLine("\nValidating: Platform Detection", ConsoleColor.Cyan);
                var platform = _platformDetector.DetectPlatform();
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"✓ Detected platform: {platform}", ConsoleColor.Yellow);
                }

                // Validate: Platform-specific prerequisites and work item existence
                if (platform == Platform.GitHub)
                {
                    if (verbose) ConsoleHelper.WriteLine("\nValidating: GitHub Prerequisites", ConsoleColor.Cyan);
                    
                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    if (repoInfo == null || repoInfo.Owner == null || repoInfo.Repo == null)
                    {
                        ConsoleHelper.WriteLine("X Could not determine GitHub repository from Git remote", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ Repository: {repoInfo.Owner}/{repoInfo.Repo}", ConsoleColor.Yellow);

                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine("✓ GitHub authentication token found", ConsoleColor.Yellow);

                    using var client = new GitHubClient(token);
                    var issue = await client.GetIssueAsync(repoInfo.Owner!, repoInfo.Repo!, id);
                    if (issue == null)
                    {
                        ConsoleHelper.WriteLine($"X GitHub issue #{id} not found", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ Issue found: {issue.Title}", ConsoleColor.Yellow);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose) ConsoleHelper.WriteLine("\nValidating: Azure DevOps Prerequisites", ConsoleColor.Cyan);
                    
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine("✓ Azure DevOps authentication token found", ConsoleColor.Yellow);

                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteLine("X Could not determine Azure DevOps organization", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ Organization: {organization}", ConsoleColor.Yellow);

                    var project = _platformDetector.GetProject();
                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var workItem = await client.GetWorkItemAsync(id);
                    if (workItem == null)
                    {
                        ConsoleHelper.WriteLine($"X Work item {id} not found", ConsoleColor.Red);
                        return 1;
                    }
                    if (verbose) ConsoleHelper.WriteLine($"✓ Work item found: {workItem.Title}", ConsoleColor.Yellow);
                }
                else
                {
                    ConsoleHelper.WriteLine("X Unsupported platform detected", ConsoleColor.Red);
                    return 1;
                }

                // Validate: Git is available and main branch exists
                if (verbose) ConsoleHelper.WriteLine("\nValidating: Git Environment", ConsoleColor.Cyan);
                
                if (!gitWrapper.BranchExists("main"))
                {
                    ConsoleHelper.WriteLine("X Main branch not found in repository", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Main branch exists", ConsoleColor.Yellow);

                // Validate: Current branch is not main (can't close from main without explicit ID)
                if (currentBranchBeforeClose == "main" && !idParam.HasValue)
                {
                    ConsoleHelper.WriteLine("X Cannot auto-detect work item ID when on main branch", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine($"✓ Currently on branch: {currentBranchBeforeClose}", ConsoleColor.Yellow);

                // ===== EXECUTION PHASE (all validations passed) =====
                if (verbose)
                {
                    ConsoleHelper.WriteLine("\n=== EXECUTION PHASE ===", ConsoleColor.Cyan);
                }

                // Switch to main branch
                if (verbose) ConsoleHelper.WriteLine("\nStep 1: Switching to main branch", ConsoleColor.Cyan);
                if (!gitWrapper.CheckoutBranch("main"))
                {
                    ConsoleHelper.WriteLine("X Failed to switch to main branch", ConsoleColor.Red);
                    return 1;
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Switched to main branch", ConsoleColor.Yellow);

                // Pull latest from origin
                if (verbose) ConsoleHelper.WriteLine("\nStep 2: Pulling latest changes from main", ConsoleColor.Cyan);
                if (!gitWrapper.PullWithRebase())
                {
                    ConsoleHelper.WriteLine("⚠ Git pull encountered an issue, but continuing...", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine("✓ Main branch synchronized", ConsoleColor.Yellow);

                // Delete feature branch
                if (verbose) ConsoleHelper.WriteLine($"\nStep 3: Deleting feature branch '{currentBranchBeforeClose}'", ConsoleColor.Cyan);
                if (!gitWrapper.DeleteBranch(currentBranchBeforeClose))
                {
                    ConsoleHelper.WriteLine($"⚠ Failed to delete branch '{currentBranchBeforeClose}', but work item closed", ConsoleColor.Yellow);
                }
                else
                {
                    if (verbose) ConsoleHelper.WriteLine($"✓ Deleted feature branch: {currentBranchBeforeClose}", ConsoleColor.Yellow);
                }

                // Success!
                Console.WriteLine();
                ConsoleHelper.WriteLine($"✓ Work item {id} closed successfully", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"  Deleted branch: {currentBranchBeforeClose}", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"  Current branch: main", ConsoleColor.Green);
                Console.WriteLine();

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"X Error: {ex.Message}", ConsoleColor.Red);
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Stack trace: {ex.StackTrace}", ConsoleColor.Red);
                }
                return 1;
            }
        }

        /// <summary>
        /// Creates a branch name from work item ID and title.
        /// Format: <id>-<slugified-title>
        /// </summary>
        private string CreateBranchName(int id, string title)
        {
            // Slugify the title: lowercase, replace spaces and special chars with hyphens
            var slug = System.Text.RegularExpressions.Regex.Replace(title, @"[^a-z0-9\s-]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").ToLowerInvariant();
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');
            
            // Limit slug length to avoid excessively long branch names
            if (slug.Length > 50)
            {
                slug = slug.Substring(0, 50).TrimEnd('-');
            }

            return $"{id}-{slug}";
        }

        /// <summary>
        /// Gets the path to the pull request template for the current platform.
        /// </summary>
        private string GetPullRequestTemplatePath(Platform platform)
        {
            if (platform == Platform.GitHub)
            {
                return Path.Combine(".github", "PULL_REQUEST_TEMPLATE", "pull_request_template.md");
            }
            else if (platform == Platform.AzureDevOps)
            {
                return Path.Combine(".azuredevops", "PULL_REQUEST_TEMPLATE.md");
            }
            return string.Empty;
        }

        /// <summary>
        /// Attempts to auto-detect work item ID from the current Git branch name.
        /// Returns null and logs errors if auto-detection fails or if on main branch.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>The detected work item ID, or null if auto-detection failed.</returns>
        private int? TryAutoDetectWorkItemIdFromBranch(bool verbose)
        {
            var gitWrapper = new GitWrapper(verbose: verbose);
            var currentBranch = gitWrapper.Branch;

            // Main branch requires explicit --id
            if (currentBranch == "main")
            {
                ConsoleHelper.WriteLine("X Work item ID is required when on main branch. Use --id <number>", ConsoleColor.Red);
                return null;
            }

            // Try to extract ID from branch name
            var workItemId = ExtractWorkItemIdFromBranch(currentBranch);
            if (!workItemId.HasValue)
            {
                ConsoleHelper.WriteLine($"X Could not auto-detect work item ID from branch name '{currentBranch}'", ConsoleColor.Red);
                ConsoleHelper.WriteLine("  Expected branch format: <number>-<description> (e.g., 123-feature-name)", ConsoleColor.Red);
                ConsoleHelper.WriteLine("  Or use --id <number> to specify explicitly", ConsoleColor.Red);
                return null;
            }

            if (verbose)
            {
                ConsoleHelper.WriteLine($"Auto-detected work item ID {workItemId} from branch '{currentBranch}'", ConsoleColor.Green);
            }

            return workItemId;
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
    }
}
