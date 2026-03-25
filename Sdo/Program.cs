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
            var verboseOption = new Option<bool>("--verbose");
            verboseOption.Description = "Enable verbose output";
            rootCommand.Options.Add(verboseOption);

            // Add commands
            rootCommand.Subcommands.Add(new Commands.MapCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.AuthCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.PullRequestCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.RepositoryCommand(verboseOption));
            rootCommand.Subcommands.Add(new Commands.WorkItemCommand(verboseOption));

            // TODO: Add subcommands for pull requests, pipelines, and users
            // Phase 3.4: Pull Request Commands
            // Phase 3.5: Pipeline Commands
            // Phase 3.6: User Commands

            // For now, just show a placeholder message
            rootCommand.SetAction((parseResult) =>
            {
                Console.WriteLine("SDO C# migration starting point.");
                Console.WriteLine("Commands will be implemented based on the Python sdo functionality.");
                return 0;
            });

            // Parse and execute
            return rootCommand.Parse(args).Invoke();
        }
    }
}