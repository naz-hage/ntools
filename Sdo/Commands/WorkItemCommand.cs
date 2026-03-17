// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItemCommand.cs
//
// This file contains the WorkItemCommand class for managing work items
// across GitHub Issues and Azure DevOps work items.

using System.CommandLine;
using Nbuild.Helpers;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command for managing work items across GitHub and Azure DevOps.
    /// </summary>
    public class WorkItemCommand : Command
    {
        private readonly PlatformService _platformDetector;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkItemCommand"/> class.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        public WorkItemCommand(Option<bool> verboseOption) : base("wi", "Work item management commands")
        {
            _platformDetector = new PlatformService();

            // Add subcommands (in alphabetical order for help display)
            AddCommentCommand(verboseOption);
            AddCreateCommand(verboseOption);
            AddListCommand(verboseOption);
            AddShowCommand(verboseOption);
            AddUpdateCommand(verboseOption);
        }

        /// <summary>
        /// Adds the 'show' subcommand to display work item details.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        private void AddShowCommand(Option<bool> verboseOption)
        {
            var showCommand = new Command("show", "Display detailed work item information");

            var idOption = new Option<int>("--id") { Description = "Work item ID (required)" };
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
            var topOption = new Option<int>("--top") { Description = "Maximum number of items to return (default: 50)" };

            listCommand.Add(typeOption);
            listCommand.Add(stateOption);
            listCommand.Add(assignedToOption);
            listCommand.Add(assignedToMeOption);
            listCommand.Add(topOption);
            listCommand.Add(verboseOption);

            listCommand.SetAction(async (parseResult) =>
            {
                var type = parseResult.GetValue(typeOption);
                var state = parseResult.GetValue(stateOption);
                var assignedTo = parseResult.GetValue(assignedToOption);
                var assignedToMe = parseResult.GetValue(assignedToMeOption);
                var top = parseResult.GetValue(topOption);
                var verbose = parseResult.GetValue(verboseOption);

                return await ListWorkItems(type, state, assignedTo, assignedToMe, top, verbose);
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

            var titleOption = new Option<string>("--title") { Description = "Work item title (required)" };
            var typeOption = new Option<string>("--type") { Description = "Work item type (PBI, Bug, Task, etc.)" };
            var descriptionOption = new Option<string?>("--description") { Description = "Work item description" };
            var assigneeOption = new Option<string?>("--assignee") { Description = "Assign to user" };

            createCommand.Add(titleOption);
            createCommand.Add(typeOption);
            createCommand.Add(descriptionOption);
            createCommand.Add(assigneeOption);
            createCommand.Add(verboseOption);

            createCommand.SetAction(async (parseResult) =>
            {
                var title = parseResult.GetValue(titleOption);
                var type = parseResult.GetValue(typeOption);
                var description = parseResult.GetValue(descriptionOption);
                var assignee = parseResult.GetValue(assigneeOption);
                var verbose = parseResult.GetValue(verboseOption);

                return await CreateWorkItem(title!, type!, description, assignee, verbose);
            });

            Subcommands.Add(createCommand);
        }

        /// <summary>
        /// Handles displaying work item details.
        /// </summary>
        /// <param name="id">The work item ID.</param>
        /// <param name="includeComments">Whether to include comments/discussion.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private async Task<int> ShowWorkItem(int id, bool includeComments, bool verbose)
        {
            try
            {
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detecting platform and retrieving work item {id}...", ConsoleColor.Green);
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detected platform: {platform}", ConsoleColor.Green);
                }

                if (platform == Platform.GitHub)
                {
                    return await ShowGitHubIssue(id, includeComments, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    return await ShowAzureDevOpsWorkItem(id, includeComments, verbose);
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
                var client = new GitHubClient();
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
                var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteLine("X AZURE_DEVOPS_PAT environment variable not set", ConsoleColor.Red);
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
            bool assignedToMe, int top, bool verbose)
        {
            // Use default value of 50 if top is not specified (0 is the default for int)
            if (top <= 0)
            {
                top = 50;
            }

            try
            {
                if (verbose)
                {
                    Console.WriteLine("Detecting platform and listing work items...");
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    Console.WriteLine($"Detected platform: {platform}");
                    if (!string.IsNullOrEmpty(type)) Console.WriteLine($"Type filter: {type}");
                    if (!string.IsNullOrEmpty(state)) Console.WriteLine($"State filter: {state}");
                    if (assignedToMe) Console.WriteLine("Assigned to me filter: enabled");
                }

                if (platform == Platform.GitHub)
                {
                    return await ListGitHubIssues(type, state, assignedTo, assignedToMe, top, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    return await ListAzureDevOpsWorkItems(type, state, assignedTo, assignedToMe, top, verbose);
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
                var client = new GitHubClient();
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

                var issues = await client.ListIssuesAsync(repoInfo.Owner, repoInfo.Repo, pageSize);

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
                if (string.IsNullOrEmpty(state))
                {
                    // Default: exclude closed issues
                    issues = issues.Where(i => i.State?.ToLower() != "closed").ToList();
                }
                else
                {
                    // Filter to specific state
                    issues = issues.Where(i => i.State?.ToLower() == state.ToLower()).ToList();
                }

                // Limit to requested number
                if (top > 0 && issues.Count > top)
                {
                    issues = issues.Take(top).ToList();
                }

                if (!issues.Any())
                {
                    ConsoleHelper.WriteLine("No issues found", ConsoleColor.Red);
                    return 0;
                }

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
            bool assignedToMe, int top, bool verbose)
        {
            try
            {
                var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteLine("X AZURE_DEVOPS_PAT environment variable not set", ConsoleColor.Red);
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

                var workItems = await client.ListWorkItemsAsync(top);

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

                // Filter by state (only filter if explicitly requested)
                if (!string.IsNullOrEmpty(state))
                {
                    // Filter to specific state
                    workItems = workItems.Where(w => w.State?.ToLower() == state.ToLower()).ToList();
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

                    using var client = new GitHubClient();

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Updating GitHub issue #{id} in {repoInfo.Owner}/{repoInfo.Repo}...", ConsoleColor.Yellow);
                    }

                    var result = await client.UpdateIssueAsync(repoInfo.Owner!, repoInfo.Repo!, id, title, state, description, assignee);

                    if (result != null)
                    {
                        ConsoleHelper.WriteLine($"✓ GitHub issue #{id} updated successfully", ConsoleColor.Green);
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
                        return 1;
                    }
                }
                else if (platform == Platform.AzureDevOps)
                {
                    // Get Azure DevOps configuration
                    var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X AZURE_DEVOPS_PAT environment variable not set", ConsoleColor.Red);
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

                    var result = await client.UpdateWorkItemAsync(id, title, state, assignee, description);

                    if (result != null)
                    {
                        ConsoleHelper.WriteLine($"✓ Work item {id} updated successfully", ConsoleColor.Green);
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

                    using var client = new GitHubClient();

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
                    var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X AZURE_DEVOPS_PAT environment variable not set", ConsoleColor.Red);
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
        private async Task<int> CreateWorkItem(string title, string type, string? description, string? assignee, bool verbose)
        {
            try
            {
                if (string.IsNullOrEmpty(title))
                {
                    ConsoleHelper.WriteLine("X Title is required for creating a work item", ConsoleColor.Red);
                    return 1;
                }

                if (string.IsNullOrEmpty(type))
                {
                    ConsoleHelper.WriteLine("X Type is required for creating a work item (PBI, Bug, Task, etc.)", ConsoleColor.Red);
                    return 1;
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Detecting platform and creating new work item...", ConsoleColor.Gray);
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    Console.WriteLine($"Detected platform: {platform}");
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

                    using var client = new GitHubClient();

                    if (verbose)
                    {
                        ConsoleHelper.WriteLine($"Creating GitHub issue in {repoInfo.Owner}/{repoInfo.Repo}...", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Description: {description ?? "(none)"}", ConsoleColor.Gray);
                    }

                    // TODO: Implement GitHub issue creation in Phase 3.2
                    ConsoleHelper.WriteLine("ℹ GitHub issue creation endpoint would be called here", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"  Description: {description ?? "(none)"}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine("✓ GitHub issue created (placeholder - not yet implemented)", ConsoleColor.Green);
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    // Get Azure DevOps configuration
                    var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteLine("X AZURE_DEVOPS_PAT environment variable not set", ConsoleColor.Red);
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
                        ConsoleHelper.WriteLine($"Creating Azure DevOps work item in {organization}/{project ?? "default"}...", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Type: {type}", ConsoleColor.Gray);
                        ConsoleHelper.WriteLine($"  Description: {description ?? "(none)"}", ConsoleColor.Gray);
                        if (!string.IsNullOrEmpty(assignee)) ConsoleHelper.WriteLine($"  Assignee: {assignee}", ConsoleColor.Gray);
                    }

                    // TODO: Implement Azure DevOps work item creation in Phase 3.2
                    ConsoleHelper.WriteLine("ℹ Azure DevOps work item creation endpoint would be called here", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"  Title: {title}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"  Type: {type}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"  Description: {description ?? "(none)"}", ConsoleColor.Gray);
                    if (!string.IsNullOrEmpty(assignee)) ConsoleHelper.WriteLine($"  Assignee: {assignee}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine("✓ Azure DevOps work item created (placeholder - not yet implemented)", ConsoleColor.Green);
                    return 0;
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
            Console.WriteLine($"State:       {workItem.State}");
            Console.WriteLine($"Created:     {workItem.CreatedDate:O}");
            Console.WriteLine($"Updated:     {workItem.ChangedDate:O}");

            if (!string.IsNullOrEmpty(workItem.Description))
            {
                Console.WriteLine();
                Console.WriteLine("Description:");
                Console.WriteLine(workItem.Description);
            }

            if (includeComments && workItem.CommentCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Comments: {workItem.CommentCount}");
            }

            Console.WriteLine();
            Console.WriteLine($"URL: {workItem.Url}");
        }

        /// <summary>
        /// Displays a list of GitHub issues in table format.
        /// </summary>
        private void DisplayIssuesList(IEnumerable<GitHubIssue> issues)
        {
            var issueList = issues.ToList();
            
            Console.WriteLine();
            Console.WriteLine($"📋 Issues ({issueList.Count} found):");
            Console.WriteLine("-".PadRight(120, '-'));
            Console.WriteLine($"{"#",-6} {"Title",-45} {"State",-7} {"Labels",-30} {"Assignee",-15}");
            Console.WriteLine("-".PadRight(120, '-'));

            foreach (var issue in issueList)
            {
                var title = (issue.Title ?? "").Length > 45 ? (issue.Title ?? "").Substring(0, 42) + "..." : issue.Title ?? "";
                var state = (issue.State ?? "unknown").ToUpper();
                var labels = issue.Labels != null && issue.Labels.Any() 
                    ? string.Join(", ", issue.Labels.Select(l => l.Name)) 
                    : "";
                labels = labels.Length > 30 ? labels.Substring(0, 27) + "..." : labels;
                var assignee = issue.Assignee?.Login ?? "Unassigned";
                
                Console.WriteLine($"#{issue.Number,-5} {title,-45} {state,-7} {labels,-30} {assignee,-15}");
            }
            
            Console.WriteLine("-".PadRight(120, '-'));
            Console.WriteLine($"\n📊 Total: {issueList.Count} issue(s)");
        }

        /// <summary>
        /// Displays a list of Azure DevOps work items in table format.
        /// </summary>
        private void DisplayWorkItemsList(IEnumerable<AzureDevOpsWorkItem> workItems)
        {
            var itemList = workItems.ToList();
            
            Console.WriteLine();
            Console.WriteLine($"📋 Work Items ({itemList.Count} found):");
            Console.WriteLine("-".PadRight(140, '-'));
            Console.WriteLine($"{"ID",-6} {"Type",-20} {"Title",-35} {"State",-12} {"Sprint",-20} {"Assigned To",-15}");
            Console.WriteLine("-".PadRight(140, '-'));

            foreach (var item in itemList)
            {
                var type = (item.Type ?? "N/A");
                var title = (item.Title ?? "").Length > 35 ? (item.Title ?? "").Substring(0, 32) + "..." : item.Title ?? "";
                var state = (item.State ?? "N/A");
                var sprint = item.Sprint ?? "";
                sprint = sprint.Length > 20 ? sprint.Substring(0, 17) + "..." : sprint;
                var assignedTo = item.AssignedTo ?? "Unassigned";
                assignedTo = assignedTo.Length > 15 ? assignedTo.Substring(0, 12) + "..." : assignedTo;
                
                Console.WriteLine($"{item.Id,-6} {type,-20} {title,-35} {state,-12} {sprint,-20} {assignedTo,-15}");
            }
            
            Console.WriteLine("-".PadRight(140, '-'));
            Console.WriteLine($"\n📊 Summary:");
            
            // Summary by type
            var typeCounts = itemList.GroupBy(x => x.Type ?? "Unknown").OrderBy(g => g.Key);
            foreach (var typeGroup in typeCounts)
            {
                Console.WriteLine($"  {typeGroup.Key}: {typeGroup.Count()}");
            }
        }
    }
}
