// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// GitHubClient.cs
//
// GitHub API client for authentication verification and basic operations.
// Reuses GitHubRelease authentication logic for token retrieval.


using System;
using Nbuild.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO.Compression;
using System.Text;

namespace Sdo.Services
{
    /// <summary>
    /// GitHub API client for authentication and basic operations.
    /// </summary>
    public class GitHubClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string? _overrideToken;
        private readonly bool _disposeHttpClient;

        /// <summary>
        /// Initializes a new instance of the GitHubClient class.
        /// </summary>
        /// <param name="token">Optional token override for testing. If null, uses standard authentication sources.</param>
        public GitHubClient(string? token = null)
        {
            _overrideToken = token;
            _httpClient = new HttpClient();
            _disposeHttpClient = true;
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
        /// Constructs a GitHubClient using an externally provided HttpClient.
        /// This is useful for unit testing where a mocked HttpMessageHandler can be supplied.
        /// </summary>
        /// <param name="httpClient">Pre-configured HttpClient instance.</param>
        /// <param name="token">Optional token override.</param>
        public GitHubClient(HttpClient httpClient, string? token = null)
        {
            _overrideToken = token;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = false; // caller owns the provided HttpClient
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

        public async Task<List<GitHubIssue>?> ListIssuesAsync(string owner, string repo, int perPage = 50, string state = "open", int top = 0)
        {
            try
            {
                var allIssues = new List<GitHubIssue>();
                int page = 1;

                int pageSize = perPage < 1 ? 1 : (perPage > 100 ? 100 : perPage);

                while (true)
                {
                    var url = $"https://api.github.com/repos/{owner}/{repo}/issues?per_page={pageSize}&page={page}&state={state}";
                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        ConsoleHelper.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}", ConsoleColor.Red);
                        return allIssues.Count > 0 ? allIssues : null;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var issues = JsonSerializer.Deserialize<List<GitHubIssue>>(content, options);

                    if (issues == null || issues.Count == 0)
                    {
                        break;
                    }

                    var filteredIssues = issues.Where(i => i.PullRequest == null).ToList();

                    allIssues.AddRange(filteredIssues);

                    if (top > 0 && allIssues.Count >= top)
                    {
                        allIssues = allIssues.Take(top).ToList();
                        break;
                    }

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

        public async Task<GitHubIssue?> UpdateIssueAsync(string owner, string repo, int issueNumber,
            string? title = null, string? state = null, string? body = null, string? assignee = null)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}";

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

        public async Task<List<Models.Repository>?> ListRepositoriesAsync(string? organization = null,
            string visibility = "all", int perPage = 30, int top = 0)
        {
            try
            {
                perPage = Math.Max(1, Math.Min(100, perPage));

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
                        break;
                    }

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

                    if (top > 0 && allRepos.Count >= top)
                    {
                        allRepos = allRepos.Take(top).ToList();
                        break;
                    }

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

        public async Task<List<Models.PullRequest>?> ListPullRequestsAsync(string owner, string repo,
            string state = "open", int perPage = 30, int top = 0)
        {
            try
            {
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
                        break;
                    }

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

                    if (top > 0 && allPRs.Count >= top)
                    {
                        allPRs = allPRs.Take(top).ToList();
                        break;
                    }

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

                    var status = check.Conclusion ?? "pending";
                    if (check.Status == "in_progress")
                    {
                        status = "running";
                    }

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

        public async Task<List<GitHubWorkflow>?> ListWorkflowsAsync(string owner, string repo)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/actions/workflows";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var workflowsResponse = JsonSerializer.Deserialize<WorkflowsResponse>(content, options);

                return workflowsResponse?.Workflows;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching GitHub workflows: {ex.Message}");
                return null;
            }
        }

        public async Task<List<GitHubWorkflowRun>?> ListWorkflowRunsAsync(string owner, string repo, long? workflowId = null, int perPage = 10)
        {
            try
            {
                var safePerPage = Math.Max(1, Math.Min(100, perPage));
                var url = workflowId.HasValue
                    ? $"https://api.github.com/repos/{owner}/{repo}/actions/workflows/{workflowId.Value}/runs?per_page={safePerPage}"
                    : $"https://api.github.com/repos/{owner}/{repo}/actions/runs?per_page={safePerPage}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error listing workflow runs: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var runsResponse = JsonSerializer.Deserialize<GitHubWorkflowRunsResponse>(content, options);
                return runsResponse?.WorkflowRuns;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing workflow runs: {ex.Message}");
                return null;
            }
        }

        public async Task<GitHubWorkflowRun?> GetWorkflowRunAsync(string owner, string repo, long runId)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/actions/runs/{runId}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error getting workflow run {runId}: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<GitHubWorkflowRun>(content, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting workflow run {runId}: {ex.Message}");
                return null;
            }
        }

        public async Task<GitHubWorkflow?> GetWorkflowAsync(string owner, string repo, long workflowId)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/actions/workflows/{workflowId}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error getting workflow {workflowId}: {response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<GitHubWorkflow>(content, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting workflow {workflowId}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> TriggerWorkflowAsync(string owner, string repo, long workflowId, string @ref, Dictionary<string, string>? inputs = null)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches";
                var body = new Dictionary<string, object>
                {
                    ["ref"] = @ref
                };
                if (inputs != null && inputs.Any())
                {
                    body["inputs"] = inputs;
                }

                var content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error triggering workflow {workflowId}: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GetWorkflowLogsAsync(string owner, string repo, long runId)
        {
            try
            {
                var url = $"https://api.github.com/repos/{owner}/{repo}/actions/runs/{runId}/logs";
                return await DownloadAndExtractLogsAsync(url);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting workflow logs for run {runId}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateWorkflowAsync(string owner, string repo, string workflowName, string yamlFilePath)
        {
            try
            {
                if (!File.Exists(yamlFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"YAML file not found: {yamlFilePath}");
                    return false;
                }

                var yamlContent = await File.ReadAllTextAsync(yamlFilePath);

                if (string.IsNullOrWhiteSpace(yamlContent))
                {
                    System.Diagnostics.Debug.WriteLine("YAML file is empty");
                    return false;
                }

                var repoCheck = await GetRepositoryAsync(owner, repo);
                if (repoCheck == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Repository {owner}/{repo} not found or not accessible");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating workflow creation: {ex.Message}");
                return false;
            }
        }
        // For brevity the file keeps the original client methods.

        /// <summary>
        /// Disposes the HTTP client if this instance owns it.
        /// </summary>
        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        /// <summary>
        /// Downloads and extracts the log content from a GitHub Actions logs_url (zip file).
        /// </summary>
        /// <param name="logsUrl">The logs_url from the workflow run.</param>
        /// <returns>Concatenated log text, or null if failed.</returns>
        public async Task<string?> DownloadAndExtractLogsAsync(string logsUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(logsUrl);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GitHub API error downloading logs: {response.StatusCode}");
                    return null;
                }

                using var zipStream = await response.Content.ReadAsStreamAsync();
                using var archive = new ZipArchive(zipStream);
                var allText = new StringBuilder();
                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0) continue;
                    using var entryStream = entry.Open();
                    using var reader = new StreamReader(entryStream);
                    allText.AppendLine($"===== {entry.FullName} =====");
                    allText.AppendLine(await reader.ReadToEndAsync());
                }
                return allText.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting GitHub logs: {ex.Message}");
                return null;
            }
        }
    }
}
