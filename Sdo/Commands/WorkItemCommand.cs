// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItemCommand.cs
//
// This file contains the WorkItemCommand class for managing work items
// across GitHub Issues and Azure DevOps work items.

using System.CommandLine;
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

            // Add subcommands
            AddShowCommand(verboseOption);
            AddListCommand(verboseOption);
            // Additional subcommands to be implemented in Phase 3.1:
            // - create
            // - update
            // - comment
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
                    Console.WriteLine($"Detecting platform and retrieving work item {id}...");
                }

                var platform = _platformDetector.DetectPlatform();

                if (verbose)
                {
                    Console.WriteLine($"Detected platform: {platform}");
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
                    Console.WriteLine("✗ Unsupported platform detected");
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✗ {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
                    Console.WriteLine("✗ Could not determine GitHub repository from Git remote");
                    return 1;
                }

                if (verbose)
                {
                    Console.WriteLine($"Fetching GitHub issue: {repoInfo.Owner}/{repoInfo.Repo}#{issueNumber}");
                }

                var issue = await client.GetIssueAsync(repoInfo.Owner, repoInfo.Repo, issueNumber);

                if (issue == null)
                {
                    Console.WriteLine($"✗ Issue #{issueNumber} not found");
                    return 1;
                }

                DisplayIssue(issue, includeComments);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to fetch GitHub issue: {ex.Message}");
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
                    Console.WriteLine("✗ AZURE_DEVOPS_PAT environment variable not set");
                    return 1;
                }

                var organization = _platformDetector.GetOrganization();
                if (string.IsNullOrEmpty(organization))
                {
                    Console.WriteLine("✗ Could not determine Azure DevOps organization from Git remote");
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
                    Console.WriteLine($"✗ Work item #{workItemId} not found");
                    return 1;
                }

                DisplayWorkItem(workItem, includeComments);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to fetch Azure DevOps work item: {ex.Message}");
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
                    Console.WriteLine("✗ Unsupported platform detected");
                    return 1;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✗ {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
                    Console.WriteLine("✗ Could not determine GitHub repository from Git remote");
                    return 1;
                }

                if (verbose)
                {
                    Console.WriteLine($"Fetching GitHub issues: {repoInfo.Owner}/{repoInfo.Repo}");
                }

                // Determine page size: use larger page when filtering to ensure sufficient filtered results
                // If --top is specified, fetch at least 100 items to ensure enough after filtering
                int pageSize = Math.Max(100, top);
                if (verbose)
                {
                    Console.WriteLine($"Fetching with perPage={pageSize} (filtering results to {top})");
                }

                var issues = await client.ListIssuesAsync(repoInfo.Owner, repoInfo.Repo, pageSize);

                if (issues == null)
                {
                    if (verbose)
                    {
                        Console.WriteLine("API returned null");
                    }
                    Console.WriteLine("No issues found");
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
                    Console.WriteLine("No issues found");
                    return 0;
                }

                DisplayIssuesList(issues);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to list GitHub issues: {ex.Message}");
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
                    Console.WriteLine("✗ AZURE_DEVOPS_PAT environment variable not set");
                    return 1;
                }

                var organization = _platformDetector.GetOrganization();
                if (string.IsNullOrEmpty(organization))
                {
                    Console.WriteLine("✗ Could not determine Azure DevOps organization from Git remote");
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
                        Console.WriteLine("✗ Failed to retrieve work items from Azure DevOps");
                        if (!string.IsNullOrEmpty(client.LastError))
                        {
                            Console.WriteLine($"  Error: {client.LastError}");
                            
                            // Provide specific guidance for common errors
                            if (client.LastError.Contains("Unauthorized"))
                            {
                                Console.WriteLine("  ");
                                Console.WriteLine("  This typically means:");
                                Console.WriteLine("    1. The AZURE_DEVOPS_PAT token doesn't have 'Work Item Query' permissions");
                                Console.WriteLine("    2. The PAT scope doesn't include 'vso.work_read'");
                                Console.WriteLine("    3. The project has work items disabled");
                                Console.WriteLine("  ");
                                Console.WriteLine("  Solution: Create a PAT with these scopes:");
                                Console.WriteLine("    - Work Item > Read");
                                Console.WriteLine("    - Code > Read");
                            }
                        }
                        else
                        {
                            Console.WriteLine("  Possible causes:");
                            Console.WriteLine("    - AZURE_DEVOPS_PAT token permissions are insufficient");
                            Console.WriteLine("    - Work items feature is disabled in the project");
                            Console.WriteLine($"  Organization: {organization}");
                            Console.WriteLine($"  Project: {project ?? "(not specified)"}");
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
                Console.WriteLine($"✗ Failed to list Azure DevOps work items: {ex.Message}");
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
