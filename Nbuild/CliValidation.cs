using System.CommandLine;

namespace Nbuild
{
    /// <summary>
    /// Provides simple command-line argument validation and typo-suggestion helpers used by the
    /// nb CLI startup path. The class is intentionally small and dependency-free so it can be
    /// executed very early in the process before the full command graph is built.
    /// </summary>
    /// <remarks>
    /// Public helpers in this class have two related responsibilities:
    /// - Detect a handful of common typos using a fixed "common typos" map and return a
    ///   human-friendly suggestion.
    /// - Provide a best-effort suggestion for unknown options based on Levenshtein distance
    ///   against the set of known options on the provided <see cref="RootCommand"/>.
    /// 
    /// Important Behavior notes:
    /// - These helpers are conservative: they only suggest corrections when the token is
    ///   clearly an option (starts with "-" or "--"). They do not attempt to parse positional
    ///   arguments or flags beyond a short sanity check.
    /// - The class writes to <see cref="Console.Error"/> only in the ValidateArgsForTypos
    ///   method which is intended to be used during process startup to perform a quick
    ///   validation pass and print friendly messages. Other helpers return strings so callers
    ///   can integrate them into different logging or UI flows (for example, unit tests).
    /// </remarks>
    internal static class CliValidation
    {
        /// <summary>
        /// Scans the provided args for a small set of common option typos and returns a
        /// ready-to-print message if a likely correction is found.
        /// </summary>
        /// <param name="args">The raw args array passed to the process (usually <c>string[] args</c>).</param>
        /// <returns>
        /// A human-friendly message describing the suggested correction, or <c>null</c> if no
        /// common-typo was detected. The returned string is formatted for direct console output.
        /// </returns>
        /// <remarks>
        /// This method only looks for the exact tokens contained in the embedded common-typos
        /// map. It is faster than computing Levenshtein distances for every token and is useful
        /// for surfacing the most frequent mistakes (for example "--dryrun" -> "--dry-run").
        /// </remarks>
        public static string? CheckForCommonTypos(string[] args)
        {
            var commonTypos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--drys-run"] = "--dry-run",
                ["--dryrun"] = "--dry-run",
                ["--dry"] = "--dry-run",
                ["--verbos"] = "--verbose",
                ["--verb"] = "--verbose",
                ["--verbo"] = "--verbose",
                ["--hep"] = "--help",
                ["--halp"] = "--help",
                ["--jsn"] = "--json",
                ["--jso"] = "--json"
            };

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("--"))
                {
                    if (commonTypos.TryGetValue(arg.ToLower(), out var correction))
                    {
                        return $"Unknown option '{arg}'. Did you mean '{correction}'?\nUse 'nb --help' or 'nb [command] --help' to see available options.";
                    }

                    if (!IsValidOption(arg, args))
                    {
                        return $"Unknown option '{arg}'.\nUse 'nb --help' or 'nb [command] --help' to see available options.";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Performs an early validation pass over the arguments and writes any friendly error
        /// messages to <c>Console.Error</c>. This is intended to run before command dispatch so
        /// callers can fail fast with helpful suggestions.
        /// </summary>
        /// <param name="args">The raw process arguments.</param>
        /// <returns>
        /// <c>true</c> when a validation error was detected and a message was written to the
        /// error stream; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Behavior specifics:
        /// - Recognises a fixed set of valid tokens (options and command names).
        /// - If an unknown option is detected it checks the common-typos map first and then
        ///   falls back to Levenshtein-based suggestions computed against a freshly-created
        ///   empty <see cref="RootCommand"/> (this keeps the helper dependency-free).
        /// - The method prints guidance on how to view help for the CLI when reporting errors.
        /// </remarks>
        public static bool ValidateArgsForTypos(string[] args)
        {
            var validTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "--help", "--version", "--dry-run", "--verbose",
                "--json", "--tag", "--repo", "--branch", "--file", "--path", "--url", "--buildtype",
                "install", "uninstall", "list", "download", "targets", "path", "git_info", "git_settag",
                "git_autotag", "git_push_autotag", "git_branch", "git_clone", "git_deletetag",
                "release_create", "pre_release_create", "release_download", "list_release"
            };

            var commonTypos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--drys-run"] = "--dry-run",
                ["--dryrun"] = "--dry-run",
                ["--dry"] = "--dry-run",
                ["--verbos"] = "--verbose",
                ["--verb"] = "--verbose",
                ["--verbo"] = "--verbose",
                ["--hep"] = "--help",
                ["--halp"] = "--help",
                ["--jsn"] = "--json",
                ["--jso"] = "--json"
            };

            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg)) continue;
                if (!arg.StartsWith("-")) continue;

                var token = arg;
                var eq = token.IndexOf('=');
                if (eq > 0) token = token.Substring(0, eq);

                if (validTokens.Contains(token)) continue;

                if (commonTypos.TryGetValue(token, out var correction))
                {
                    Console.Error.WriteLine($"Unknown option '{token}'. Did you mean '{correction}'?");
                    Console.Error.WriteLine($"Use 'nb --help' or 'nb [command] --help' to see available options.");
                    return true;
                }

                var suggestion = GetOptionSuggestion(token, new RootCommand());
                if (!string.IsNullOrEmpty(suggestion))
                {
                    Console.Error.WriteLine($"Unknown option '{token}'. Did you mean '{suggestion}'?");
                    Console.Error.WriteLine($"Use 'nb --help' or 'nb [command] --help' to see available options.");
                    return true;
                }

                Console.Error.WriteLine($"Unknown option '{token}'. Use 'nb --help' or 'nb [command] --help' to see available options.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Given an invalid option token this method attempts to return a single-word suggestion
        /// for what the user probably meant.
        /// </summary>
        /// <param name="invalidOption">The invalid option token (for example "--dryrun").</param>
        /// <param name="rootCommand">A <see cref="RootCommand"/> instance whose <c>Options</c>
        /// collection will be inspected to compute Levenshtein-based suggestions.</param>
        /// <returns>
        /// A suggested option string (for example "--dry-run"), or <c>null</c> when no suitable
        /// suggestion was found.
        /// </returns>
        /// <remarks>
        /// The method first checks the small common-typos map and then computes nearest
        /// neighbours using a Levenshtein distance threshold of 2. Because a fresh
        /// <see cref="RootCommand"/> is typically empty, callers who want suggestions based on
        /// actual registered options should pass the real root command instance.
        /// </remarks>
        public static string? GetOptionSuggestion(string invalidOption, RootCommand rootCommand)
        {
            var commonTypos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--drys-run"] = "--dry-run",
                ["--dryrun"] = "--dry-run",
                ["--dry"] = "--dry-run",
                ["--verbos"] = "--verbose",
                ["--verb"] = "--verbose",
                ["--verbo"] = "--verbose",
                ["--hep"] = "--help",
                ["--halp"] = "--help"
            };

            if (commonTypos.TryGetValue(invalidOption.ToLower(), out var correction))
            {
                return correction;
            }

            var allOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in rootCommand.Options)
            {
                allOptions.Add($"--{option.Name}");
                foreach (var alias in option.Aliases)
                {
                    allOptions.Add(alias);
                }
            }

            var bestMatch = allOptions
                .Where(opt => CalculateLevenshteinDistance(invalidOption.ToLower(), opt.ToLower()) <= 2)
                .OrderBy(opt => CalculateLevenshteinDistance(invalidOption.ToLower(), opt.ToLower()))
                .FirstOrDefault();

            return bestMatch;
        }

        /// <summary>
        /// Compute the Levenshtein edit distance between two strings.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="target">The target string.</param>
        /// <returns>The number of single-character edits required to transform <c>source</c> into <c>target</c>.</returns>
        /// <remarks>
        /// The Levenshtein distance is the minimum number of single-character edits
        /// (insertions, deletions or substitutions) required to change one string into another.
        /// This method implements the classic dynamic programming approach.
        ///
        /// The algorithm was described by Vladimir Levenshtein (1965/1966). For background and
        /// further reading see the Wikipedia entry:
        /// https://en.wikipedia.org/wiki/Levenshtein_distance
        /// </remarks>
        private static int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var matrix = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }

        /// <summary>
        /// A tiny sanity check used by CheckForCommonTypos to avoid suggesting corrections for
        /// tokens that are not known option names. The method is intentionally conservative.
        /// </summary>
        /// <param name="option">The option token to check (for example "--dry-run").</param>
        /// <param name="args">The original args array (unused currently but kept for future checks).</param>
        /// <returns><c>true</c> when the option is considered valid; otherwise <c>false</c>.</returns>
        private static bool IsValidOption(string option, string[] args)
        {
            var validOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "--help", "--version", "--dry-run", "--verbose",
                "--json", "--tag", "--repo", "--branch", "--file",
                "--path", "--url", "--buildtype"
            };

            return validOptions.Contains(option.ToLower());
        }
    }
}
