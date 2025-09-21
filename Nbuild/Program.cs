using Nbuild;
using NbuildTasks;
using System.CommandLine;

namespace nb
{
    public static class Program
    {
        public static int Main(params string[] args)
        {
            ConsoleHelper.WriteLine($"{GetNversionString()}\n", ConsoleColor.Yellow);
            var rootCommand = new RootCommand("Nbuild - Build and DevOps Utility");
            // Global dry-run option: add as a root-level/global option so it can be provided on any command.
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run", "Perform a dry run: show actions but do not perform side effects") { IsRequired = false };
            rootCommand.AddGlobalOption(dryRunOption);
            AddInstallCommand(rootCommand);
            AddUninstallCommand(rootCommand);
            AddListCommand(rootCommand);
            AddDownloadCommand(rootCommand);
            AddPathCommand(rootCommand);
            AddGitInfoCommand(rootCommand);
            AddGitSetTagCommand(rootCommand);
            AddGitAutoTagCommand(rootCommand);
            AddGitPushAutoTagCommand(rootCommand);
            AddGitBranchCommand(rootCommand);
            AddGitCloneCommand(rootCommand);
            AddGitDeleteTagCommand(rootCommand);
            AddReleaseCreateCommand(rootCommand);
            AddPreReleaseCreateCommand(rootCommand);
            AddReleaseDownloadCommand(rootCommand);
            AddListReleaseCommand(rootCommand);
            AddTargetsCommand(rootCommand);
            rootCommand.TreatUnmatchedTokensAsErrors = false;
            rootCommand.SetHandler((System.CommandLine.Invocation.InvocationContext ctx) => {
                var unmatched = ctx.ParseResult.UnmatchedTokens;
                if (unmatched.Count == 1)
                {
                    var target = unmatched[0];
                    ConsoleHelper.WriteLine($"Executing target: {target}", ConsoleColor.Green);
                    var resultHelper = BuildStarter.Build(target, false);
                    Environment.ExitCode = resultHelper.Code;
                }
                else if (unmatched.Count > 1)
                {
                    Console.Error.WriteLine($"Unknown command or too many arguments: {string.Join(' ', unmatched)}");
                    Environment.ExitCode = 1;
                }
            });
            // Parse the provided args to capture global options (like --dry-run) before invoking handlers.
            var parseResult = rootCommand.Parse(args);

            return rootCommand.Invoke(args);
        }

        // Try to call Nversion.Get in a compatibility-safe way. Some compiled binaries or older versions
        // may expose a different overload (for example Get(string)). We prefer calling the parameterless
        // overload when available, otherwise attempt to invoke any available Get method via reflection.
        private static string GetNversionString()
        {
            try
            {
                // Try the straightforward call first
                return Nversion.Get();
            }
            catch (System.MissingMethodException)
            {
                try
                {
                    var t = typeof(Nversion);
                    // Try find parameterless 'Get' via reflection
                    var m0 = t.GetMethod("Get", System.Type.EmptyTypes);
                    if (m0 != null)
                    {
                        var r = m0.Invoke(null, null);
                        return r?.ToString() ?? string.Empty;
                    }

                    // Try any 'Get' with a single parameter; pass an empty string or default value
                    var candidates = t.GetMethods().Where(mi => mi.Name == "Get" && mi.GetParameters().Length == 1);
                    foreach (var mi in candidates)
                    {
                        var p = mi.GetParameters()[0];
                        object? arg = null;
                        if (p.ParameterType == typeof(string)) arg = "";
                        else
                        {
                            try { arg = Activator.CreateInstance(p.ParameterType); } catch { arg = null; }
                        }
                        var r = mi.Invoke(null, new object?[] { arg });
                        return r?.ToString() ?? string.Empty;
                    }
                }
                catch
                {
                    // swallow and fallback
                }
            }
            catch
            {
                // any other exceptions - return a fallback
            }

            return "ntools (version unknown)";
        }

    private static void AddDownloadCommand(System.CommandLine.RootCommand rootCommand)
        {
            var downloadCommand = new System.CommandLine.Command("download", "Download tools and applications specified in the manifest file.");
            var jsonOption = new System.CommandLine.Option<string>("--json", "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");

            downloadCommand.AddOption(jsonOption);
            downloadCommand.AddOption(verboseOption);
            downloadCommand.SetHandler((string json, bool verbose, bool dryRun) => {
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                var exitCode = HandleDownloadCommand(json, verbose, dryRun);
                Environment.ExitCode = exitCode;
            }, jsonOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(downloadCommand);
        }

    private static void AddPathCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("path", "Display each segment of your PATH environment variable on a separate line. Similar to 'echo %PATH%'.");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            cmd.AddOption(verboseOption);
            cmd.SetHandler((bool verbose) => {
                var result = Nbuild.Command.DisplayPathSegments();
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying PATH segments.", ConsoleColor.Gray);
                Environment.ExitCode = result.Code;
            }, verboseOption);
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitInfoCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitInfoCommand = new System.CommandLine.Command(
                "git_info",
                "Displays the current git information for the local repository, including branch, and latest tag.\n\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_info --verbose\n"
            );
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitInfoCommand.AddOption(verboseOption);
            gitInfoCommand.SetHandler((bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying git info.", ConsoleColor.Gray);
                var exitCode = HandleGitInfoCommand();
                Environment.ExitCode = exitCode;
            }, verboseOption);
            rootCommand.AddCommand(gitInfoCommand);
        }


    private static void AddGitSetTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitSetTagCommand = new System.CommandLine.Command("git_settag",
                "Sets a git tag in the local repository.\n\n" +
                "Required option:\n" +
                "  --tag   The tag to set (e.g., 1.24.33)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_settag --tag 1.24.33 --verbose\n");
            var tagOption = new System.CommandLine.Option<string>("--tag", "Tag to set (e.g., 1.24.33)")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitSetTagCommand.AddOption(tagOption);
            gitSetTagCommand.AddOption(verboseOption);
            gitSetTagCommand.SetHandler((string tag, bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Setting git tag: {tag}", ConsoleColor.Gray);
                var exitCode = HandleGitSetTagCommand(tag);
                Environment.ExitCode = exitCode;
            }, tagOption, verboseOption);
            rootCommand.AddCommand(gitSetTagCommand);
        }

    private static void AddGitAutoTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitAutoTagCommand = new System.CommandLine.Command("git_autotag",
                "Automatically sets the next git tag based on build type.\n\n" +
                "Required option:\n" +
                "  --buildtype   Build type (STAGE or PROD)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_autotag --buildtype STAGE --verbose\n");
            gitAutoTagCommand.AddAlias("auto_tag");
            var buildTypeOption = new System.CommandLine.Option<string>("--buildtype", "Specifies the build type used for this command. Possible values: STAGE, PROD")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitAutoTagCommand.AddOption(buildTypeOption);
            gitAutoTagCommand.AddOption(verboseOption);
            gitAutoTagCommand.SetHandler((string buildType, bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Auto-tagging for build type: {buildType}", ConsoleColor.Gray);
                var exitCode = HandleGitAutoTagCommand(buildType, false);
                Environment.ExitCode = exitCode;
            }, buildTypeOption, verboseOption);
            rootCommand.AddCommand(gitAutoTagCommand);
        }

    private static void AddGitPushAutoTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitPushAutoTagCommand = new System.CommandLine.Command("git_push_autotag",
                "Sets the next git tag based on build type and pushes to remote.\n\n" +
                "Required option:\n" +
                "  --buildtype   Build type (STAGE or PROD)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_push_autotag --buildtype PROD --verbose\n");
            var buildTypeOption = new System.CommandLine.Option<string>("--buildtype", "Specifies the build type used for this command. Possible values: STAGE, PROD")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitPushAutoTagCommand.AddOption(buildTypeOption);
            gitPushAutoTagCommand.AddOption(verboseOption);
            gitPushAutoTagCommand.SetHandler((string buildType, bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Push auto-tag for build type: {buildType}", ConsoleColor.Gray);
                var exitCode = HandleGitAutoTagCommand(buildType, true);
                Environment.ExitCode = exitCode;
            }, buildTypeOption, verboseOption);
            rootCommand.AddCommand(gitPushAutoTagCommand);
        }

    private static void AddGitBranchCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitBranchCommand = new System.CommandLine.Command("git_branch",
                "Displays the current git branch in the local repository.\n\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_branch --verbose\n");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitBranchCommand.AddOption(verboseOption);
            gitBranchCommand.SetHandler((bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying git branch.", ConsoleColor.Gray);
                var exitCode = HandleGitBranchCommand();
                Environment.ExitCode = exitCode;
            }, verboseOption);
            rootCommand.AddCommand(gitBranchCommand);
        }

    private static void AddGitCloneCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitCloneCommand = new System.CommandLine.Command("git_clone",
                "Clones a Git repository to a specified path.\n\n" +
                "Required option:\n" +
                "  --url   Git repository URL\n" +
                "Optional options:\n" +
                "  --path      Path to clone into (default: current directory)\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_clone --url https://github.com/user/repo --path ./repo --verbose\n");
            var urlOption = new System.CommandLine.Option<string>("--url", "Specifies the Git repository URL")
            {
                IsRequired = true
            };
            var pathOption = new System.CommandLine.Option<string>("--path", "The path where the repo will be cloned. If not specified, the current directory will be used");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitCloneCommand.AddOption(urlOption);
            gitCloneCommand.AddOption(pathOption);
            gitCloneCommand.AddOption(verboseOption);
            gitCloneCommand.SetHandler((string url, string path, bool verbose, bool dryRun) => {
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Cloning repo: {url} to {path}", ConsoleColor.Gray);
                var exitCode = HandleGitCloneCommand(url, path, verbose, dryRun);
                Environment.ExitCode = exitCode;
            }, urlOption, pathOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(gitCloneCommand);
        }

    private static void AddGitDeleteTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitDeleteTagCommand = new System.CommandLine.Command("git_deletetag",
                "Deletes a git tag from the local repository.\n\n" +
                "Required option:\n" +
                "  --tag   The tag to delete (e.g., 1.24.33)\n" +
                "Optional option:\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_deletetag --tag 1.24.33 --verbose\n");
            var tagOption = new System.CommandLine.Option<string>("--tag", "Tag to delete (e.g., 1.24.33)")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitDeleteTagCommand.AddOption(tagOption);
            gitDeleteTagCommand.AddOption(verboseOption);
            gitDeleteTagCommand.SetHandler((string tag, bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Deleting git tag: {tag}", ConsoleColor.Gray);
                var exitCode = HandleGitDeleteTagCommand(tag);
                Environment.ExitCode = exitCode;
            }, tagOption, verboseOption);
            rootCommand.AddCommand(gitDeleteTagCommand);
        }

        private static void AddReleaseCreateCommand(System.CommandLine.RootCommand rootCommand)
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
            var repoOption = new System.CommandLine.Option<string>("--repo",
                "Git repository. Accepts:\n  - repoName (uses OWNER env variable)\n  - userName/repoName\n  - Full GitHub URL (https://github.com/userName/repoName)")
            {
                IsRequired = true
            };
            var tagOption = new System.CommandLine.Option<string>("--tag", "Specifies the tag used")
            {
                IsRequired = true
            };
            var branchOption = new System.CommandLine.Option<string>("--branch", "Specifies the branch name")
            {
                IsRequired = true
            };
            var fileOption = new System.CommandLine.Option<string>("--file", "Specifies the asset file name. Must include full path")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            releaseCreateCommand.AddOption(repoOption);
            releaseCreateCommand.AddOption(tagOption);
            releaseCreateCommand.AddOption(branchOption);
            releaseCreateCommand.AddOption(fileOption);
            releaseCreateCommand.AddOption(verboseOption);
            releaseCreateCommand.SetHandler(async (string repo, string tag, string branch, string file, bool verbose, bool dryRun) =>
            {
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
                Environment.ExitCode = exitCode;
            }, repoOption, tagOption, branchOption, fileOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(releaseCreateCommand);
        }

    private static void AddPreReleaseCreateCommand(System.CommandLine.RootCommand rootCommand)
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
            var repoOption = new System.CommandLine.Option<string>("--repo", "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)")
            {
                IsRequired = true
            };
            var tagOption = new System.CommandLine.Option<string>("--tag", "Specifies the tag used")
            {
                IsRequired = true
            };
            var branchOption = new System.CommandLine.Option<string>("--branch", "Specifies the branch name")
            {
                IsRequired = true
            };
            var fileOption = new System.CommandLine.Option<string>("--file", "Specifies the asset file name. Must include full path")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            preReleaseCreateCommand.AddOption(repoOption);
            preReleaseCreateCommand.AddOption(tagOption);
            preReleaseCreateCommand.AddOption(branchOption);
            preReleaseCreateCommand.AddOption(fileOption);
            preReleaseCreateCommand.AddOption(verboseOption);
            preReleaseCreateCommand.SetHandler(async (string repo, string tag, string branch, string file, bool verbose, bool dryRun) => {
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
                Environment.ExitCode = exitCode;
            }, repoOption, tagOption, branchOption, fileOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(preReleaseCreateCommand);
        }

    private static void AddReleaseDownloadCommand(System.CommandLine.RootCommand rootCommand)
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
            var repoOption = new System.CommandLine.Option<string>("--repo", "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)")
            {
                IsRequired = true
            };
            var tagOption = new System.CommandLine.Option<string>("--tag", "Specifies the tag used")
            {
                IsRequired = true
            };
            var pathOption = new System.CommandLine.Option<string>("--path", "Specifies the path used for this command. If not specified, the current directory will be used")
            {
                IsRequired = false
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            releaseDownloadCommand.AddOption(repoOption);
            releaseDownloadCommand.AddOption(tagOption);
            releaseDownloadCommand.AddOption(pathOption);
            releaseDownloadCommand.AddOption(verboseOption);
            releaseDownloadCommand.SetHandler(async (string repo, string tag, string path, bool verbose, bool dryRun) => {
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Downloading asset for repo: {repo}, tag: {tag}, path: {path}", ConsoleColor.Gray);
                var exitCode = await HandleReleaseDownloadCommand(repo, tag, path, dryRun);
                Environment.ExitCode = exitCode;
            }, repoOption, tagOption, pathOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(releaseDownloadCommand);
        }

    private static void AddListReleaseCommand(System.CommandLine.RootCommand rootCommand)
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
            var repoOption = new System.CommandLine.Option<string>("--repo", "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            listReleaseCommand.AddOption(repoOption);
            listReleaseCommand.AddOption(verboseOption);
            listReleaseCommand.SetHandler(async (string repo, bool verbose, bool dryRun) => {
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                if (verbose) ConsoleHelper.WriteLine($"[VERBOSE] Listing releases for repo: {repo}", ConsoleColor.Gray);
                var exitCode = await HandleListReleasesCommand(repo, verbose, dryRun);
                Environment.ExitCode = exitCode;
            }, repoOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(listReleaseCommand);
        }

    private static void AddTargetsCommand(System.CommandLine.RootCommand rootCommand)
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
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            cmd.AddOption(verboseOption);
            cmd.SetHandler((bool verbose) => {
                if (verbose) ConsoleHelper.WriteLine("[VERBOSE] Displaying build targets.", ConsoleColor.Gray);
                var result = Nbuild.BuildStarter.DisplayTargets(Environment.CurrentDirectory);
                Environment.ExitCode = result.Code;
            }, verboseOption);
            rootCommand.AddCommand(cmd);
        }

    private static void AddInstallCommand(System.CommandLine.RootCommand rootCommand)
        {
            var installCommand = new System.CommandLine.Command("install", "Install tools and applications specified in the manifest file.");
            var jsonOption = new System.CommandLine.Option<string>("--json", "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            installCommand.AddOption(jsonOption);
            installCommand.AddOption(verboseOption);
            installCommand.SetHandler((string json, bool verbose, bool dryRun) => {
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                var exitCode = HandleInstallCommand(json, verbose, dryRun);
                Environment.ExitCode = exitCode;
            }, jsonOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(installCommand);
        }

    private static void AddUninstallCommand(System.CommandLine.RootCommand rootCommand)
        {
            var uninstallCommand = new System.CommandLine.Command("uninstall", "Uninstall tools and applications specified in the manifest file.");
            var jsonOption = new System.CommandLine.Option<string>("--json", "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            uninstallCommand.AddOption(jsonOption);
            uninstallCommand.AddOption(verboseOption);
            uninstallCommand.SetHandler((string json, bool verbose, bool dryRun) => {
                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }
                var exitCode = HandleUninstallCommand(json, verbose, dryRun);
                Environment.ExitCode = exitCode;
            }, jsonOption, verboseOption, ((System.CommandLine.RootCommand)rootCommand).Options.OfType<System.CommandLine.Option<bool>>().First(o => o.Name == "dry-run"));
            rootCommand.AddCommand(uninstallCommand);
        }

    private static void AddListCommand(System.CommandLine.RootCommand rootCommand)
        {
            var listCommand = new System.CommandLine.Command(
                "list",
                "Display a formatted table of all tools and their versions.\nUse this command to audit, compare, or document the state of your development environment."
            );
            var jsonOption = new System.CommandLine.Option<string>("--json", () =>
            {
                var programFiles = Environment.GetEnvironmentVariable("ProgramFiles") ?? "C:\\Program Files";
                var defaultPath = $"{programFiles}\\NBuild\\ntools.json";
                return $"\"{defaultPath}\"";
            }, "Full path to the manifest file containing your tool definitions.\nIf the path contains spaces, use double quotes.");
            listCommand.AddOption(jsonOption);
            listCommand.SetHandler((string json) =>
            {
                var exitCode = HandleListCommand(json);
                Environment.ExitCode = exitCode;
            }, jsonOption);
            rootCommand.AddCommand(listCommand);
        }

    private static int HandleListCommand(string json)
        {
            try
            {
                var result = Nbuild.Command.List(json, false);
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
                var result = Nbuild.Command.Install(json, verbose, dryRun);
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
                var result = Nbuild.Command.Uninstall(json, verbose, dryRun);
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
                var result = Nbuild.Command.Download(json, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleGitInfoCommand(bool verbose = false)
        {
            try
            {
                var result = Nbuild.Command.DisplayGitInfo();
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleGitSetTagCommand(string tag, bool verbose = false)
        {
            try
            {
                var result = Nbuild.Command.SetTag(tag, verbose);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleGitAutoTagCommand(string buildType, bool push)
        {
            try
            {
                var result = Nbuild.Command.SetAutoTag(buildType, push);
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
                var result = Nbuild.Command.DisplayGitBranch();
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleGitCloneCommand(string url, string path, bool verbose, bool dryRun)
        {
            try
            {
                var result = Nbuild.Command.Clone(url, path, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleGitDeleteTagCommand(string tag)
        {
            try
            {
                var result = Nbuild.Command.DeleteTag(tag);
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
                var result = await Nbuild.Command.CreateRelease(repo, tag, branch, file, preRelease, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }
    private static async Task<int> HandleReleaseDownloadCommand(string repo, string tag, string path, bool dryRun)
        {
            try
            {
                var result = await Nbuild.Command.DownloadAsset(repo, tag, path, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }
    private static async Task<int> HandleListReleasesCommand(string repo, bool verbose, bool dryRun)
        {
            try
            {
                var result = await Nbuild.Command.ListReleases(repo, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }
    }
}
