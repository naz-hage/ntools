using System;

namespace NbuildTasks
{
    /// <summary>
    /// Provides utility methods for consistent error message display.
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// Displays an error message to the console with consistent formatting.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="details">Optional additional details about the error.</param>
        public static void WriteError(string message, string details = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: ");
            Console.ResetColor();
            Console.WriteLine(message);

            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine(details);
            }
        }

        /// <summary>
        /// Displays a GitHub authentication error with helpful guidance.
        /// </summary>
        /// <param name="repositoryUrl">The repository URL that requires authentication.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="isPublicRepo">Whether the repository is public.</param>
        public static void WriteGitHubAuthError(string repositoryUrl, string operation, bool isPublicRepo = false)
        {
            var message = $"GitHub authentication required for {operation} operation on repository '{repositoryUrl}'.";

            string details;
            if (isPublicRepo)
            {
                details = $"For public repositories, you may not need authentication for read operations. " +
                          $"Set the API_GITHUB_KEY environment variable or use GitHub CLI authentication. " +
                          $"Run 'gh auth login' to authenticate with GitHub CLI.";
            }
            else
            {
                details = $"Set the API_GITHUB_KEY environment variable with your GitHub personal access token, " +
                          $"or authenticate using GitHub CLI with 'gh auth login'.";
            }

            WriteError(message, details);
        }
    }
}