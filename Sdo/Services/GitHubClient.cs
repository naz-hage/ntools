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
        /// Gets a specific GitHub issue.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="issueNumber">Issue number.</param>
        /// <returns>The GitHub issue, or null if not found.</returns>
        public async Task<GitHubIssue?> GetIssueAsync(string owner, string repo, int issueNumber)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var issue = JsonSerializer.Deserialize<GitHubIssue>(content, options);
                return issue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching GitHub issue: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lists GitHub issues in a repository.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="perPage">Number of issues to return (max 100).</param>
        /// <returns>List of GitHub issues.</returns>
        public async Task<List<GitHubIssue>?> ListIssuesAsync(string owner, string repo, int perPage = 50)
        {
            try
            {
                var allIssues = new List<GitHubIssue>();
                int page = 1;
                int pageSize = 100; // GitHub API max is 100 per page
                
                // Fetch all pages (no limit on total count)
                while (true)
                {
                    var url = $"https://api.github.com/repos/{owner}/{repo}/issues?per_page={pageSize}&page={page}&state=all";
                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                        // Return what we have so far if first page succeeded
                        return allIssues.Count > 0 ? allIssues : null;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var issues = JsonSerializer.Deserialize<List<GitHubIssue>>(content, options);

                    if (issues == null || issues.Count == 0)
                    {
                        // No more results
                        break;
                    }

                    allIssues.AddRange(issues);
                    
                    // If we got fewer items than pageSize, we've reached the end
                    if (issues.Count < pageSize)
                    {
                        break;
                    }

                    page++;
                }

                return allIssues;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing GitHub issues: {ex.Message}");
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

    /// <summary>
    /// Represents a GitHub issue.
    /// </summary>
    public class GitHubIssue
    {
        /// <summary>
        /// Gets or sets the issue ID (using long to support large GitHub issue IDs).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the issue number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the issue title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the issue body/description.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the issue state (open, closed).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the number of comments.
        /// </summary>
        public int Comments { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update date.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the issue.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the labels assigned to the issue.
        /// </summary>
        public List<GitHubLabel>? Labels { get; set; }

        /// <summary>
        /// Gets or sets the assignee of the issue.
        /// </summary>
        public GitHubUser? Assignee { get; set; }
    }

    /// <summary>
    /// Represents a GitHub issue label.
    /// </summary>
    public class GitHubLabel
    {
        /// <summary>
        /// Gets or sets the label name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the label description.
        /// </summary>
        public string? Description { get; set; }
    }
}