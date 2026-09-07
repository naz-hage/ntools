using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Sdo.Services;

internal sealed class BackupRunner
{
    private static readonly string[] EnvironmentVariableNames =
    {
        "USERPROFILE",
        "USERNAME",
        "APPDATA"
    };

    public int Run(string inputPath, bool verbose, bool dryRun)
    {
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file '{inputPath}' not found");
            return 2;
        }

        try
        {
            var configuration = JsonSerializer.Deserialize<BackupConfiguration>(File.ReadAllText(inputPath));
            if (configuration?.BackupsList == null)
            {
                Console.Error.WriteLine("Error: Backup configuration must contain a BackupsList array.");
                return 1;
            }

            foreach (var backup in configuration.BackupsList)
            {
                if (backup.Source == null || backup.Destination == null || backup.BackupOptions == null)
                {
                    continue;
                }

                var source = ReplaceEnvironmentVariables(backup.Source);
                var destination = ReplaceEnvironmentVariables(backup.Destination);
                var options = ReplaceEnvironmentVariables(backup.BackupOptions);
                var logFile = string.IsNullOrEmpty(backup.LogFile)
                    ? null
                    : ReplaceEnvironmentVariables(backup.LogFile);

                var description = new StringBuilder()
                    .AppendLine($" Source: {source}")
                    .AppendLine($" Destination: {destination}")
                    .AppendLine($" BackupOptions: {options}");

                AppendExclusions(description, "ExcludeFolders", backup.ExcludeFolders, "/XD", ref options);
                AppendExclusions(description, "ExcludeFiles", backup.ExcludeFiles, "/XF", ref options);

                if (logFile != null)
                {
                    description.AppendLine($" LogFile:{logFile}");
                    options += $" /log+:{logFile}";
                }

                Console.WriteLine(description.ToString());
                if (dryRun)
                {
                    Console.WriteLine("mockup...\n");
                    continue;
                }

                var result = RunRobocopy(source, destination, options, verbose);
                DisplayOutput(result, logFile);
                Console.WriteLine($"Exit Code: {result.ExitCode}");
                if (result.ExitCode != 0)
                {
                    return result.ExitCode;
                }
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"An exception occurred: {exception.Message}");
            return 1;
        }
    }

    private static void AppendExclusions(
        StringBuilder description,
        string label,
        List<string>? exclusions,
        string option,
        ref string backupOptions)
    {
        if (exclusions == null)
        {
            return;
        }

        description.Append($" {label}:");
        foreach (var exclusion in exclusions)
        {
            var value = ReplaceEnvironmentVariables(exclusion);
            var quotedValue = value.Contains(' ') ? $"\"{value}\"" : value;
            description.Append($" {quotedValue},");
            backupOptions += $" {option} {quotedValue}";
        }

        description.Length--;
        description.AppendLine();
    }

    private static ProcessResult RunRobocopy(string source, string destination, string options, bool verbose)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "robocopy.exe",
                Arguments = $"\"{source}\" \"{destination}\" {options}",
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (verbose && !string.IsNullOrEmpty(error))
        {
            Console.Error.WriteLine(error);
        }

        return new ProcessResult(process.ExitCode, output);
    }

    private static void DisplayOutput(ProcessResult result, string? logFile)
    {
        if (logFile != null && File.Exists(logFile))
        {
            Console.WriteLine("----------------------------------------------------------------------------");
            foreach (var line in File.ReadLines(logFile).TakeLast(12))
            {
                Console.WriteLine(line);
            }
            return;
        }

        if (logFile != null)
        {
            Console.WriteLine($"Log file not found: {logFile}");
            return;
        }

        foreach (var line in result.Output.Split(Environment.NewLine).TakeLast(9))
        {
            if (!string.IsNullOrEmpty(line))
            {
                Console.WriteLine(line);
            }
        }
    }

    private static string ReplaceEnvironmentVariables(string value)
    {
        foreach (var variableName in EnvironmentVariableNames)
        {
            var variableValue = Environment.GetEnvironmentVariable(variableName);
            if (variableValue != null)
            {
                value = value.Replace($"%{variableName}%", variableValue, StringComparison.OrdinalIgnoreCase);
            }
        }

        return value;
    }

    private sealed record ProcessResult(int ExitCode, string Output);

    private sealed class BackupConfiguration
    {
        public List<BackupItem>? BackupsList { get; set; }
    }

    private sealed class BackupItem
    {
        public string? Source { get; set; }
        public string? Destination { get; set; }
        public string? BackupOptions { get; set; }
        public List<string>? ExcludeFolders { get; set; }
        public List<string>? ExcludeFiles { get; set; }
        public string? LogFile { get; set; }
    }
}