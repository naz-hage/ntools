using System.CommandLine;

namespace Nbuild;

public static class GitCommands
{
    public static Command GetGitCommand()
    {
        var gitCommand = new Command("git", "Git-related commands.");

        // Add subcommands
        gitCommand.AddCommand(new Command("tag", "Get the current Git tag."));
        gitCommand.AddCommand(new Command("settag", "Set a specific Git tag.")
        {
            new Option<string>("--tag", "The tag to set.")
        });
        gitCommand.AddCommand(new Command("autotag", "Automatically set the next tag based on the build type.")
        {
            new Option<string>("--buildtype", "The build type (STAGE or PROD).")
        });
        gitCommand.AddCommand(new Command("branch", "Get the current Git branch."));
        gitCommand.AddCommand(new Command("clone", "Clone a Git repository.")
        {
            new Option<string>("--url", "The URL of the repository to clone.")
        });
        gitCommand.AddCommand(new Command("deletetag", "Delete a specific Git tag.")
        {
            new Option<string>("--tag", "The tag to delete.")
        });

        return gitCommand;
    }
}