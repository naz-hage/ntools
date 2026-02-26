// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// MapCommand.cs
//
// This file contains the MapCommand class for displaying command mappings
// between SDO and native CLI tools (GitHub CLI and Azure CLI).

using System.CommandLine;

namespace Sdo.Commands
{
    /// <summary>
    /// Command for displaying mappings between SDO commands and native CLI tools.
    /// </summary>
    public class MapCommand : Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapCommand"/> class.
        /// </summary>
        /// <param name="verboseOption">The global verbose option.</param>
        public MapCommand(Option<bool> verboseOption) : base("map", "Show command mappings between SDO and native CLI tools")
        {
            // Add platform option (optional)
            var platformOption = new Option<string>("--platform");
            platformOption.Description = "Platform to show mappings for (gh=github, azdo=azure-devops, leave empty for auto-detect)";
            Add(platformOption);

            // Add --all option
            var allOption = new Option<bool>("--all");
            allOption.Description = "Show all mappings for both platforms";
            Add(allOption);

            this.SetAction((parseResult) =>
            {
                var platform = parseResult.GetValue(platformOption) ?? "";
                var showAll = parseResult.GetValue(allOption);
                var verbose = parseResult.GetValue(verboseOption);

                return HandleMapCommand(platform, showAll, verbose);
            });
        }

        /// <summary>
        /// Handles the map command execution.
        /// </summary>
        /// <param name="platform">The platform to show mappings for.</param>
        /// <param name="showAll">Whether to show all mappings.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <returns>Exit code.</returns>
        private static int HandleMapCommand(string? platform, bool showAll, bool verbose)
        {
            try
            {
                // Read the embedded mapping.md resource
                var assembly = typeof(MapCommand).Assembly;
                var resourceName = "Sdo.mapping.md";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.Error.WriteLine("Error: Could not find embedded mapping resource.");
                    return 1;
                }

                using var reader = new StreamReader(stream);
                var mappingContent = reader.ReadToEnd();

                // Parse and display mappings based on arguments
                DisplayMappings(mappingContent, platform, showAll);

                return 0;
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                else
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
                return 1;
            }
        }

        /// <summary>
        /// Displays the command mappings based on the specified criteria.
        /// </summary>
        /// <param name="mappingContent">The full mapping content.</param>
        /// <param name="platform">The platform filter (gh, azdo, or null for auto-detect).</param>
        /// <param name="showAll">Whether to show all mappings.</param>
        private static void DisplayMappings(string mappingContent, string? platform, bool showAll)
        {
            Console.WriteLine("SDO Command Mappings");
            Console.WriteLine("===================");
            Console.WriteLine();

            // Determine which platform to show
            string targetPlatform = platform?.ToLower() ?? "auto";

            if (showAll)
            {
                Console.WriteLine("Showing all command mappings for both GitHub and Azure DevOps:");
                Console.WriteLine();
                Console.WriteLine(mappingContent);
            }
            else if (targetPlatform == "auto")
            {
                // Auto-detect platform (simplified for now - show GitHub focused)
                Console.WriteLine("Auto-detected platform: GitHub");
                Console.WriteLine("Showing GitHub CLI mappings:");
                Console.WriteLine();
                DisplayPlatformMappings(mappingContent, "gh");
            }
            else if (targetPlatform == "gh" || targetPlatform == "github")
            {
                Console.WriteLine("Showing GitHub CLI mappings:");
                Console.WriteLine();
                DisplayPlatformMappings(mappingContent, "gh");
            }
            else if (targetPlatform == "azdo" || targetPlatform == "azure-devops" || targetPlatform == "az")
            {
                Console.WriteLine("Showing Azure DevOps CLI mappings:");
                Console.WriteLine();
                DisplayPlatformMappings(mappingContent, "azdo");
            }
            else
            {
                Console.Error.WriteLine($"Unknown platform: {platform}");
                Console.WriteLine("Supported platforms: gh (GitHub), azdo (Azure DevOps)");
                return;
            }
        }

        /// <summary>
        /// Displays mappings for a specific platform.
        /// </summary>
        /// <param name="mappingContent">The full mapping content.</param>
        /// <param name="platform">The platform to display (gh or azdo).</param>
        private static void DisplayPlatformMappings(string mappingContent, string platform)
        {
            var lines = mappingContent.Split('\n');
            bool inTable = false;
            string currentSection = "";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip the header and intro sections
                if (trimmedLine.StartsWith("# SDO Command Mappings") ||
                    trimmedLine.StartsWith("This document provides") ||
                    trimmedLine.StartsWith("**Quick Access**") ||
                    trimmedLine.Contains("Use `sdo map`") ||
                    trimmedLine.StartsWith("## Notes") ||
                    trimmedLine.Contains("Is it worth the effort"))
                {
                    continue;
                }

                // Section headers
                if (trimmedLine.StartsWith("## "))
                {
                    currentSection = trimmedLine.Substring(3);
                    Console.WriteLine($"## {currentSection}");
                    Console.WriteLine();
                    continue;
                }

                // Table headers
                if (trimmedLine.StartsWith("| SDO Command") && trimmedLine.Contains("| GitHub CLI") && trimmedLine.Contains("| Azure CLI"))
                {
                    inTable = true;
                    if (platform == "gh")
                    {
                        Console.WriteLine("| SDO Command | GitHub CLI |");
                        Console.WriteLine("|-------------|------------|");
                    }
                    else if (platform == "azdo")
                    {
                        Console.WriteLine("| SDO Command | Azure CLI |");
                        Console.WriteLine("|-------------|-----------|");
                    }
                    continue;
                }

                // Table content
                if (inTable && trimmedLine.StartsWith("| `sdo"))
                {
                    var parts = trimmedLine.Split('|');
                    if (parts.Length >= 4)
                    {
                        var sdoCommand = parts[1].Trim();
                        var ghCommand = parts[2].Trim();
                        var azCommand = parts[3].Trim();

                        if (platform == "gh" && !string.IsNullOrEmpty(ghCommand))
                        {
                            Console.WriteLine($"| {sdoCommand} | {ghCommand} |");
                        }
                        else if (platform == "azdo" && !string.IsNullOrEmpty(azCommand))
                        {
                            Console.WriteLine($"| {sdoCommand} | {azCommand} |");
                        }
                    }
                }

                // End of table
                if (inTable && string.IsNullOrWhiteSpace(trimmedLine))
                {
                    inTable = false;
                    Console.WriteLine();
                }
            }
        }
    }
}