using System.CommandLine;

// -----------------------------------------------------------------------------
// File: GitCloneCommand.cs
// Purpose: Registers the `git_clone` subcommand and wires CLI options to the
//          underlying IGitCloneService implementation.
// -----------------------------------------------------------------------------
namespace Nbuild.Commands
{
    /// <summary>
    /// Provides registration for the <c>git_clone</c> command.
    /// </summary>
    /// <remarks>
    /// Responsibilities:
    /// - Declare the command and its local options (url, path, verbose).
    /// - Wire the global <c>--dry-run</c> option (passed as <paramref name="dryRunOption"/>)
    ///   into the command handler so the parsed boolean can be forwarded to the
    ///   service.
    /// - Use an <see cref="System.CommandLine.Invocation.InvocationContext"/>
    ///   handler to access the parse result and invocation <see cref="IConsole"/>
    ///   and call into <see cref="Interfaces.IGitCloneService"/>.
    ///
    /// Design notes:
    /// - The command does minimal work: it maps parsed CLI values to primitive
    ///   method arguments and delegates behavior to the injected service. This
    ///   keeps parsing/validation separate from domain logic and makes testing
    ///   the handler straightforward (inject a fake service and assert calls).
    /// </remarks>
    internal static class GitCloneCommand
    {
        /// <summary>
        /// Register the <c>git_clone</c> command on the supplied <paramref name="rootCommand"/>.
        /// </summary>
        /// <param name="rootCommand">Root command to attach the subcommand to.</param>
        /// <param name="dryRunOption">Shared global <c>--dry-run</c> option instance created
        /// in <c>Program</c>. The handler will call <c>parse.GetValueForOption(dryRunOption)</c>
        /// to obtain the boolean value.</param>
        /// <param name="cloneService">Injected service implementing the clone behavior.
        /// The service receives primitive arguments (string/bool) and an <see cref="IConsole"/>
        /// for output. This keeps the command thin and testable.</param>
        public static void Register(RootCommand rootCommand, Option<bool> dryRunOption, Interfaces.IGitCloneService cloneService)
        {
            var gitCloneCommand = new System.CommandLine.Command("git_clone",
                "Clones a Git repository to a specified path.\n\n" +
                "Required option:\n" +
                "  --url   Git repository URL\n" +
                "Optional options:\n" +
                "  --path      Path to clone into (default: current directory)\n" +
                "  --verbose   Verbose output\n\n" +
                "Example:\n" +
                "  nb git_clone --url https://github.com/user/repo --path ./repo --verbose\n");

            var urlOption = new Option<string>("--url", "Specifies the Git repository URL") { IsRequired = true };
            var pathOption = new Option<string>("--path", "The path where the repo will be cloned. If not specified, the current directory will be used");
            var verboseOption = new Option<bool>("--verbose", "Verbose output");

            gitCloneCommand.AddOption(urlOption);
            gitCloneCommand.AddOption(pathOption);
            gitCloneCommand.AddOption(verboseOption);

            // Use an InvocationContext handler to access the parse result and the invocation console.
            gitCloneCommand.SetHandler((System.CommandLine.Invocation.InvocationContext ctx) =>
            {
                var parse = ctx.ParseResult;
                var url = parse.GetValueForOption(urlOption);
                var path = parse.GetValueForOption(pathOption);
                // Use command-local verbose option only
                var verbose = parse.GetValueForOption(verboseOption);
                var dryRun = parse.GetValueForOption(dryRunOption);

                var exitCode = cloneService.Clone(url ?? string.Empty, path ?? string.Empty, verbose, dryRun, ctx.Console);
                Environment.ExitCode = exitCode;
            });

            rootCommand.AddCommand(gitCloneCommand);
        }

        // Handler logic moved to IGitCloneService; local helper removed to avoid duplication.
    }
}
