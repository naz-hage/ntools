using System.CommandLine;
using System.Reflection;
using System.Text.Json;

namespace Sdo.Commands
{
    public sealed class BackupCommand : System.CommandLine.Command
    {
        private const string ResourceName = "Sdo.Resources.Nbackup.json";

        public BackupCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
            : base("backup", "Environment and workspace backup utilities")
        {
            AddInitCommand();
            AddRunCommand(verboseOption, dryRunOption);
        }

        private void AddInitCommand()
        {
            var initCommand = new System.CommandLine.Command("init", "Extract the sample backup configuration.");
            var outputOption = new Option<string>("--output")
            {
                Description = "Output path for the sample backup configuration",
                Required = true
            };
            initCommand.Add(outputOption);
            initCommand.SetAction(parseResult =>
            {
                var outputPath = parseResult.GetValue(outputOption)!;
                try
                {
                    var assembly = typeof(BackupCommand).Assembly;
                    using var stream = assembly.GetManifestResourceStream(ResourceName);
                    if (stream == null)
                    {
                        Console.Error.WriteLine("Error: Embedded backup configuration was not found.");
                        return 1;
                    }

                    using var file = File.Create(outputPath);
                    stream.CopyTo(file);
                    Console.WriteLine($"Extracted sample nbackup.json to {outputPath}");
                    return 0;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return 1;
                }
            });

            Subcommands.Add(initCommand);
        }

        private void AddRunCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var runCommand = new System.CommandLine.Command("run", "Validate or perform a configured backup.");
            var inputOption = new Option<string>("--input")
            {
                Description = "Path to the backup JSON configuration",
                Required = true
            };
            runCommand.Add(inputOption);
            runCommand.Add(verboseOption);
            runCommand.Add(dryRunOption);
            runCommand.SetAction(parseResult =>
            {
                var inputPath = parseResult.GetValue(inputOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (!File.Exists(inputPath))
                {
                    Console.Error.WriteLine($"Input file '{inputPath}' not found");
                    return 2;
                }

                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(inputPath));
                    if (!document.RootElement.TryGetProperty("BackupsList", out var backups) || backups.ValueKind != JsonValueKind.Array)
                    {
                        Console.Error.WriteLine("Error: Backup configuration must contain a BackupsList array.");
                        return 1;
                    }

                    if (verbose)
                    {
                        Console.WriteLine($"Validated {backups.GetArrayLength()} backup configuration(s).");
                    }

                    if (dryRun)
                    {
                        Console.WriteLine("DRY-RUN: backup execution skipped.");
                        return 0;
                    }

                    Console.Error.WriteLine("Error: Backup execution is not yet enabled in SDO.");
                    return 1;
                }
                catch (JsonException exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return 1;
                }
            });

            Subcommands.Add(runCommand);
        }
    }
}