using NbuildTasks;
using Ntools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Nbackup
{
    public class NBackup
    {
        private static readonly Dictionary<string, string?> _environmentVariables = new(){
                { "USERPROFILE", Environment.GetEnvironmentVariable("USERPROFILE") },
                { "USERNAME", Environment.GetEnvironmentVariable("USERNAME") },
                { "APPDATA", Environment.GetEnvironmentVariable("APPDATA") },
            };

        public static ResultHelper Perform(Cli options)
        {
            ResultHelper result = new();
            if ((!string.IsNullOrEmpty(options.Input)) && (File.Exists(options.Input)))
            {
                try
                {
                    string json = File.ReadAllText(options.Input);
                    var backups = JsonSerializer.Deserialize<Backup>(json);
                    if (backups != null && backups.BackupsList != null)
                    {
                        foreach (Backup? backup in backups.BackupsList)
                        {
                            if (IsNotNull(backup.Source) &&
                                IsNotNull(backup.Destination) &&
                                IsNotNull(backup.BackupOptions))
                            {
                                backup.Source = ReplaceEnvironmentVariables(backup.Source);

                                backup.Destination = ReplaceEnvironmentVariables(backup.Destination);

                                backup.BackupOptions = ReplaceEnvironmentVariables(backup.BackupOptions);

                                string arguments = $" Source: {backup.Source}\n" +
                                                    $" Destination: {backup.Destination}\n" +
                                                    $" BackupOptions: {backup.BackupOptions}\n";

                                if (backup.ExcludeFolders != null)
                                {
                                    arguments += $" ExcludeFolders:";
                                    foreach (var item in backup.ExcludeFolders)
                                    {
                                        // if item contains spaces, then enclose it in double quotes
                                        if (item.Contains(" "))
                                        {
                                            arguments += $" \"{item}\",";
                                            backup.BackupOptions += $" /XD \"{item}\"";
                                        }
                                        else
                                        {
                                            arguments += $" {item},";
                                            backup.BackupOptions += $" /XD {item}";
                                        }
                                    }
                                    arguments = arguments.TrimEnd(',') + "\n";
                                }

                                if (backup.ExcludeFiles != null)
                                {
                                    arguments += $" ExcludeFiles:";
                                    foreach (var item in backup.ExcludeFiles)
                                    {
                                        // if item contains spaces, then enclose it in double quotes
                                        if (item.Contains(" "))
                                        {
                                            arguments += $" \"{item}\",";
                                            backup.BackupOptions += $" /XF \"{item}\"";
                                        }
                                        else
                                        {
                                            arguments += $" {item},";
                                            backup.BackupOptions += $" /XF {item}";
                                        }
                                    }
                                    arguments = arguments.TrimEnd(',') + "\n";
                                }

                                if (!string.IsNullOrEmpty(backup.LogFile))
                                {
                                    backup.LogFile = ReplaceEnvironmentVariables(backup.LogFile);
                                    arguments += $" LogFile:{backup.LogFile}\n";
                                    backup.BackupOptions += $" /log+:{backup.LogFile}";
                                }

                                Console.WriteLine(arguments);

                                if (options.PerformBackup)
                                {
                                    result = Perform(backup.Source, backup.Destination, backup.BackupOptions, options.Verbose);

                                    // Read log file and display last 12 lines
                                    DisplayOutput(result, backup);
                                    Console.WriteLine($"Exit Code: {result.Code}");
                                }
                                else
                                {
                                    Console.WriteLine($"mockup...\n");
                                }
                            }


                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Code = ResultHelper.Exception;
                    Console.WriteLine($"An exception occurred: {ex.Message}");
                }
            }
            else
            {
                result.Code = ResultHelper.FileNotFound;
                Console.WriteLine($"Input file '{options.Input}' not found");
            }

            return result;
        }

        /// <summary>
        /// full argument list:
        /// https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/robocopy
        /// some robocopy switches which can enhance performance
        /// /S — Copy subdirectories, but not empty ones.
        /// /E — Copy Subdirectories, including empty ones.
        /// /Z — Copy files in restartable mode.
        /// /ZB — Uses restartable mode, if access denied use backup mode.
        /// /R:5 — Retry 5 times (you can specify a different number, default is 1 million).
        /// /W:5 — Wait 5 seconds before retrying(you can specify a different number, default is 30 seconds).
        /// /TBD — Wait for sharenames To Be Defined(retry error 67).
        /// /NP — No Progress – don’t display percentage copied.
        /// /V — Produce verbose output, showing skipped files.
        /// /MT:32 — Do multi-threaded copies with n threads (default is 8).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="backupOptions"></param>
        /// <returns></returns>
        public static ResultHelper Perform(string source, string destination, string backupOptions, bool verbose = false)
        {
            var process = new Process
            {
                StartInfo = {
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System),
                    FileName = "robocopy.exe",
                    Arguments = $"\"{source}\" \"{destination}\" {backupOptions}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true }
            };

            // Update the filename to the full path of executable in the PATH environment variable
            process.StartInfo.FileName = FileMappins.GetFullPathOfFile(process.StartInfo.FileName);

            var result = process.LockStart(verbose);

            return result;
        }

        private static string ReplaceEnvironmentVariables(string source)
        {
            StringBuilder destination = new(source);

            foreach (var kvp in _environmentVariables)
            {
                if (kvp.Value != null)
                {
                    destination.Replace($"%{kvp.Key}%", kvp.Value);
                }
            }
            return destination.ToString();
        }

        private static void DisplayOutput(ResultHelper result, Backup backup)
        {
            if (!string.IsNullOrEmpty(backup.LogFile))
            {
                if (File.Exists(backup.LogFile))
                {
                    Console.WriteLine("----------------------------------------------------------------------------");

                    string[] lines = File.ReadAllLines(backup.LogFile);
                    int start = lines.Length - 12;
                    if (start < 0)
                    {
                        start = 0;
                    }
                    for (int i = start; i < lines.Length; i++)
                    {
                        Console.WriteLine(lines[i]);
                    }
                }
                else
                {
                    Console.WriteLine($"Log file not found: {backup.LogFile}");
                }
            }
            else
            {
                // display the last 0 lines of result.Output
                int start = result.Output.Count - 9;
                if (start < 0)
                {
                    start = 0;
                }
                for (int i = start; i < result.Output.Count; i++)
                {
                    Console.WriteLine(result.Output[i]);
                }
            }
        }

        private static bool IsNotNull([NotNullWhen(true)] object? obj)
        {
            return obj != null;
        }
    }
}
