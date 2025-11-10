using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitHubRelease
{
    /// <summary>
    /// Defines the types of operations that can be performed on GitHub repositories.
    /// </summary>
    public enum GitHubOperation
    {
        /// <summary>
        /// Read-only operations (e.g., listing releases, getting repository info).
        /// </summary>
        Read,

        /// <summary>
        /// Write operations (e.g., creating releases, uploading assets).
        /// </summary>
        Write
    }

    /// <summary>
    /// Represents the visibility of a GitHub repository.
    /// </summary>
    public enum RepositoryVisibility
    {
        /// <summary>
        /// Public repository - accessible without authentication for read operations.
        /// </summary>
        Public,

        /// <summary>
        /// Private repository - requires authentication for all operations.
        /// </summary>
        Private,

        /// <summary>
        /// Repository visibility could not be determined.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Centralized service for managing GitHub authentication requirements based on repository visibility and operation type.
    /// </summary>
    public class GitHubAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly bool _verbose;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubAuthService"/> class.
        /// </summary>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        public GitHubAuthService(bool verbose = false)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubRelease/1.0");
            _verbose = verbose;
        }

        /// <summary>
        /// Determines if authentication is required for the specified repository and operation.
        /// </summary>
        /// <param name="repositoryUrl">The GitHub repository URL (e.g., "https://github.com/owner/repo" or "owner/repo").</param>
        /// <param name="operation">The type of operation being performed.</param>
        /// <returns>True if authentication is required, false otherwise.</returns>
        public async Task<bool> RequiresAuthenticationAsync(string repositoryUrl, GitHubOperation operation)
        {
            var visibility = await GetRepositoryVisibilityAsync(repositoryUrl);

            return RequiresAuthentication(visibility, operation);
        }

        /// <summary>
        /// Determines if authentication is required based on repository visibility and operation type.
        /// </summary>
        /// <param name="visibility">The repository visibility.</param>
        /// <param name="operation">The type of operation being performed.</param>
        /// <returns>True if authentication is required, false otherwise.</returns>
        public bool RequiresAuthentication(RepositoryVisibility visibility, GitHubOperation operation)
        {
            switch (visibility)
            {
                case RepositoryVisibility.Public:
                    // Public repos only require auth for write operations
                    return operation == GitHubOperation.Write;

                case RepositoryVisibility.Private:
                    // Private repos always require authentication
                    return true;

                case RepositoryVisibility.Unknown:
                default:
                    // When visibility is unknown, require authentication to be safe
                    if (_verbose)
                    {
                        Console.WriteLine("[VERBOSE] Repository visibility unknown, requiring authentication for safety");
                    }
                    return true;
            }
        }

        /// <summary>
        /// Gets the repository visibility by making an unauthenticated API call to GitHub.
        /// </summary>
        /// <param name="repositoryUrl">The GitHub repository URL (e.g., "https://github.com/owner/repo" or "owner/repo").</param>
        /// <returns>The repository visibility.</returns>
        public async Task<RepositoryVisibility> GetRepositoryVisibilityAsync(string repositoryUrl)
        {
            try
            {
                // Parse repository URL to extract owner/repo
                var (owner, repo) = ParseRepositoryUrl(repositoryUrl);
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    if (_verbose)
                    {
                        Console.WriteLine($"[VERBOSE] Could not parse repository URL: {repositoryUrl}");
                    }
                    return RepositoryVisibility.Unknown;
                }

                // Make unauthenticated API call to check if repo is public
                var apiUrl = $"https://api.github.com/repos/{owner}/{repo}";
                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the response to check visibility
                    var content = await response.Content.ReadAsStringAsync();
                    var repoData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (repoData.TryGetProperty("private", out var privateProp) && privateProp.GetBoolean())
                    {
                        if (_verbose)
                        {
                            Console.WriteLine($"[VERBOSE] Repository {owner}/{repo} detected as private");
                        }
                        return RepositoryVisibility.Private;
                    }
                    else
                    {
                        if (_verbose)
                        {
                            Console.WriteLine($"[VERBOSE] Repository {owner}/{repo} detected as public");
                        }
                        return RepositoryVisibility.Public;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Repository doesn't exist or is private and we don't have access
                    if (_verbose)
                    {
                        Console.WriteLine($"[VERBOSE] Repository not found or private: {owner}/{repo}");
                    }
                    return RepositoryVisibility.Private; // Assume private if not found
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // Rate limited or blocked - assume private for safety
                    if (_verbose)
                    {
                        Console.WriteLine($"[VERBOSE] API access forbidden for {owner}/{repo}, assuming private");
                    }
                    return RepositoryVisibility.Private;
                }
                else
                {
                    if (_verbose)
                    {
                        Console.WriteLine($"[VERBOSE] Unexpected API response ({response.StatusCode}) for {owner}/{repo}");
                    }
                    return RepositoryVisibility.Unknown;
                }
            }
            catch (Exception ex)
            {
                if (_verbose)
                {
                    Console.WriteLine($"[VERBOSE] Error checking repository visibility: {ex.Message}");
                }
                return RepositoryVisibility.Unknown;
            }
        }

        /// <summary>
        /// Parses a GitHub repository URL to extract owner and repository name.
        /// </summary>
        /// <param name="repositoryUrl">The repository URL in various formats.</param>
        /// <returns>A tuple containing (owner, repo) or (null, null) if parsing fails.</returns>
        private (string? owner, string? repo) ParseRepositoryUrl(string repositoryUrl)
        {
            if (string.IsNullOrEmpty(repositoryUrl))
            {
                return (null, null);
            }

            // Remove protocol and domain if present
            var cleanUrl = repositoryUrl
                .Replace("https://github.com/", "")
                .Replace("http://github.com/", "")
                .Replace("github.com/", "")
                .Trim('/');

            // Check if URL still contains github.com (invalid format)
            if (cleanUrl.Contains("github.com") || cleanUrl.Contains("http") || cleanUrl.Contains("://"))
            {
                return (null, null);
            }

            // Split by '/' to get owner/repo
            var parts = cleanUrl.Split('/');
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
            {
                // Basic validation - reject obviously invalid identifiers
                foreach (var part in parts)
                {
                    if (part.Contains(' ') || part.Contains('<') || part.Contains('>') ||
                        part.Contains('/') || part.Contains('\\') || part.Contains('?') ||
                        part.Contains('#') || part.Length > 39)
                    {
                        return (null, null);
                    }
                }
                return (parts[0], parts[1]);
            }

            return (null, null);
        }

        /// <summary>
        /// Gets an authentication error message based on the repository and operation.
        /// </summary>
        /// <param name="repositoryUrl">The repository URL.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <returns>A user-friendly error message.</returns>
        public async Task<string> GetAuthenticationErrorMessageAsync(string repositoryUrl, GitHubOperation operation)
        {
            var visibility = await GetRepositoryVisibilityAsync(repositoryUrl);

            switch (visibility)
            {
                case RepositoryVisibility.Public:
                    return operation == GitHubOperation.Write
                        ? $"Authentication required for write operations on public repository '{repositoryUrl}'. Please set API_GITHUB_KEY environment variable."
                        : $"Unexpected authentication requirement for read operation on public repository '{repositoryUrl}'.";

                case RepositoryVisibility.Private:
                    return $"Authentication required for all operations on private repository '{repositoryUrl}'. Please set API_GITHUB_KEY environment variable.";

                case RepositoryVisibility.Unknown:
                default:
                    return $"Authentication required for repository '{repositoryUrl}' (visibility could not be determined). Please set API_GITHUB_KEY environment variable.";
            }
        }
    }
}