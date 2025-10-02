// -----------------------------------------------------------------------------
// File: PathManager.cs
// Purpose: Centralizes all PATH environment variable operations to provide
//          safe, testable, and consistent PATH manipulation.
// -----------------------------------------------------------------------------
using System;
using Nbuild;

namespace Nbuild.Services
{
    /// <summary>
    /// Centralizes all PATH environment variable operations to provide safe,
    /// testable, and consistent PATH manipulation. This class replaces direct
    /// calls to Environment.SetEnvironmentVariable("PATH", ...) throughout the
    /// codebase to reduce test flakiness and protect against accidental PATH
    /// corruption.
    /// </summary>
    /// <remarks>
    /// Key behaviors:
    /// - All operations target EnvironmentVariableTarget.User (user PATH)
    /// - PATH segments are separated by semicolons (;)
    /// - AddPath prepends new segments (they appear first in PATH)
    /// - Deduplication preserves first occurrence ordering
    /// - Null/empty inputs are handled gracefully
    /// - Provides snapshot/restore for safe testing
    /// </remarks>
    public static class PathManager
    {
        private const char PathSeparator = ';';

        /// <summary>
        /// Gets the current user PATH environment variable.
        /// </summary>
        /// <returns>The current user PATH string, or empty string if not set.</returns>
        public static string GetUserPath()
        {
            return Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        }

        /// <summary>
        /// Sets the user PATH environment variable to the specified value.
        /// </summary>
        /// <param name="path">The PATH string to set. If null or empty, clears the PATH.</param>
        public static void SetUserPath(string? path)
        {
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
        }

        /// <summary>
        /// Adds a path segment to the user PATH if it doesn't already exist.
        /// The new segment is prepended (appears first) in the PATH.
        /// </summary>
        /// <param name="pathSegment">The path segment to add. If null or empty, no action is taken.</param>
        /// <remarks>
        /// This operation is idempotent - adding the same path multiple times
        /// has the same effect as adding it once.
        /// </remarks>
        public static void AddPath(string? pathSegment)
        {
            if (string.IsNullOrWhiteSpace(pathSegment))
                return;

            var currentPath = GetUserPath();
            var segments = GetPathSegments(currentPath);

            // Check if the path segment already exists (case-insensitive comparison)
            var normalizedSegment = pathSegment.Trim();
            if (segments.Any(s => string.Equals(s, normalizedSegment, StringComparison.OrdinalIgnoreCase)))
                return;

            // Prepend the new segment
            var newPath = string.IsNullOrEmpty(currentPath)
                ? normalizedSegment
                : $"{normalizedSegment}{PathSeparator}{currentPath}";

            SetUserPath(newPath);
        }

        /// <summary>
        /// Removes a path segment from the user PATH if it exists.
        /// </summary>
        /// <param name="pathSegment">The path segment to remove. If null or empty, no action is taken.</param>
        /// <remarks>
        /// This operation is idempotent - removing a non-existent or empty path has no effect.
        /// </remarks>
        public static void RemovePath(string? pathSegment)
        {
            if (string.IsNullOrWhiteSpace(pathSegment))
                return;

            var currentPath = GetUserPath();
            var segments = GetPathSegments(currentPath);

            var normalizedSegment = pathSegment.Trim();
            var filteredSegments = segments.Where(s =>
                !string.Equals(s, normalizedSegment, StringComparison.OrdinalIgnoreCase)).ToArray();

            // Only update if we actually removed something
            if (filteredSegments.Length != segments.Length)
            {
                var newPath = string.Join(PathSeparator.ToString(), filteredSegments);
                SetUserPath(newPath);
                ConsoleHelper.WriteLine($"√ {pathSegment} removed from user PATH.", ConsoleColor.Green);
            }
        }

        /// <summary>
        /// Splits a PATH string into individual path segments.
        /// </summary>
        /// <param name="path">The PATH string to split. If null, uses the current user PATH.</param>
        /// <returns>An array of path segments, with empty entries filtered out.</returns>
        public static string[] GetPathSegments(string? path = null)
        {
            var pathToSplit = path ?? GetUserPath();
            if (string.IsNullOrEmpty(pathToSplit))
                return Array.Empty<string>();

            return pathToSplit.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Where(s => !string.IsNullOrEmpty(s))
                             .ToArray();
        }

        /// <summary>
        /// Removes duplicate path segments from the PATH, preserving the first occurrence
        /// of each unique segment. Updates the user PATH with the deduplicated result.
        /// </summary>
        /// <param name="path">The PATH string to deduplicate. If null, uses and updates the current user PATH.</param>
        /// <returns>The deduplicated PATH string.</returns>
        /// <remarks>
        /// This method preserves the order of first occurrences. For example:
        /// Input: "C:\a;C:\b;C:\a;C:\c"
        /// Output: "C:\a;C:\b;C:\c"
        /// </remarks>
        public static string RemoveDuplicatePathSegments(string? path = null)
        {
            var pathToProcess = path ?? GetUserPath();
            var segments = GetPathSegments(pathToProcess);

            // Use a case-insensitive comparison but preserve original casing
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uniqueSegments = new List<string>();

            foreach (var segment in segments)
            {
                if (seen.Add(segment))
                {
                    uniqueSegments.Add(segment);
                }
            }

            var deduplicatedPath = string.Join(PathSeparator.ToString(), uniqueSegments);

            // If no path was provided, update the user PATH
            if (path == null)
            {
                SetUserPath(deduplicatedPath);
            }

            return deduplicatedPath;
        }
 
        /// <summary>
        /// Displays the current PATH segments to the console.
        /// </summary>
        public static void DisplayPathSegments()
        {
            var path = GetUserPath();
            var pathSegments = RemoveDuplicatePathSegments(path);
            ConsoleHelper.WriteLine($"PATH Segments:", ConsoleColor.Yellow);
            foreach (var segment in GetPathSegments(pathSegments))
            {
                Console.WriteLine($" '{segment}'");
            }
        }

        /// <summary>
        /// Checks if a path segment is present in the user PATH environment variable.
        /// </summary>
        /// <param name="pathSegment">The path segment to check. If null or empty, returns false.</param>
        /// <returns>True if the path segment is present in the PATH, otherwise false.</returns>
        /// <remarks>
        /// This method performs a case-insensitive comparison.
        /// </remarks>
        public static bool IsPathPresent(string? pathSegment)
        {
            if (string.IsNullOrWhiteSpace(pathSegment))
                return false;

            var currentPath = GetUserPath();
            var segments = GetPathSegments(currentPath);

            var normalizedSegment = pathSegment.Trim();
            return segments.Any(s => string.Equals(s, normalizedSegment, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a snapshot of the current user PATH for later restoration.
        /// Useful for tests that need to temporarily modify PATH.
        /// </summary>
        /// <returns>A snapshot object that can be used to restore the PATH.</returns>
        public static PathSnapshot CreateSnapshot()
        {
            return new PathSnapshot(GetUserPath());
        }

        /// <summary>
        /// Restores the user PATH from a snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        /// <exception cref="ArgumentNullException">Thrown if snapshot is null.</exception>
        public static void RestoreSnapshot(PathSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            SetUserPath(snapshot.OriginalPath);
        }

        /// <summary>
        /// Adds the application's install path to the user PATH environment variable.
        /// </summary>
        /// <param name="installPath">The install path to add to the PATH. If null or empty, no action is taken.</param>
        /// <remarks>
        /// This method checks if the install path is already present in the user PATH environment variable.
        /// If not, it adds the install path to the PATH. It also logs the action using the Colorizer.
        /// </remarks>
        public static void AddAppInstallPathToEnvironmentPath(string? installPath)
        {
            if (string.IsNullOrWhiteSpace(installPath))
                return;

            if (IsPathPresent(installPath))
            {
                ConsoleHelper.WriteLine($"{installPath} is already in PATH.", ConsoleColor.Yellow);
                return;
            }

            AddPath(installPath);
            ConsoleHelper.WriteLine($"√ {installPath} added to user PATH.", ConsoleColor.Green);
        }
    }

    /// <summary>
    /// Represents a snapshot of the PATH environment variable for safe restoration.
    /// </summary>
    public sealed class PathSnapshot
    {
        /// <summary>
        /// Gets the original PATH value captured in this snapshot.
        /// </summary>
        public string OriginalPath { get; }

        /// <summary>
        /// Initializes a new instance of the PathSnapshot class.
        /// </summary>
        /// <param name="originalPath">The original PATH value to store.</param>
        public PathSnapshot(string? originalPath)
        {
            OriginalPath = originalPath ?? string.Empty;
        }
    }
}