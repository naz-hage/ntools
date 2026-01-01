using Nbuild.Commands;
using Nbuild.Services;
using NbuildTasks;
using System.CommandLine;

namespace Nbuild
{
    public static class Program
    {
        public static int Main(params string[] args)
        {
            ConsoleHelper.WriteLine($"{Nversion.Get()}\n", ConsoleColor.Yellow);
            // Check for common option typos BEFORE parsing
            var typoCheck = CliValidation.CheckForCommonTypos(args);
            if (typoCheck != null)
            {
                Console.Error.WriteLine(typoCheck);
                return 1;
            }

            var rootCommand = new RootCommand("Nbuild - Build and DevOps Utility");
            // Global dry-run option: add as a root-level/global option so it can be provided on any command.
            var dryRunOption = new Option<bool>("--dry-run") 
            { 
                Description = "Perform a dry run: show actions but do not perform side effects",
                Required = false 
            };
            rootCommand.Options.Add(dryRunOption);

            // NOTE: prefer per-command --verbose options; no global --verbose option registered
            AddInstallCommand(rootCommand, dryRunOption);
            AddUninstallCommand(rootCommand, dryRunOption);
            AddListCommand(rootCommand);
            AddDownloadCommand(rootCommand, dryRunOption);
            AddPathCommand(rootCommand);
            AddGitInfoCommand(rootCommand, dryRunOption);
            AddGitSetTagCommand(rootCommand, dryRunOption);
            AddGitAutoTagCommand(rootCommand, dryRunOption);
            AddGitPushAutoTagCommand(rootCommand, dryRunOption);
            AddGitBranchCommand(rootCommand);

            // register git_clone command from dedicated class (pass global dry-run option and service)
            GitCloneCommand.Register(rootCommand, dryRunOption, new GitCloneService());
            AddGitDeleteTagCommand(rootCommand, dryRunOption);
            AddReleaseCreateCommand(rootCommand);
            AddPreReleaseCreateCommand(rootCommand);
            AddReleaseDownloadCommand(rootCommand);
            AddListReleaseCommand(rootCommand, dryRunOption);
            AddTargetsCommand(rootCommand);

            // Enable strict validation for the root command but allow unmatched tokens for build targets
            rootCommand.TreatUnmatchedTokensAsErrors = false;

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

                if (unmatched.Count == 1)
                {
                    var target = unmatched[0];
                    ConsoleHelper.WriteLine($"Executing target: {target}", ConsoleColor.Green);
                    var resultHelper = BuildStarter.Build(target, false);
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

        // Try to call Nversion.Get in a compatibility-safe way. Some compiled binaries or older versions
        // may expose a different overload (for example Get(string)). We prefer calling the parameterless
        // overload when available, otherwise attempt to invoke any available Get method via reflection.

        private static void AddDownloadCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var downloadCommand = new System.CommandLine.Command("download", "Download tools and applications specified in the manifest file.");

            // Enable strict option validation for this subcommand
            downloadCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };

            downloadCommand.Options.Add(jsonOption);
            downloadCommand.Options.Add(verboseOption);
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

        private static void AddPathCommand(RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("path", "Display each segment of the effective PATH environment variable on a separate line, with duplicates removed. Shows the complete PATH that processes actually use (Machine + User PATH combined).");
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            cmd.Options.Add(verboseOption);
            cmd.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                PathManager.DisplayPathSegments();
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying PATH segments.", ConsoleColor.Gray);
                return 0;
            });
            rootCommand.Subcommands.Add(cmd);
        }

        private static void AddGitInfoCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var gitInfoCommand = new System.CommandLine.Command(
                "git_info",
                "Displays the current git information for the local repository, including branch, and latest tag.\n\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_info --verbose\n"
            );
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            gitInfoCommand.Options.Add(verboseOption);
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

        private static void AddGitSetTagCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var gitSetTagCommand = new System.CommandLine.Command("git_settag",
                "Sets a git tag in the local repository.\n\n" +
                "Required option:\n" +
                "  --tag   The tag to set (e.g., 1.24.33)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_settag --tag 1.24.33 --verbose\n");
            var tagOption = new Option<string>("--tag") { Description = "Tag to set (e.g., 1.24.33)", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            gitSetTagCommand.Options.Add(tagOption);
            gitSetTagCommand.Options.Add(verboseOption);
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

        private static void AddGitAutoTagCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var gitAutoTagCommand = new System.CommandLine.Command("git_autotag",
                "Automatically sets the next git tag based on build type.\n\n" +
                "Required option:\n" +
                "  --buildtype   Build type (STAGE or PROD)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_autotag --buildtype STAGE --verbose\n");
            gitAutoTagCommand.Aliases.Add("auto_tag");
            var buildTypeOption = new Option<string>("--buildtype") { Description = "Specifies the build type used for this command. Possible values: STAGE, PROD", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            gitAutoTagCommand.Options.Add(buildTypeOption);
            gitAutoTagCommand.Options.Add(verboseOption);
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

        private static void AddGitPushAutoTagCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var gitPushAutoTagCommand = new System.CommandLine.Command("git_push_autotag",
                "Sets the next git tag based on build type and pushes to remote.\n\n" +
                "Required option:\n" +
                "  --buildtype   Build type (STAGE or PROD)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_push_autotag --buildtype PROD --verbose\n");
            var buildTypeOption = new Option<string>("--buildtype") { Description = "Specifies the build type used for this command. Possible values: STAGE, PROD", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            gitPushAutoTagCommand.Options.Add(buildTypeOption);
            gitPushAutoTagCommand.Options.Add(verboseOption);
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

        private static void AddGitBranchCommand(RootCommand rootCommand)
        {
            var gitBranchCommand = new System.CommandLine.Command("git_branch",
                "Displays the current git branch in the local repository.\n\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_branch --verbose\n"
            );
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            gitBranchCommand.Options.Add(verboseOption);
            gitBranchCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying git branch.", ConsoleColor.Gray);
                var exitCode = HandleGitBranchCommand();
                return exitCode;
            });
            rootCommand.Subcommands.Add(gitBranchCommand);
        }


        private static void AddGitDeleteTagCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var gitDeleteTagCommand = new System.CommandLine.Command("git_deletetag",
                "Deletes a git tag from the local repository.\n\n" +
                "Required option:\n" +
                "  --tag   The tag to delete (e.g., 1.24.33)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_deletetag --tag 1.24.33 --verbose\n");
            var tagOption = new Option<string>("--tag") { Description = "Tag to delete (e.g., 1.24.33)", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            gitDeleteTagCommand.Options.Add(tagOption);
            gitDeleteTagCommand.Options.Add(verboseOption);
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

        private static void AddReleaseCreateCommand(RootCommand rootCommand)
        {
            var releaseCreateCommand = new System.CommandLine.Command("release_create",
                "Creates a GitHub release.\n\n" +
                "Required options:\n" +
                "  --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)\n" +
                "  --tag    Tag to use for the release (e.g., 1.24.33)\n" +
                "  --branch Branch name to release from (e.g., main)\n" +
                "  --file   Asset file name (full path required)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Examples:\n" +
                "  nb release_create --repo user/repo --tag 1.24.33 --branch main --file C:\\path\\to\\asset.zip --verbose\n" +
                "  nb release_create --repo https://github.com/user/repo --tag 1.24.33 --branch main --file ./asset.zip --verbose\n");
            var repoOption = new Option<string>("--repo") { Description = "Git repository. Accepts:\n  - repoName (uses OWNER env variable)\n  - userName/repoName\n  - Full GitHub URL (https://github.com/userName/repoName)", Required = true };
            var tagOption = new Option<string>("--tag") { Description = "Specifies the tag used", Required = true };
            var branchOption = new Option<string>("--branch") { Description = "Specifies the branch name", Required = true };
            var fileOption = new Option<string>("--file") { Description = "Specifies the asset file name. Must include full path", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            releaseCreateCommand.Options.Add(repoOption);
            releaseCreateCommand.Options.Add(tagOption);
            releaseCreateCommand.Options.Add(branchOption);
            releaseCreateCommand.Options.Add(fileOption);
            releaseCreateCommand.Options.Add(verboseOption);
            releaseCreateCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var branch = parseResult.GetValue(branchOption)!;
                var file = parseResult.GetValue(fileOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(rootCommand.Options.OfType<Option<bool>>().First(o => o.Name == "dry-run"));
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

        private static void AddPreReleaseCreateCommand(RootCommand rootCommand)
        {
            var preReleaseCreateCommand = new System.CommandLine.Command(
                "pre_release_create",
                "Creates a GitHub pre-release.\n\n" +
                "Required options:\n" +
                "  --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)\n" +
                "  --tag    Tag to use for the pre-release (e.g., 1.24.33)\n" +
                "  --branch Branch name to release from (e.g., main)\n" +
                "  --file   Asset file name (full path required)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb pre_release_create --repo user/repo --tag 1.24.33 --branch main --file C:\\path\\to\\asset.zip --verbose\n"
            );
            var repoOption = new Option<string>("--repo") { Description = "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)", Required = true };
            var tagOption = new Option<string>("--tag") { Description = "Specifies the tag used", Required = true };
            var branchOption = new Option<string>("--branch") { Description = "Specifies the branch name", Required = true };
            var fileOption = new Option<string>("--file") { Description = "Specifies the asset file name. Must include full path", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            preReleaseCreateCommand.Options.Add(repoOption);
            preReleaseCreateCommand.Options.Add(tagOption);
            preReleaseCreateCommand.Options.Add(branchOption);
            preReleaseCreateCommand.Options.Add(fileOption);
            preReleaseCreateCommand.Options.Add(verboseOption);
            preReleaseCreateCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var branch = parseResult.GetValue(branchOption)!;
                var file = parseResult.GetValue(fileOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(rootCommand.Options.OfType<Option<bool>>().First(o => o.Name == "dry-run"));
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

        private static void AddReleaseDownloadCommand(RootCommand rootCommand)
        {
            var releaseDownloadCommand = new System.CommandLine.Command(
                "release_download",
                "Downloads a specific asset from a GitHub release.\n\n" +
                "Required options:\n" +
                "  --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)\n" +
                "  --tag    Tag to use for the release (e.g., 1.24.33)\n" +
                "Optional option:\n" +
                "  --path   Path to download asset to (default: current directory)\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb release_download --repo user/repo --tag 1.24.33 --path C:\\downloads --verbose\n"
            );
            var repoOption = new Option<string>("--repo") { Description = "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)", Required = true };
            var tagOption = new Option<string>("--tag") { Description = "Specifies the tag used", Required = true };
            var pathOption = new Option<string>("--path") { Description = "Specifies the path used for this command. If not specified, the current directory will be used", Required = false };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            releaseDownloadCommand.Options.Add(repoOption);
            releaseDownloadCommand.Options.Add(tagOption);
            releaseDownloadCommand.Options.Add(pathOption);
            releaseDownloadCommand.Options.Add(verboseOption);
            releaseDownloadCommand.SetAction(async (System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var repo = parseResult.GetValue(repoOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var path = parseResult.GetValue(pathOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(rootCommand.Options.OfType<Option<bool>>().First(o => o.Name == "dry-run"));
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

        private static void AddListReleaseCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var listReleaseCommand = new System.CommandLine.Command(
                "list_release",
                "Lists the latest 3 releases for the specified repository, and the latest pre-release if newer.\n\n" +
                "Required option:\n" +
                "  --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb list_release --repo user/repo --verbose\n"
            );
            var repoOption = new Option<string>("--repo") { Description = "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            listReleaseCommand.Options.Add(repoOption);
            listReleaseCommand.Options.Add(verboseOption);
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

        private static void AddTargetsCommand(RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command(
                "targets",
                "Displays all available build targets for the current solution or project.\n\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "You can run any listed target directly using nb.exe.\n" +
                "Example: If 'core' is listed, you can run:\n" +
                "  nb core\n\n" +
                "To list all targets:\n" +
                "  nb targets --verbose\n"
            );
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            cmd.Options.Add(verboseOption);
            cmd.SetAction((System.CommandLine.ParseResult parseResult) =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying build targets.", ConsoleColor.Gray);
                var result = BuildStarter.DisplayTargets(Environment.CurrentDirectory);
                return result.Code;
            });
            rootCommand.Subcommands.Add(cmd);
        }

        private static void AddInstallCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var installCommand = new System.CommandLine.Command("install", "Install tools and applications specified in the manifest file.");

            // Enable strict option validation for this subcommand
            installCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            installCommand.Options.Add(jsonOption);
            installCommand.Options.Add(verboseOption);
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

        private static void AddUninstallCommand(RootCommand rootCommand, Option<bool> dryRunOption)
        {
            var uninstallCommand = new System.CommandLine.Command("uninstall", "Uninstall tools and applications specified in the manifest file.");
            var jsonOption = new Option<string>("--json") { Description = "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.", Required = true };
            var verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            uninstallCommand.Options.Add(jsonOption);
            uninstallCommand.Options.Add(verboseOption);
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

