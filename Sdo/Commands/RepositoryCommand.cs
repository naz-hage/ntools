// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// RepositoryCommand.cs
//
// Command handler for repository management operations.
// Supports both GitHub repositories and Azure DevOps Git repositories.

using System.CommandLine;
using Nbuild.Helpers;
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
        private readonly Sdo.Mapping.IMappingGenerator _mappingGenerator;
        private readonly Sdo.Mapping.IMappingPresenter _mappingPresenter;
        private readonly Option<bool>? _dryRunOption;

        /// <summary>
        /// Initializes a new instance of the RepositoryCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public RepositoryCommand(Option<bool> verboseOption, Sdo.Mapping.IMappingGenerator? mappingGenerator = null, Sdo.Mapping.IMappingPresenter? mappingPresenter = null, Option<bool>? dryRunOption = null) : base("repo", "Repository management commands")
        {
            _platformDetector = new PlatformService();
            _mappingGenerator = mappingGenerator ?? new Sdo.Mapping.MappingGenerator();
            _mappingPresenter = mappingPresenter ?? new Sdo.Mapping.ConsoleMappingPresenter();
            _dryRunOption = dryRunOption;

            // Add subcommands (in alphabetical order)
            AddBranchCommand(verboseOption);
            AddCloneCommand(verboseOption);
            AddCreateCommand(verboseOption);
            AddDeleteCommand(verboseOption);
            AddInfoCommand(verboseOption);
            AddListCommand(verboseOption);
            AddShowCommand(verboseOption);
            AddTagCommand(verboseOption);
        }

        private void AddBranchCommand(Option<bool> verboseOption)
        {
            var branchCommand = new System.CommandLine.Command(
                "branch",
                "Display the current branch in the local Git repository");

            branchCommand.Add(verboseOption);
            branchCommand.SetAction(parseResult =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                if (verbose)
                {
                    ConsoleHelper.WriteLine("[VERBOSE] Displaying git branch.", ConsoleColor.Gray);
                }

                return Nbuild.Command.DisplayGitBranch().Code;
            });

            Subcommands.Add(branchCommand);
        }

        private void AddCloneCommand(Option<bool> verboseOption)
        {
            var cloneCommand = new System.CommandLine.Command(
                "clone",
                "Clone a Git repository to a specified path");
            var urlOption = new Option<string>("--url")
            {
                Description = "Git repository URL",
                Required = true
            };
            var pathOption = new Option<string?>("--path")
            {
                Description = "Destination path; defaults to the current directory"
            };

            cloneCommand.Add(urlOption);
            cloneCommand.Add(pathOption);
            cloneCommand.Add(verboseOption);
            if (_dryRunOption != null)
            {
                cloneCommand.Add(_dryRunOption);
            }

            cloneCommand.SetAction(parseResult =>
            {
                var url = parseResult.GetValue(urlOption);
                var path = parseResult.GetValue(pathOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = _dryRunOption != null && parseResult.GetValue(_dryRunOption);
                return Nbuild.Command.Clone(url, path, verbose, dryRun).Code;
            });

            Subcommands.Add(cloneCommand);
        }

        private void AddTagCommand(Option<bool> verboseOption)
        {
            var tagCommand = new System.CommandLine.Command(
                "tag",
                "Manage Git tags");

            AddAutoTagCommand(tagCommand, verboseOption);
            AddDeleteTagCommand(tagCommand, verboseOption);
            AddPushAutoTagCommand(tagCommand, verboseOption);
            AddSetTagCommand(tagCommand, verboseOption);

            Subcommands.Add(tagCommand);
        }

        private void AddAutoTagCommand(System.CommandLine.Command tagCommand, Option<bool> verboseOption)
        {
            var autoCommand = new System.CommandLine.Command(
                "auto",
                "Automatically set the next Git tag based on build type");
            var buildTypeOption = new Option<string>("--buildtype")
            {
                Description = "Build type: STAGE or PROD",
                Required = true
            };

            autoCommand.Add(buildTypeOption);
            autoCommand.Add(verboseOption);
            AddDryRunOption(autoCommand);
            autoCommand.SetAction(parseResult =>
            {
                var buildType = parseResult.GetValue(buildTypeOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult);
                return Nbuild.Command.SetAutoTag(buildType, false, verbose, dryRun).Code;
            });

            tagCommand.Subcommands.Add(autoCommand);
        }

        private void AddDeleteTagCommand(System.CommandLine.Command tagCommand, Option<bool> verboseOption)
        {
            var deleteCommand = new System.CommandLine.Command(
                "delete",
                "Delete a Git tag");
            var tagOption = new Option<string>("--tag")
            {
                Description = "Tag to delete",
                Required = true
            };

            deleteCommand.Add(tagOption);
            deleteCommand.Add(verboseOption);
            AddDryRunOption(deleteCommand);
            deleteCommand.SetAction(parseResult =>
            {
                var tag = parseResult.GetValue(tagOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult);
                return Nbuild.Command.DeleteTag(tag, verbose, dryRun).Code;
            });

            tagCommand.Subcommands.Add(deleteCommand);
        }

        private void AddPushAutoTagCommand(System.CommandLine.Command tagCommand, Option<bool> verboseOption)
        {
            var pushAutoCommand = new System.CommandLine.Command(
                "push-auto",
                "Automatically set and push the next Git tag based on build type");
            var buildTypeOption = new Option<string>("--buildtype")
            {
                Description = "Build type: STAGE or PROD",
                Required = true
            };

            pushAutoCommand.Add(buildTypeOption);
            pushAutoCommand.Add(verboseOption);
            AddDryRunOption(pushAutoCommand);
            pushAutoCommand.SetAction(parseResult =>
            {
                var buildType = parseResult.GetValue(buildTypeOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult);
                return Nbuild.Command.SetAutoTag(buildType, true, verbose, dryRun).Code;
            });

            tagCommand.Subcommands.Add(pushAutoCommand);
        }

        private void AddSetTagCommand(System.CommandLine.Command tagCommand, Option<bool> verboseOption)
        {
            var setCommand = new System.CommandLine.Command(
                "set",
                "Set a Git tag");
            var tagOption = new Option<string>("--tag")
            {
                Description = "Tag to set",
                Required = true
            };

            setCommand.Add(tagOption);
            setCommand.Add(verboseOption);
            AddDryRunOption(setCommand);
            setCommand.SetAction(parseResult =>
            {
                var tag = parseResult.GetValue(tagOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult);
                return Nbuild.Command.SetTag(tag, verbose, dryRun).Code;
            });

            tagCommand.Subcommands.Add(setCommand);
        }

        private void AddDryRunOption(System.CommandLine.Command command)
        {
            if (_dryRunOption != null)
            {
                command.Add(_dryRunOption);
            }
        }

        private bool GetDryRunValue(System.CommandLine.ParseResult parseResult)
        {
            return _dryRunOption != null && parseResult.GetValue(_dryRunOption);
        }

        private void AddInfoCommand(Option<bool> verboseOption)
        {
            var infoCommand = new System.CommandLine.Command(
                "info",
                "Display local Git repository information, including the current branch and latest tag");

            infoCommand.Add(verboseOption);
            if (_dryRunOption != null)
            {
                infoCommand.Add(_dryRunOption);
            }
            infoCommand.SetAction((parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = _dryRunOption != null && parseResult.GetValue(_dryRunOption);
                if (verbose)
                {
                    ConsoleHelper.WriteLine("[VERBOSE] Displaying Git repository information.", ConsoleColor.Gray);
                }

                return Nbuild.Command.DisplayGitInfo(verbose, dryRun).Code;
            });

            Subcommands.Add(infoCommand);
        }

        private void AddCreateCommand(Option<bool> verboseOption)
        {
            var createCommand = new System.CommandLine.Command("create", "Create a new repository");
            var nameArgument = new Argument<string>("name") { Description = "Repository name" };
            var descriptionOption = new Option<string?>("--description") { Description = "Repository description" };
            var privateOption = new Option<bool>("--private") { Description = "Make repository private" };

            createCommand.Add(nameArgument);
            createCommand.Add(descriptionOption);
            createCommand.Add(privateOption);
            createCommand.Add(verboseOption);

            createCommand.SetAction(async (parseResult) =>
            {
                var name = parseResult.GetValue(nameArgument);
                var description = parseResult.GetValue(descriptionOption);
                var isPrivate = parseResult.GetValue(privateOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await CreateRepository(name!, description, isPrivate, verbose);
            });

            Subcommands.Add(createCommand);
        }

        private void AddDeleteCommand(Option<bool> verboseOption)
        {
            var deleteCommand = new System.CommandLine.Command("delete", "Delete a repository");
            var forceOption = new Option<bool>("--force") { Description = "Skip confirmation prompt" };

            deleteCommand.Add(forceOption);
            deleteCommand.Add(verboseOption);

            deleteCommand.SetAction(async (parseResult) =>
            {
                var force = parseResult.GetValue(forceOption);
                var verbose = parseResult.GetValue(verboseOption);
                return await DeleteRepository(force, verbose);
            });

            Subcommands.Add(deleteCommand);
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
            var showCommand = new System.CommandLine.Command("show", "Display repository information from current Git remote");

            showCommand.Add(verboseOption);

            showCommand.SetAction(async (parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                return await ShowRepository(verbose);
            });

            Subcommands.Add(showCommand);
        }

        /// <summary>
        /// Extracts a clean error message from API error responses.
        /// Handles both GitHub and Azure DevOps JSON error formats.
        /// </summary>
        private string ExtractErrorMessage(string fullError)
        {
            if (string.IsNullOrEmpty(fullError))
                return "Unknown error occurred";

            // Try to extract from JSON error response
            try
            {
                var jsonStart = fullError.IndexOf("{");
                if (jsonStart >= 0)
                {
                    var jsonStr = fullError.Substring(jsonStart);
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(jsonStr, options);

                    if (errorObj?.TryGetValue("message", out var msgElement) == true)
                    {
                        var msg = msgElement.GetString();
                        if (!string.IsNullOrEmpty(msg))
                        {
                            return msg;
                        }
                    }
                }
            }
            catch { /* Fall through to return original */ }

            // If no JSON message found, return the error up to the first newline (removes JSON body)
            var firstLine = fullError.Split('\n')[0];
            return firstLine;
        }

        private async Task<int> ListRepositories(int top, bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Listing repositories...");

                var platform = _platformDetector.DetectPlatform();

                if (platform == Platform.GitHub)
                {
                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    var repoInfo = _platformDetector.GetRepositoryInfo();
                    var owner = repoInfo?.Owner ?? "unknown";

                    using var client = new GitHubClient(token);
                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        _mappingPresenter.Present(_mappingGenerator.RepoListGitHub(owner, top));
                    }
                    var repos = await client.ListRepositoriesAsync(null, "all", 30, top);
                    if (repos == null) return 1;

                    // Display header
                    Console.WriteLine($"Active Repositories ({repos.Count} found):");
                    Console.WriteLine(new string('-', 70));

                    // Display each repository
                    foreach (var repo in repos)
                    {
                        Console.WriteLine($" -  {repo.Name}");
                        Console.WriteLine($"    URL: {repo.Url}");
                        if (!string.IsNullOrEmpty(repo.DefaultBranch))
                        {
                            Console.WriteLine($"    Branch: {repo.DefaultBranch}");
                        }
                        string visibility = repo.IsPrivate ? "Private" : "Public";
                        Console.WriteLine($"    Visibility: {visibility}");
                    }
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                    {
                        ConsoleHelper.WriteError("X Unable to determine Azure DevOps organization or project");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);
                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        _mappingPresenter.Present(_mappingGenerator.RepoListAzure(project, organization, top));
                    }
                    var repos = await client.ListRepositoriesAsync(project, top);
                    if (repos == null) return 1;

                    // Display header
                    Console.WriteLine($"Active Repositories ({repos.Count} found):");
                    Console.WriteLine(new string('-', 70));

                    // Display each repository
                    foreach (var repo in repos)
                    {
                        Console.WriteLine($" -  {repo.Name}");
                        Console.WriteLine($"    URL: {repo.Url}");
                        if (!string.IsNullOrEmpty(repo.DefaultBranch))
                        {
                            Console.WriteLine($"    Branch: {repo.DefaultBranch}");
                        }
                    }
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

        private async Task<int> ShowRepository(bool verbose)
        {
            try
            {
                if (verbose) Console.WriteLine("Retrieving repository information from current Git remote...");

                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    return 1;
                }

                if (platform == Platform.GitHub)
                {
                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    using var client = new GitHubClient(token);
                    var repo = await client.GetRepositoryAsync(repoInfo.Owner ?? "", repoInfo.Repo ?? "");
                    if (repo == null)
                    {
                        ConsoleHelper.WriteError("X Repository not found");
                        return 1;
                    }
                    Console.WriteLine("Repository Information:");
                    Console.WriteLine($"  Name: {repo.Name}");
                    Console.WriteLine($"  ID: {repo.PlatformId}");
                    Console.WriteLine($"  URL: {repo.Url}");
                    if (!string.IsNullOrEmpty(repo.DefaultBranch))
                        Console.WriteLine($"  Default Branch: {repo.DefaultBranch}");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    if (string.IsNullOrEmpty(organization))
                    {
                        ConsoleHelper.WriteError("X Could not determine Azure DevOps organization");
                        return 1;
                    }

                    var project = _platformDetector.GetProject();
                    using var client = new AzureDevOpsClient(pat, organization, project);
                    var repo = await client.GetRepositoryAsync(project ?? "", repoInfo.Repo ?? "");
                    if (repo == null)
                    {
                        ConsoleHelper.WriteError("X Repository not found");
                        return 1;
                    }
                    Console.WriteLine("Repository Information:");
                    Console.WriteLine($"  Name: {repo.Name}");
                    Console.WriteLine($"  ID: {repo.PlatformId}");
                    Console.WriteLine($"  URL: {repo.Url}");
                    if (!string.IsNullOrEmpty(repo.DefaultBranch))
                        Console.WriteLine($"  Default Branch: {repo.DefaultBranch}");
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

        private async Task<int> CreateRepository(string name, string? description, bool isPrivate, bool verbose)
        {
            try
            {
                var platform = _platformDetector.DetectPlatform();

                if (platform == Platform.GitHub)
                {
                    if (verbose) Console.WriteLine("Creating GitHub repository...");
                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    using var client = new GitHubClient(token);
                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        _mappingPresenter.Present(_mappingGenerator.RepoCreateGitHub(name, isPrivate, description!));
                    }
                    var repo = await client.CreateRepositoryAsync(name, description, isPrivate);
                    ConsoleHelper.WriteLine($"✓ Repository '{repo!.Name}' created successfully", ConsoleColor.Green);
                    Console.WriteLine($"  URL: {repo.Url}");
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    if (verbose) Console.WriteLine("Creating Azure DevOps repository...");

                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                    {
                        ConsoleHelper.WriteError("X Unable to determine Azure DevOps organization or project");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);

                    // Show external mapping command when verbose
                    if (verbose)
                    {
                        var mapping = $"az repos create --name \"{name}\" --project \"{project}\" --organization \"{organization}\"";
                        ConsoleHelper.WriteLine(mapping, ConsoleColor.Yellow);
                    }

                    // Get the project ID (required by Create API)
                    if (verbose) Console.WriteLine($"  Fetching project ID for '{project}'...");
                    var projectInfo = await client.GetProjectAsync(project);
                    if (projectInfo == null)
                    {
                        var cleanError = ExtractErrorMessage(client.LastError ?? "Failed to get project details");
                        ConsoleHelper.WriteError($"X {cleanError}");
                        return 1;
                    }

                    var repo = await client.CreateRepositoryAsync(projectInfo.Id ?? project, name);
                    if (repo == null)
                    {
                        var cleanError = ExtractErrorMessage(client.LastError ?? "Failed to create repository");
                        ConsoleHelper.WriteError($"X {cleanError}");
                        return 1;
                    }
                    ConsoleHelper.WriteLine($"✓ Repository '{repo.Name}' created successfully", ConsoleColor.Green);
                    Console.WriteLine($"  URL: {repo.Url}");
                    return 0;
                }

                ConsoleHelper.WriteError("X Unsupported platform");
                return 1;
            }
            catch (Exception ex)
            {
                var errorMsg = ExtractErrorMessage(ex.Message);
                ConsoleHelper.WriteError($"X Error: {errorMsg}");
                return 1;
            }
        }

        private async Task<int> DeleteRepository(bool force, bool verbose)
        {
            try
            {
                var platform = _platformDetector.DetectPlatform();
                var repoInfo = _platformDetector.GetRepositoryInfo();

                if (repoInfo == null || (string.IsNullOrEmpty(repoInfo.Owner) || string.IsNullOrEmpty(repoInfo.Repo)))
                {
                    ConsoleHelper.WriteError("X Unable to determine repository from current Git remote");
                    return 1;
                }

                var platformName = platform switch
                {
                    Platform.GitHub => "GitHub",
                    Platform.AzureDevOps => "Azure DevOps",
                    _ => "Unknown"
                };
                if (verbose) Console.WriteLine($"Deleting {platformName} repository '{repoInfo.Repo}'...");

                // Confirm deletion unless --force flag is used
                if (!force)
                {
                    Console.Write($"Are you sure you want to delete repository '{repoInfo.Repo}'? (yes/no): ");
                    var response = Console.ReadLine();
                    if (response?.ToLower() != "yes")
                    {
                        Console.WriteLine("Deletion cancelled");
                        return 0;
                    }
                }

                if (platform == Platform.GitHub)
                {
                    var token = await GetAuthenticationTokenAsync(Platform.GitHub);
                    if (string.IsNullOrEmpty(token))
                    {
                        ConsoleHelper.WriteLine("X Error: No authentication token found. Run 'sdo auth' to setup authentication.", ConsoleColor.Red);
                        return 1;
                    }

                    using var client = new GitHubClient(token);
                    await client.DeleteRepositoryAsync(repoInfo.Owner ?? "", repoInfo.Repo ?? "");
                    ConsoleHelper.WriteLine($"✓ Repository '{repoInfo.Repo}' deleted successfully", ConsoleColor.Green);
                    return 0;
                }
                else if (platform == Platform.AzureDevOps)
                {
                    var pat = await GetAuthenticationTokenAsync(Platform.AzureDevOps);
                    if (string.IsNullOrEmpty(pat))
                    {
                        ConsoleHelper.WriteError("X Error: No authentication token found. Run 'sdo auth' to setup authentication.");
                        return 1;
                    }

                    var organization = _platformDetector.GetOrganization();
                    var project = _platformDetector.GetProject();
                    if (string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
                    {
                        ConsoleHelper.WriteError("X Unable to determine Azure DevOps organization or project");
                        return 1;
                    }

                    using var client = new AzureDevOpsClient(pat, organization, project);

                    // Get the project ID (required by Delete API)
                    if (verbose) Console.WriteLine($"  Fetching project ID for '{project}'...");
                    var projectInfo = await client.GetProjectAsync(project);
                    if (projectInfo == null)
                    {
                        ConsoleHelper.WriteError($"X {client.LastError ?? "Failed to get project details"}");
                        return 1;
                    }

                    // Get the repository ID (required by Delete API)
                    if (verbose) Console.WriteLine($"  Fetching repository ID for '{repoInfo.Repo}'...");
                    var repoDetails = await client.GetRepositoryAsync(projectInfo.Id ?? project, repoInfo.Repo ?? "");
                    if (repoDetails == null)
                    {
                        var cleanError = ExtractErrorMessage(client.LastError ?? "Repository not found");
                        ConsoleHelper.WriteError($"X {cleanError}");
                        return 1;
                    }

                    var success = await client.DeleteRepositoryAsync(projectInfo.Id ?? project, repoDetails.PlatformId ?? repoInfo.Repo ?? "");
                    if (!success)
                    {
                        var cleanError = ExtractErrorMessage(client.LastError ?? "Failed to delete repository");
                        ConsoleHelper.WriteError($"X {cleanError}");
                        return 1;
                    }
                    ConsoleHelper.WriteLine($"✓ Repository '{repoInfo.Repo}' deleted successfully", ConsoleColor.Green);
                    return 0;
                }

                ConsoleHelper.WriteError("X Unsupported platform");
                return 1;
            }
            catch (Exception ex)
            {
                var errorMsg = ExtractErrorMessage(ex.Message);
                ConsoleHelper.WriteError($"X Error: {errorMsg}");
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
    }
}
