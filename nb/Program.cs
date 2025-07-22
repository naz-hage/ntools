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
            var downloadCommand = new System.CommandLine.Command("download", "Download tools from JSON");
            var jsonOption = new System.CommandLine.Option<string>("--json", "Path to JSON file");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            downloadCommand.AddOption(jsonOption);
            downloadCommand.AddOption(verboseOption);
            downloadCommand.SetHandler((string json, bool verbose) => {
                try
                {
                    var result = Nbuild.Command.Download(json, verbose);
                    Environment.Exit(result.Code);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(-1);
                }
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
            var cmd = new System.CommandLine.Command("git_info", "Display git info");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git info command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitSetTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("git_settag", "Set git tag");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git settag command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitAutoTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("git_autotag", "Set git autotag");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git autotag command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitPushAutoTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("git_push_autotag", "Push git autotag");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git push autotag command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitBranchCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("git_branch", "Display git branch");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git branch command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitCloneCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("git_clone", "Clone git repository");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git clone command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddGitDeleteTagCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("git_deletetag", "Delete git tag");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Git deletetag command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddReleaseCreateCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("release_create", "Create release");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Release create command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddPreReleaseCreateCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("pre_release_create", "Create pre-release");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Pre-release create command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddReleaseDownloadCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("release_download", "Download release asset");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ Release download command executed.]");
            });
            rootCommand.AddCommand(cmd);
        }

    private static void AddListReleaseCommand(System.CommandLine.RootCommand rootCommand)
        {
            var cmd = new System.CommandLine.Command("list_release", "List releases");
            cmd.SetHandler(() => {
                ConsoleHelper.WriteLine("[Green!√ List release command executed.]");
            });
            rootCommand.AddCommand(cmd);
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
            var installCommand = new System.CommandLine.Command("install", "Install tools from JSON");
            var jsonOption = new System.CommandLine.Option<string>("--json", "Path to JSON file");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            installCommand.AddOption(jsonOption);
            installCommand.AddOption(verboseOption);
            installCommand.SetHandler((string json, bool verbose) => {
                try
                {
                    var result = Nbuild.Command.Install(json, verbose);
                    Environment.Exit(result.Code);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(-1);
                }
            }, jsonOption, verboseOption);
            rootCommand.AddCommand(installCommand);
        }

    private static void AddUninstallCommand(System.CommandLine.RootCommand rootCommand)
        {
            var uninstallCommand = new System.CommandLine.Command("uninstall", "Uninstall tools from JSON");
            var jsonOption = new System.CommandLine.Option<string>("--json", "Path to JSON file");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose", "Verbose output");
            uninstallCommand.AddOption(jsonOption);
            uninstallCommand.AddOption(verboseOption);
            uninstallCommand.SetHandler((string json, bool verbose) => {
                try
                {
                    var result = Nbuild.Command.Uninstall(json, verbose);
                    Environment.Exit(result.Code);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(-1);
                }
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
                try
                {
                    var result = Nbuild.Command.List(json, false);
                    Environment.Exit(result.Code);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(-1);
                }
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
    }
}
