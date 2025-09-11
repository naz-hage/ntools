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

        // Static readonly mapping for better performance - avoid recreating on every invocation
        private static readonly Dictionary<string, string[]> Mappings = new Dictionary<string, string[]>
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

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "Starting version update process...");

                var jsonFiles = Directory.GetFiles(DevSetupPath, "*.json");
                var versionMap = new Dictionary<string, (string Name, string Version)>();

                // Extract versions from all NbuildAppList entries in all JSON files
                foreach (var jsonFile in jsonFiles)
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(jsonFile);
                        var jsonDoc = JsonDocument.Parse(jsonContent);

                        if (jsonDoc.RootElement.TryGetProperty("NbuildAppList", out var appList) &&
                            appList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var app in appList.EnumerateArray())
                            {
                                if (app.TryGetProperty("Name", out var nameElement) &&
                                    app.TryGetProperty("Version", out var versionElement))
                                {
                                    var name = nameElement.GetString();
                                    var version = versionElement.GetString();
                                    // Use tool name as key to allow multiple tools from multiple files
                                    versionMap[name] = (name, version);
                                    Log.LogMessage(MessageImportance.Normal, $"Found {name}: {version}");
                                }
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

        private void UpdateMarkdownFile(string DevSetupPath, Dictionary<string, (string Name, string Version)> versionMap)
        {
            var lines = File.ReadAllLines(DevSetupPath);
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

            File.WriteAllLines(DevSetupPath, lines);
        }

        private static bool IsToolMatch(string toolName, string jsonName)
        {
            return Mappings.ContainsKey(toolName) &&
                   Mappings[toolName].Any(m => m.Equals(jsonName, StringComparison.OrdinalIgnoreCase));
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
    // SetupPreCommitHooks class removed: now isolated in feature branch

    public class GenerateCommitMessage : Task
    {
        [Required]
        public string WorkingDirectory { get; set; }

        public string CommitMessageFile { get; set; } = ".commit-message";

        public string CommitType { get; set; } = "feat";

        public string Scope { get; set; }

        [Output]
        public string CommitMessage { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "Generating commit message...");

                // Try to read from file first
                var commitMessagePath = Path.Combine(WorkingDirectory, CommitMessageFile);
                if (File.Exists(commitMessagePath))
                {
                    CommitMessage = File.ReadAllText(commitMessagePath).Trim();
                    Log.LogMessage(MessageImportance.Normal, $"Using commit message from file: {CommitMessageFile}");
                    return true;
                }

                // Generate dynamic commit message based on changed files
                CommitMessage = GenerateDynamicCommitMessage();

                // Optionally save the generated message for review
                File.WriteAllText(commitMessagePath, CommitMessage);
                Log.LogMessage(MessageImportance.Normal, $"Generated commit message saved to: {CommitMessageFile}");

                Log.LogMessage(MessageImportance.High, $"Generated commit message: {CommitMessage}");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error generating commit message: {ex.Message}");
                // Fallback to a generic message
                CommitMessage = $"{CommitType}: Update project files";
                return true; // Don't fail the build for this
            }
        }

        private string GenerateDynamicCommitMessage()
        {
            try
            {
                // Get list of changed files using git status
                var gitStatus = GetGitStatus();
                var changedFiles = ParseGitStatus(gitStatus);

                // Analyze changes and generate appropriate message
                var analysis = AnalyzeChanges(changedFiles);
                return FormatCommitMessage(analysis);
            }
            catch
            {
                // Fallback to basic message
                return $"{CommitType}: Update project components";
            }
        }

        private string GetGitStatus()
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain",
                    WorkingDirectory = WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private static List<(string Status, string File)> ParseGitStatus(string gitOutput)
        {
            var changes = new List<(string Status, string File)>();
            var lines = gitOutput.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Length > 3)
                {
                    var status = line.Substring(0, 2).Trim();
                    var file = line.Substring(3).Trim();
                    changes.Add((status, file));
                }
            }

            return changes;
        }

        private static CommitAnalysis AnalyzeChanges(List<(string Status, string File)> changes)
        {
            var analysis = new CommitAnalysis();

            foreach (var (status, file) in changes)
            {
                AnalyzeFile(file, analysis);
                AnalyzeStatus(status, analysis);
            }

            return analysis;
        }

        private static void AnalyzeFile(string file, CommitAnalysis analysis)
        {
            var fileName = Path.GetFileName(file).ToLower();
            var extension = Path.GetExtension(file).ToLower();
            var directory = Path.GetDirectoryName(file)?.ToLower() ?? "";

            // Categorize changes
            if (directory.Contains("docs")) analysis.DocsChanged = true;
            if (fileName.Contains("test")) analysis.TestsChanged = true;
            if (extension == ".ps1" || extension == ".sh") analysis.ScriptsChanged = true;
            if (extension == ".cs") analysis.CodeChanged = true;
            if (fileName.Contains("package") || fileName.Contains("version")) analysis.PackageOrVersionChanged = true;
            if (fileName.Contains("workflow") || fileName.Contains("action") || directory.Contains(".github")) analysis.CiChanged = true;
            if (fileName.Contains("hook") || fileName.Contains("pre-commit")) analysis.HooksChanged = true;
            if (extension == ".yml" || extension == ".yaml" || extension == ".json") analysis.ConfigChanged = true;
        }

        private static void AnalyzeStatus(string status, CommitAnalysis analysis)
        {
            if (status.Contains("A")) analysis.NewFiles++;
            if (status.Contains("M")) analysis.ModifiedFiles++;
            if (status.Contains("D")) analysis.DeletedFiles++;
        }

        private string FormatCommitMessage(CommitAnalysis analysis)
        {
            var parts = new List<string>();
            var scope = !string.IsNullOrEmpty(Scope) ? Scope : DetermineScope(analysis);

            var type = CommitType;
            if (analysis.DocsChanged && !analysis.CodeChanged && !analysis.ScriptsChanged)
                type = "docs";
            else if (analysis.TestsChanged && !analysis.CodeChanged)
                type = "test";
            else if (analysis.CiChanged)
                type = "ci";
            else if (analysis.ConfigChanged && !analysis.CodeChanged)
                type = "config";

            // Build main message
            var scopePart = !string.IsNullOrEmpty(scope) ? $"({scope})" : "";
            var mainMessage = $"{type}{scopePart}: {GenerateDescription(analysis)}";

            parts.Add(mainMessage);

            // Add details if multiple categories
            var details = GenerateDetails(analysis);
            if (!string.IsNullOrEmpty(details))
            {
                parts.Add("");
                parts.Add(details);
            }

            return string.Join("\n", parts);
        }

        private static string DetermineScope(CommitAnalysis analysis)
        {
            if (analysis.PackageOrVersionChanged) return "packages";
            if (analysis.HooksChanged) return "hooks";
            if (analysis.CiChanged) return "ci";
            if (analysis.DocsChanged) return "docs";
            if (analysis.TestsChanged) return "tests";
            if (analysis.ScriptsChanged) return "scripts";
            return "";
        }

        private static string GenerateDescription(CommitAnalysis analysis)
        {
            if (analysis.PackageOrVersionChanged)
                return "Update package management and version automation";
            if (analysis.HooksChanged)
                return "Update pre-commit hooks and quality gates";
            if (analysis.CiChanged)
                return "Update CI/CD workflows and automation";
            if (analysis.DocsChanged && analysis.ScriptsChanged)
                return "Update documentation and automation scripts";
            if (analysis.DocsChanged)
                return "Update documentation";
            if (analysis.ScriptsChanged)
                return "Update automation scripts";
            if (analysis.CodeChanged)
                return "Update core functionality";
            if (analysis.ConfigChanged)
                return "Update configuration files";

            return "Update project components";
        }

        private static string GenerateDetails(CommitAnalysis analysis)
        {
            var details = new List<string>();

            if (analysis.NewFiles > 0)
                details.Add($"- Add {analysis.NewFiles} new file(s)");
            if (analysis.ModifiedFiles > 0)
                details.Add($"- Modify {analysis.ModifiedFiles} existing file(s)");
            if (analysis.DeletedFiles > 0)
                details.Add($"- Remove {analysis.DeletedFiles} file(s)");

            if (analysis.DocsChanged) details.Add("- Update documentation");
            if (analysis.ScriptsChanged) details.Add("- Update automation scripts");
            if (analysis.HooksChanged) details.Add("- Update pre-commit hooks");
            if (analysis.CiChanged) details.Add("- Update CI/CD workflows");
            if (analysis.TestsChanged) details.Add("- Update tests");

            return details.Count > 3 ? string.Join("\n", details) : "";
        }

        private sealed class CommitAnalysis
        {
            public bool DocsChanged { get; set; }
            public bool TestsChanged { get; set; }
            public bool ScriptsChanged { get; set; }
            public bool CodeChanged { get; set; }
            public bool PackageOrVersionChanged { get; set; }
            public bool CiChanged { get; set; }
            public bool HooksChanged { get; set; }
            public bool ConfigChanged { get; set; }
            public int NewFiles { get; set; }
            public int ModifiedFiles { get; set; }
            public int DeletedFiles { get; set; }
        }
    }
}
