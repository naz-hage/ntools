// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
// 
// Nbuild Program.cs
// 
// This file contains the main entry point and command setup for the Nbuild CLI tool.
// Nbuild is a build and DevOps utility that provides MSBuild integration and various
// development operations through a command-line interface.

using Nbuild.Commands;
using Nbuild.Services;
using NbuildTasks;
using System.CommandLine;

namespace Nbuild
{
    /// <summary>
    /// Main program class for the Nbuild CLI application.
    /// </summary>
    /// <remarks>
    /// This class sets up the System.CommandLine root command with global options
    /// and various subcommands for build and DevOps operations. It handles
    /// argument parsing, validation, and execution of user commands.
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// Main entry point for the Nbuild application.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method initializes the CLI, processes global options, validates arguments,
        /// sets up commands, and executes the appropriate action based on user input.
        /// It handles both matched commands and unmatched tokens (treated as build targets).
        /// </remarks>
        public static int Main(params string[] args)
        {
            ConsoleHelper.WriteLine($"{Nversion.Get()}\n", ConsoleColor.Yellow);
            
            // Pre-process arguments to move global options to the front so they work regardless of position
            args = PreProcessGlobalOptions(args);
            
            // Check for common option typos BEFORE parsing
            var typoCheck = CliValidation.CheckForCommonTypos(args);
            if (typoCheck != null)
            {
                Console.Error.WriteLine(typoCheck);
                return 1;
            }

            var rootCommand = new RootCommand("Nbuild - Build and DevOps Utility");
            // Global options: add as root-level/global options so they can be provided on any command.
            var dryRunOption = new Option<bool>("--dry-run") 
            { 
                Description = "Perform a dry run: show actions but do not perform side effects",
                Required = false 
            };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            rootCommand.Options.Add(dryRunOption);
            rootCommand.Options.Add(verboseOption);
            AddInstallCommand(rootCommand, dryRunOption, verboseOption);
            AddUninstallCommand(rootCommand, dryRunOption, verboseOption);
            AddListCommand(rootCommand);
            AddDownloadCommand(rootCommand, dryRunOption, verboseOption);
            AddPathCommand(rootCommand, verboseOption);
            AddGitInfoCommand(rootCommand, dryRunOption, verboseOption);
            AddGitSetTagCommand(rootCommand, dryRunOption, verboseOption);
            AddGitAutoTagCommand(rootCommand, dryRunOption, verboseOption);
            AddGitPushAutoTagCommand(rootCommand, dryRunOption, verboseOption);
            AddGitBranchCommand(rootCommand, verboseOption);

            // register git_clone command from dedicated class (pass global dry-run option and service)
            GitCloneCommand.Register(rootCommand, dryRunOption, verboseOption, new GitCloneService());
            AddGitDeleteTagCommand(rootCommand, dryRunOption, verboseOption);
            AddReleaseCreateCommand(rootCommand, dryRunOption, verboseOption);
            AddPreReleaseCreateCommand(rootCommand, dryRunOption, verboseOption);
            AddReleaseDownloadCommand(rootCommand, dryRunOption, verboseOption);
            AddListReleaseCommand(rootCommand, dryRunOption, verboseOption);
            AddTargetsCommand(rootCommand, verboseOption);

            // Enable strict validation for the root command but allow unmatched tokens for build targets
            rootCommand.TreatUnmatchedTokensAsErrors = false;

            // Handle unmatched tokens as potential build targets
            rootCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var unmatched = parseResult.UnmatchedTokens;

                // Check for potential option typos before treating as build targets
                var potentialOptions = unmatched.Where(token => token.StartsWith("-")).ToList();
                if (potentialOptions.Any())
                {
                    foreach (var option in potentialOptions)
                    {
                        var suggestion = CliValidation.GetOptionSuggestion(option, rootCommand);
                        if (!string.IsNullOrEmpty(suggestion))
                        {
                            Console.Error.WriteLine($"Unknown option '{option}'. Did you mean '{suggestion}'?");
                            return 1;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Unknown option '{option}'. Use 'nb --help' to see available options.");
                            return 1;
                        }
                    }
                }

                // If there are unmatched tokens, treat them as build targets
                if (unmatched.Count == 1)
                {
                    var verbose = parseResult.GetValue(verboseOption);
                    var target = unmatched[0];
                    ConsoleHelper.WriteLine($"Executing target: {target}", ConsoleColor.Green);
                    var resultHelper = BuildStarter.Build(target, verbose);
                    if (resultHelper.IsFail())
                    {
                        ConsoleHelper.WriteLine($"Failed to execute target '{target}': {resultHelper.GetFirstOutput()}", ConsoleColor.Red);
                    }
                    return resultHelper.Code;
                }
                else if (unmatched.Count > 1)
                {
                    Console.Error.WriteLine($"Unknown command or too many arguments: {string.Join(' ', unmatched)}");
                    return 1;
                }
                return 0;
            });
            
            // Validate for common option typos before execution
            if (CliValidation.ValidateArgsForTypos(args))
            {
                return 1; // Exit with error code if typos found
            }

            return rootCommand.Parse(args).Invoke();
        }

        /// <summary>
        /// Pre-processes command line arguments to move global options (--dry-run, --verbose) to the front
        /// so they work regardless of their position in the command line.
        /// </summary>
        /// <param name="args">The original command line arguments.</param>
        /// <returns>A reordered array with global options at the beginning.</returns>
        /// <remarks>
        /// This method ensures that global options are processed correctly by System.CommandLine
        /// by placing them at the start of the argument array. This is necessary because
        /// global options need to be recognized before command-specific parsing occurs.
        /// </remarks>
        private static string[] PreProcessGlobalOptions(string[] args)
        {
            if (args.Length == 0)
                return args;

            var globalOptions = new List<string>();
            var otherArgs = new List<string>();

            foreach (var arg in args)
            {
                if (arg == "--dry-run" || arg == "--verbose")
                {
                    globalOptions.Add(arg);
                }
                else
                {
                    otherArgs.Add(arg);
                }
            }

            // Return global options first, then other arguments
            return globalOptions.Concat(otherArgs).ToArray();
        }

        // Try to call Nversion.Get in a compatibility-safe way. Some compiled binaries or older versions
        // may expose a different overload (for example Get(string)). We prefer calling the parameterless
        // overload when available, otherwise attempt to invoke any available Get method via reflection.

        /// <summary>
        /// Adds the download command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the download command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command allows users to download tools and applications based on a manifest file.
        /// It supports dry-run mode for testing and verbose output for detailed information.
        /// </remarks>
        private static void AddDownloadCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var downloadCommand = new System.CommandLine.Command("download", "Download tools and applications specified in the manifest file.");

            // Enable strict option validation for this subcommand
            downloadCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.", Required = true };

            downloadCommand.Options.Add(jsonOption);
            downloadCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var json = parseResult.GetValue(jsonOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                var exitCode = HandleDownloadCommand(json, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(downloadCommand);
        }

        /// <summary>
        /// Adds the path command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the path command to.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command displays the PATH environment variable segments in a clean format,
        /// removing duplicates and showing the effective path used by processes.
        /// </remarks>
        private static void AddPathCommand(RootCommand rootCommand, Option<bool> verboseOption)
        {
            var cmd = new System.CommandLine.Command("path", "Display each segment of the effective PATH environment variable on a separate line, with duplicates removed. Shows the complete PATH that processes actually use (Machine + User PATH combined).");
            cmd.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                PathManager.DisplayPathSegments();
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying PATH segments.", ConsoleColor.Gray);
                return 0;
            });
            rootCommand.Subcommands.Add(cmd);
        }

        /// <summary>
        /// Adds the git_info command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the git_info command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command displays current git repository information including branch and latest tag.
        /// It provides essential git status information for development workflows.
        /// </remarks>
        private static void AddGitInfoCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var gitInfoCommand = new System.CommandLine.Command(
                "git_info",
                "Displays the current git information for the local repository, including branch, and latest tag.\n\n"
            );
            gitInfoCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying Git repository information.", ConsoleColor.Gray);
                var exitCode = HandleGitInfoCommand(verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitInfoCommand);
        }

        /// <summary>
        /// Adds the git_settag command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the git_settag command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command allows users to manually set a git tag in the local repository.
        /// It requires a tag parameter and supports dry-run mode for testing.
        /// </remarks>
        private static void AddGitSetTagCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var gitSetTagCommand = new System.CommandLine.Command("git_settag",
                "Sets a git tag in the local repository.");
            var tagOption = new Option<string>("--tag") { Description = "Tag to set (e.g., 1.24.33)", Required = true };
            gitSetTagCommand.Options.Add(tagOption);
            gitSetTagCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var tag = parseResult.GetValue(tagOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Setting git tag: {tag}", ConsoleColor.Gray);
                var exitCode = HandleGitSetTagCommand(tag, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitSetTagCommand);
        }

        /// <summary>
        /// Adds the git_autotag command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the git_autotag command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command automatically generates and sets the next git tag based on the specified build type.
        /// It supports both staging and production build types for automated versioning.
        /// </remarks>
        private static void AddGitAutoTagCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var gitAutoTagCommand = new System.CommandLine.Command("git_autotag",
                "Automatically sets the next git tag based on build type.");
            var buildTypeOption = new Option<string>("--buildtype") { Description = "Specifies the build type used for this command. Possible values: STAGE, PROD", Required = true };
            gitAutoTagCommand.Options.Add(buildTypeOption);
            gitAutoTagCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var buildType = parseResult.GetValue(buildTypeOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Auto-tagging for build type: {buildType}", ConsoleColor.Gray);
                var exitCode = HandleGitAutoTagCommand(buildType, false, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitAutoTagCommand);
        }

        /// <summary>
        /// Adds the git_push_autotag command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the git_push_autotag command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command automatically generates a git tag based on build type and pushes it to the remote repository.
        /// It combines auto-tagging with remote synchronization for CI/CD workflows.
        /// </remarks>
        private static void AddGitPushAutoTagCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var gitPushAutoTagCommand = new System.CommandLine.Command("git_push_autotag",
                "Sets the next git tag based on build type and pushes to remote.");
            var buildTypeOption = new Option<string>("--buildtype") { Description = "Specifies the build type used for this command. Possible values: STAGE, PROD", Required = true };
            gitPushAutoTagCommand.Options.Add(buildTypeOption);
            gitPushAutoTagCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var buildType = parseResult.GetValue(buildTypeOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Push auto-tag for build type: {buildType}", ConsoleColor.Gray);
                var exitCode = HandleGitAutoTagCommand(buildType, true, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitPushAutoTagCommand);
        }

        /// <summary>
        /// Adds the git_branch command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the git_branch command to.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command displays the current git branch name for the local repository.
        /// It provides quick access to branch information for development workflows.
        /// </remarks>
        private static void AddGitBranchCommand(RootCommand rootCommand, Option<bool> verboseOption)
        {
            var gitBranchCommand = new System.CommandLine.Command("git_branch",
                "Displays the current git branch in the local repository."
            );
            gitBranchCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying git branch.", ConsoleColor.Gray);
                var exitCode = HandleGitBranchCommand();
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitBranchCommand);
        }

        /// <summary>
        /// Adds the git_deletetag command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the git_deletetag command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command deletes a specified git tag from the local repository.
        /// It requires a tag parameter and supports dry-run mode for safe testing.
        /// </remarks>
        private static void AddGitDeleteTagCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var gitDeleteTagCommand = new System.CommandLine.Command("git_deletetag",
                "Deletes a git tag from the local repository.");
            var tagOption = new Option<string>("--tag") { Description = "Tag to delete (e.g., 1.24.33)", Required = true };
            gitDeleteTagCommand.Options.Add(tagOption);
            gitDeleteTagCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var tag = parseResult.GetValue(tagOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Deleting git tag: {tag}", ConsoleColor.Gray);
                var exitCode = HandleGitDeleteTagCommand(tag, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitDeleteTagCommand);
        }

        /// <summary>
        /// Adds the release_create command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the release_create command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command creates a GitHub release with the specified tag and optional assets.
        /// It integrates with GitHub's release API for automated release management.
        /// </remarks>
        private static void AddReleaseCreateCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var releaseCreateCommand = new System.CommandLine.Command("release_create",
                "Creates a GitHub release.");
            var repoOption = new Option<string>("--repo") { Description = "Git repository. Accepts:\n  - repoName (uses OWNER env variable)\n  - userName/repoName\n  - Full GitHub URL (https://github.com/userName/repoName)", Required = true };
            var tagOption = new Option<string>("--tag") { Description = "Specifies the tag used", Required = true };
            var branchOption = new Option<string>("--branch") { Description = "Specifies the branch name", Required = true };
            var fileOption = new Option<string>("--file") { Description = "Specifies the asset file name. Must include full path", Required = true };
            releaseCreateCommand.Options.Add(repoOption);
            releaseCreateCommand.Options.Add(tagOption);
            releaseCreateCommand.Options.Add(branchOption);
            releaseCreateCommand.Options.Add(fileOption);
            releaseCreateCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var branch = parseResult.GetValue(branchOption)!;
                var file = parseResult.GetValue(fileOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"[VERBOSE] Creating release for repo: {repo}, tag: {tag}, branch: {branch}, file: {file}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"[VERBOSE] OWNER env: {Environment.GetEnvironmentVariable("OWNER")}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"[VERBOSE] API_GITHUB_KEY env: {(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) ? "(not set)" : "(set)")}", ConsoleColor.Gray);
                }
                var exitCode = await HandleReleaseCreateCommand(repo, tag, branch, file, false, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(releaseCreateCommand);
        }

        /// <summary>
        /// Adds the pre_release_create command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the pre_release_create command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command creates a GitHub pre-release (draft release) with the specified parameters.
        /// Pre-releases are typically used for beta versions or release candidates.
        /// </remarks>
        private static void AddPreReleaseCreateCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var preReleaseCreateCommand = new System.CommandLine.Command(
                "pre_release_create",
                "Creates a GitHub pre-release."
            );
            var repoOption = new Option<string>("--repo") { Description = "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)", Required = true };
            var tagOption = new Option<string>("--tag") { Description = "Specifies the tag used", Required = true };
            var branchOption = new Option<string>("--branch") { Description = "Specifies the branch name", Required = true };
            var fileOption = new Option<string>("--file") { Description = "Specifies the asset file name. Must include full path", Required = true };
            preReleaseCreateCommand.Options.Add(repoOption);
            preReleaseCreateCommand.Options.Add(tagOption);
            preReleaseCreateCommand.Options.Add(branchOption);
            preReleaseCreateCommand.Options.Add(fileOption);
            preReleaseCreateCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var branch = parseResult.GetValue(branchOption)!;
                var file = parseResult.GetValue(fileOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose)
                {
                    ConsoleHelper.WriteLine($"[VERBOSE] Creating pre-release for repo: {repo}, tag: {tag}, branch: {branch}, file: {file}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"[VERBOSE] OWNER env: {Environment.GetEnvironmentVariable("OWNER")}", ConsoleColor.Gray);
                    ConsoleHelper.WriteLine($"[VERBOSE] API_GITHUB_KEY env: {(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) ? "(not set)" : "(set)")}", ConsoleColor.Gray);
                }

                // Substitute repoName with OWNER/repoName if only a single name is provided
                if (!string.IsNullOrWhiteSpace(repo) && !repo.Contains("/") && !repo.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var owner = Environment.GetEnvironmentVariable("OWNER");
                    if (!string.IsNullOrEmpty(owner))
                    {
                        repo = $"{owner}/{repo}";
                        if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Substituted repo argument with OWNER: {repo}", ConsoleColor.Gray);
                    }
                }

                var exitCode = await HandleReleaseCreateCommand(repo, tag, branch, file, true, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(preReleaseCreateCommand);
        }

        /// <summary>
        /// Adds the release_download command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the release_download command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command downloads assets from a specific GitHub release.
        /// It supports specifying repository, tag, and download path for asset retrieval.
        /// </remarks>
        private static void AddReleaseDownloadCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var releaseDownloadCommand = new System.CommandLine.Command(
                "release_download",
                "Downloads a specific asset from a GitHub release."
            );
            var repoOption = new Option<string>("--repo") { Description = "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)", Required = true };
            var tagOption = new Option<string>("--tag") { Description = "Specifies the tag used", Required = true };
            var pathOption = new Option<string>("--path") { Description = "Specifies the path used for this command. If not specified, the current directory will be used", Required = false };
            releaseDownloadCommand.Options.Add(repoOption);
            releaseDownloadCommand.Options.Add(tagOption);
            releaseDownloadCommand.Options.Add(pathOption);
            releaseDownloadCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var path = parseResult.GetValue(pathOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Downloading asset for repo: {repo}, tag: {tag}, path: {path}", ConsoleColor.Gray);
                var exitCode = await HandleReleaseDownloadCommand(repo, tag, path, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(releaseDownloadCommand);
        }

        /// <summary>
        /// Adds the list_release command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the list_release command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command lists the latest releases for a specified GitHub repository.
        /// It provides an overview of available releases for version management.
        /// </remarks>
        private static void AddListReleaseCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var listReleaseCommand = new System.CommandLine.Command(
                "list_release",
                "Lists the latest releases for the specified repository."
            );
            var repoOption = new Option<string>("--repo") { Description = "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)", Required = true };
            listReleaseCommand.Options.Add(repoOption);
            listReleaseCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine($"Verbose mode enabled", ConsoleColor.Yellow);
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Listing releases for repo: {repo}", ConsoleColor.Gray);
                var exitCode = await HandleListReleasesCommand(repo, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(listReleaseCommand);
        }

        /// <summary>
        /// Adds the targets command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the targets command to.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command displays all available MSBuild targets for the current project.
        /// It helps users discover what build operations are available in their project.
        /// </remarks>
        private static void AddTargetsCommand(RootCommand rootCommand, Option<bool> verboseOption)
        {
            var cmd = new System.CommandLine.Command(
                "targets",
                "Displays all available build targets for the current solution or project."
            );
            cmd.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying build targets.", ConsoleColor.Gray);
                var result = BuildStarter.DisplayTargets(Environment.CurrentDirectory);
                return result.Code;
            });
            rootCommand.Subcommands.Add(cmd);
        }

        /// <summary>
        /// Adds the install command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the install command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command installs tools and applications based on a manifest file.
        /// It supports dry-run mode for testing installations without making changes.
        /// </remarks>
        private static void AddInstallCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var installCommand = new System.CommandLine.Command("install", "Install tools and applications specified in the manifest file.");

            // Enable strict option validation for this subcommand
            installCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.", Required = true };
            installCommand.Options.Add(jsonOption);
            installCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var json = parseResult.GetValue(jsonOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                var exitCode = HandleInstallCommand(json, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(installCommand);
        }

        /// <summary>
        /// Adds the uninstall command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the uninstall command to.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <remarks>
        /// This command uninstalls tools and applications based on a manifest file.
        /// It provides the reverse operation of the install command for cleanup.
        /// </remarks>
        private static void AddUninstallCommand(RootCommand rootCommand, Option<bool> dryRunOption, Option<bool> verboseOption)
        {
            var uninstallCommand = new System.CommandLine.Command("uninstall", "Uninstall tools and applications specified in the manifest file.");
            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.", Required = true };
            uninstallCommand.Options.Add(jsonOption);
            uninstallCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var json = parseResult.GetValue(jsonOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                var exitCode = HandleUninstallCommand(json, verbose, dryRun);
                return exitCode;
            });
            rootCommand.Subcommands.Add(uninstallCommand);
        }

        /// <summary>
        /// Adds the list command to the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the list command to.</param>
        /// <remarks>
        /// This command displays a formatted table of all installed tools and their versions.
        /// It helps users audit and document their development environment setup.
        /// </remarks>
        private static void AddListCommand(RootCommand rootCommand)
        {
            var listCommand = new System.CommandLine.Command(
                "list",
                "Display a formatted table of all tools and their versions.\nUse this command to audit, compare, or document the state of your development environment."
            );

            // Enable strict option validation for this subcommand
            listCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes." };
            jsonOption.DefaultValueFactory = _ =>
            {
                var programFiles = Environment.GetEnvironmentVariable("ProgramFiles") ?? "C:\\Program Files";
                var defaultPath = $"{programFiles}\\NBuild\\ntools.json";
                return $"\"{defaultPath}\"";
            };
            listCommand.Options.Add(jsonOption);
            listCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var json = parseResult.GetValue(jsonOption) ?? string.Empty;
                var exitCode = HandleListCommand(json);
                return exitCode;
            });
            rootCommand.Subcommands.Add(listCommand);
        }

        /// <summary>
        /// Handles the list command execution.
        /// </summary>
        /// <param name="json">Path to the manifest JSON file.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method processes the list command by calling the Command.List method
        /// to display installed tools and their versions in a formatted table.
        /// </remarks>
        private static int HandleListCommand(string json)
        {
            try
            {
                var result = Command.List(json, false);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the install command execution.
        /// </summary>
        /// <param name="json">Path to the manifest JSON file.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method processes the install command by calling the Command.Install method
        /// to install tools and applications specified in the manifest file.
        /// Supports dry-run mode for testing installations.
        /// </remarks>
        private static int HandleInstallCommand(string json, bool verbose, bool dryRun)
        {
            try
            {
                var result = Command.Install(json, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the uninstall command execution.
        /// </summary>
        /// <param name="json">Path to the manifest JSON file.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method processes the uninstall command by calling the Command.Uninstall method
        /// to remove tools and applications specified in the manifest file.
        /// Supports dry-run mode for testing uninstallations.
        /// </remarks>
        private static int HandleUninstallCommand(string json, bool verbose, bool dryRun)
        {
            try
            {
                var result = Command.Uninstall(json, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the download command execution.
        /// </summary>
        /// <param name="json">Path to the manifest JSON file.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method processes the download command by calling the Command.Download method
        /// to download tools and applications specified in the manifest file.
        /// Supports dry-run mode for testing downloads.
        /// </remarks>
        private static int HandleDownloadCommand(string json, bool verbose, bool dryRun)
        {
            try
            {
                var result = Command.Download(json, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the git_info command execution.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose output. Defaults to false.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes. Defaults to false.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method displays current git repository information including branch and latest tag.
        /// It provides essential git status information for development workflows.
        /// </remarks>
        private static int HandleGitInfoCommand(bool verbose = false, bool dryRun = false)
        {
            try
            {
                var result = Command.DisplayGitInfo(verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the git_settag command execution.
        /// </summary>
        /// <param name="tag">The tag to set in the git repository.</param>
        /// <param name="verbose">Whether to enable verbose output. Defaults to false.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes. Defaults to false.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method sets a git tag in the local repository.
        /// It supports dry-run mode for testing tag operations.
        /// </remarks>
        private static int HandleGitSetTagCommand(string tag, bool verbose = false, bool dryRun = false)
        {
            try
            {
                var result = Command.SetTag(tag, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the git_autotag command execution.
        /// </summary>
        /// <param name="buildType">The build type for automatic tagging (STAGE or PROD).</param>
        /// <param name="push">Whether to push the tag to remote repository.</param>
        /// <param name="verbose">Whether to enable verbose output. Defaults to false.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes. Defaults to false.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method automatically generates and sets git tags based on build type.
        /// It can optionally push the tags to the remote repository.
        /// Supports dry-run mode for testing tag operations.
        /// </remarks>
        private static int HandleGitAutoTagCommand(string buildType, bool push, bool verbose = false, bool dryRun = false)
        {
            try
            {
                var result = Command.SetAutoTag(buildType, push, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Handles the git_branch command execution.
        /// </summary>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method displays the current git branch name for the local repository.
        /// It provides quick access to branch information for development workflows.
        /// </remarks>
        private static int HandleGitBranchCommand()
        {
            try
            {
                var result = Command.DisplayGitBranch();
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        // Git clone logic moved to IGitCloneService; helper removed.

        /// <summary>
        /// Handles the git_deletetag command execution.
        /// </summary>
        /// <param name="tag">The tag to delete from the git repository.</param>
        /// <param name="verbose">Whether to enable verbose output. Defaults to false.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes. Defaults to false.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method deletes a specified git tag from the local repository.
        /// It supports dry-run mode for testing tag deletion operations.
        /// </remarks>
        private static int HandleGitDeleteTagCommand(string tag, bool verbose = false, bool dryRun = false)
        {
            try
            {
                var result = Command.DeleteTag(tag, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }
        /// <summary>
        /// Handles the release_create command execution.
        /// </summary>
        /// <param name="repo">The GitHub repository identifier.</param>
        /// <param name="tag">The tag for the release.</param>
        /// <param name="branch">The branch for the release.</param>
        /// <param name="file">The asset file to attach to the release.</param>
        /// <param name="preRelease">Whether this is a pre-release.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method creates a GitHub release with the specified parameters and optional assets.
        /// It integrates with GitHub's release API for automated release management.
        /// Supports both regular releases and pre-releases.
        /// </remarks>
        private static async Task<int> HandleReleaseCreateCommand(string repo, string tag, string branch, string file, bool preRelease, bool dryRun)
        {
            try
            {
                var result = await Command.CreateRelease(repo, tag, branch, file, preRelease, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                // Provide better error messages for common authentication issues
                if (ex.Message.Contains("API_GITHUB_KEY") || ex.Message.Contains("Environment variable") || ex.Message.Contains("GitHub"))
                {
                    NbuildTasks.ErrorHelper.WriteGitHubAuthError($"https://github.com/{repo}", "create release", false);
                    Console.WriteLine("\nFor more information about GitHub authentication, see: https://docs.github.com/en/authentication");
                }
                else
                {
                    NbuildTasks.ErrorHelper.WriteError(ex.Message);
                }
                return -1;
            }
        }
        /// <summary>
        /// Handles the release_download command execution.
        /// </summary>
        /// <param name="repo">The GitHub repository identifier.</param>
        /// <param name="tag">The tag of the release to download from.</param>
        /// <param name="path">The download path for the asset.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method downloads assets from a specific GitHub release.
        /// It supports specifying repository, tag, and download path for asset retrieval.
        /// </remarks>
        private static async Task<int> HandleReleaseDownloadCommand(string repo, string tag, string path, bool dryRun)
        {
            try
            {
                var result = await Command.DownloadAsset(repo, tag, path, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                // Provide better error messages for common authentication issues
                if (ex.Message.Contains("API_GITHUB_KEY") || ex.Message.Contains("Environment variable") || ex.Message.Contains("GitHub"))
                {
                    NbuildTasks.ErrorHelper.WriteGitHubAuthError($"https://github.com/{repo}", "download asset", true);
                    Console.WriteLine("\nFor more information about GitHub authentication, see: https://docs.github.com/en/authentication");
                }
                else
                {
                    NbuildTasks.ErrorHelper.WriteError(ex.Message);
                }
                return -1;
            }
        }
        /// <summary>
        /// Handles the list_release command execution.
        /// </summary>
        /// <param name="repo">The GitHub repository identifier.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        /// <remarks>
        /// This method lists the latest releases for a specified GitHub repository.
        /// It provides an overview of available releases for version management.
        /// </remarks>
        private static async Task<int> HandleListReleasesCommand(string repo, bool verbose, bool dryRun)
        {
            try
            {
                var result = await Command.ListReleases(repo, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                // Provide better error messages for common authentication issues
                if (ex.Message.Contains("API_GITHUB_KEY") || ex.Message.Contains("Environment variable") || ex.Message.Contains("GitHub"))
                {
                    NbuildTasks.ErrorHelper.WriteGitHubAuthError($"https://github.com/{repo}", "list releases", true);
                    Console.WriteLine("\nFor more information about GitHub authentication, see: https://docs.github.com/en/authentication");
                }
                else
                {
                    NbuildTasks.ErrorHelper.WriteError(ex.Message);
                }
                return -1;
            }
        }
    }
}

