using System.CommandLine;

namespace Nbuild;

public class Program
{
    public static int Main(string[] args)
    {
        // Create the root command
        var rootCommand = new RootCommand("Unified nb tool for build, git, and release management.");

        // Add build-related commands
        rootCommand.AddCommand(BuildCommands.GetBuildCommand());

        // Add git-related commands
        rootCommand.AddCommand(GitCommands.GetGitCommand());

        // Add release-related commands
        rootCommand.AddCommand(ReleaseCommands.GetReleaseCommand());

        // Parse and invoke the commands
        return rootCommand.InvokeAsync(args).Result;
    }
}