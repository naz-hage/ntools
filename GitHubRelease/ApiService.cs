using System.Net.Http.Headers;

namespace GitHubRelease
{
    /// <summary>
    /// Represents an API service for interacting with GitHub.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient Client;
        private readonly bool Verbose;
        private readonly GitHubAuthService? AuthService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiService"/> class.
        /// </summary>
        public ApiService(bool verbose = false)
        {
            Verbose = verbose;
            Client = new HttpClient();
            AuthService = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiService"/> class with authentication service.
        /// </summary>
        public ApiService(GitHubAuthService authService, bool verbose = false)
        {
            Verbose = verbose;
            Client = new HttpClient();
            AuthService = authService;
        }

        /// <summary>
        /// Gets the HTTP client instance.
        /// </summary>
        /// <returns>The HTTP client instance.</returns>
        public HttpClient GetClient()
        {
            return Client;
        }

        /// <summary>
        /// Sets up the headers for making API requests.
        /// </summary>
        /// <param name="download">Indicates whether the request is for downloading content.</param>
        /// <remarks>
        /// - If <paramref name="download"/> is true, the headers are configured for downloading files.
        /// - If <paramref name="download"/> is false, the headers are configured for standard API requests.
        /// - Authentication is added based on the GitHubAuthService if available, otherwise falls back to the legacy Credentials.GetToken() approach.
        /// </remarks>
        public void SetupHeaders(bool download = false)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Credentials.GetToken()}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");

            if (download)
            {
                Client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubRelease/1.0)");
            }
            else
            {
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json")); // Accept JSON for metadata
            }
        }

        /// <summary>
        /// Sets up the headers for making API requests to a specific GitHub repository.
        /// </summary>
        /// <param name="repositoryUrl">The GitHub repository URL.</param>
        /// <param name="operation">The type of GitHub operation being performed.</param>
        /// <param name="download">Indicates whether the request is for downloading content.</param>
        /// <remarks>
        /// - If <paramref name="download"/> is true, the headers are configured for downloading files.
        /// - If <paramref name="download"/> is false, the headers are configured for standard API requests.
        /// - Authentication is conditionally added based on repository visibility and operation type using GitHubAuthService.
        /// </remarks>
        public async Task SetupHeadersAsync(string repositoryUrl, GitHubOperation operation, bool download = false)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("User-Agent", "request");

            // Check if authentication is required
            if (AuthService != null)
            {
                bool requiresAuth = await AuthService.RequiresAuthenticationAsync(repositoryUrl, operation);
                if (requiresAuth)
                {
                    Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Credentials.GetToken()}");
                    if (Verbose)
                    {
                        Console.WriteLine($"[VERBOSE] Authentication required for {operation} operation on {repositoryUrl}");
                    }
                }
                else
                {
                    if (Verbose)
                    {
                        Console.WriteLine($"[VERBOSE] No authentication required for {operation} operation on {repositoryUrl}");
                    }
                }
            }
            else
            {
                // Fallback to legacy behavior
                Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Credentials.GetToken()}");
                if (Verbose)
                {
                    Console.WriteLine($"[VERBOSE] Using legacy authentication (no GitHubAuthService provided)");
                }
            }

            if (download)
            {
                Client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubRelease/1.0)");
            }
            else
            {
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json")); // Accept JSON for metadata
            }
        }

        /// <summary>
        /// Sends a GET request to the specified URI.
        /// </summary>
        /// <param name="uri">The URI to send the request to.</param>
        /// <returns>The HTTP response message.</returns>
        public async Task<HttpResponseMessage> GetAsync(string uri)
        {
            if (Verbose) Console.WriteLine($"GET uri: {uri}");
            return await Client.GetAsync(uri);
        }

        /// <summary>
        /// Sends a POST request to the specified URI with the given content.
        /// </summary>
        /// <param name="uri">The URI to send the request to.</param>
        /// <param name="content">The HTTP content to send with the request.</param>
        /// <returns>The HTTP response message.</returns>
        public async Task<HttpResponseMessage> PostAsync(string uri, HttpContent content)
        {
            if (Verbose) Console.WriteLine($"POST uri: {uri}");
            return await Client.PostAsync(uri, content);
        }

        /// <summary>
        /// Sends a DELETE request to the specified URI.
        /// </summary>
        /// <param name="uri">The URI to send the request to.</param>
        /// <returns>The HTTP response message.</returns>
        public async Task<HttpResponseMessage> DeleteAsync(string uri)
        {
            if (Verbose) Console.WriteLine($"DELETE uri: {uri}");
            return await Client.DeleteAsync(uri);
        }
    }
}
