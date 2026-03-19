// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// GitHubClient.cs
//
// GitHub API client for authentication verification and basic operations.
// Reuses GitHubRelease authentication logic for token retrieval.

using Nbuild;
using Nbuild.Helpers;
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
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", authToken);
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
                        ConsoleHelper.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}", ConsoleColor.Red);
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
        /// Creates a new GitHub issue.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="title">Issue title (required).</param>
        /// <param name="body">Issue body/description (optional).</param>
        /// <param name="labels">Labels to assign to the issue (optional).</param>
        /// <param name="assignee">Assignee login name (optional).</param>
        /// <returns>The created GitHub issue, or null if creation failed.</returns>
        public async Task<GitHubIssue?> CreateIssueAsync(string owner, string repo, string title, 
            string? body = null, string[]? labels = null, string? assignee = null)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/issues";

                var issueData = new Dictionary<string, object>
                {
                    { "title", title }
                };

                if (!string.IsNullOrEmpty(body))
                    issueData["body"] = body;

                if (labels != null && labels.Length > 0)
                    issueData["labels"] = labels;

                if (!string.IsNullOrEmpty(assignee))
                    issueData["assignee"] = assignee;

                var content = JsonSerializer.Serialize(issueData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

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
                System.Diagnostics.Debug.WriteLine($"Error creating GitHub issue: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lists repositories for the authenticated user or a specific organization.
        /// </summary>
        /// <param name="organization">Optional organization name. If null, lists authenticated user's repositories.</param>
        /// <param name="visibility">Filter by visibility: 'all', 'public', or 'private'. Default: 'all'.</param>
        /// <param name="perPage">Number of results per page (1-100). Default: 30.</param>
        /// <param name="top">Maximum number of results to return. Default: 0 (all).</param>
        /// <returns>List of repositories, or null if operation failed.</returns>
        public async Task<List<Models.Repository>?> ListRepositoriesAsync(string? organization = null,
            string visibility = "all", int perPage = 30, int top = 0)
        {
            try
            {
                // Clamp perPage to valid GitHub API range (1-100)
                perPage = Math.Max(1, Math.Min(100, perPage));

                // Determine the URL based on whether we're querying user or org repos
                string baseUrl = string.IsNullOrEmpty(organization)
                    ? "https://api.github.com/user/repos"
                    : $"https://api.github.com/orgs/{organization}/repos";

                var allRepos = new List<Models.Repository>();
                int page = 1;

                while (true)
                {
                    var url = $"{baseUrl}?page={page}&per_page={perPage}";
                    if (visibility != "all")
                    {
                        url += $"&visibility={visibility}";
                    }

                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                        return null;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var repos = JsonSerializer.Deserialize<List<GitHubRepositoryResponse>>(content, options);

                    if (repos == null || repos.Count == 0)
                    {
                        // No more results
                        break;
                    }

                    // Convert to Repository model
                    allRepos.AddRange(repos.Select(r => new Models.Repository
                    {
                        Name = r.Name,
                        Description = r.Description,
                        Owner = r.Owner?.Login,
                        Url = r.HtmlUrl,
                        IsPrivate = r.Private,
                        DefaultBranch = r.DefaultBranch,
                        PlatformId = r.Id.ToString()
                    }));

                    // Check if we've reached the requested limit
                    if (top > 0 && allRepos.Count >= top)
                    {
                        allRepos = allRepos.Take(top).ToList();
                        break;
                    }

                    // If we got fewer items than pageSize, we've reached the end
                    if (repos.Count < perPage)
                    {
                        break;
                    }

                    page++;
                }

                return allRepos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing GitHub repositories: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets details for a specific repository.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <returns>Repository details, or null if not found.</returns>
        public async Task<Models.Repository?> GetRepositoryAsync(string owner, string repo)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var repoData = JsonSerializer.Deserialize<GitHubRepositoryResponse>(content, options);

                if (repoData == null)
                {
                    return null;
                }

                return new Models.Repository
                {
                    Name = repoData.Name,
                    Description = repoData.Description,
                    Owner = repoData.Owner?.Login,
                    Url = repoData.HtmlUrl,
                    IsPrivate = repoData.Private,
                    DefaultBranch = repoData.DefaultBranch,
                    PlatformId = repoData.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting GitHub repository: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a new repository for the authenticated user.
        /// </summary>
        /// <param name="name">Repository name (required).</param>
        /// <param name="description">Repository description (optional).</param>
        /// <param name="isPrivate">Whether the repository should be private.</param>
        /// <returns>Created repository details, or null if creation failed.</returns>
        public async Task<Models.Repository?> CreateRepositoryAsync(string name, string? description = null, bool isPrivate = false)
        {
            try
            {
                var url = "https://api.github.com/user/repos";

                var createData = new
                {
                    name,
                    description,
                    @private = isPrivate,
                    auto_init = true
                };

                var content = JsonSerializer.Serialize(createData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"GitHub API error ({response.StatusCode}): {response.ReasonPhrase}. {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var repoData = JsonSerializer.Deserialize<GitHubRepositoryResponse>(responseContent, options);

                if (repoData == null)
                {
                    throw new InvalidOperationException("Failed to parse GitHub API response");
                }

                return new Models.Repository
                {
                    Name = repoData.Name,
                    Description = repoData.Description,
                    Owner = repoData.Owner?.Login,
                    Url = repoData.HtmlUrl,
                    IsPrivate = repoData.Private,
                    DefaultBranch = repoData.DefaultBranch,
                    PlatformId = repoData.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating GitHub repository: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a repository.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        public async Task<bool> DeleteRepositoryAsync(string owner, string repo)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"GitHub API error ({response.StatusCode}): {response.ReasonPhrase}. {errorContent}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting GitHub repository: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates repository settings.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="description">New repository description (optional).</param>
        /// <param name="isPrivate">New privacy setting (optional).</param>
        /// <returns>Updated repository details, or null if update failed.</returns>
        public async Task<Models.Repository?> UpdateRepositoryAsync(string owner, string repo,
            string? description = null, bool? isPrivate = null)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}";

                var updateData = new Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(description))
                {
                    updateData["description"] = description;
                }
                if (isPrivate.HasValue)
                {
                    updateData["private"] = isPrivate.Value;
                }

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
                var repoData = JsonSerializer.Deserialize<GitHubRepositoryResponse>(responseContent, options);

                if (repoData == null)
                {
                    return null;
                }

                return new Models.Repository
                {
                    Name = repoData.Name,
                    Description = repoData.Description,
                    Owner = repoData.Owner?.Login,
                    Url = repoData.HtmlUrl,
                    IsPrivate = repoData.Private,
                    DefaultBranch = repoData.DefaultBranch,
                    PlatformId = repoData.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating GitHub repository: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a new pull request.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="title">Pull request title (required).</param>
        /// <param name="body">Pull request description (optional).</param>
        /// <param name="head">Branch name or commit SHA to merge from (required).</param>
        /// <param name="baseRef">Branch name to merge into (required).</param>
        /// <param name="draft">Whether the PR should be a draft (optional).</param>
        /// <returns>Created pull request details, or null if creation failed.</returns>
        public async Task<Models.PullRequest?> CreatePullRequestAsync(string owner, string repo, string title,
            string head, string baseRef, string? body = null, bool draft = false)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/pulls";

                var createData = new
                {
                    title,
                    head,
                    @base = baseRef,
                    body,
                    draft
                };

                var content = JsonSerializer.Serialize(createData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"GitHub API error ({response.StatusCode}): {response.ReasonPhrase}. {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prData = JsonSerializer.Deserialize<GitHubPullRequest>(responseContent, options);

                if (prData == null)
                {
                    throw new InvalidOperationException("Failed to parse GitHub API response");
                }

                return new Models.PullRequest
                {
                    Number = prData.Number,
                    Title = prData.Title,
                    Description = prData.Body,
                    Status = prData.State,
                    Url = prData.HtmlUrl,
                    Author = prData.User?.Login,
                    SourceBranch = prData.Head?.Ref,
                    HeadSha = prData.Head?.Sha,
                    TargetBranch = prData.Base?.Ref,
                    CreatedAt = prData.CreatedAt,
                    UpdatedAt = prData.UpdatedAt,
                    IsDraft = prData.Draft,
                    MergedAt = prData.MergedAt,
                    IsMerged = prData.Merged
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating GitHub pull request: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets details for a specific pull request.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="prNumber">Pull request number.</param>
        /// <returns>Pull request details, or null if not found.</returns>
        public async Task<Models.PullRequest?> GetPullRequestAsync(string owner, string repo, int prNumber)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prData = JsonSerializer.Deserialize<GitHubPullRequest>(content, options);

                if (prData == null)
                {
                    return null;
                }

                return new Models.PullRequest
                {
                    Number = prData.Number,
                    Title = prData.Title,
                    Description = prData.Body,
                    Status = prData.State,
                    Url = prData.HtmlUrl,
                    Author = prData.User?.Login,
                    SourceBranch = prData.Head?.Ref,
                    HeadSha = prData.Head?.Sha,
                    TargetBranch = prData.Base?.Ref,
                    CreatedAt = prData.CreatedAt,
                    UpdatedAt = prData.UpdatedAt,
                    IsDraft = prData.Draft,
                    MergedAt = prData.MergedAt,
                    IsMerged = prData.Merged
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting GitHub pull request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lists pull requests for a repository.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="state">Filter by state: 'open', 'closed', or 'all'. Default: 'open'.</param>
        /// <param name="perPage">Number of results per page (1-100). Default: 30.</param>
        /// <param name="top">Maximum number of results to return. Default: 0 (all).</param>
        /// <returns>List of pull requests, or null if operation failed.</returns>
        public async Task<List<Models.PullRequest>?> ListPullRequestsAsync(string owner, string repo,
            string state = "open", int perPage = 30, int top = 0)
        {
            try
            {
                // Clamp perPage to GitHub API limits (1-100)
                perPage = Math.Max(1, Math.Min(100, perPage));

                var allPRs = new List<Models.PullRequest>();
                int page = 1;

                while (true)
                {
                    var url = $"https://api.github.com/repos/{owner}/{repo}/pulls?per_page={perPage}&page={page}&state={state}";
                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                        return null;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var prs = JsonSerializer.Deserialize<List<GitHubPullRequest>>(content, options);

                    if (prs == null || prs.Count == 0)
                    {
                        // No more results
                        break;
                    }

                    // Convert to PullRequest model
                    allPRs.AddRange(prs.Select(pr => new Models.PullRequest
                    {
                        Number = pr.Number,
                        Title = pr.Title,
                        Description = pr.Body,
                        Status = pr.State,
                        Url = pr.HtmlUrl,
                        Author = pr.User?.Login,
                        SourceBranch = pr.Head?.Ref,
                        HeadSha = pr.Head?.Sha,
                        TargetBranch = pr.Base?.Ref,
                        CreatedAt = pr.CreatedAt,
                        UpdatedAt = pr.UpdatedAt,
                        IsDraft = pr.Draft,
                        MergedAt = pr.MergedAt,
                        IsMerged = pr.Merged
                    }));

                    // Check if we've reached the requested limit
                    if (top > 0 && allPRs.Count >= top)
                    {
                        allPRs = allPRs.Take(top).ToList();
                        break;
                    }

                    // If we got fewer items than perPage, we've reached the end
                    if (prs.Count < perPage)
                    {
                        break;
                    }

                    page++;
                }

                return allPRs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing GitHub pull requests: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Approves a pull request by creating a review.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="prNumber">Pull request number.</param>
        /// <returns>True if approval was successful, false otherwise.</returns>
        public async Task<bool> ApprovePullRequestAsync(string owner, string repo, int prNumber)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}/reviews";

                var reviewData = new
                {
                    @event = "APPROVE"
                };

                var content = JsonSerializer.Serialize(reviewData);
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
                System.Diagnostics.Debug.WriteLine($"Error approving GitHub pull request: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Merges a pull request.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="prNumber">Pull request number.</param>
        /// <param name="mergeMethod">Merge method: 'merge', 'squash', or 'rebase'. Default: 'merge'.</param>
        /// <returns>True if merge was successful, false otherwise.</returns>
        public async Task<bool> MergePullRequestAsync(string owner, string repo, int prNumber,
            string mergeMethod = "merge")
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}/merge";

                var mergeData = new
                {
                    merge_method = mergeMethod
                };

                var content = JsonSerializer.Serialize(mergeData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error merging GitHub pull request: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates a pull request.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="prNumber">Pull request number.</param>
        /// <param name="title">New pull request title (optional).</param>
        /// <param name="body">New pull request body (optional).</param>
        /// <param name="state">New pull request state: 'open' or 'closed' (optional).</param>
        /// <returns>Updated pull request details, or null if update failed.</returns>
        public async Task<Models.PullRequest?> UpdatePullRequestAsync(string owner, string repo, int prNumber,
            string? title = null, string? body = null, string? state = null)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}";

                var updateData = new Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(title))
                    updateData["title"] = title;
                if (!string.IsNullOrEmpty(body))
                    updateData["body"] = body;
                if (!string.IsNullOrEmpty(state))
                    updateData["state"] = state;

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
                var prData = JsonSerializer.Deserialize<GitHubPullRequest>(responseContent, options);

                if (prData == null)
                {
                    return null;
                }

                return new Models.PullRequest
                {
                    Number = prData.Number,
                    Title = prData.Title,
                    Description = prData.Body,
                    Status = prData.State,
                    Url = prData.HtmlUrl,
                    Author = prData.User?.Login,
                    SourceBranch = prData.Head?.Ref,
                    HeadSha = prData.Head?.Sha,
                    TargetBranch = prData.Base?.Ref,
                    CreatedAt = prData.CreatedAt,
                    UpdatedAt = prData.UpdatedAt,
                    IsDraft = prData.Draft,
                    MergedAt = prData.MergedAt,
                    IsMerged = prData.Merged
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating GitHub pull request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets check runs for a specific commit/PR head.
        /// </summary>
        /// <param name="owner">Repository owner.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="headSha">The commit SHA of the PR head.</param>
        /// <returns>List of check runs with name, status, and URL, or null if operation failed.</returns>
        public async Task<List<(string Name, string Status, string Duration, string Url)>?> GetCheckRunsAsync(
            string owner, string repo, string headSha)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/commits/{headSha}/check-runs";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error getting check runs: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<CheckRunsResponse>(content, options);

                if (result?.CheckRuns == null || result.CheckRuns.Count == 0)
                {
                    return new List<(string, string, string, string)>();
                }

                var checks = new List<(string, string, string, string)>();
                foreach (var check in result.CheckRuns)
                {
                    // Calculate duration
                    string duration = "pending";
                    if (check.CompletedAt.HasValue && check.StartedAt.HasValue)
                    {
                        var span = check.CompletedAt.Value - check.StartedAt.Value;
                        if (span.TotalSeconds < 60)
                        {
                            duration = $"{span.TotalSeconds:F0}s";
                        }
                        else if (span.TotalMinutes < 60)
                        {
                            duration = $"{span.TotalMinutes:F0}m{span.Seconds}s";
                        }
                        else
                        {
                            duration = $"{span.Hours}h{span.Minutes}m";
                        }
                    }

                    // Map conclusion to status
                    var status = check.Conclusion ?? "pending";
                    if (check.Status == "in_progress")
                    {
                        status = "running";
                    }

                    // Get the details URL - GitHub checks have a details_url
                    var checkUrl = check.DetailsUrl ?? $"https://github.com/{owner}/{repo}/pull";

                    checks.Add((check.Name ?? "Unknown", status, duration, checkUrl));
                }

                return checks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting check runs: {ex.Message}");
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
    /// Represents a GitHub repository API response.
    /// </summary>
    public class GitHubRepositoryResponse
    {
        /// <summary>
        /// Gets or sets the repository ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the repository description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the repository is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// Gets or sets the default branch name.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the repository.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public GitHubUser? Owner { get; set; }

        /// <summary>
        /// Gets or sets the number of stargazers.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of watchers.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("watchers_count")]
        public int WatchersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of forks.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("forks_count")]
        public int ForksCount { get; set; }

        /// <summary>
        /// Gets or sets the repository topics/tags.
        /// </summary>
        public List<string>? Topics { get; set; }
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

    /// <summary>
    /// Represents a GitHub pull request API response.
    /// </summary>
    public class GitHubPullRequest
    {
        /// <summary>
        /// Gets or sets the pull request ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the pull request number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the pull request body/description.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the pull request state (open, closed).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets whether the pull request is a draft.
        /// </summary>
        public bool Draft { get; set; }

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
        /// Gets or sets the merge date (if merged).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("merged_at")]
        public DateTime? MergedAt { get; set; }

        /// <summary>
        /// Gets or sets whether the pull request is merged.
        /// </summary>
        public bool Merged { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the pull request.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the pull request author/user.
        /// </summary>
        public GitHubUser? User { get; set; }

        /// <summary>
        /// Gets or sets the source branch information.
        /// </summary>
        public GitHubRef? Head { get; set; }

        /// <summary>
        /// Gets or sets the target branch information.
        /// </summary>
        public GitHubRef? Base { get; set; }
    }

    /// <summary>
    /// Represents a GitHub ref (branch) reference.
    /// </summary>
    public class GitHubRef
    {
        /// <summary>
        /// Gets or sets the ref name (branch name).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("ref")]
        public string? Ref { get; set; }

        /// <summary>
        /// Gets or sets the commit SHA.
        /// </summary>
        public string? Sha { get; set; }
    }

    /// <summary>
    /// Represents the check runs response from GitHub API.
    /// </summary>
    public class CheckRunsResponse
    {
        /// <summary>
        /// Gets or sets the list of check runs.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("check_runs")]
        public List<CheckRun>? CheckRuns { get; set; }
    }

    /// <summary>
    /// Represents a GitHub check run.
    /// </summary>
    public class CheckRun
    {
        /// <summary>
        /// Gets or sets the check run name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the check run status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the check run conclusion.
        /// </summary>
        public string? Conclusion { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the completion time.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the details URL.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("details_url")]
        public string? DetailsUrl { get; set; }
    }
}