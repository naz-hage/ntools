using System.CommandLine;
using Nbuild.Helpers;
using NbuildCommand = Nbuild.Command;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for release operations.
    /// </summary>
    public class ReleaseCommand : System.CommandLine.Command
    {
        /// <summary>
        /// Initializes a new instance of the ReleaseCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        /// <param name="dryRunOption">Option for read-only dry-run output.</param>
        public ReleaseCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
            : base("release", "GitHub and Azure DevOps release management")
        {
            AddListCommand(verboseOption, dryRunOption);
        }

        private void AddListCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var listCommand = new System.CommandLine.Command(
                "list",
                "Lists the latest releases for the specified repository.");
            var repositoryOption = new Option<string>("--repo")
            {
                Description = "GitHub repository in owner/name or URL format",
                Required = true
            };

            listCommand.Options.Add(repositoryOption);
            listCommand.Add(verboseOption);
            listCommand.Add(dryRunOption);
            listCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                var repository = parseResult.GetValue(repositoryOption)!;
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Listing releases for repo: {repository}", ConsoleColor.Gray);
                }

                try
                {
                    var result = await NbuildCommand.ListReleases(repository, verbose, dryRun);
                    return result.Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(listCommand);
        }
    }
}