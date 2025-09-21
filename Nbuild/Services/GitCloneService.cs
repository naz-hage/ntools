using Nbuild.Interfaces;
using System.CommandLine;

// -----------------------------------------------------------------------------
// File: GitCloneService.cs
// Purpose: Implements IGitCloneService; wraps command-level Clone call and
//          forwards output to the provided IConsole. Supports dry-run semantics
//          and returns a process-style integer exit code.
// -----------------------------------------------------------------------------
namespace Nbuild.Services
{
    /// <summary>
    /// Service that performs a git clone operation.
    /// </summary>
    /// <remarks>
    /// This service is intentionally thin: it converts parsed CLI arguments into
    /// a call to the shared <c>Command.Clone</c> implementation and writes
    /// human-readable messages to the provided <see cref="IConsole"/>.
    ///
    /// Key behaviors:
    /// - Dry-run: when <paramref name="dryRun"/> is true, the service emits a
    ///   clear DRY-RUN message and avoids any destructive operations. The
    ///   underlying <c>Command.Clone</c> is expected to also respect the
    ///   dry-run flag and return an appropriate result.
    /// - IConsole: all user-facing text is written via the supplied
    ///   <see cref="IConsole"/> using <c>ConsoleExtensions.WriteLine</c>. This
    ///   keeps the service testable and avoids direct Console calls.
    /// - Return codes: the method returns an int exit code (0 for success,
    ///   negative values for internal errors) rather than setting
    ///   <c>Environment.ExitCode</c> itself. Callers (command handlers) can set
    ///   the process exit code if desired.
    ///
    /// Design notes:
    /// - The service takes simple primitives (string/bool) so it can be used by
    ///   both production commands and unit tests without needing a full CLI
    ///   parsing context.
    /// - Keep this implementation small: error handling, formatting, and
    ///   presentation live here, while the domain behavior is implemented in
    ///   <c>Command.Clone</c>.
    /// </remarks>
    public class GitCloneService : IGitCloneService
    {
        /// <summary>
        /// Clone a git repository to the target path.
        /// </summary>
        /// <param name="url">The repository URL to clone. Must not be null; empty
        /// values will be treated as an invalid input.</param>
        /// <param name="path">Destination path for the clone. If empty, the
        /// current working directory will be used.</param>
        /// <param name="verbose">When true, produce additional diagnostic
        /// information to the provided console.</param>
        /// <param name="dryRun">When true, perform a dry-run: show actions but
        /// do not perform destructive side effects. Both this service and the
        /// underlying <c>Command.Clone</c> should honor this flag.</param>
        /// <param name="console">The <see cref="IConsole"/> to write output to.
        /// This allows tests to capture/inspect output without touching the
        /// real console.</param>
        /// <returns>Process-like integer exit code. 0 on success; negative on
        /// internal error; otherwise use codes returned from
        /// <c>Command.Clone</c>.</returns>
        public int Clone(string url, string path, bool verbose, bool dryRun, IConsole console)
        {
            try
            {
                if (dryRun)
                {
                    // Use System.CommandLine.ConsoleExtensions to write to the provided IConsole
                    ConsoleExtensions.WriteLine(console, "DRY-RUN: running in dry-run mode; no destructive actions will be performed.");
                }

                var result = Command.Clone(url, path, verbose, dryRun);
                return result.Code;
            }
            catch (Exception ex)
            {
                // Surface a friendly error message to the supplied console so
                // callers (and tests) can verify output.
                ConsoleExtensions.WriteLine(console, $"Error: {ex.Message}");
                return -1;
            }
        }
    }
}
