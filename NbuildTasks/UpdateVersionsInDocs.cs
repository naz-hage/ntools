using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NbuildTasks
{
    public class UpdateVersionsInDocs : Task
    {
        [Required]
        public string DevSetupPath { get; set; }

        [Required]
        public string DocsPath { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "Starting version update process...");

                var jsonFiles = Directory.GetFiles(DevSetupPath, "*.json");
                var versionMap = new Dictionary<string, (string Name, string Version)>();

                // Extract versions from JSON files
                foreach (var jsonFile in jsonFiles)
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(jsonFile);
                        var jsonDoc = JsonDocument.Parse(jsonContent);

                        if (jsonDoc.RootElement.TryGetProperty("NbuildAppList", out var appList) &&
                            appList.GetArrayLength() > 0)
                        {
                            var firstApp = appList[0];
                            if (firstApp.TryGetProperty("Name", out var nameElement) &&
                                firstApp.TryGetProperty("Version", out var versionElement))
                            {
                                var name = nameElement.GetString();
                                var version = versionElement.GetString();
                                versionMap[Path.GetFileName(jsonFile)] = (name, version);
                                Log.LogMessage(MessageImportance.Normal, $"Found {name}: {version}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Failed to parse {jsonFile}: {ex.Message}");
                    }
                }

                // Update markdown file
                UpdateMarkdownFile(DocsPath, versionMap);

                Log.LogMessage(MessageImportance.High, "Version update completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error updating versions: {ex.Message}");
                return false;
            }
        }

        private void UpdateMarkdownFile(string markdownPath, Dictionary<string, (string Name, string Version)> versionMap)
        {
            var lines = File.ReadAllLines(markdownPath);
            var today = DateTime.Now.ToString("dd-MMM-yy");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var match = Regex.Match(line, @"^\| \[([^\]]+)\]");

                if (match.Success)
                {
                    var toolName = match.Groups[1].Value;

                    // Find matching version
                    foreach (var kvp in versionMap)
                    {
                        var (name, version) = kvp.Value;

                        if (IsToolMatch(toolName, name))
                        {
                            // Update the line
                            var tableMatch = Regex.Match(line, @"(\| \[[^\]]+\]\([^)]+\)\s+\| )([^|]+)(\| )([^|]+)(\|.*)");
                            if (tableMatch.Success)
                            {
                                lines[i] = tableMatch.Groups[1].Value +
                                          version.PadRight(11) +
                                          tableMatch.Groups[3].Value +
                                          today.PadRight(15) +
                                          tableMatch.Groups[5].Value;

                                Log.LogMessage(MessageImportance.Normal, $"Updated {toolName}: {version}");
                                break;
                            }
                        }
                    }
                }
            }

            File.WriteAllLines(markdownPath, lines);
        }

        private static bool IsToolMatch(string toolName, string jsonName)
        {
            // Tool name mapping logic
            var mappings = new Dictionary<string, string[]>
            {
                { "Node.js", new[] { "Node.js" } },
                { "PowerShell", new[] { "Powershell" } },
                { "Python", new[] { "Python" } },
                { "Git for Windows", new[] { "Git for Windows" } },
                { "Visual Studio Code", new[] { "Visual Studio Code" } },
                { "NuGet", new[] { "Nuget" } },
                { "Terraform", new[] { "Terraform" } },
                { "Terraform Lint", new[] { "terraform lint" } },
                { "kubernetes", new[] { "kubectl" } },
                { "minikube", new[] { "minikube" } },
                { "Azure CLI", new[] { "AzureCLI" } },
                { "MongoDB Community Server", new[] { "MongoDB" } },
                { "pnpm", new[] { "pnpm" } },
                { "Ntools", new[] { "Ntools" } }
            };

            return mappings.ContainsKey(toolName) &&
                   mappings[toolName].Any(m => m.Equals(jsonName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class SetupPreCommitHooks : Task
    {
        [Required]
        public string GitDirectory { get; set; }

        [Required]
        public string HooksSourceDirectory { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "Setting up pre-commit hooks...");

                var hooksDir = Path.Combine(GitDirectory, "hooks");
                if (!Directory.Exists(hooksDir))
                {
                    Directory.CreateDirectory(hooksDir);
                }

                // Copy hook files
                var hookFiles = Directory.GetFiles(HooksSourceDirectory);
                foreach (var hookFile in hookFiles)
                {
                    var fileName = Path.GetFileName(hookFile);
                    var destPath = Path.Combine(hooksDir, fileName);

                    File.Copy(hookFile, destPath, true);

                    // Make executable on Unix-like systems
                    if (Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        var chmod = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "chmod",
                                Arguments = $"+x {destPath}",
                                UseShellExecute = false
                            }
                        };
                        chmod.Start();
                        chmod.WaitForExit();
                    }

                    Log.LogMessage(MessageImportance.Normal, $"Installed hook: {fileName}");
                }

                Log.LogMessage(MessageImportance.High, "Pre-commit hooks setup completed!");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error setting up pre-commit hooks: {ex.Message}");
                return false;
            }
        }
    }
}
