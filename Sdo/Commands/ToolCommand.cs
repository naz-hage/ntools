using System.CommandLine;
using Launcher.Helpers;
using NbuildCommand = Nbuild.Command;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for environment tool management.
    /// </summary>
    public class ToolCommand : System.CommandLine.Command
    {
        /// <summary>
        /// Initializes a new instance of the ToolCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public ToolCommand(Option<bool> verboseOption, Option<bool>? dryRunOption = null) : base("tool", "Environment tool installation, auditing, and manifest management")
        {
            AddInstallCommand(verboseOption, dryRunOption);
            AddListCommand(verboseOption);
            AddUninstallCommand(verboseOption, dryRunOption);
            AddDownloadCommand(verboseOption, dryRunOption);
        }

        private void AddInstallCommand(Option<bool> verboseOption, Option<bool>? dryRunOption)
        {
            var installCommand = new System.CommandLine.Command("install", "Install tools from a manifest or by application name.");
            var jsonOption = new Option<string?>("--json", ["-j"]) { Description = "Path to the tools manifest" };
            var manifestOption = new Option<string?>("--manifest", ["-m"]) { Description = "Path to a JSON or YAML tools manifest" };
            var nameOption = new Option<string?>("--name", ["-n"]) { Description = "Application name to find in apps.json" };
            var versionOption = new Option<string?>("--appversion", ["-av"]) { Description = "Optional application version override" };

            installCommand.Add(jsonOption);
            installCommand.Add(manifestOption);
            installCommand.Add(nameOption);
            installCommand.Add(versionOption);
            installCommand.Add(verboseOption);
            AddDryRunOption(installCommand, dryRunOption);
            installCommand.SetAction(parseResult =>
            {
                var json = parseResult.GetValue(jsonOption);
                var manifest = parseResult.GetValue(manifestOption);
                var name = parseResult.GetValue(nameOption);
                var version = parseResult.GetValue(versionOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult, dryRunOption);

                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(manifest))
                {
                    Console.Error.WriteLine("Error: Use either --json or --manifest, not both.");
                    return 1;
                }

                var input = manifest ?? json;
                if (string.IsNullOrEmpty(input) && string.IsNullOrEmpty(name))
                {
                    Console.Error.WriteLine("Error: Either --json (-j) or --name (-n) must be specified.");
                    return 1;
                }

                WriteDryRunNotice(dryRun);
                try
                {
                    return NbuildCommand.Install(input, name, version, verbose, dryRun).Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(installCommand);
        }

        private void AddUninstallCommand(Option<bool> verboseOption, Option<bool>? dryRunOption)
        {
            var uninstallCommand = new System.CommandLine.Command("uninstall", "Uninstall tools from a manifest.");
            var jsonOption = new Option<string>("--json") { Description = "Path to the tools manifest", Required = true };
            var manifestOption = new Option<string?>("--manifest", ["-m"]) { Description = "Path to a JSON or YAML tools manifest" };
            jsonOption.Required = false;
            uninstallCommand.Add(jsonOption);
            uninstallCommand.Add(manifestOption);
            uninstallCommand.Add(verboseOption);
            AddDryRunOption(uninstallCommand, dryRunOption);
            uninstallCommand.SetAction(parseResult =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult, dryRunOption);
                var json = parseResult.GetValue(jsonOption);
                var manifest = parseResult.GetValue(manifestOption);
                if (string.IsNullOrEmpty(json) == string.IsNullOrEmpty(manifest))
                {
                    Console.Error.WriteLine("Error: Specify exactly one of --json or --manifest.");
                    return 1;
                }
                WriteDryRunNotice(dryRun);
                try
                {
                    return NbuildCommand.Uninstall(manifest ?? json, verbose, dryRun).Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(uninstallCommand);
        }

        private void AddDownloadCommand(Option<bool> verboseOption, Option<bool>? dryRunOption)
        {
            var downloadCommand = new System.CommandLine.Command("download", "Download tools from a manifest.");
            var jsonOption = new Option<string?>("--json", ["-j"]) { Description = "Path to the JSON tools manifest" };
            var manifestOption = new Option<string?>("--manifest", ["-m"]) { Description = "Path to a JSON or YAML tools manifest" };
            downloadCommand.Add(jsonOption);
            downloadCommand.Add(manifestOption);
            downloadCommand.Add(verboseOption);
            AddDryRunOption(downloadCommand, dryRunOption);
            downloadCommand.SetAction(parseResult =>
            {
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = GetDryRunValue(parseResult, dryRunOption);
                var json = parseResult.GetValue(jsonOption);
                var manifest = parseResult.GetValue(manifestOption);
                if (string.IsNullOrEmpty(json) == string.IsNullOrEmpty(manifest))
                {
                    Console.Error.WriteLine("Error: Specify exactly one of --json or --manifest.");
                    return 1;
                }
                WriteDryRunNotice(dryRun);
                try
                {
                    return NbuildCommand.Download(manifest ?? json, verbose, dryRun).Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(downloadCommand);
        }

        private static void AddDryRunOption(System.CommandLine.Command command, Option<bool>? dryRunOption)
        {
            if (dryRunOption != null)
            {
                command.Add(dryRunOption);
            }
        }

        private static bool GetDryRunValue(ParseResult parseResult, Option<bool>? dryRunOption)
        {
            return dryRunOption != null && parseResult.GetValue(dryRunOption);
        }

        private static void WriteDryRunNotice(bool dryRun)
        {
            if (dryRun)
            {
                ConsoleHelper.WriteWarning("DRY-RUN: running in dry-run mode; no destructive actions will be performed.");
            }
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new System.CommandLine.Command(
                "list",
                "Display a formatted table of all tools and their versions.");
            listCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json", ["-j"])
            {
                Description = "Full path to the manifest file containing your tool definitions."
            };
            jsonOption.DefaultValueFactory = _ =>
            {
                if (Environment.GetEnvironmentVariable("ProgramFiles") is string programFiles)
                {
                    return $"\"{Path.Combine(programFiles, "nbuild", "apps.json")}\"";
                }

                return $"\"{NbuildCommand.DefaultAppsFile}\"";
            };

            var manifestOption = new Option<string?>("--manifest", ["-m"])
            {
                Description = "Path to a JSON or YAML tools manifest"
            };

            listCommand.Options.Add(jsonOption);
            listCommand.Options.Add(manifestOption);
            listCommand.Add(verboseOption);
            listCommand.SetAction(parseResult =>
            {
                var json = parseResult.GetValue(jsonOption) ?? string.Empty;
                var manifest = parseResult.GetValue(manifestOption);
                var verbose = parseResult.GetValue(verboseOption);

                if (!string.IsNullOrEmpty(manifest))
                {
                    json = manifest;
                }

                try
                {
                    return NbuildCommand.List(json, verbose).Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(listCommand);
        }
    }
}