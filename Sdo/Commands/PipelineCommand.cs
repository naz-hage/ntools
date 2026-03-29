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
            var buildIdArg = new Argument<long?>("build-id") { Arity = ArgumentArity.ZeroOrOne, Description = "Build/run ID (optional, shows latest if not provided)" };
            var buildIdOption = new Option<long?>("--build-id") { Description = "Build/run ID" };

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
            var pipelineIdOrNameArg = new Argument<string?>("pipeline-id-or-name")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Pipeline ID or name (optional; Azure DevOps only)"
            };

            showCommand.Add(pipelineIdOrNameArg);
            showCommand.Add(verboseOption);

            showCommand.SetAction(async (parseResult) =>
            {
                var pipelineIdOrName = parseResult.GetValue(pipelineIdOrNameArg);
                var verbose = parseResult.GetValue(verboseOption);
                return await ShowPipeline(pipelineIdOrName, verbose);
            });

            Subcommands.Add(showCommand);
        }

        private void AddStatusCommand(Option<bool> verboseOption)
        {
            var statusCommand = new System.CommandLine.Command("status", "Query pipeline build/run status");
            var buildIdArg = new Argument<long?>("build-id") { Arity = ArgumentArity.ZeroOrOne, Description = "Build/run ID (optional, shows latest if not provided)" };

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
            var fileOption = new Option<string?>("--file") { Description = "Path to pipeline/workflow YAML file to update in repository" };
            var pipelineOption = new Option<string?>("--pipeline") { Description = "Pipeline/workflow ID or name (optional)" };
            var messageOption = new Option<string?>("--message") { Description = "Commit message to use when updating the file" };
            var branchOption = new Option<string?>("--branch") { Description = "Branch to push the update to" };

            updateCommand.Add(fileOption);
            updateCommand.Add(pipelineOption);
            updateCommand.Add(messageOption);
            updateCommand.Add(branchOption);
            updateCommand.Add(verboseOption);

            updateCommand.SetAction(async (parseResult) =>
            {
                var file = parseResult.GetValue(fileOption);
                var pipeline = parseResult.GetValue(pipelineOption);
                var message = parseResult.GetValue(messageOption);
                var branch = parseResult.GetValue(branchOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await UpdatePipeline(file, pipeline, message, branch, verbose);
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

                // Fetch neutral pipeline definitions from GitHub API
                using (var client = new Sdo.Services.GitHubClient())
                {
                    var definitions = await client.ListPipelineDefinitionsAsync(owner, repo);

                    if (definitions == null || definitions.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No workflows found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }

                    // Apply repository name filter if specified
                    if (!string.IsNullOrEmpty(filterRepo))
                    {
                        definitions = definitions.Where(w =>
                            (w.Name ?? "").Contains(filterRepo, StringComparison.OrdinalIgnoreCase) ||
                            (w.Path ?? "").Contains(filterRepo, StringComparison.OrdinalIgnoreCase)
                        ).ToList();

                        if (definitions.Count == 0)
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

                    foreach (var def in definitions)
                    {
                        var name = def.Name ?? "Unnamed Workflow";
                        if (name.Length > 38)
                        {
                            name = name.Substring(0, 35) + "...";
                        }

                        Console.WriteLine($"{name,-40} {def.State,-10} {def.PlatformId}");
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
                    var pipelines = await client.ListPipelineDefinitionsAsync(project);

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

                        var status = pipeline.State ?? "unknown";
                        Console.WriteLine($"{name,-40} {status,-15} {pipeline.PlatformId}");
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

        private async Task<int> GetLogs(long? buildId, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Retrieving pipeline logs from current Git remote...");

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
                        DisplayGitHubMapping("run logs");
                    }
                    return await GetGitHubLogs(repoInfo.Owner, repoInfo.Repo, buildId, verbose);
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose)
                    {
                        DisplayAzureDevOpsMapping("build logs");
                    }
                    return await GetAzureDevOpsLogs(repoInfo.Organization, repoInfo.Project, repoInfo.Repository, buildId, verbose);
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error retrieving logs: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> RunPipeline(string? pipelineName, string branch, bool verbose)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branch))
                {
                    ConsoleHelper.WriteError("X Branch is required to run a pipeline. Use --branch <name>");
                    return 1;
                }

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    if (verbose) DisplayGitHubMapping("workflow run");

                    using var client = new Sdo.Services.GitHubClient();

                    // Resolve workflow id (long) from numeric input or by name
                    long? workflowId = null;
                    if (!string.IsNullOrWhiteSpace(pipelineName) && long.TryParse(pipelineName, out var parsedId))
                    {
                        workflowId = parsedId;
                    }
                    else
                    {
                        var defs = await client.ListPipelineDefinitionsAsync(repoInfo.Owner, repoInfo.Repo);
                        if (defs == null || defs.Count == 0)
                        {
                            ConsoleHelper.WriteError("X No workflows found in repository");
                            return 1;
                        }

                        if (!string.IsNullOrWhiteSpace(pipelineName))
                        {
                            var match = defs.FirstOrDefault(d => string.Equals(d.Name, pipelineName, StringComparison.OrdinalIgnoreCase) || (d.Path ?? "").Contains(pipelineName, StringComparison.OrdinalIgnoreCase));
                            if (match != null && long.TryParse(match.PlatformId, out var mid)) workflowId = mid;
                        }

                        // fallback: pick first active workflow
                        if (!workflowId.HasValue)
                        {
                            var first = defs.FirstOrDefault(d => string.Equals(d.State, "active", StringComparison.OrdinalIgnoreCase)) ?? defs.First();
                            if (!long.TryParse(first.PlatformId, out var fid))
                            {
                                ConsoleHelper.WriteError("X Could not parse workflow id");
                                return 1;
                            }
                            workflowId = fid;
                        }
                    }

                    if (!workflowId.HasValue)
                    {
                        ConsoleHelper.WriteError("X Could not resolve workflow to run");
                        return 1;
                    }

                    var branchRef = branch.StartsWith("refs/") ? branch : branch;
                    var triggered = await client.TriggerPipelineAsync(repoInfo.Owner, repoInfo.Repo, workflowId.Value, branchRef);
                    if (!triggered)
                    {
                        ConsoleHelper.WriteError("X Failed to trigger GitHub workflow");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"[OK] Workflow dispatch triggered (workflow id: {workflowId.Value})", ConsoleColor.Green);

                    // Try to find the created run
                    var runs = await client.ListPipelineRunsAsync(repoInfo.Owner, repoInfo.Repo, workflowId, 5);
                    var run = runs?.FirstOrDefault(r => string.Equals(r.Branch, branch, StringComparison.OrdinalIgnoreCase)) ?? runs?.OrderByDescending(r => r.FinishedAt ?? r.StartedAt).FirstOrDefault();
                    if (run != null)
                    {
                        Console.WriteLine($"Run ID: {run.PlatformId}  Status: {run.Status ?? "unknown"}  URL: {run.Url}");
                    }

                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (string.IsNullOrEmpty(repoInfo.Organization) || string.IsNullOrEmpty(repoInfo.Project))
                    {
                        ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                        return 1;
                    }

                    var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                        Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                        return 1;
                    }

                    using var client = new Sdo.Services.AzureDevOpsClient(pat, repoInfo.Organization, repoInfo.Project);

                    int? definitionId = null;
                    if (!string.IsNullOrWhiteSpace(pipelineName) && int.TryParse(pipelineName, out var parsed))
                    {
                        definitionId = parsed;
                    }
                    else
                    {
                        var pipelines = await client.ListPipelineDefinitionsAsync(repoInfo.Project);
                        if (pipelines == null || pipelines.Count == 0)
                        {
                            ConsoleHelper.WriteError("X No pipelines found in project");
                            return 1;
                        }

                        if (!string.IsNullOrWhiteSpace(pipelineName))
                        {
                            var match = pipelines.FirstOrDefault(p => string.Equals(p.Name, pipelineName, StringComparison.OrdinalIgnoreCase) || (p.Path ?? "").Contains(pipelineName, StringComparison.OrdinalIgnoreCase));
                            if (match != null && int.TryParse(match.PlatformId, out var mid)) definitionId = mid;
                        }

                        if (!definitionId.HasValue)
                        {
                            // Try to find pipeline matching repository name
                            var repoMatch = pipelines.FirstOrDefault(p => (p.Name ?? "").Contains(repoInfo.Repository ?? string.Empty, StringComparison.OrdinalIgnoreCase));
                            if (repoMatch != null && int.TryParse(repoMatch.PlatformId, out var rmid)) definitionId = rmid;
                        }

                        if (!definitionId.HasValue)
                        {
                            // Fallback to first pipeline
                            if (!int.TryParse(pipelines.First().PlatformId, out var fid))
                            {
                                ConsoleHelper.WriteError("X Failed to resolve pipeline id");
                                return 1;
                            }
                            definitionId = fid;
                        }
                    }

                    if (!definitionId.HasValue)
                    {
                        ConsoleHelper.WriteError("X Could not resolve pipeline definition id to run");
                        return 1;
                    }

                    var buildId = await client.RunPipelineAsync(repoInfo.Project, definitionId.Value, branch);
                    if (buildId < 0)
                    {
                        ConsoleHelper.WriteError($"X Failed to queue build: {client.LastError}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"[OK] Build queued (ID: {buildId})", ConsoleColor.Green);

                    // Prefer neutral pipeline run wrapper
                    var run = await client.GetPipelineRunAsync(repoInfo.Project, buildId);
                    if (run != null)
                    {
                        Console.WriteLine($"  Pipeline: {run.Name}  Build Number: {run.Name}  Status: {run.Status}");
                    }
                    else
                    {
                        // Fallback to raw build DTO if wrapper failed to convert
                        var build = await client.GetBuildAsync(repoInfo.Project, buildId);
                        if (build != null)
                        {
                            Console.WriteLine($"  Pipeline: {build.Definition?.Name}  Build Number: {build.BuildNumber}  Status: {build.Status}");
                        }
                    }

                    return 0;
                }

                ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error running pipeline: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> ShowPipeline(string? pipelineIdOrName, bool verbose)
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
                    return await ShowAzureDevOpsPipeline(repoInfo.Organization, repoInfo.Project, repoInfo.Repository, pipelineIdOrName, verbose);
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

                // Fetch neutral pipeline definitions from GitHub API
                using (var client = new Sdo.Services.GitHubClient())
                {
                    var definitions = await client.ListPipelineDefinitionsAsync(owner, repo);

                    if (definitions == null || definitions.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No workflows found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Workflows in this repository:", ConsoleColor.Cyan);
                    Console.WriteLine();

                    foreach (var def in definitions)
                    {
                        var stateColor = (def.State ?? "").Equals("active", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Green : ConsoleColor.Yellow;
                        ConsoleHelper.WriteLine($"  • {def.Name ?? "Unnamed Workflow"}", stateColor);
                        Console.WriteLine($"    State: {def.State}");
                        Console.WriteLine($"    ID: {def.PlatformId}");
                        if (!string.IsNullOrEmpty(def.Path))
                        {
                            Console.WriteLine($"    Path: {def.Path}");
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

        private async Task<int> ShowAzureDevOpsPipeline(string? organization, string? project, string? repository, string? pipelineIdOrName, bool verbose)
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
                    if (!string.IsNullOrWhiteSpace(pipelineIdOrName))
                    {
                        var pipeline = await client.GetPipelineDefinitionAsync(project, pipelineIdOrName);
                        if (pipeline == null)
                        {
                            var details = string.IsNullOrEmpty(client.LastError) ? string.Empty : $": {client.LastError}";
                            ConsoleHelper.WriteError($"X Pipeline not found{details}");
                            return 1;
                        }

                        Console.WriteLine();
                        ConsoleHelper.WriteLine("✓ Pipeline details:", ConsoleColor.Cyan);
                        Console.WriteLine($"  Name: {pipeline.Name ?? "Unnamed Pipeline"}");
                        Console.WriteLine($"  ID: {pipeline.PlatformId}");
                        Console.WriteLine($"  Status: {pipeline.State ?? "unknown"}");
                        Console.WriteLine($"  Type: {pipeline.Type ?? "unknown"}");
                        if (!string.IsNullOrEmpty(pipeline.Path))
                        {
                            Console.WriteLine($"  Path: {pipeline.Path}");
                        }
                        if (pipeline.CreatedAt.HasValue)
                        {
                            Console.WriteLine($"  Created: {pipeline.CreatedAt:g}");
                        }
                        if (pipeline.UpdatedAt.HasValue)
                        {
                            Console.WriteLine($"  Modified: {pipeline.UpdatedAt:g}");
                        }

                        return 0;
                    }

                    var pipelines = await client.ListPipelineDefinitionsAsync(project);

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
                        var statusColor = (pipeline.State ?? "") == "enabled" ? ConsoleColor.Green : ConsoleColor.Yellow;
                        ConsoleHelper.WriteLine($"  • {pipeline.Name ?? "Unnamed Pipeline"}", statusColor);
                        Console.WriteLine($"    ID: {pipeline.PlatformId}");
                        Console.WriteLine($"    Status: {pipeline.State}");
                        if (!string.IsNullOrEmpty(pipeline.Path))
                        {
                            Console.WriteLine($"    Path: {pipeline.Path}");
                        }
                        if (pipeline.CreatedAt.HasValue)
                        {
                            Console.WriteLine($"    Created: {pipeline.CreatedAt:g}");
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
                    var success = await client.DeletePipelineAsync(project, pipelineId);
                    if (!success)
                    {
                        ConsoleHelper.WriteError($"X Failed to delete pipeline: {client.LastError}");
                        return 1;
                    }

                    ConsoleHelper.WriteLine($"[OK] Pipeline '{pipelineId}' deleted successfully.", ConsoleColor.Green);
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

                // Fetch recent pipeline runs (neutral) from GitHub
                using (var client = new Sdo.Services.GitHubClient())
                {
                    var runs = await client.ListPipelineRunsAsync(owner, repo, null, 10);

                    if (runs == null || runs.Count == 0)
                    {
                        ConsoleHelper.WriteLine("No workflow runs found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }

                    Console.WriteLine();
                    ConsoleHelper.WriteLine("✓ Latest workflow runs:", ConsoleColor.Cyan);
                    Console.WriteLine();
                    Console.WriteLine($"{"Run ID",-12} {"Workflow Name",-35} {"Last Run",-20} {"Status",-12} {"Result"}");
                    Console.WriteLine(new string('-', 90));

                    foreach (var run in runs)
                    {
                        var name = run.Name ?? "Unnamed";
                        if (name.Length > 33)
                            name = name.Substring(0, 30) + "...";

                        var lastRun = run.FinishedAt.HasValue ? run.FinishedAt.Value.ToString("g") : (run.StartedAt.HasValue ? run.StartedAt.Value.ToString("g") : "Unknown");
                        var status = run.Status ?? "unknown";
                        var result = run.Result ?? "pending";
                        Console.WriteLine($"{run.PlatformId,-12} {name,-35} {lastRun,-20} {status,-12} {result}");
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
                    // Use neutral pipeline definitions API
                    var pipelines = await client.ListPipelineDefinitionsAsync(project);

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

                        var lastBuild = pipeline.UpdatedAt.HasValue ? pipeline.UpdatedAt.Value.ToString("g") : "Never";
                        Console.WriteLine($"{name,-40} {lastBuild,-20} {pipeline.State}");
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

        private async Task<int> GetStatus(long? buildId, bool verbose)
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

        private async Task<int> GetGitHubRunStatus(string owner, string repo, long? buildId, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                using var client = new Sdo.Services.GitHubClient();
                PipelineRun? run;

                if (buildId.HasValue)
                {
                    run = await client.GetPipelineRunAsync(owner, repo, buildId.Value);
                    if (run == null)
                    {
                        ConsoleHelper.WriteError($"X Workflow run not found for ID {buildId.Value}");
                        return 1;
                    }
                }
                else
                {
                    var runs = await client.ListPipelineRunsAsync(owner, repo, null, 1);
                    run = runs?.FirstOrDefault();
                    if (run == null)
                    {
                        ConsoleHelper.WriteLine("No workflow runs found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }
                }

                Console.WriteLine();
                ConsoleHelper.WriteLine("✓ Workflow Run Status", ConsoleColor.Cyan);
                Console.WriteLine($"  Run ID:     {run.PlatformId}");
                Console.WriteLine($"  Name:       {run.Name ?? "unknown"}");
                Console.WriteLine($"  Branch:     {run.Branch ?? "unknown"}");
                Console.WriteLine($"  Status:     {run.Status ?? "unknown"}");
                Console.WriteLine($"  Result:     {run.Result ?? "pending"}");
                if (run.StartedAt.HasValue)
                {
                    Console.WriteLine($"  Started:    {run.StartedAt.Value:g}");
                }
                if (run.FinishedAt.HasValue)
                {
                    Console.WriteLine($"  Finished:   {run.FinishedAt.Value:g}");
                }
                if (!string.IsNullOrEmpty(run.Url))
                {
                    Console.WriteLine($"  URL:        {run.Url}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get GitHub run status: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetAzureDevOpsRunStatus(string? organization, string? project, string? repository, long? buildId, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"✓ Repository: {repository ?? "unknown"}", ConsoleColor.Green);

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                using var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project);
                PipelineRun? run;

                if (buildId.HasValue)
                {
                    run = await client.GetPipelineRunAsync(project, (int)buildId.Value);
                    if (run == null)
                    {
                        ConsoleHelper.WriteError($"X Build not found for ID {buildId.Value}");
                        return 1;
                    }
                }
                else
                {
                    var runs = await client.ListPipelineRunsAsync(project, 1, null);
                    run = runs?.FirstOrDefault();
                    if (run == null)
                    {
                        ConsoleHelper.WriteLine("No builds found in this project.", ConsoleColor.Yellow);
                        return 0;
                    }
                }

                Console.WriteLine();
                ConsoleHelper.WriteLine("✓ Build Status", ConsoleColor.Cyan);
                Console.WriteLine($"  Build ID:    {run.PlatformId}");
                Console.WriteLine($"  Number:      {run.Name ?? "unknown"}");
                Console.WriteLine($"  Pipeline:    {run.Name ?? "unknown"}");
                Console.WriteLine($"  Status:      {run.Status ?? "unknown"}");
                Console.WriteLine($"  Result:      {run.Result ?? "pending"}");
                Console.WriteLine($"  Branch:      {run.Branch ?? "unknown"}");
                if (run.StartedAt.HasValue)
                {
                    Console.WriteLine($"  Started:     {run.StartedAt.Value:g}");
                }
                if (run.FinishedAt.HasValue)
                {
                    Console.WriteLine($"  Finished:    {run.FinishedAt.Value:g}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get Azure DevOps build status: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetGitHubLogs(string owner, string repo, long? buildId, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ GitHub Repository: {owner}/{repo}", ConsoleColor.Green);

                using var client = new Sdo.Services.GitHubClient();
                PipelineRun? run;

                if (buildId.HasValue)
                {
                    run = await client.GetPipelineRunAsync(owner, repo, buildId.Value);
                    if (run == null)
                    {
                        ConsoleHelper.WriteError($"X Workflow run not found for ID {buildId.Value}");
                        return 1;
                    }
                }
                else
                {
                    var runs = await client.ListPipelineRunsAsync(owner, repo, null, 1);
                    run = runs?.FirstOrDefault();
                    if (run == null)
                    {
                        ConsoleHelper.WriteLine("No workflow runs found in this repository.", ConsoleColor.Yellow);
                        return 0;
                    }
                }

                Console.WriteLine();
                ConsoleHelper.WriteLine("✓ Workflow Run Logs", ConsoleColor.Cyan);
                Console.WriteLine($"  Run ID:      {run.PlatformId}");
                Console.WriteLine($"  Name:        {run.Name ?? "unknown"}");
                Console.WriteLine($"  Status:      {run.Status ?? "unknown"}");
                Console.WriteLine($"  Result:      {run.Result ?? "pending"}");
                Console.WriteLine($"  Details URL: {run.Url ?? "not available"}");

                // Download and print log content
                long runId;
                if (long.TryParse(run.PlatformId, out runId))
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteLine("--- Log Content ---", ConsoleColor.Yellow);
                    var logText = await client.GetPipelineRunLogsAsync(owner, repo, runId);
                    if (!string.IsNullOrWhiteSpace(logText))
                    {
                        Console.WriteLine(logText.TrimEnd());
                    }
                    else
                    {
                        Console.WriteLine("[No log content available or failed to download]");
                    }
                }
                else
                {
                    Console.WriteLine("[No run id available to download logs]");
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get GitHub logs: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> GetAzureDevOpsLogs(string? organization, string? project, string? repository, long? buildId, bool verbose)
        {
            try
            {
                ConsoleHelper.WriteLine($"✓ Azure DevOps Project: {organization ?? "unknown"}/{project ?? "unknown"}", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"✓ Repository: {repository ?? "unknown"}", ConsoleColor.Green);

                if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                {
                    ConsoleHelper.WriteError("X Organization and project are required for Azure DevOps");
                    return 1;
                }

                var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                if (string.IsNullOrEmpty(pat))
                {
                    ConsoleHelper.WriteError("X Azure DevOps authentication required.");
                    Console.WriteLine("Please set AZURE_DEVOPS_PAT environment variable or run: sdo.net auth azdo");
                    return 1;
                }

                using var client = new Sdo.Services.AzureDevOpsClient(pat, organization, project);
                PipelineRun? run;

                if (buildId.HasValue)
                {
                    run = await client.GetPipelineRunAsync(project, (int)buildId.Value);
                    if (run == null)
                    {
                        ConsoleHelper.WriteError($"X Build not found for ID {buildId.Value}");
                        return 1;
                    }
                }
                else
                {
                    var runs = await client.ListPipelineRunsAsync(project, 1, null);
                    run = runs?.FirstOrDefault();
                    if (run == null)
                    {
                        ConsoleHelper.WriteLine("No builds found in this project.", ConsoleColor.Yellow);
                        return 0;
                    }
                }

                Console.WriteLine();
                ConsoleHelper.WriteLine("✓ Build Logs", ConsoleColor.Cyan);
                Console.WriteLine($"  Build ID:    {run.PlatformId}");
                Console.WriteLine($"  Number:      {run.Name ?? "unknown"}");
                Console.WriteLine($"  Status:      {run.Status ?? "unknown"}");
                Console.WriteLine($"  Result:      {run.Result ?? "pending"}");
                Console.WriteLine($"  Build URL:   {run.Url ?? "not available"}");

                // Attempt to download and display log content
                try
                {
                    if (int.TryParse(run.PlatformId, out var intId))
                    {
                        var logText = await client.GetPipelineRunLogsAsync(project, intId);
                        if (!string.IsNullOrWhiteSpace(logText))
                        {
                            Console.WriteLine();
                            ConsoleHelper.WriteLine("--- Log Content ---", ConsoleColor.Yellow);
                            Console.WriteLine(logText.TrimEnd());
                        }
                        else
                        {
                            Console.WriteLine("[No log content available or failed to download]");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[No valid build id available to download logs]");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"X Error downloading build logs: {ex.Message}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Failed to get Azure DevOps logs: {ex.Message}");
                return 1;
            }
        }

        private async Task<int> UpdatePipeline(string? filePath, string? pipelineId, string? commitMessage, string? branch, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("[INFO] Updating pipeline/workflow configuration...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    return 1;
                }

                // If no file provided, fall back to guidance (non-destructive)
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    if (platform == Platform.GitHub)
                    {
                        if (verbose) DisplayGitHubMapping("workflow update");
                        ConsoleHelper.WriteLine("[INFO] To update GitHub workflow, modify the .github/workflows/<file>.yml and commit/push the change.", ConsoleColor.Yellow);
                        Console.WriteLine("  Steps:");
                        Console.WriteLine("    1. Edit .github/workflows/<workflow-file>.yml");
                        Console.WriteLine("    2. git add <file> && git commit -m 'Update workflow' && git push");
                        return 0;
                    }
                    else if (platform == Platform.AzureDevOps)
                    {
                        if (verbose) DisplayAzureDevOpsMapping("pipeline update");
                        ConsoleHelper.WriteLine("[INFO] To update an Azure DevOps pipeline, update the YAML in your repository and commit/push. Alternatively, update the pipeline definition via Azure DevOps REST API.", ConsoleColor.Yellow);
                        Console.WriteLine("  Steps:");
                        Console.WriteLine("    1. Edit azure-pipelines/<pipeline>.yml");
                        Console.WriteLine("    2. git add <file> && git commit -m 'Update pipeline' && git push");
                        Console.WriteLine("    3. If pipeline uses pipeline definition in UI, update via Azure DevOps portal or REST API");
                        return 0;
                    }

                    ConsoleHelper.WriteError("X Unable to detect platform from Git remote");
                    return 1;
                }

                // Validate file
                if (!File.Exists(filePath))
                {
                    ConsoleHelper.WriteError($"X File not found: {filePath}");
                    return 1;
                }

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".yml" && ext != ".yaml")
                {
                    ConsoleHelper.WriteError("X Pipeline definition file must be YAML (.yml or .yaml)");
                    return 1;
                }

                var message = string.IsNullOrWhiteSpace(commitMessage) ? (platform == Platform.GitHub ? "Update workflow via sdo" : "Update pipeline via sdo") : commitMessage;

                // Stage, commit, push locally. This approach updates YAML-backed pipelines for both GitHub and Azure DevOps.
                if (verbose) Console.WriteLine($"[INFO] Staging file: {filePath}");

                var addPsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"add \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var addProc = System.Diagnostics.Process.Start(addPsi))
                {
                    if (addProc != null)
                    {
                        var aOut = await addProc.StandardOutput.ReadToEndAsync();
                        var aErr = await addProc.StandardError.ReadToEndAsync();
                        await addProc.WaitForExitAsync();
                        if (addProc.ExitCode != 0)
                        {
                            ConsoleHelper.WriteError($"X git add failed: {aErr.Trim()}");
                            return 1;
                        }
                    }
                }

                var commitPsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"commit -m \"{message.Replace("\"", "'\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var commitProc = System.Diagnostics.Process.Start(commitPsi))
                {
                    if (commitProc != null)
                    {
                        var cOut = await commitProc.StandardOutput.ReadToEndAsync();
                        var cErr = await commitProc.StandardError.ReadToEndAsync();
                        await commitProc.WaitForExitAsync();
                        if (commitProc.ExitCode != 0)
                        {
                            // If there's nothing to commit, continue
                            if (cOut.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase) || cErr.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine("[INFO] No changes to commit.");
                            }
                            else
                            {
                                ConsoleHelper.WriteError($"X git commit failed: {cErr.Trim()}");
                                return 1;
                            }
                        }
                    }
                }

                var pushArgs = string.IsNullOrWhiteSpace(branch) ? "push" : $"push origin {branch}";
                var pushPsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = pushArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var pushProc = System.Diagnostics.Process.Start(pushPsi))
                {
                    if (pushProc != null)
                    {
                        var pOut = await pushProc.StandardOutput.ReadToEndAsync();
                        var pErr = await pushProc.StandardError.ReadToEndAsync();
                        await pushProc.WaitForExitAsync();
                        if (pushProc.ExitCode != 0)
                        {
                            ConsoleHelper.WriteError($"X git push failed: {pErr.Trim()}");
                            return 1;
                        }
                    }
                }

                ConsoleHelper.WriteLine("[OK] Pipeline/workflow file updated and pushed.", ConsoleColor.Green);

                // Attempt API-driven update for Azure DevOps definitions when a pipeline id was provided
                if (platform == Platform.AzureDevOps && !string.IsNullOrWhiteSpace(pipelineId))
                {
                    if (int.TryParse(pipelineId, out var defId))
                    {
                        var pat = AzureDevOpsCredentials.GetTokenOrDefault();
                        if (!string.IsNullOrEmpty(pat))
                        {
                            var org = repoInfo?.Organization ?? repoInfo?.Owner ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(org) || string.IsNullOrWhiteSpace(repoInfo?.Project))
                            {
                                ConsoleHelper.WriteLine("[WARN] Unable to determine Azure DevOps organization/project; skipping REST update.", ConsoleColor.Yellow);
                            }
                            else
                            {
                                try
                                {
                                    using var client = new Sdo.Services.AzureDevOpsClient(pat, org, repoInfo.Project);
                                    var ok = await client.UpdatePipelineDefinitionYamlAsync(repoInfo.Project, defId, filePath!);
                                    if (ok)
                                    {
                                        ConsoleHelper.WriteLine("[OK] Azure DevOps pipeline definition updated via REST API.", ConsoleColor.Green);
                                    }
                                    else
                                    {
                                        ConsoleHelper.WriteLine($"[WARN] Azure DevOps REST update failed: {client.LastError}", ConsoleColor.Yellow);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleHelper.WriteLine($"[WARN] Exception calling Azure DevOps REST update: {ex.Message}", ConsoleColor.Yellow);
                                }
                            }
                        }
                        else
                        {
                            ConsoleHelper.WriteLine("[INFO] AZURE_DEVOPS_PAT not found; skipping REST update.", ConsoleColor.Yellow);
                        }
                    }
                }

                // Note: For Azure DevOps UI-backed definitions, further REST API updates may be required. Consider adding UpdatePipelineDefinitionAsync in AzureDevOpsClient when needed.
                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"X Error updating pipeline: {ex.Message}");
                return 1;
            }
        }

        #endregion
    }
}
