using launcher;
using Launcher;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace nbackup
{
    public class NBackup
    {
        public static Result Perform(Cli options)
        {
            Result result = new();
            if (File.Exists(options.Input))
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
                                    result = Perform(backup.Source, backup.Destination, backup.BackupOptions);
                                }
                                else
                                {
                                    Console.WriteLine($"mockup...\n");
                                }
                            }

                            Console.WriteLine($"    --> Source      :   {backup.Source}");
                            Console.WriteLine($"    --> Destination :   {backup.Destination}");
                            Console.WriteLine($"    --> roboCopyOptions: {backup.BackupOptions}");
                            Console.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Code = Result.Exception;
                    Console.WriteLine($"An exception occurred: {ex.Message}");
                }
            }
            else
            {
                result.Code = Result.FileNotFound;
                Console.WriteLine($"Input file 'backup.json' not found");
            }

            return result;
        }

        private static string ReplaceEnvironmentVariables(string source)
        {
            string destination = source.Replace(
                               "%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));

            destination = destination.Replace(
                                "%USERNAME%", Environment.GetEnvironmentVariable("USERNAME"));

            return destination;
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
        public static Result Perform(string source, string destination, string backupOptions)
        {
            Parameters parameters = new()
            {
                WorkingDir = @"c:\windows\system32",
                FileName = "robocopy.exe",
                Arguments = $"\"{source}\" \"{destination}\" {backupOptions}",
                RedirectStandardOutput = false,
                Verbose = true
            };

            Result result = Launcher.Launcher.Start(parameters);
            foreach (var item in result.Output)
            {
                if (item.Trim().StartsWith("Dirs :") ||
                    item.Trim().StartsWith("Files :") ||
                    item.Trim().StartsWith("Bytes :") ||
                    item.Trim().StartsWith("Times :") ||
                    item.Trim().StartsWith("Ended :") ||
                    item.ToLower().Trim().Contains("error") ||
                    item.Contains("Total    Copied"))
                {
                    Console.WriteLine(item);
                }
            }

            return result;
        }
        
        private static bool IsNotNull([NotNullWhen(true)] object? obj) => obj != null;
    }
}
