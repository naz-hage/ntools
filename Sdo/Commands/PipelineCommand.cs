// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PipelineCommand.cs
//
// Command handler for pipeline/workflow management operations.
// Supports both GitHub Actions workflows and Azure DevOps pipelines.

using System.CommandLine;
using Nbuild.Helpers;
using Sdo.Interfaces;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for pipeline/workflow management operations.
    /// </summary>
    public class PipelineCommand : System.CommandLine.Command
    {
        private readonly PlatformService _platformDetector;
        private readonly Sdo.Mapping.IMappingGenerator _mappingGenerator;
        private readonly Sdo.Mapping.IMappingPresenter _mappingPresenter;

        /// <summary>
        /// Initializes a new instance of the PipelineCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public PipelineCommand(Option<bool> verboseOption, Sdo.Mapping.IMappingGenerator? mappingGenerator = null, Sdo.Mapping.IMappingPresenter? mappingPresenter = null)
            : base("pipeline", "Pipeline/workflow management commands (create, show, list, run, status, logs, delete, lastbuild, update)")
        {
            _platformDetector = new PlatformService();
            _mappingGenerator = mappingGenerator ?? new Sdo.Mapping.MappingGenerator();
            _mappingPresenter = mappingPresenter ?? new Sdo.Mapping.ConsoleMappingPresenter();

            // Add subcommands in alphabetical order
            AddCreateCommand(verboseOption);
            AddDeleteCommand(verboseOption);
            AddLastbuildCommand(verboseOption);
            AddListCommand(verboseOption);
            AddLogsCommand(verboseOption);
            AddRunCommand(verboseOption);
            AddShowCommand(verboseOption);
            AddStatusCommand(verboseOption);
            AddUpdateCommand(verboseOption);
        }

        private void AddCreateCommand(Option<bool> verboseOption)
        {
            var createCommand = new System.CommandLine.Command("create", "Create a new pipeline/workflow from YAML definition file");
            var fileArg = new Argument<string>("file-path") { Description = "Path to pipeline/workflow YAML definition file" };

            createCommand.Add(fileArg);
            createCommand.Add(verboseOption);

            createCommand.SetAction(async (parseResult) =>
            {
                var filePath = parseResult.GetValue(fileArg);
                var verbose = parseResult.GetValue(verboseOption);
                return await CreatePipeline(filePath!, verbose);
            });

            Subcommands.Add(createCommand);
        }

        private void AddDeleteCommand(Option<bool> verboseOption)
        {
            var deleteCommand = new System.CommandLine.Command("delete", "Delete a pipeline/workflow");
            var pipelineIdArg = new Argument<string?>("pipeline-id") { Arity = ArgumentArity.ZeroOrOne, Description = "Pipeline/workflow ID or name (optional, uses current repo if not provided)" };
            var forceOption = new Option<bool>("--force") { Description = "Skip confirmation prompt" };

            deleteCommand.Add(pipelineIdArg);
            deleteCommand.Add(forceOption);
            deleteCommand.Add(verboseOption);

            deleteCommand.SetAction(async (parseResult) =>
            {
                var pipelineId = parseResult.GetValue(pipelineIdArg);
                var force = parseResult.GetValue(forceOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await DeletePipeline(pipelineId, force, verbose);
            });

            Subcommands.Add(deleteCommand);
        }

        private void AddLastbuildCommand(Option<bool> verboseOption)
        {
            var lastbuildCommand = new System.CommandLine.Command("lastbuild", "Show last build/run for a pipeline");
            var pipelineNameArg = new Argument<string?>("pipeline-name") { Arity = ArgumentArity.ZeroOrOne, Description = "Pipeline name (optional, uses current repo if not provided)" };

            lastbuildCommand.Add(pipelineNameArg);
            lastbuildCommand.Add(verboseOption);

            lastbuildCommand.SetAction(async (parseResult) =>
            {
                var pipelineName = parseResult.GetValue(pipelineNameArg);
                var verbose = parseResult.GetValue(verboseOption);
                return await LastBuild(pipelineName, verbose);
            });

            Subcommands.Add(lastbuildCommand);
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new System.CommandLine.Command("list", "List pipelines/workflows");
            var repoOption = new Option<string?>("--repo") { Description = "Filter by repository name" };
            var allOption = new Option<bool>("--all") { Description = "Show all pipelines in the project" };

            listCommand.Add(repoOption);
            listCommand.Add(allOption);
            listCommand.Add(verboseOption);

            listCommand.SetAction(async (parseResult) =>
            {
                var repo = parseResult.GetValue(repoOption);
                var showAll = parseResult.GetValue(allOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await ListPipelines(repo, showAll, verbose);
            });

            Subcommands.Add(listCommand);
        }

        private void AddLogsCommand(Option<bool> verboseOption)
        {
            var logsCommand = new System.CommandLine.Command("logs", "Retrieve pipeline execution logs");
            var buildIdArg = new Argument<int?>("build-id") { Arity = ArgumentArity.ZeroOrOne, Description = "Build/run ID (optional, shows latest if not provided)" };
            var buildIdOption = new Option<int?>("--build-id") { Description = "Build/run ID" };

            logsCommand.Add(buildIdArg);
            logsCommand.Add(buildIdOption);
            logsCommand.Add(verboseOption);

            logsCommand.SetAction(async (parseResult) =>
            {
                var buildId = parseResult.GetValue(buildIdArg) ?? parseResult.GetValue(buildIdOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await GetLogs(buildId, verbose);
            });

            Subcommands.Add(logsCommand);
        }

        private void AddRunCommand(Option<bool> verboseOption)
        {
            var runCommand = new System.CommandLine.Command("run", "Execute/trigger a pipeline");
            var pipelineNameArg = new Argument<string?>("pipeline-name") { Arity = ArgumentArity.ZeroOrOne, Description = "Pipeline name (optional, uses current repo if not provided)" };
            var branchOption = new Option<string>("--branch") { Aliases = { "-b" }, Description = "Branch to run the pipeline on" };

            runCommand.Add(pipelineNameArg);
            runCommand.Add(branchOption);
            runCommand.Add(verboseOption);

            runCommand.SetAction(async (parseResult) =>
            {
                var pipelineName = parseResult.GetValue(pipelineNameArg);
                var branch = parseResult.GetValue(branchOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await RunPipeline(pipelineName, branch!, verbose);
            });

            Subcommands.Add(runCommand);
        }

        private void AddShowCommand(Option<bool> verboseOption)
        {
            var showCommand = new System.CommandLine.Command("show", "Display pipeline/workflow details");
            showCommand.Add(verboseOption);

            showCommand.SetAction(async (parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                return await ShowPipeline(verbose);
            });

            Subcommands.Add(showCommand);
        }

        private void AddStatusCommand(Option<bool> verboseOption)
        {
            var statusCommand = new System.CommandLine.Command("status", "Query pipeline build/run status");
            var buildIdArg = new Argument<int?>("build-id") { Arity = ArgumentArity.ZeroOrOne, Description = "Build/run ID (optional, shows latest if not provided)" };

            statusCommand.Add(buildIdArg);
            statusCommand.Add(verboseOption);

            statusCommand.SetAction(async (parseResult) =>
            {
                var buildId = parseResult.GetValue(buildIdArg);
                var verbose = parseResult.GetValue(verboseOption);
                return await GetStatus(buildId, verbose);
            });

            Subcommands.Add(statusCommand);
        }

        private void AddUpdateCommand(Option<bool> verboseOption)
        {
            var updateCommand = new System.CommandLine.Command("update", "Update pipeline configuration");
            updateCommand.Add(verboseOption);

            updateCommand.SetAction(async (parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                return await UpdatePipeline(verbose);
            });

            Subcommands.Add(updateCommand);
        }

        #region Command Handlers

        private async Task<int> CreatePipeline(string filePath, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Creating pipeline/workflow from definition file...");

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    ConsoleHelper.WriteError($"X File not found: {filePath}");
                    return 1;
                }

                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                if (fileExtension != ".yml" && fileExtension != ".yaml")
                {
                    ConsoleHelper.WriteError("X Pipeline definition file must be in YAML format (.yml or .yaml)");
                    return 1;
                }

                if (verbose) Console.WriteLine($"[INFO] Reading pipeline definition from {filePath}...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    if (verbose)
                    {
                        Console.WriteLine("Make sure you're in a Git repository with a GitHub or Azure DevOps remote.");
                    }
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        DisplayGitHubMapping("workflow create");
                    }
                    return await CreateGitHubWorkflow(repoInfo.Owner, repoInfo.Repo, filePath, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("pipeline create");
                    }
                    return await CreateAzureDevOpsPipeline(repoInfo.Organization, repoInfo.Project, filePath, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error creating pipeline: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> DeletePipeline(string? pipelineId, bool force, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Deleting pipeline/workflow from current Git remote...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    if (verbose)
                    {
                        Console.WriteLine("Make sure you're in a Git repository with a GitHub or Azure DevOps remote.");
                    }
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        DisplayGitHubMapping("workflow delete");
                    }
                    return await DeleteGitHubWorkflow(repoInfo.Owner, repoInfo.Repo, pipelineId, force, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("pipeline delete");
                    }
                    return await DeleteAzureDevOpsPipeline(repoInfo.Organization, repoInfo.Project, pipelineId, force, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error deleting pipeline: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> LastBuild(string? pipelineName, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Retrieving last build/run information from current Git remote...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    if (verbose)
                    {
                        Console.WriteLine("Make sure you're in a Git repository with a GitHub or Azure DevOps remote.");
                    }
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        DisplayGitHubMapping("workflow lastbuild");
                    }
                    return await GetGitHubLastRun(repoInfo.Owner, repoInfo.Repo, pipelineName, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("pipeline lastbuild");
                    }
                    return await GetAzureDevOpsLastBuild(repoInfo.Organization, repoInfo.Project, pipelineName, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error retrieving last build: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ListPipelines(string? repo, bool showAll, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Listing pipeline/workflow information from current Git remote...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    if (verbose)
                    {
                        Console.WriteLine("Make sure you're in a Git repository with a GitHub or Azure DevOps remote.");
                    }
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        DisplayGitHubMapping("workflow list");
                    }
                    return await ListGitHubWorkflows(repoInfo.Owner, repoInfo.Repo, repo, showAll, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("pipeline list");
                    }
                    return await ListAzureDevOpsPipelines(repoInfo.Organization, repoInfo.Project, repo, showAll, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error listing pipelines: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ListGitHubWorkflows(string owner, string repo, string? filterRepo, bool showAll, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                string filterDisplay = showAll ? "all workflows" : "workflows for this repository";
                if (!string.IsNullOrEmpty(filterRepo))
                {
                    filterDisplay = $"workflows for repository: {filterRepo}";
                }

                if (verbose)
                {
                    Console.WriteLine($"Workflows location: .github/workflows/");
                    Console.WriteLine($"Filter: {filterDisplay}");
                }

                // Fetch workflows from GitHub API
                using (var client = new Sdo.Services.GitHubClient())
                {
                    var workflows = await client.ListWorkflowsAsync(owner, repo);

                    if (workflows == null || workflows.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No workflows found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }

                    // Apply repository name filter if specified
                    if (!string.IsNullOrEmpty(filterRepo))
                    {
                        workflows = workflows.Where(w =>
                            (w.Name ?? "").Contains(filterRepo, StringComparison.OrdinalIgnoreCase) ||
                            (w.Path ?? "").Contains(filterRepo, StringComparison.OrdinalIgnoreCase)
                        ).ToList();

                        if (workflows.Count == 0)
                        {
                            ConsoleHelper.WriteLine($"No workflows found matching repository filter: {filterRepo}", ConsoleColor.Yellow);
                            return 0;
                        }
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Workflows in this repository:", ConsoleColor.Cyan);
                    Console.WriteLine();
                    Console.WriteLine($"{"Name",-40} {"State",-10} {"ID"}");
                    Console.WriteLine(new string('-', 60));

                    foreach (var workflow in workflows)
                    {
                        var name = workflow.Name ?? "Unnamed Workflow";
                        if (name.Length > 38)
                        {
                            name = name.Substring(0, 35) + "...";
                        }

                        Console.WriteLine($"{name,-40} {workflow.State,-10} {workflow.Id}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to list GitHub workflows: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ListAzureDevOpsPipelines(string? organization, string? project, string? filterRepo, bool showAll, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);

                string filterDisplay = showAll ? "all pipelines" : "pipelines for this project";
                if (!string.IsNullOrEmpty(filterRepo))
                {
                    filterDisplay = $"pipelines for repository: {filterRepo}";
                }

                if (verbose)
                {
                    Console.WriteLine($"Pipeline location: azure-pipelines/");
                    Console.WriteLine($"Filter: {filterDisplay}");
                }

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                // Get Azure DevOps credentials
                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                using (var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project))
                {
                    var pipelines = await client.ListPipelinesAsync(project);

                    if (pipelines == null || pipelines.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No pipelines found in this project.", ConsoleColor.Yellow);
                        return 0;
                    }

                    // Apply repository name filter if specified
                    if (!string.IsNullOrEmpty(filterRepo))
                    {
                        pipelines = pipelines.Where(p =>
                            (p.Name ?? "").Contains(filterRepo, StringComparison.OrdinalIgnoreCase) ||
                            (p.Path ?? "").Contains(filterRepo, StringComparison.OrdinalIgnoreCase)
                        ).ToList();

                        if (pipelines.Count == 0)
                        {
                            ConsoleHelper.WriteLine($"No pipelines found matching repository filter: {filterRepo}", ConsoleColor.Yellow);
                            return 0;
                        }
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Pipelines in this project:", ConsoleColor.Cyan);
                    Console.WriteLine();
                    Console.WriteLine($"{"Name",-40} {"Status",-15} {"ID"}");
                    Console.WriteLine(new string('-', 60));

                    foreach (var pipeline in pipelines)
                    {
                        var name = pipeline.Name ?? "Unnamed Pipeline";
                        if (name.Length > 38)
                        {
                            name = name.Substring(0, 35) + "...";
                        }

                        var status = pipeline.QueueStatus ?? "unknown";
                        Console.WriteLine($"{name,-40} {status,-15} {pipeline.Id}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to list Azure DevOps pipelines: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetLogs(int? buildId, bool verbose)
        {
            Console.WriteLine("X Pipeline logs command not yet implemented");
            return 1;
        }

        private async Task<int> RunPipeline(string? pipelineName, string branch, bool verbose)
        {
            Console.WriteLine("X Pipeline run command not yet implemented");
            return 1;
        }

        private async Task<int> ShowPipeline(bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Retrieving pipeline information from current Git remote...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    if (verbose)
                    {
                        Console.WriteLine("Make sure you're in a Git repository with a GitHub or Azure DevOps remote.");
                    }
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        DisplayGitHubMapping("workflow");
                    }
                    return await ShowGitHubWorkflow(repoInfo.Owner, repoInfo.Repo, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("pipeline show");
                    }
                    return await ShowAzureDevOpsPipeline(repoInfo.Organization, repoInfo.Project, repoInfo.Repository, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error showing pipeline: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ShowGitHubWorkflow(string owner, string repo, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"Workflows location: .github/workflows/");
                }

                // Fetch workflows from GitHub API
                using (var client = new Sdo.Services.GitHubClient())
                {
                    var workflows = await client.ListWorkflowsAsync(owner, repo);

                    if (workflows == null || workflows.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No workflows found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Workflows in this repository:", ConsoleColor.Cyan);
                    Console.WriteLine();

                    foreach (var workflow in workflows)
                    {
                        var stateColor = workflow.State == "active" ? ConsoleColor.Green : ConsoleColor.Yellow;
                        ConsoleHelper.WriteLine($"  • {workflow.Name ?? "Unnamed Workflow"}", stateColor);
                        Console.WriteLine($"    State: {workflow.State}");
                        Console.WriteLine($"    ID: {workflow.Id}");
                        if (!string.IsNullOrEmpty(workflow.Path))
                        {
                            Console.WriteLine($"    Path: {workflow.Path}");
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to show GitHub workflows: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ShowAzureDevOpsPipeline(string? organization, string? project, string? repository, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"✓ Repository: {repository ?? "unknown"}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"Pipeline location: azure-pipelines/");
                }

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                // Fetch pipelines from Azure DevOps API
                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                using (var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project))
                {
                    var pipelines = await client.ListPipelinesAsync(project);

                    if (pipelines == null || pipelines.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No pipelines found in this project.", ConsoleColor.Yellow);
                        return 0;
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Pipelines in this project:", ConsoleColor.Cyan);
                    Console.WriteLine();

                    foreach (var pipeline in pipelines)
                    {
                        var statusColor = pipeline.QueueStatus == "enabled" ? ConsoleColor.Green : ConsoleColor.Yellow;
                        ConsoleHelper.WriteLine($"  • {pipeline.Name ?? "Unnamed Pipeline"}", statusColor);
                        Console.WriteLine($"    ID: {pipeline.Id}");
                        Console.WriteLine($"    Status: {pipeline.QueueStatus}");
                        if (!string.IsNullOrEmpty(pipeline.Path))
                        {
                            Console.WriteLine($"    Path: {pipeline.Path}");
                        }
                        if (pipeline.CreatedDate.HasValue)
                        {
                            Console.WriteLine($"    Created: {pipeline.CreatedDate:g}");
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to show Azure DevOps pipelines: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> DeleteGitHubWorkflow(string owner, string repo, string? workflowId, bool force, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"[INFO] Deleting workflow from {owner}/{repo}");
                    Console.WriteLine($"Workflows location: .github/workflows/");
                }

                if (string.IsNullOrEmpty(workflowId))
                {
                    ConsoleHelper.WriteError("X Workflow ID or name is required");
                    return 1;
                }

                if (!force)
                {
                    Console.WriteLine();
                    Console.Write($"Delete workflow '{workflowId}'? (y/N): ");
                    var response = Console.ReadLine();
                    if (response?.ToLower() != "y")
                    {
                        Console.WriteLine("Delete cancelled.");
                        return 0;
                    }
                }

                // For GitHub, workflow deletion requires disabling the workflow file in the repository
                // This would typically be done via a commit that removes or disables the workflow
                ConsoleHelper.WriteLine($"[OK] Workflow deletion would be processed", ConsoleColor.Green);
                Console.WriteLine();
                ConsoleHelper.WriteLine($"To delete this workflow:", ConsoleColor.Cyan);
                Console.WriteLine($"  1. Delete or rename the {workflowId}.yml file from .github/workflows/");
                Console.WriteLine($"  2. Commit and push the change");
                Console.WriteLine($"  3. Workflow will be removed from GitHub Actions");

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to delete GitHub workflow: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> DeleteAzureDevOpsPipeline(string? organization, string? project, string? pipelineId, bool force, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"[INFO] Deleting pipeline from {organization}/{project}");
                    Console.WriteLine($"Pipeline location: azure-pipelines/");
                }

                if (string.IsNullOrEmpty(pipelineId))
                {
                    ConsoleHelper.WriteError("X Pipeline ID or name is required");
                    return 1;
                }

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                // Get Azure DevOps credentials
                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                if (!force)
                {
                    Console.WriteLine();
                    Console.Write($"Delete pipeline '{pipelineId}'? (y/N): ");
                    var response = Console.ReadLine();
                    if (response?.ToLower() != "y")
                    {
                        Console.WriteLine("Delete cancelled.");
                        return 0;
                    }
                }

                using (var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project))
                {
                    // For now, show what would happen
                    // In a real implementation, we would call DeletePipelineAsync
                    ConsoleHelper.WriteLine($"[OK] Pipeline deletion would be processed", ConsoleColor.Green);
                    Console.WriteLine();
                    ConsoleHelper.WriteLine($"Details:", ConsoleColor.Cyan);
                    Console.WriteLine($"  Pipeline ID: {pipelineId}");
                    Console.WriteLine($"  Project: {project}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to delete Azure DevOps pipeline: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetGitHubLastRun(string owner, string repo, string? workflowName, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"[INFO] Retrieving last workflow run");
                    Console.WriteLine($"Workflows location: .github/workflows/");
                }

                // Fetch workflows from GitHub API first
                using (var client = new Sdo.Services.GitHubClient())
                {
                    var workflows = await client.ListWorkflowsAsync(owner, repo);

                    if (workflows == null || workflows.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No workflows found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }

                    // For now, show the workflows available
                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Latest workflow runs:", ConsoleColor.Cyan);
                    Console.WriteLine();
                    Console.WriteLine($"{"Workflow Name",-40} {"Last Run",-20} {"Status"}");
                    Console.WriteLine(new string('-', 70));

                    foreach (var workflow in workflows.Take(5))
                    {
                        var name = workflow.Name ?? "Unnamed Workflow";
                        if (name.Length > 38)
                        {
                            name = name.Substring(0, 35) + "...";
                        }

                        var lastRun = workflow.UpdatedAt.HasValue ? workflow.UpdatedAt.Value.ToString("g") : "Never";
                        Console.WriteLine($"{name,-40} {lastRun,-20} {workflow.State}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get GitHub last run: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetAzureDevOpsLastBuild(string? organization, string? project, string? pipelineName, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"[INFO] Retrieving last build information");
                    Console.WriteLine($"Pipeline location: azure-pipelines/");
                }

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                // Get Azure DevOps credentials
                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                using (var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project))
                {
                    var pipelines = await client.ListPipelinesAsync(project);

                    if (pipelines == null || pipelines.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No pipelines found in this project.", ConsoleColor.Yellow);
                        return 0;
                    }

                    // Filter by pipeline name if provided
                    if (!string.IsNullOrEmpty(pipelineName))
                    {
                        pipelines = pipelines.Where(p =>
                            (p.Name ?? "").Contains(pipelineName, StringComparison.OrdinalIgnoreCase)
                        ).ToList();
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Recent builds:", ConsoleColor.Cyan);
                    Console.WriteLine();
                    Console.WriteLine($"{"Pipeline Name",-40} {"Last Build",-20} {"Status"}");
                    Console.WriteLine(new string('-', 70));

                    foreach (var pipeline in pipelines.Take(5))
                    {
                        var name = pipeline.Name ?? "Unnamed Pipeline";
                        if (name.Length > 38)
                        {
                            name = name.Substring(0, 35) + "...";
                        }

                        var lastBuild = pipeline.ModifiedDate.HasValue ? pipeline.ModifiedDate.Value.ToString("g") : "Never";
                        Console.WriteLine($"{name,-40} {lastBuild,-20} {pipeline.QueueStatus}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get Azure DevOps last build: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> CreateGitHubWorkflow(string owner, string repo, string filePath, bool verbose)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"[INFO] Reading workflow definition from {filePath}");
                    Console.WriteLine($"Workflows location: .github/workflows/");
                }

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    ConsoleHelper.WriteError($"X File not found: {filePath}");
                    return 1;
                }

                var yamlContent = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(yamlContent))
                {
                    ConsoleHelper.WriteError("X YAML file is empty");
                    return 1;
                }

                // Use GitHub API to validate and prepare workflow creation
                using var githubClient = new GitHubClient();
                if (verbose)
                {
                    Console.WriteLine($"[INFO] Validating workflow with GitHub API...");
                }

                var success = await githubClient.CreateWorkflowAsync(owner, repo, fileName, filePath);
                if (!success)
                {
                    ConsoleHelper.WriteError($"X Workflow validation failed: Repository {owner}/{repo} not found or not accessible");
                    return 1;
                }

                // Workflow validation passed
                var workflowPath = Path.Combine(".github", "workflows", fileName);
                ConsoleHelper.WriteLine($"[OK] Workflow definition validated successfully", ConsoleColor.Green);
                Console.WriteLine();
                ConsoleHelper.WriteLine($"Workflow Details:", ConsoleColor.Cyan);
                Console.WriteLine($"  Repository: {owner}/{repo}");
                Console.WriteLine($"  Filename:   {fileName}");
                Console.WriteLine($"  Path:       {workflowPath}");
                Console.WriteLine();
                ConsoleHelper.WriteLine($"To create and activate this workflow:", ConsoleColor.Cyan);
                Console.WriteLine($"  1. Copy {fileName} to .github/workflows/ in your repository");
                Console.WriteLine($"  2. Commit the file: git add {workflowPath}");
                Console.WriteLine($"  3. Commit changes: git commit -m 'Add GitHub Actions workflow'");
                Console.WriteLine($"  4. Push to repository: git push");
                Console.WriteLine($"  5. Workflow will be activated in GitHub Actions");

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to create GitHub workflow: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> CreateAzureDevOpsPipeline(string? organization, string? project, string filePath, bool verbose)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);

                if (verbose)
                {
                    Console.WriteLine($"[INFO] Reading pipeline definition from {filePath}");
                    Console.WriteLine($"Pipeline location: azure-pipelines/");
                }

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    ConsoleHelper.WriteError($"X File not found: {filePath}");
                    return 1;
                }

                var yamlContent = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(yamlContent))
                {
                    ConsoleHelper.WriteError("X YAML file is empty");
                    return 1;
                }

                // Get Azure DevOps credentials
                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                using (var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"[INFO] Creating pipeline with Azure DevOps API...");
                    }

                    var pipelineFileName = Path.GetFileNameWithoutExtension(fileName);
                    var pipelineId = await client.CreatePipelineAsync(project, pipelineFileName, filePath);

                    if (pipelineId < 0)
                    {
                        ConsoleHelper.WriteError($"X Failed to create pipeline: {client.LastError}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"[OK] Pipeline created successfully", ConsoleColor.Green);
                    Console.WriteLine();
                    ConsoleHelper.WriteLine($"Pipeline Details:", ConsoleColor.Cyan);
                    Console.WriteLine($"  Organization: {organization}");
                    Console.WriteLine($"  Project:      {project}");
                    Console.WriteLine($"  Pipeline ID:  {pipelineId}");
                    Console.WriteLine($"  Name:         {pipelineFileName}");
                    Console.WriteLine($"  YAML File:    {fileName}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to create Azure DevOps pipeline: {ex.Message}");
                return 1;
            }
        }

        private void DisplayGitHubMapping(string command)

        {
            Console.WriteLine();
            ConsoleHelper.WriteLine("[INFO] GitHub equivalent command:", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine($"   gh workflow list --repo <owner>/<repo>", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine($"   gh workflow view <workflow-id> --repo <owner>/<repo>", ConsoleColor.Yellow);
            Console.WriteLine();
        }

        private void DisplayAzureDevOpsMapping(string command)
        {
            Console.WriteLine();
            ConsoleHelper.WriteLine("[INFO] Azure DevOps equivalent command:", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine($"   az pipelines list --project <project>", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine($"   az pipelines show --id <pipeline-id> --project <project>", ConsoleColor.Yellow);
            Console.WriteLine();
        }

        private Task<string?> GetAuthenticationTokenAsync(Platform platform)
        {
            // For now, this is a placeholder
            // In actual implementation, use AuthenticationService
            return Task.FromResult<string?>(null);
        }

        private async Task<int> GetStatus(int? buildId, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Retrieving pipeline build/run status from current Git remote...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    if (verbose)
                    {
                        Console.WriteLine("Make sure you're in a Git repository with a GitHub or Azure DevOps remote.");
                    }
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose)
                    {
                        DisplayGitHubMapping("run");
                    }
                    return await GetGitHubRunStatus(repoInfo.Owner, repoInfo.Repo, buildId, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("pipeline show");
                    }
                    return await GetAzureDevOpsRunStatus(repoInfo.Organization, repoInfo.Project, repoInfo.Repository, buildId, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error getting pipeline status: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetGitHubRunStatus(string owner, string repo, int? buildId, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                if (buildId.HasValue)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Build ID: {buildId}");
                    }
                }
                else
                {
                    if (verbose)
                    {
                        Console.WriteLine("Showing latest build/run status");
                    }
                }

                // Placeholder: In Phase 3.5, we'll add GetRunStatusAsync to GitHubClient
                ConsoleHelper.WriteLine("[*] Pipeline status would be displayed here", ConsoleColor.Yellow);

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get GitHub run status: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetAzureDevOpsRunStatus(string? organization, string? project, string? repository, int? buildId, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"✓ Repository: {repository ?? "unknown"}", ConsoleColor.Green);

                if (buildId.HasValue)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Build ID: {buildId}");
                    }
                }
                else
                {
                    if (verbose)
                    {
                        Console.WriteLine("Showing latest build/run status");
                    }
                }

                // Placeholder: In Phase 3.5, we'll add GetBuildStatusAsync to AzureDevOpsClient
                ConsoleHelper.WriteLine("[*] Pipeline status would be displayed here", ConsoleColor.Yellow);

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get Azure DevOps build status: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> UpdatePipeline(bool verbose)
        {
            Console.WriteLine("X Pipeline update command not yet implemented");
            return 1;
        }

        #endregion
    }
}
