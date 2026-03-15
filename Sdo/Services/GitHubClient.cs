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
        /// <param name="perPage">Number of issues to return (clamped to 1-100). Defaults to 50.</param>
        /// <param name="state">Issue state filter: 'open', 'closed', or 'all'. Defaults to 'open'.</param>
        /// <param name="top">Maximum total issues to retrieve. 0 means no limit.</param>
        /// <returns>List of GitHub issues (excluding pull requests).</returns>
        public async Task<List<GitHubIssue>?> ListIssuesAsync(string owner, string repo, int perPage = 50, string state = "open", int top = 0)
        {
            try
            {
                var allIssues = new List<GitHubIssue>();
                int page = 1;

                // Clamp perPage to GitHub API limits (1-100)
                int pageSize = perPage < 1 ? 1 : (perPage > 100 ? 100 : perPage);

                // Fetch pages until we have enough items or reach the end
                while (true)
                {
                    var url = $"https://api.github.com/repos/{owner}/{repo}/issues?per_page={pageSize}&page={page}&state={state}";
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

                    // Filter out pull requests (they have the pull_request field)
                    var filteredIssues = issues.Where(i => i.PullRequest == null).ToList();

                    allIssues.AddRange(filteredIssues);

                    // Check if we've reached the requested limit
                    if (top > 0 && allIssues.Count >= top)
                    {
                        // Trim to exact count
                        allIssues = allIssues.Take(top).ToList();
                        break;
                    }

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
        /// Updates a GitHub issue.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="issueNumber">Issue number.</param>
        /// <param name="title">New issue title (optional).</param>
        /// <param name="state">New issue state: 'open' or 'closed' (optional).</param>
        /// <param name="body">New issue body/description (optional).</param>
        /// <param name="assignee">New assignee login name (optional).</param>
        /// <returns>The updated GitHub issue, or null if update failed.</returns>
        public async Task<GitHubIssue?> UpdateIssueAsync(string owner, string repo, int issueNumber,
            string? title = null, string? state = null, string? body = null, string? assignee = null)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}";

                // Build request body with only provided fields
                var updateData = new Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(title))
                    updateData["title"] = title;
                if (!string.IsNullOrEmpty(state))
                    updateData["state"] = state;
                if (!string.IsNullOrEmpty(body))
                    updateData["body"] = body;
                if (!string.IsNullOrEmpty(assignee))
                    updateData["assignee"] = assignee;

                var content = JsonSerializer.Serialize(updateData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var issue = JsonSerializer.Deserialize<GitHubIssue>(responseContent, options);
                return issue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating GitHub issue: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a comment to a GitHub issue.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="issueNumber">Issue number.</param>
        /// <param name="body">Comment text.</param>
        /// <returns>True if comment was added successfully, false otherwise.</returns>
        public async Task<bool> AddCommentAsync(string owner, string repo, int issueNumber, string body)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}/comments";

                var commentData = new { body };
                var content = JsonSerializer.Serialize(commentData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding GitHub issue comment: {ex.Message}");
                return false;
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

        /// <summary>
        /// Gets or sets the pull request object (null for issues, non-null for PRs).
        /// Used to distinguish between issues and pull requests.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("pull_request")]
        public object? PullRequest { get; set; }
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