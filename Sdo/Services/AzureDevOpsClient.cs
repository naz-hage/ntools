// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AzureDevOpsClient.cs
//
// Azure DevOps API client for authentication verification and basic operations.

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Azure DevOps API client for authentication and basic operations.
    /// </summary>
    public class AzureDevOpsClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _pat;
        private readonly string _organization;
        private readonly string? _project;
        private string? _lastError;

        /// <summary>
        /// Gets the last error message from API calls.
        /// </summary>
        public string? LastError => _lastError;

        /// <summary>
        /// Initializes a new instance of the AzureDevOpsClient class.
        /// </summary>
        /// <param name="pat">The Azure DevOps Personal Access Token.</param>
        /// <param name="organization">The Azure DevOps organization name.</param>
        /// <param name="project">The Azure DevOps project name (optional).</param>
        public AzureDevOpsClient(string pat, string organization, string? project = null)
        {
            _pat = pat;
            _organization = organization;
            _project = project;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{_pat}")));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Verifies the authentication token by making a request to the Azure DevOps API.
        /// </summary>
        /// <returns>True if authentication is successful, false otherwise.</returns>
        public async Task<bool> VerifyAuthenticationAsync()
        {
            try
            {
                // Use projects API to verify authentication, similar to Python SDO
                var url = $"https://dev.azure.com/{_organization}/_apis/projects?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                // Check for authentication/authorization failures
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    response.StatusCode == System.Net.HttpStatusCode.NonAuthoritativeInformation)
                {
                    Console.WriteLine($"Debug: API returned {response.StatusCode} - authentication/authorization failed");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Debug: Error response content: {errorContent}");
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Debug: API returned {response.StatusCode} - not successful");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Debug: Error response: {errorContent}");

                    // If we get a 500 error, it might be a service issue, not auth issue
                    // Let's try a different approach - check if the PAT format is correct by examining it
                    Console.WriteLine($"Debug: PAT length is {_pat?.Length ?? 0} characters");
                    if (_pat != null && _pat.Length < 50)
                    {
                        Console.WriteLine("Debug: Warning - PAT appears to be unusually short. Azure DevOps PATs are typically 52 characters.");
                    }

                    return false;
                }

                // If we get a successful response, authentication worked
                Console.WriteLine($"Debug: API returned {response.StatusCode} - authentication successful");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Debug: Exception during API call: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the authenticated user's information.
        /// </summary>
        /// <returns>The user information, or null if request fails.</returns>
        public async Task<AzureDevOpsUser?> GetUserAsync()
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/_apis/connectionData?api-version=7.1";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var connectionData = JsonSerializer.Deserialize<ConnectionData>(content);
                return connectionData?.AuthenticatedUser;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets multiple work items in a single batch API call (more efficient than individual calls).
        /// </summary>
        /// <param name="workItemIds">The work item IDs to fetch.</param>
        /// <returns>List of work items.</returns>
        private async Task<List<AzureDevOpsWorkItem>> GetWorkItemsBatchAsync(List<int> workItemIds)
        {
            try
            {
                if (workItemIds == null || workItemIds.Count == 0)
                {
                    return new List<AzureDevOpsWorkItem>();
                }

                // Azure DevOps batch API: GET /workitems?ids=1,2,3,...&$expand=all
                var idsString = string.Join(",", workItemIds);
                var url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems?ids={idsString}&api-version=7.0&$expand=all";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Batch API error: {response.StatusCode}");
                    return new List<AzureDevOpsWorkItem>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<BatchWorkItemsResponse>(content, options);

                if (result?.Value == null)
                {
                    return new List<AzureDevOpsWorkItem>();
                }

                // Convert WorkItemResponse objects to AzureDevOpsWorkItem
                return result.Value.Select(r => r.ToWorkItem()).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in batch fetch: {ex.Message}");
                return new List<AzureDevOpsWorkItem>();
            }
        }

        /// <summary>
        /// Gets a specific Azure DevOps work item.
        /// </summary>
        /// <param name="workItemId">The work item ID.</param>
        /// <returns>The work item, or null if not found.</returns>
        public async Task<AzureDevOpsWorkItem?> GetWorkItemAsync(int workItemId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems/{workItemId}?api-version=7.0&$expand=all";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var workItem = JsonSerializer.Deserialize<WorkItemResponse>(content, options);
                
                return workItem?.ToWorkItem();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lists Azure DevOps work items.
        /// </summary>
        /// <param name="top">Maximum number of items to return.</param>
        /// <returns>List of work items.</returns>
        public async Task<List<AzureDevOpsWorkItem>?> ListWorkItemsAsync(int top = 50)
        {
            try
            {
                // Use WIQL query to get recent work items
                var wiql = new
                {
                    query = $"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [System.CreatedDate], [System.ChangedDate] FROM WorkItems ORDER BY [System.ChangedDate] DESC"
                };

                // Include project in URL if specified
                string url;
                if (!string.IsNullOrEmpty(_project))
                {
                    url = $"https://dev.azure.com/{_organization}/{_project}/_apis/wit/wiql?api-version=7.0";
                }
                else
                {
                    url = $"https://dev.azure.com/{_organization}/_apis/wit/wiql?api-version=7.0";
                }

                var content = System.Text.Json.JsonSerializer.Serialize(wiql);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] URL: {url}");
                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] WIQL Query: {wiql.query}");

                var response = await _httpClient.PostAsync(url, request);

                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"API Error {response.StatusCode}: {response.ReasonPhrase}\nURL: {url}\nResponse: {errorContent}";
                    System.Diagnostics.Debug.WriteLine(_lastError);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] Response Content Length: {responseContent.Length}");
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var queryResult = JsonSerializer.Deserialize<WorkItemQueryResult>(responseContent, options);

                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] WorkItems Count: {queryResult?.WorkItems?.Count ?? 0}");

                if (queryResult?.WorkItems == null || !queryResult.WorkItems.Any())
                {
                    return new List<AzureDevOpsWorkItem>();
                }

                // Fetch full details using batch API (more efficient than sequential calls)
                var workItemIds = queryResult.WorkItems.Take(top).Select(w => w.Id).ToList();
                var workItems = await GetWorkItemsBatchAsync(workItemIds);

                return workItems;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error listing Azure DevOps work items: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Gets project details to verify project access.
        /// </summary>
        public async Task<bool> VerifyProjectAccessAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_project))
                {
                    return false;
                }

                var url = $"https://dev.azure.com/{_organization}/_apis/projects/{_project}?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Project access failed: {response.StatusCode} {response.ReasonPhrase} - {errorContent}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Project verification failed: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Gets the current token's scope information.
        /// </summary>
        /// <returns>Dictionary of scope information, or null if unable to retrieve.</returns>
        public async Task<Dictionary<string, object>?> GetTokenInfoAsync()
        {
            try
            {
                // Query the profile/me endpoint to get current user/token info
                var url = $"https://dev.azure.com/{_organization}/_apis/profile/me?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Failed to get token info: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var info = JsonSerializer.Deserialize<Dictionary<string, object>?>(content, options);
                return info;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception getting token info: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Gets the scope/permission information for the current token.
        /// For non-JWT tokens, queries the API to determine access level.
        /// </summary>
        /// <returns>A dictionary with scope/permission information, or null if unable to retrieve.</returns>
        public async Task<Dictionary<string, string>?> GetTokenScopesAsync()
        {
            try
            {
                // For Azure DevOps PAT tokens, we can't extract scopes from the token itself
                // Instead, we check what the token can access via API calls
                var result = new Dictionary<string, string>
                {
                    { "Token Type", "Azure DevOps Personal Access Token (PAT)" },
                    { "Organization", _organization }
                };

                // Test organization access
                var orgUrl = $"https://dev.azure.com/{_organization}/_apis/projects?api-version=7.0";
                var response = await _httpClient.GetAsync(orgUrl);
                result["Organization Access"] = response.IsSuccessStatusCode ? "✓ Allowed" : "✗ Denied";

                // Test project access if specified
                if (!string.IsNullOrEmpty(_project))
                {
                    var projectUrl = $"https://dev.azure.com/{_organization}/{_project}/_apis/teams?api-version=7.0";
                    var projectResponse = await _httpClient.GetAsync(projectUrl);
                    result["Project Access"] = projectResponse.IsSuccessStatusCode ? "✓ Allowed" : "✗ Denied";
                }

                // Test Work Item Query access - this is what fails in workitem list
                var wiqlUrl = $"https://dev.azure.com/{_organization}/_apis/wit/wiql?api-version=7.0";
                var wiqlRequest = new StringContent("{\"query\":\"SELECT [System.Id] FROM WorkItems LIMIT 0\"}", System.Text.Encoding.UTF8, "application/json");
                var wiqlResponse = await _httpClient.PostAsync(wiqlUrl, wiqlRequest);
                result["Work Item Query Access"] = wiqlResponse.IsSuccessStatusCode ? "✓ Allowed" : "✗ Denied";

                return result;
            }
            catch (Exception ex)
            {
                return new Dictionary<string, string>
                {
                    { "Error", $"Failed to check token permissions: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Updates a work item in Azure DevOps.
        /// </summary>
        /// <param name="workItemId">The work item ID.</param>
        /// <param name="title">New title (optional).</param>
        /// <param name="state">New state (optional).</param>
        /// <param name="assignee">New assignee email (optional).</param>
        /// <param name="description">New description (optional).</param>
        /// <param name="verbose">Enable verbose logging.</param>
        /// <returns>The updated work item, or null if update failed.</returns>
        public async Task<AzureDevOpsWorkItem?> UpdateWorkItemAsync(int workItemId, string? title = null,
            string? state = null, string? assignee = null, string? description = null, bool verbose = false)
        {
            try
            {
                // Build JSON patch operations for updated fields
                var operations = new List<object>();

                if (!string.IsNullOrEmpty(title))
                    operations.Add(new { op = "add", path = "/fields/System.Title", value = title });

                if (!string.IsNullOrEmpty(state))
                    operations.Add(new { op = "add", path = "/fields/System.State", value = state });

                if (!string.IsNullOrEmpty(description))
                    operations.Add(new { op = "add", path = "/fields/System.Description", value = description });

                if (!string.IsNullOrEmpty(assignee))
                    operations.Add(new { op = "add", path = "/fields/System.AssignedTo", value = assignee });

                if (operations.Count == 0)
                {
                    if (verbose)
                        Console.WriteLine("[VERBOSE] No fields to update");
                    return null;
                }

                var url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems/{workItemId}?api-version=7.0";

                var content = JsonSerializer.Serialize(operations);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json-patch+json");

                if (verbose)
                    Console.WriteLine($"[VERBOSE] Sending PATCH request to {url}");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Update failed: {response.StatusCode} {response.ReasonPhrase}\n{errorContent}";
                    if (verbose)
                        Console.WriteLine($"[VERBOSE] {_lastError}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var workItem = JsonSerializer.Deserialize<WorkItemResponse>(responseBody, options);

                return workItem?.ToWorkItem();
            }
            catch (Exception ex)
            {
                _lastError = $"Exception updating work item: {ex.Message}";
                if (verbose)
                    Console.WriteLine($"[VERBOSE] {_lastError}");
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Adds a comment to a work item in Azure DevOps.
        /// </summary>
        /// <param name="workItemId">The work item ID.</param>
        /// <param name="message">The comment message.</param>
        /// <param name="verbose">Enable verbose logging.</param>
        /// <returns>True if comment was added successfully, false otherwise.</returns>
        public async Task<bool> AddCommentAsync(int workItemId, string message, bool verbose = false)
        {
            try
            {
                // In Azure DevOps, comments are added via the discussion field using JSON patch
                var operations = new List<object>
                {
                    new { op = "add", path = "/fields/System.History", value = message }
                };

                var url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems/{workItemId}?api-version=7.0";

                var content = JsonSerializer.Serialize(operations);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json-patch+json");

                if (verbose)
                    Console.WriteLine($"[VERBOSE] Adding comment to work item {workItemId}");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Add comment failed: {response.StatusCode} {response.ReasonPhrase}\n{errorContent}";
                    if (verbose)
                        Console.WriteLine($"[VERBOSE] {_lastError}");
                    return false;
                }

                if (verbose)
                    Console.WriteLine("[VERBOSE] Comment added successfully");
                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception adding comment: {ex.Message}";
                if (verbose)
                    Console.WriteLine($"[VERBOSE] {_lastError}");
                System.Diagnostics.Debug.WriteLine(_lastError);
                return false;
            }
        }

        /// <summary>
        /// Lists all projects in the organization.
        /// </summary>
        /// <param name="top">Maximum number of projects to return. Default: 0 (all).</param>
        /// <param name="continuationToken">Token for pagination (optional).</param>
        /// <returns>List of projects, or null if operation failed.</returns>
        public async Task<List<AzureDevOpsProject>?> ListProjectsAsync(int top = 0, string? continuationToken = null)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/_apis/projects?api-version=7.0";
                if (top > 0)
                {
                    url += $"&$top={top}";
                }
                if (!string.IsNullOrEmpty(continuationToken))
                {
                    url += $"&continuationToken={continuationToken}";
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"List projects failed: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AzureDevOpsProjectListResponse>(content, options);

                if (result == null || result.Value == null)
                {
                    return new List<AzureDevOpsProject>();
                }

                return result.Value.Select(p => new AzureDevOpsProject
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Url = p.Url,
                    State = p.State
                }).ToList();
            }
            catch (Exception ex)
            {
                _lastError = $"Exception listing projects: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Gets details for a specific project.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <returns>Project details, or null if not found.</returns>
        public async Task<AzureDevOpsProject?> GetProjectAsync(string projectId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/_apis/projects/{projectId}?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Get project failed: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var projectData = JsonSerializer.Deserialize<AzureDevOpsProjectResponse>(content, options);

                if (projectData == null)
                {
                    return null;
                }

                return new AzureDevOpsProject
                {
                    Id = projectData.Id,
                    Name = projectData.Name,
                    Description = projectData.Description,
                    Url = projectData.Url,
                    State = projectData.State
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception getting project: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Lists repositories in a project.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="top">Maximum number of repositories to return. Default: 0 (all).</param>
        /// <returns>List of repositories, or null if operation failed.</returns>
        public async Task<List<Models.Repository>?> ListRepositoriesAsync(string projectId, int top = 0)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories?api-version=7.0";
                if (top > 0)
                {
                    url += $"&$top={top}";
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"List repositories failed: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AzureDevOpsRepositoryListResponse>(content, options);

                if (result == null || result.Value == null)
                {
                    return new List<Models.Repository>();
                }

                return result.Value.Select(r => new Models.Repository
                {
                    Name = r.Name,
                    Url = r.WebUrl,
                    DefaultBranch = r.DefaultBranch,
                    PlatformId = r.Id,
                    Owner = _organization
                }).ToList();
            }
            catch (Exception ex)
            {
                _lastError = $"Exception listing repositories: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Gets details for a specific repository.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <returns>Repository details, or null if not found.</returns>
        public async Task<Models.Repository?> GetRepositoryAsync(string projectId, string repositoryId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Get repository failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var repoData = JsonSerializer.Deserialize<AzureDevOpsRepositoryResponse>(content, options);

                if (repoData == null)
                {
                    _lastError = "Failed to parse repository response";
                    return null;
                }

                return new Models.Repository
                {
                    Name = repoData.Name,
                    Url = repoData.WebUrl,
                    DefaultBranch = repoData.DefaultBranch,
                    PlatformId = repoData.Id,
                    Owner = _organization
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception getting repository: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Creates a new Git repository in a project.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryName">Name for the new repository.</param>
        /// <returns>Created repository details, or null if creation failed.</returns>
        public async Task<Models.Repository?> CreateRepositoryAsync(string projectId, string repositoryName)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories?api-version=7.0";

                var createData = new
                {
                    name = repositoryName,
                    project = new { id = projectId }
                };

                var content = JsonSerializer.Serialize(createData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Create repository failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var repoData = JsonSerializer.Deserialize<AzureDevOpsRepositoryResponse>(responseContent, options);

                if (repoData == null)
                {
                    _lastError = "Failed to parse repository response";
                    return null;
                }

                return new Models.Repository
                {
                    Name = repoData.Name,
                    Url = repoData.WebUrl,
                    DefaultBranch = repoData.DefaultBranch,
                    PlatformId = repoData.Id,
                    Owner = _organization
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception creating repository: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Deletes a Git repository.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        public async Task<bool> DeleteRepositoryAsync(string projectId, string repositoryId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}?api-version=7.0";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Delete repository failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception deleting repository: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return false;
            }
        }

        /// <summary>
        /// Updates repository settings.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="defaultBranch">New default branch name (optional).</param>
        /// <returns>Updated repository details, or null if update failed.</returns>
        public async Task<Models.Repository?> UpdateRepositoryAsync(string projectId, string repositoryId,
            string? defaultBranch = null)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}?api-version=7.0";

                var updateData = new Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(defaultBranch))
                {
                    updateData["defaultBranch"] = defaultBranch;
                }

                var content = JsonSerializer.Serialize(updateData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Update repository failed: {response.StatusCode} {response.ReasonPhrase}\n{errorContent}";
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var repoData = JsonSerializer.Deserialize<AzureDevOpsRepositoryResponse>(responseContent, options);

                if (repoData == null)
                {
                    return null;
                }

                return new Models.Repository
                {
                    Name = repoData.Name,
                    Url = repoData.WebUrl,
                    DefaultBranch = repoData.DefaultBranch,
                    PlatformId = repoData.Id,
                    Owner = _organization
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception updating repository: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Creates a new pull request.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="title">Pull request title (required).</param>
        /// <param name="sourceRefName">Source branch name (required).</param>
        /// <param name="targetRefName">Target branch name (required).</param>
        /// <param name="description">Pull request description (optional).</param>
        /// <returns>Created pull request details, or null if creation failed.</returns>
        public async Task<Models.PullRequest?> CreatePullRequestAsync(string projectId, string repositoryId,
            string title, string sourceRefName, string targetRefName, string? description = null)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullrequests?api-version=7.0";

                var createData = new
                {
                    sourceRefName,
                    targetRefName,
                    title,
                    description
                };

                var content = JsonSerializer.Serialize(createData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Create pull request failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prData = JsonSerializer.Deserialize<AzureDevOpsPullRequestResponse>(responseContent, options);

                if (prData == null)
                {
                    _lastError = "Failed to parse pull request response";
                    return null;
                }

                return new Models.PullRequest
                {
                    Number = prData.PullRequestId,
                    Title = prData.Title,
                    Description = prData.Description,
                    Status = prData.Status,
                    Url = prData.Url,
                    Author = prData.CreatedBy?.DisplayName,
                    SourceBranch = prData.SourceRefName,
                    TargetBranch = prData.TargetRefName,
                    CreatedAt = prData.CreationDate,
                    UpdatedAt = prData.CreationDate
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception creating pull request: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Gets details for a specific pull request.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="prId">Pull request ID.</param>
        /// <returns>Pull request details, or null if not found.</returns>
        public async Task<Models.PullRequest?> GetPullRequestAsync(string projectId, string repositoryId, int prId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullrequests/{prId}?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Get pull request failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prData = JsonSerializer.Deserialize<AzureDevOpsPullRequestResponse>(content, options);

                if (prData == null)
                {
                    _lastError = "Failed to parse pull request response";
                    return null;
                }

                return new Models.PullRequest
                {
                    Number = prData.PullRequestId,
                    Title = prData.Title,
                    Description = prData.Description,
                    Status = prData.Status,
                    Url = prData.Url,
                    Author = prData.CreatedBy?.DisplayName,
                    SourceBranch = prData.SourceRefName,
                    TargetBranch = prData.TargetRefName,
                    CreatedAt = prData.CreationDate,
                    UpdatedAt = prData.CreationDate
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception getting pull request: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Lists pull requests for a repository.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="status">Filter by status: 'active', 'completed', 'abandoned', 'all'. Default: 'active'.</param>
        /// <param name="top">Maximum number of results to return. Default: 0 (all).</param>
        /// <returns>List of pull requests, or null if operation failed.</returns>
        public async Task<List<Models.PullRequest>?> ListPullRequestsAsync(string projectId, string repositoryId,
            string status = "active", int top = 0)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullrequests?api-version=7.0";

                var queryParams = new List<string> { $"searchCriteria.status={status}" };
                if (top > 0)
                {
                    queryParams.Add($"$top={top}");
                }

                if (queryParams.Count > 0)
                {
                    url += "&" + string.Join("&", queryParams);
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"List pull requests failed: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AzureDevOpsPullRequestListResponse>(content, options);

                if (result == null || result.Value == null)
                {
                    return new List<Models.PullRequest>();
                }

                return result.Value.Select(pr => new Models.PullRequest
                {
                    Number = pr.PullRequestId,
                    Title = pr.Title,
                    Description = pr.Description,
                    Status = pr.Status,
                    Url = pr.Url,
                    Author = pr.CreatedBy?.DisplayName,
                    SourceBranch = pr.SourceRefName,
                    TargetBranch = pr.TargetRefName,
                    CreatedAt = pr.CreationDate,
                    UpdatedAt = pr.CreationDate
                }).ToList();
            }
            catch (Exception ex)
            {
                _lastError = $"Exception listing pull requests: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Approves a pull request.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="prId">Pull request ID.</param>
        /// <returns>True if approval was successful, false otherwise.</returns>
        public async Task<bool> ApprovePullRequestAsync(string projectId, string repositoryId, int prId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullrequests/{prId}/reviewers?api-version=7.0";

                // Get current user first to add as reviewer with approved vote
                var userUrl = $"https://dev.azure.com/{_organization}/_apis/connectiondata?api-version=7.0";
                var userResponse = await _httpClient.GetAsync(userUrl);

                if (!userResponse.IsSuccessStatusCode)
                {
                    _lastError = "Failed to get current user for approval";
                    return false;
                }

                var userData = await userResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var connData = JsonSerializer.Deserialize<ConnectionData>(userData, options);

                if (connData?.AuthenticatedUser?.Id == null)
                {
                    _lastError = "Could not determine current user ID";
                    return false;
                }

                var reviewData = new { vote = 10 }; // 10 = approved in Azure DevOps
                var content = JsonSerializer.Serialize(reviewData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Approve pull request failed: {response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception approving pull request: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return false;
            }
        }

        /// <summary>
        /// Merges a pull request.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="prId">Pull request ID.</param>
        /// <returns>True if merge was successful, false otherwise.</returns>
        public async Task<bool> MergePullRequestAsync(string projectId, string repositoryId, int prId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullrequests/{prId}?api-version=7.0";

                var mergeData = new { status = "completed" };
                var content = JsonSerializer.Serialize(mergeData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Merge pull request failed: {response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception merging pull request: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return false;
            }
        }

        /// <summary>
        /// Updates a pull request.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="prId">Pull request ID.</param>
        /// <param name="title">New pull request title (optional).</param>
        /// <param name="description">New pull request description (optional).</param>
        /// <param name="status">New pull request status (optional).</param>
        /// <returns>Updated pull request details, or null if update failed.</returns>
        public async Task<Models.PullRequest?> UpdatePullRequestAsync(string projectId, string repositoryId,
            int prId, string? title = null, string? description = null, string? status = null)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullrequests/{prId}?api-version=7.0";

                var updateData = new Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(title))
                    updateData["title"] = title;
                if (!string.IsNullOrEmpty(description))
                    updateData["description"] = description;
                if (!string.IsNullOrEmpty(status))
                    updateData["status"] = status;

                var content = JsonSerializer.Serialize(updateData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Update pull request failed: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var prData = JsonSerializer.Deserialize<AzureDevOpsPullRequestResponse>(responseContent, options);

                if (prData == null)
                {
                    return null;
                }

                return new Models.PullRequest
                {
                    Number = prData.PullRequestId,
                    Title = prData.Title,
                    Description = prData.Description,
                    Status = prData.Status,
                    Url = prData.Url,
                    Author = prData.CreatedBy?.DisplayName,
                    SourceBranch = prData.SourceRefName,
                    TargetBranch = prData.TargetRefName,
                    CreatedAt = prData.CreationDate,
                    UpdatedAt = prData.CreationDate
                };
            }
            catch (Exception ex)
            {
                _lastError = $"Exception updating pull request: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
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
    /// Represents an Azure DevOps project.
    /// </summary>
    public class AzureDevOpsProject
    {
        /// <summary>
        /// Gets or sets the project ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the project description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the project URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the project state (e.g., 'wellFormed').
        /// </summary>
        public string? State { get; set; }
    }

    /// <summary>
    /// Represents an Azure DevOps project API response.
    /// </summary>
    public class AzureDevOpsProjectResponse
    {
        /// <summary>
        /// Gets or sets the project ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the project description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the project URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the project state.
        /// </summary>
        public string? State { get; set; }
    }

    /// <summary>
    /// Represents a list of Azure DevOps projects API response.
    /// </summary>
    public class AzureDevOpsProjectListResponse
    {
        /// <summary>
        /// Gets or sets the list of projects.
        /// </summary>
        public List<AzureDevOpsProjectResponse>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }

    /// <summary>
    /// Represents an Azure DevOps Git repository API response.
    /// </summary>
    public class AzureDevOpsRepositoryResponse
    {
        /// <summary>
        /// Gets or sets the repository ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the repository URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the repository web URL.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }

        /// <summary>
        /// Gets or sets the default branch name.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("defaultBranch")]
        public string? DefaultBranch { get; set; }

        /// <summary>
        /// Gets or sets the repository size in bytes.
        /// </summary>
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents a list of Azure DevOps Git repositories API response.
    /// </summary>
    public class AzureDevOpsRepositoryListResponse
    {
        /// <summary>
        /// Gets or sets the list of repositories.
        /// </summary>
        public List<AzureDevOpsRepositoryResponse>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }

    /// <summary>
    /// Represents a work item query result.
    /// </summary>
    public class WorkItemQueryResult
    {
        /// <summary>
        /// Gets or sets the list of work items.
        /// </summary>
        public List<WorkItemReference>? WorkItems { get; set; }
    }

    /// <summary>
    /// Represents a work item reference from a query.
    /// </summary>
    public class WorkItemReference
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item URL.
        /// </summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Represents the API response for a work item.
    /// </summary>
    public class WorkItemResponse
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item URL (top-level property from API response).
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the work item fields.
        /// </summary>
        public Dictionary<string, object?>? Fields { get; set; }

        /// <summary>
        /// Converts to AzureDevOpsWorkItem.
        /// </summary>
        public AzureDevOpsWorkItem ToWorkItem()
        {
            var item = new AzureDevOpsWorkItem
            {
                Id = Id,
                // Prefer top-level Url property, fall back to Fields if not available
                Url = !string.IsNullOrEmpty(Url) ? Url :
                    (Fields?.ContainsKey("System.Url") == true ? Fields["System.Url"]?.ToString() : null),
            };

            if (Fields != null)
            {
                item.Title = Fields.ContainsKey("System.Title") ? Fields["System.Title"]?.ToString() ?? "Unknown" : "Unknown";
                item.Type = Fields.ContainsKey("System.WorkItemType") ? Fields["System.WorkItemType"]?.ToString() ?? "Unknown" : "Unknown";
                item.State = Fields.ContainsKey("System.State") ? Fields["System.State"]?.ToString() ?? "New" : "New";
                item.Description = Fields.ContainsKey("System.Description") ? Fields["System.Description"]?.ToString() : null;
                
                if (Fields.ContainsKey("System.CreatedDate") && DateTime.TryParse(Fields["System.CreatedDate"]?.ToString(), out var createdDate))
                {
                    item.CreatedDate = createdDate;
                }

                if (Fields.ContainsKey("System.ChangedDate") && DateTime.TryParse(Fields["System.ChangedDate"]?.ToString(), out var changedDate))
                {
                    item.ChangedDate = changedDate;
                }

                // Extract sprint/iteration path - get the last component after backslash
                if (Fields.ContainsKey("System.IterationPath"))
                {
                    var iterationPath = Fields["System.IterationPath"]?.ToString();
                    if (!string.IsNullOrEmpty(iterationPath) && iterationPath.Contains("\\"))
                    {
                        item.Sprint = iterationPath.Split("\\").LastOrDefault();
                    }
                    else if (!string.IsNullOrEmpty(iterationPath))
                    {
                        item.Sprint = iterationPath;
                    }
                }

                // Extract assigned to user name
                if (Fields.ContainsKey("System.AssignedTo"))
                {
                    var assignedToField = Fields["System.AssignedTo"];
                    if (assignedToField != null)
                    {
                        // Try to handle as JsonElement with displayName property
                        if (assignedToField is JsonElement jElement)
                        {
                            if (jElement.TryGetProperty("displayName", out var displayName))
                            {
                                item.AssignedTo = displayName.GetString();
                            }
                            else if (jElement.ValueKind == JsonValueKind.String)
                            {
                                item.AssignedTo = jElement.GetString();
                            }
                        }
                        else
                        {
                            // Fallback: just convert to string
                            item.AssignedTo = assignedToField.ToString();
                        }
                    }
                }

                // Comments count is typically in comments section, defaulting to 0
                item.CommentCount = 0;
            }

            return item;
        }
    }

    /// <summary>
    /// Represents the batch API response for multiple work items.
    /// </summary>
    public class BatchWorkItemsResponse
    {
        /// <summary>
        /// Gets or sets the list of work item responses.
        /// </summary>
        [JsonPropertyName("value")]
        public List<WorkItemResponse>? Value { get; set; }
    }

    /// <summary>
    /// Represents an Azure DevOps work item.
    /// </summary>
    public class AzureDevOpsWorkItem
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the work item type (PBI, Task, Bug, etc.).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the work item state (New, Approved, Committed, Done, etc.).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the work item description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last change date.
        /// </summary>
        public DateTime ChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the number of comments.
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// Gets or sets the work item URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the sprint/iteration name.
        /// </summary>
        public string? Sprint { get; set; }

        /// <summary>
        /// Gets or sets the assigned to user name.
        /// </summary>
        public string? AssignedTo { get; set; }
    }

    /// <summary>
    /// Represents Azure DevOps connection data.
    /// </summary>
    public class ConnectionData
    {
        /// <summary>
        /// Gets or sets the authenticated user.
        /// </summary>
        public AzureDevOpsUser? AuthenticatedUser { get; set; }
    }

    /// <summary>
    /// Represents an Azure DevOps user.
    /// </summary>
    public class AzureDevOpsUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? UniqueName { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? PreferredEmail { get; set; }
    }

    /// <summary>
    /// Represents an Azure DevOps pull request API response.
    /// </summary>
    public class AzureDevOpsPullRequestResponse
    {
        /// <summary>
        /// Gets or sets the pull request ID.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("pullRequestId")]
        public int PullRequestId { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the pull request description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the pull request status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the source ref name (branch).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sourceRefName")]
        public string? SourceRefName { get; set; }

        /// <summary>
        /// Gets or sets the target ref name (branch).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("targetRefName")]
        public string? TargetRefName { get; set; }

        /// <summary>
        /// Gets or sets the pull request URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets creation date.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the user who created the pull request.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("createdBy")]
        public AzureDevOpsUser? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a list of Azure DevOps pull requests API response.
    /// </summary>
    public class AzureDevOpsPullRequestListResponse
    {
        /// <summary>
        /// Gets or sets the list of pull requests.
        /// </summary>
        public List<AzureDevOpsPullRequestResponse>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }
}