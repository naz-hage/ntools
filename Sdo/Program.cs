// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// Sdo Program.cs
//
// This file contains the main entry point and command setup for the Sdo CLI tool.
// Sdo is a Simple DevOps Operations tool that provides unified operations
// for Azure DevOps and GitHub work item and repository management.

using Nbuild.Helpers;
using NbuildTasks;
using System.CommandLine;

namespace Sdo
{
    /// <summary>
    /// Main program class for the Sdo CLI application.
    /// </summary>
    /// <remarks>
    /// This class sets up the System.CommandLine root command with global options
    /// and various subcommands for DevOps operations across Azure DevOps and GitHub.
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// Main entry point for the Sdo application.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        public static int Main(params string[] args)
        {
            ConsoleHelper.WriteLine($"{Nversion.Get()}\n", ConsoleColor.Yellow);

            // Create the root command
            var rootCommand = new RootCommand("Simple DevOps Operations CLI tool for Azure DevOps and GitHub");

            // Add global options
            var verboseOption = new Option<bool>("--verbose", ["-v"])
            {
                Description = "Enable verbose output"
            };
            var dryRunOption = new Option<bool>("--dry-run", ["-n"])
            {
                Description = "Perform a dry run without side effects"
            };
            rootCommand.Options.Add(verboseOption);
            rootCommand.Options.Add(dryRunOption);

            // Add commands
            rootCommand.Subcommands.Add(new Commands.MapCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.AuthCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.PipelineCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.PullRequestCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.RepositoryCommand(verboseOption, dryRunOption: dryRunOption));
            rootCommand.Subcommands.Add(new Commands.WorkItemCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.UserCommand(verboseOption));

            // Migration command groups. Implementations are added incrementally while
            // the legacy nb, nbackup, and lf commands remain available.
            rootCommand.Subcommands.Add(new Commands.ToolCommand(verboseOption, dryRunOption));
            rootCommand.Subcommands.Add(new Commands.EnvironmentCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.BuildCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.ReleaseCommand(verboseOption, dryRunOption));
            rootCommand.Subcommands.Add(new Commands.BackupCommand(verboseOption, dryRunOption));
            rootCommand.Subcommands.Add(new Commands.FileCommand(verboseOption));

            // Preserve nb compatibility: one unmatched token is an MSBuild target.
            rootCommand.TreatUnmatchedTokensAsErrors = false;
            rootCommand.SetAction((parseResult) =>
            {
                var unmatched = parseResult.UnmatchedTokens;
                var potentialOptions = unmatched.Where(token => token.StartsWith("-", StringComparison.Ordinal)).ToList();
                if (potentialOptions.Count > 0)
                {
                    foreach (var option in potentialOptions)
                    {
                        Console.Error.WriteLine($"Unknown option '{option}'. Run 'sdo --help' to see available options.");
                    }

                    return 1;
                }

                if (unmatched.Count == 1)
                {
                    var target = unmatched[0];
                    var verbose = parseResult.GetValue(verboseOption);
                    ConsoleHelper.WriteLine($"Executing target: {target}", ConsoleColor.Green);
                    var result = Nbuild.BuildStarter.Build(target, verbose);
                    if (result.IsFail())
                    {
                        ConsoleHelper.WriteLine($"Failed to execute target '{target}': {result.GetFirstOutput()}", ConsoleColor.Red);
                    }

                    return result.Code;
                }

                if (unmatched.Count > 1)
                {
                    Console.Error.WriteLine($"Unknown command or too many arguments: {string.Join(' ', unmatched)}");
                    return 1;
                }

                Console.WriteLine("Error: Please specify a command (map, auth, pipeline, pr, repo, wi, user, tool, env, build, release, backup, file)");
                Console.WriteLine("Run 'sdo --help' for usage information.");
                return 1;
            });

            // Parse and execute
            return rootCommand.Parse(args).Invoke();
        }
    }
}