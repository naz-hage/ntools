using System.CommandLine;

// -----------------------------------------------------------------------------
// File: IGitCloneService.cs
// Purpose: Defines the contract for a service that performs git clone operations.
// -----------------------------------------------------------------------------
namespace Nbuild.Interfaces
{
    /// <summary>
    /// Contract for a service that performs a git clone operation.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are responsible for executing the
    /// domain logic needed to clone a repository. The interface takes simple
    /// primitives and an <see cref="IConsole"/> so implementations remain
    /// testable without requiring the full CLI parsing context.
    ///
    /// Important notes:
    /// - <c>dryRun</c> should be honored by implementations: when true, the
    ///   operation should avoid destructive side effects and instead report
    ///   the actions it would take.
    /// - <c>IConsole</c> is provided to allow writing human-facing messages
    ///   without depending on static Console calls, making unit tests easier.
    /// - The integer return value follows a process-style convention: 0 for
    ///   success, other values for failures (negative values may indicate
    ///   internal errors).
    /// </remarks>
    public interface IGitCloneService
    {
        /// <summary>
        /// Clone a Git repository to the specified path.
        /// </summary>
        /// <param name="url">Repository URL to clone (required).</param>
        /// <param name="path">Target path for the clone; when empty, use the current directory.</param>
        /// <param name="verbose">If true, emit additional diagnostic output.</param>
        /// <param name="dryRun">If true, perform a dry-run: describe actions but do not perform destructive operations.</param>
        /// <param name="console">Console instance to write output to; must not be null.</param>
        /// <returns>Process-style integer exit code: 0 on success, non-zero on failure.</returns>
        int Clone(string url, string path, bool verbose, bool dryRun, IConsole console);
    }
}
