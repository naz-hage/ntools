// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// GitHubClient.cs
//
// GitHub API client for authentication verification and basic operations.
// Reuses GitHubRelease authentication logic for token retrieval.

using System.Net.Http.Headers;
using System.Text.Json;

namespace Sdo.Services
{
    /// <summary>
    /// GitHub API client for authentication and basic operations.
    /// </summary>
    public class GitHubClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string? _overrideToken;

        /// <summary>
        /// Initializes a new instance of the GitHubClient class.
        /// </summary>
        /// <param name="token">Optional token override for testing. If null, uses standard authentication sources.</param>
        public GitHubClient(string? token = null)
        {
            _overrideToken = token;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("sdo-cli", "1.0"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            // Set authorization header
            var authToken = _overrideToken ?? GitHubRelease.Credentials.GetTokenOrDefault();
            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        /// <summary>
        /// Verifies the authentication token by making a request to the GitHub API.
        /// </summary>
        /// <returns>True if authentication is successful, false otherwise.</returns>
        public async Task<bool> VerifyAuthenticationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://api.github.com/user");
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // Additional check: try to parse the response to ensure it's valid JSON
                var content = await response.Content.ReadAsStringAsync();
                return content.Contains("\"login\"") || content.Contains("\"id\"");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the authenticated user's information.
        /// </summary>
        /// <returns>The user information, or null if request fails.</returns>
        public async Task<GitHubUser?> GetUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://api.github.com/user");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GitHubUser>(content);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Represents a GitHub user.
    /// </summary>
    public class GitHubUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? Login { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? Email { get; set; }
    }
}