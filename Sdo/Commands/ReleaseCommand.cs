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
            AddCreateCommand(verboseOption, dryRunOption);
            AddDownloadCommand(verboseOption, dryRunOption);
            AddListCommand(verboseOption, dryRunOption);
        }

        private void AddCreateCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var createCommand = new System.CommandLine.Command("create", "Creates a GitHub release.");
            var repositoryOption = new Option<string>("--repo", ["-r"])
            {
                Description = "GitHub repository in owner/name or URL format",
                Required = true
            };
            var tagOption = new Option<string>("--tag", ["-t"]) { Description = "Release tag", Required = true };
            var branchOption = new Option<string>("--branch", ["-b"]) { Description = "Release target branch", Required = true };
            var fileOption = new Option<string>("--file", ["-f"]) { Description = "Release asset file path", Required = true };
            var prereleaseOption = new Option<bool>("--prerelease", ["-p"]) { Description = "Create a pre-release" };

            createCommand.Add(repositoryOption);
            createCommand.Add(tagOption);
            createCommand.Add(branchOption);
            createCommand.Add(fileOption);
            createCommand.Add(prereleaseOption);
            createCommand.Add(verboseOption);
            createCommand.Add(dryRunOption);
            createCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                var repository = parseResult.GetValue(repositoryOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var branch = parseResult.GetValue(branchOption)!;
                var file = parseResult.GetValue(fileOption)!;
                var prerelease = parseResult.GetValue(prereleaseOption);
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Creating {(prerelease ? "pre-release" : "release")} for repo: {repository}, tag: {tag}, branch: {branch}, file: {file}", ConsoleColor.Gray);
                }

                try
                {
                    var result = await NbuildCommand.CreateRelease(repository, tag, branch, file, prerelease, dryRun, verbose);
                    return result.Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(createCommand);
        }

        private void AddDownloadCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var downloadCommand = new System.CommandLine.Command("download", "Downloads a release asset.");
            var repositoryOption = new Option<string>("--repo", ["-r"])
            {
                Description = "GitHub repository in owner/name or URL format",
                Required = true
            };
            var tagOption = new Option<string>("--tag", ["-t"]) { Description = "Release tag", Required = true };
            var pathOption = new Option<string?>("--path", ["-p"]) { Description = "Absolute directory for the downloaded asset" };

            downloadCommand.Add(repositoryOption);
            downloadCommand.Add(tagOption);
            downloadCommand.Add(pathOption);
            downloadCommand.Add(verboseOption);
            downloadCommand.Add(dryRunOption);
            downloadCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                var repository = parseResult.GetValue(repositoryOption)!;
                var tag = parseResult.GetValue(tagOption)!;
                var path = parseResult.GetValue(pathOption) ?? Directory.GetCurrentDirectory();
                var verbose = parseResult.GetValue(verboseOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (dryRun)
                {
                    ConsoleHelper.WriteLine("DRY-RUN: running in dry-run mode; no destructive actions will be performed.", ConsoleColor.Yellow);
                }

                if (verbose)
                {
                    ConsoleHelper.WriteLine($"Downloading asset for repo: {repository}, tag: {tag}, path: {path}", ConsoleColor.Gray);
                }

                try
                {
                    var result = await NbuildCommand.DownloadAsset(repository, tag, path, dryRun);
                    return result.Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(downloadCommand);
        }

        private void AddListCommand(Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var listCommand = new System.CommandLine.Command(
                "list",
                "Lists the latest releases for the specified repository.");
            var repositoryOption = new Option<string>("--repo", ["-r"])
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