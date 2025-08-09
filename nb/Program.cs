using Nbuild;
using NbuildTasks;
using System.CommandLine;

namespace nb
{
    public static class Program
    {
        public static int Main(params string[] args)
        {
            ConsoleHelper.WriteLine($"{Nversion.Get()}\n", ConsoleColor.Yellow);
            var rootCommand = new RootCommand("Nbuild - Build and DevOps Utility");
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
            return rootCommand.Invoke(args);
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
            downloadCommand.SetHandler((string json, bool verbose) => {
                var exitCode = HandleDownloadCommand(json, verbose);
                Environment.ExitCode = exitCode;
            }, jsonOption, verboseOption);
            rootCommand.AddCommand(downloadCommand);
        }

    private static void AddPathCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("path", "Display path segments");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Path command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitInfoCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitInfoCommand = new System.CommandLine.Command("git_info", "Displays the current git information for the local repository");
            gitInfoCommand.SetHandler(() => {
                var exitCode = HandleGitInfoCommand();
                Environment.ExitCode = exitCode;
            });
            rootCommand.AddCommand(gitInfoCommand);
        }


    private static void AddGitSetTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitSetTagCommand = new System.CommandLine.Command("git_settag", "Sets the specified tag using the --tag option");
            var tagOption = new System.CommandLine.Option<string>("--tag", "Specifies the tag used")
            {
                IsRequired = true
            };
            gitSetTagCommand.AddOption(tagOption);
            gitSetTagCommand.SetHandler((string tag) => {
                var exitCode = HandleGitSetTagCommand(tag);
                Environment.ExitCode = exitCode;
            }, tagOption);
            rootCommand.AddCommand(gitSetTagCommand);
        }

    private static void AddGitAutoTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitAutoTagCommand = new System.CommandLine.Command("git_autotag", "Sets the next tag based on the build type: STAGE or PROD");
            gitAutoTagCommand.AddAlias("auto_tag");
            var buildTypeOption = new System.CommandLine.Option<string>("--buildtype", "Specifies the build type used for this command. Possible values: STAGE, PROD")
            {
                IsRequired = true
            };
            gitAutoTagCommand.AddOption(buildTypeOption);
            gitAutoTagCommand.SetHandler((string buildType) => {
                var exitCode = HandleGitAutoTagCommand(buildType, false);
                Environment.ExitCode = exitCode;
            }, buildTypeOption);
            rootCommand.AddCommand(gitAutoTagCommand);
        }

    private static void AddGitPushAutoTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitPushAutoTagCommand = new System.CommandLine.Command("git_push_autotag", "Sets the next tag based on the build type and pushes to the remote repository");
            var buildTypeOption = new System.CommandLine.Option<string>("--buildtype", "Specifies the build type used for this command. Possible values: STAGE, PROD")
            {
                IsRequired = true
            };
            gitPushAutoTagCommand.AddOption(buildTypeOption);
            gitPushAutoTagCommand.SetHandler((string buildType) => {
                var exitCode = HandleGitAutoTagCommand(buildType, true);
                Environment.ExitCode = exitCode;
            }, buildTypeOption);
            rootCommand.AddCommand(gitPushAutoTagCommand);
        }

    private static void AddGitBranchCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitBranchCommand = new System.CommandLine.Command("git_branch", "Displays the current git branch in the local repository");
            gitBranchCommand.SetHandler(() => {
                var exitCode = HandleGitBranchCommand();
                Environment.ExitCode = exitCode;
            });
            rootCommand.AddCommand(gitBranchCommand);
        }

    private static void AddGitCloneCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitCloneCommand = new System.CommandLine.Command("git_clone", "Clones the specified Git repository using the --url option");
            var urlOption = new System.CommandLine.Option<string>("--url", "Specifies the Git repository URL")
            {
                IsRequired = true
            };
            var pathOption = new System.CommandLine.Option<string>("--path", "The path where the repo will be cloned. If not specified, the current directory will be used");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            gitCloneCommand.AddOption(urlOption);
            gitCloneCommand.AddOption(pathOption);
            gitCloneCommand.AddOption(verboseOption);
            gitCloneCommand.SetHandler((string url, string path, bool verbose) => {
                var exitCode = HandleGitCloneCommand(url, path, verbose);
                Environment.ExitCode = exitCode;
            }, urlOption, pathOption, verboseOption);
            rootCommand.AddCommand(gitCloneCommand);
        }

    private static void AddGitDeleteTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var gitDeleteTagCommand = new System.CommandLine.Command("git_deletetag", "Deletes the specified tag using the --tag option");
            var tagOption = new System.CommandLine.Option<string>("--tag", "Specifies the tag used")
            {
                IsRequired = true
            };
            gitDeleteTagCommand.AddOption(tagOption);
            gitDeleteTagCommand.SetHandler((string tag) => {
                var exitCode = HandleGitDeleteTagCommand(tag);
                Environment.ExitCode = exitCode;
            }, tagOption);
            rootCommand.AddCommand(gitDeleteTagCommand);
        }

    private static void AddReleaseCreateCommand(System.CommandLine.RootCommand rootCommand)
        {
            var releaseCreateCommand = new System.CommandLine.Command("release_create", "Creates a GitHub release. Requires --repo, --tag, --branch, and --file options");
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
            releaseCreateCommand.AddOption(repoOption);
            releaseCreateCommand.AddOption(tagOption);
            releaseCreateCommand.AddOption(branchOption);
            releaseCreateCommand.AddOption(fileOption);
            releaseCreateCommand.SetHandler(async (string repo, string tag, string branch, string file) => {
                var exitCode = await HandleReleaseCreateCommand(repo, tag, branch, file, false);
                Environment.ExitCode = exitCode;
            }, repoOption, tagOption, branchOption, fileOption);
            rootCommand.AddCommand(releaseCreateCommand);
        }

    private static void AddPreReleaseCreateCommand(System.CommandLine.RootCommand rootCommand)
        {
            var preReleaseCreateCommand = new System.CommandLine.Command("pre_release_create", "Creates a GitHub pre-release. Requires --repo, --tag, --branch, and --file options");
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
            preReleaseCreateCommand.AddOption(repoOption);
            preReleaseCreateCommand.AddOption(tagOption);
            preReleaseCreateCommand.AddOption(branchOption);
            preReleaseCreateCommand.AddOption(fileOption);
            preReleaseCreateCommand.SetHandler(async (string repo, string tag, string branch, string file) => {
                var exitCode = await HandleReleaseCreateCommand(repo, tag, branch, file, true);
                Environment.ExitCode = exitCode;
            }, repoOption, tagOption, branchOption, fileOption);
            rootCommand.AddCommand(preReleaseCreateCommand);
        }

    private static void AddReleaseDownloadCommand(System.CommandLine.RootCommand rootCommand)
        {
            var releaseDownloadCommand = new System.CommandLine.Command("release_download", "Downloads a specific asset from a GitHub release. Requires --repo, --tag, and --path (optional, defaults to current directory)");
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
            releaseDownloadCommand.AddOption(repoOption);
            releaseDownloadCommand.AddOption(tagOption);
            releaseDownloadCommand.AddOption(pathOption);
            releaseDownloadCommand.SetHandler(async (string repo, string tag, string path) => {
                var exitCode = await HandleReleaseDownloadCommand(repo, tag, path);
                Environment.ExitCode = exitCode;
            }, repoOption, tagOption, pathOption);
            rootCommand.AddCommand(releaseDownloadCommand);
        }

    private static void AddListReleaseCommand(System.CommandLine.RootCommand rootCommand)
        {
            var listReleaseCommand = new System.CommandLine.Command("list_release", "Lists latest 3 releases for the specified repository (and latest pre-release if newer). Requires ----repo");
            var repoOption = new System.CommandLine.Option<string>("--repo", "Specifies the Git repository in any of the following formats:\n- repoName  (UserName is declared the `OWNER` environment variable)\n- userName/repoName\n- https://github.com/userName/repoName (Full URL to the repository on GitHub)")
            {
                IsRequired = true
            };
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            listReleaseCommand.AddOption(repoOption);
            listReleaseCommand.AddOption(verboseOption);
            listReleaseCommand.SetHandler(async (string repo, bool verbose) => {
                var exitCode = await HandleListReleasesCommand(repo, verbose);
                Environment.ExitCode = exitCode;
            }, repoOption, verboseOption);
            rootCommand.AddCommand(listReleaseCommand);
        }

    private static void AddTargetsCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("targets", "Display build targets");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Targets command executed.]");
            });
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
            installCommand.SetHandler((string json, bool verbose) => {
                var exitCode = HandleInstallCommand(json, verbose);
                Environment.ExitCode = exitCode;
            }, jsonOption, verboseOption);
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
            uninstallCommand.SetHandler((string json, bool verbose) => {
                var exitCode = HandleUninstallCommand(json, verbose);
                Environment.ExitCode = exitCode;
            }, jsonOption, verboseOption);
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

    private static int HandleInstallCommand(string json, bool verbose)
        {
            try
            {
                var result = Nbuild.Command.Install(json, verbose);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleUninstallCommand(string json, bool verbose)
        {
            try
            {
                var result = Nbuild.Command.Uninstall(json, verbose);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleDownloadCommand(string json, bool verbose)
        {
            try
            {
                var result = Nbuild.Command.Download(json, verbose);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

    private static int HandleGitInfoCommand()
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

    private static int HandleGitSetTagCommand(string tag)
        {
            try
            {
                var result = Nbuild.Command.SetTag(tag);
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

    private static int HandleGitCloneCommand(string url, string path, bool verbose)
        {
            try
            {
                var result = Nbuild.Command.Clone(url, path, verbose);
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
    private static async Task<int> HandleReleaseCreateCommand(string repo, string tag, string branch, string file, bool preRelease)
        {
            try
            {
                var result = await Nbuild.Command.CreateRelease(repo, tag, branch, file, preRelease);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }
    private static async Task<int> HandleReleaseDownloadCommand(string repo, string tag, string path)
        {
            try
            {
                var result = await Nbuild.Command.DownloadAsset(repo, tag, path);
                return result.Code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }
    private static async Task<int> HandleListReleasesCommand(string repo, bool verbose)
        {
            try
            {
                var result = await Nbuild.Command.ListReleases(repo, verbose);
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
