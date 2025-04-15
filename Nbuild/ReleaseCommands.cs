using System.CommandLine;

namespace Nbuild;

public static class ReleaseCommands
{
    public static Command GetReleaseCommand()
    {
        var releaseCommand = new Command("release", "GitHub release-related commands.");

        // Add subcommands
        releaseCommand.AddCommand(new Command("create", "Create a GitHub release.")
        {
            new Option<string>("--repo", "The repository name (e.g., userName/repo)."),
            new Option<string>("--tag", "The tag for the release."),
            new Option<string>("--branch", "The branch for the release."),
            new Option<string>("--file", "The file to include in the release.")
        });
        releaseCommand.AddCommand(new Command("download", "Download an asset from a GitHub release.")
        {
            new Option<string>("--repo", "The repository name (e.g., userName/repo)."),
            new Option<string>("--tag", "The tag of the release."),
            new Option<string>("--path", "The path to save the downloaded asset.")
        });
        releaseCommand.AddCommand(new Command("list", "List all releases for a repository.")
        {
            new Option<string>("--repo", "The repository name (e.g., userName/repo).")
        });

        return releaseCommand;
    }
}