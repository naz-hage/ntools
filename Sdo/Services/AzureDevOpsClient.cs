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
        /// Adds a hyperlink relation to a work item pointing to a pull request web URL.
        /// </summary>
        /// <param name="workItemId">Work item numeric ID.</param>
        /// <param name="prWebUrl">The user-facing pull request web URL.</param>
        /// <returns>True if the link was added successfully, false otherwise.</returns>
        public async Task<bool> LinkWorkItemAsync(int workItemId, int prId, string repositoryId)
        {
            // Try az CLI first (preferred - creates Development link)
            try
            {
                var azArgs = $"repos pr work-item add --id {prId} --work-items {workItemId} --org https://dev.azure.com/{_organization} --project {System.Uri.EscapeDataString(_project ?? string.Empty)}";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = azArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    var stdout = await proc.StandardOutput.ReadToEndAsync();
                    var stderr = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (proc.ExitCode == 0)
                    {
                        return true;
                    }

                    // az failed - capture error and fall back to REST patch
                    _lastError = $"az CLI failed: {stderr.Trim()}";
                }
                else
                {
                    _lastError = "Failed to start az CLI process";
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _lastError = "Azure CLI (az) not found in PATH";
            }
            catch (Exception ex)
            {
                _lastError = $"az CLI call exception: {ex.Message}";
            }

            // Fallback: try to create a Development-style ArtifactLink via REST (preferred over a plain Hyperlink)
            try
            {
                if (string.IsNullOrEmpty(_project))
                {
                    _lastError = "Project not specified; cannot create ArtifactLink via REST fallback";
                }
                else
                {
                    // Fetch project GUID
                    var projUrl = $"https://dev.azure.com/{_organization}/_apis/projects/{Uri.EscapeDataString(_project)}?api-version=7.0";
                    var projResp = await _httpClient.GetAsync(projUrl);
                    if (projResp.IsSuccessStatusCode)
                    {
                        var projContent = await projResp.Content.ReadAsStringAsync();
                        var projObj = JsonSerializer.Deserialize<JsonElement>(projContent);
                        var projectGuid = projObj.GetProperty("id").GetString();

                        // Fetch repository GUID (repositoryId may be name or id)
                        var repoUrl = $"https://dev.azure.com/{_organization}/{Uri.EscapeDataString(_project)}/_apis/git/repositories/{Uri.EscapeDataString(repositoryId)}?api-version=7.0";
                        var repoResp = await _httpClient.GetAsync(repoUrl);
                        if (repoResp.IsSuccessStatusCode)
                        {
                            var repoContent = await repoResp.Content.ReadAsStringAsync();
                            var repoObj = JsonSerializer.Deserialize<JsonElement>(repoContent);
                            var repoGuid = repoObj.GetProperty("id").GetString();

                            if (!string.IsNullOrEmpty(projectGuid) && !string.IsNullOrEmpty(repoGuid))
                            {
                                // Construct ArtifactLink URL expected by Azure DevOps
                                var artifactUrl = $"vstfs:///Git/PullRequestId/{projectGuid}%2F{repoGuid}%2F{prId}";
                                var patchDoc = new[]
                                {
                                    new
                                    {
                                        op = "add",
                                        path = "/relations/-",
                                        value = new
                                        {
                                            rel = "ArtifactLink",
                                            url = artifactUrl,
                                            attributes = new { name = "Pull Request" }
                                        }
                                    }
                                };

                                var url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems/{workItemId}?api-version=7.0";
                                var content = new StringContent(JsonSerializer.Serialize(patchDoc), System.Text.Encoding.UTF8, "application/json-patch+json");
                                var response = await _httpClient.PatchAsync(url, content);
                                if (response.IsSuccessStatusCode)
                                {
                                    return true;
                                }

                                var errorContent = await response.Content.ReadAsStringAsync();
                                _lastError = $"ArtifactLink patch failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                            }
                            else
                            {
                                _lastError = "Failed to obtain project or repository GUIDs for ArtifactLink creation";
                            }
                        }
                        else
                        {
                            var rc = await repoResp.Content.ReadAsStringAsync();
                            _lastError = $"Failed to fetch repository info: {repoResp.StatusCode} {repoResp.ReasonPhrase}\n{rc}";
                        }
                    }
                    else
                    {
                        var pc = await projResp.Content.ReadAsStringAsync();
                        _lastError = $"Failed to fetch project info: {projResp.StatusCode} {projResp.ReasonPhrase}\n{pc}";
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Exception linking work item (artifact fallback): {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
            }

            // Final fallback: add a plain Hyperlink relation so the work item still points to the PR
            try
            {
                var webUrl = $"https://dev.azure.com/{_organization}/{_project}/_git/{repositoryId}/pullrequest/{prId}";
                var url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems/{workItemId}?api-version=7.0";

                var patchDoc = new[]
                {
                    new
                    {
                        op = "add",
                        path = "/relations/-",
                        value = new
                        {
                            rel = "Hyperlink",
                            url = webUrl,
                            attributes = new { comment = "Linked from SDO pull request" }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(patchDoc), System.Text.Encoding.UTF8, "application/json-patch+json");
                var response = await _httpClient.PatchAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Link work item failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception linking work item (fallback): {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return false;
            }
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
        /// Gets multiple work items in batched API calls (avoiding URL length limits).
        /// Splits large requests into chunks to stay under the HTTP URL length limit.
        /// </summary>
        /// <param name="workItemIds">The work item IDs to fetch.</param>
        /// <param name="verbose">Whether to log verbose output to console.</param>
        /// <returns>List of work items.</returns>
        private async Task<List<AzureDevOpsWorkItem>> GetWorkItemsBatchAsync(List<int> workItemIds, bool verbose = false)
        {
            try
            {
                if (workItemIds == null || workItemIds.Count == 0)
                {
                    return new List<AzureDevOpsWorkItem>();
                }

                // Azure DevOps batch API has URL length limits (~2000-8000 chars depending on server configuration)
                // Each work item ID is 1-5 characters, plus a comma separator
                // A conservative estimate: ~100-200 IDs per request is safe
                const int batchSize = 100;
                var allWorkItems = new List<AzureDevOpsWorkItem>();
                var totalBatches = (workItemIds.Count + batchSize - 1) / batchSize;

                System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Fetching {workItemIds.Count} work items in {totalBatches} batch(es)");
                if (verbose)
                {
                    Console.WriteLine($"[GetWorkItemsBatchAsync] Fetching {workItemIds.Count} work items in {totalBatches} batch(es)");
                }

                // Process in batches to avoid URL length errors
                for (int i = 0; i < workItemIds.Count; i += batchSize)
                {
                    var batchIds = workItemIds.Skip(i).Take(batchSize).ToList();
                    var batchNum = (i / batchSize) + 1;

                    System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Processing batch {batchNum}/{totalBatches} ({batchIds.Count} items)");

                    // Azure DevOps batch API: GET /workitems?ids=1,2,3,...&$expand=all
                    // Include project in URL if specified (for proper access control)
                    var idsString = string.Join(",", batchIds);
                    string url;
                    if (!string.IsNullOrEmpty(_project))
                    {
                        url = $"https://dev.azure.com/{_organization}/{_project}/_apis/wit/workitems?ids={idsString}&api-version=7.0&$expand=all";
                    }
                    else
                    {
                        url = $"https://dev.azure.com/{_organization}/_apis/wit/workitems?ids={idsString}&api-version=7.0&$expand=all";
                    }

                    System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Batch {batchNum} URL length: {url.Length} chars");

                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        var errorMsg = $"Batch API error on batch {batchNum}/{totalBatches}: {response.StatusCode} {response.ReasonPhrase}";
                        System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] {errorMsg}");
                        System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Response: {errorContent}");
                        if (verbose)
                        {
                            Console.WriteLine($"[GetWorkItemsBatchAsync] {errorMsg}");
                        }
                        // Continue with other batches instead of failing completely
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<BatchWorkItemsResponse>(content, options);

                    if (result?.Value != null && result.Value.Count > 0)
                    {
                        var batchItems = result.Value.Select(r => r.ToWorkItem()).ToList();
                        allWorkItems.AddRange(batchItems);
                        System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Batch {batchNum} fetched {batchItems.Count} items");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Batch {batchNum} returned no items");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Total fetched: {allWorkItems.Count} work items");
                if (verbose)
                {
                    Console.WriteLine($"[GetWorkItemsBatchAsync] Total fetched: {allWorkItems.Count} work items from {totalBatches} batch(es)");
                }

                return allWorkItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Error in batch fetch: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetWorkItemsBatchAsync] Exception: {ex}");
                if (verbose)
                {
                    Console.WriteLine($"[GetWorkItemsBatchAsync] Error in batch fetch: {ex.Message}");
                }
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
        /// Gets work items linked to a pull request.
        /// </summary>
        /// <param name="projectId">Project ID or name.</param>
        /// <param name="repositoryId">Repository ID or name.</param>
        /// <param name="prId">Pull request ID.</param>
        /// <returns>List of linked work items (may be empty).</returns>
        public async Task<List<AzureDevOpsWorkItem>> GetPullRequestWorkItemsAsync(string projectId, string repositoryId, int prId)
        {
            var result = new List<AzureDevOpsWorkItem>();
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{projectId}/_apis/git/repositories/{repositoryId}/pullRequests/{prId}/workitems?api-version=7.0";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"GetPullRequestWorkItemsAsync failed: {response.StatusCode} {response.ReasonPhrase}");
                    return result;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<JsonElement>(content);
                if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        int id = 0;
                        if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                        {
                            id = idProp.GetInt32();
                        }
                        else if (item.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                        {
                            var u = urlProp.GetString() ?? string.Empty;
                            // extract trailing numeric id
                            var parts = u.TrimEnd('/').Split('/');
                            if (int.TryParse(parts.Last(), out var parsed)) id = parsed;
                        }

                        if (id > 0)
                        {
                            var wi = await GetWorkItemAsync(id);
                            if (wi != null) result.Add(wi);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetPullRequestWorkItemsAsync: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Lists Azure DevOps work items.
        /// </summary>
        /// <param name="top">Maximum number of items to return.</param>
        /// <param name="areaPath">Optional area path filter (e.g., 'Project\Area\SubArea').</param>
        /// <param name="iteration">Optional iteration path filter (e.g., 'Project\Sprint 1').</param>
        /// <param name="verbose">Whether to log verbose output to console.</param>
        /// <returns>List of work items.</returns>
        public async Task<List<AzureDevOpsWorkItem>?> ListWorkItemsAsync(int top = 50, string? areaPath = null, string? iteration = null, bool verbose = false)
        {
            try
            {
                // Use WIQL query to get recent work items
                var conditions = new List<string>();
                
                if (!string.IsNullOrEmpty(areaPath))
                {
                    // Use UNDER operator to include the area and all sub-areas
                    conditions.Add($"[System.AreaPath] UNDER '{areaPath}'");
                }
                
                if (!string.IsNullOrEmpty(iteration))
                {
                    // Use UNDER operator to include the iteration and all sub-iterations
                    conditions.Add($"[System.IterationPath] UNDER '{iteration}'");
                }

                string whereClause = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;

                var wiql = new
                {
                    query = $"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State], [System.CreatedDate], [System.ChangedDate] FROM WorkItems{whereClause} ORDER BY [System.ChangedDate] DESC"
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
                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] Response Content Length: {responseContent.Length}");
                if (verbose)
                {
                    Console.WriteLine($"[ListWorkItemsAsync] Response Status: {response.StatusCode}");
                    Console.WriteLine("[ListWorkItemsAsync] Response (truncated):");
                    var preview = responseContent.Length > 2000 ? responseContent.Substring(0, 2000) + "..." : responseContent;
                    Console.WriteLine(preview);
                }
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var queryResult = JsonSerializer.Deserialize<WorkItemQueryResult?>(responseContent, options);

                System.Diagnostics.Debug.WriteLine($"[ListWorkItemsAsync] WorkItems Count (wiql): {queryResult?.WorkItems?.Count ?? 0}");
                if (verbose)
                {
                    Console.WriteLine($"[ListWorkItemsAsync] WorkItems Count (wiql): {queryResult?.WorkItems?.Count ?? 0}");
                }

                // If WIQL returned a set of work item references, use them
                if (queryResult?.WorkItems != null && queryResult.WorkItems.Any())
                {
                    var workItemIds = queryResult.WorkItems.Take(top > 0 ? top : queryResult.WorkItems.Count).Select(w => w.Id).ToList();
                    var workItems = await GetWorkItemsBatchAsync(workItemIds, verbose);
                    if (verbose)
                    {
                        Console.WriteLine($"[ListWorkItemsAsync] Fetched {workItems.Count} work items via batch API");
                    }
                    return workItems;
                }

                // If WIQL didn't return references, and parsing didn't yield items, return empty list
                System.Diagnostics.Debug.WriteLine("[ListWorkItemsAsync] No work items parsed from WIQL response");
                return new List<AzureDevOpsWorkItem>();
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
                result["Organization Access"] = response.IsSuccessStatusCode ? "✓ Allowed" : "X Denied";

                // Test project access if specified
                if (!string.IsNullOrEmpty(_project))
                {
                    var projectUrl = $"https://dev.azure.com/{_organization}/{_project}/_apis/teams?api-version=7.0";
                    var projectResponse = await _httpClient.GetAsync(projectUrl);
                    result["Project Access"] = projectResponse.IsSuccessStatusCode ? "✓ Allowed" : "X Denied";
                }

                // Test Work Item Query access - this is what fails in workitem list
                var wiqlUrl = $"https://dev.azure.com/{_organization}/_apis/wit/wiql?api-version=7.0";
                var wiqlRequest = new StringContent("{\"query\":\"SELECT [System.Id] FROM WorkItems LIMIT 0\"}", System.Text.Encoding.UTF8, "application/json");
                var wiqlResponse = await _httpClient.PostAsync(wiqlUrl, wiqlRequest);
                result["Work Item Query Access"] = wiqlResponse.IsSuccessStatusCode ? "✓ Allowed" : "X Denied";

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
                if (verbose)
                {
                    Console.WriteLine("[VERBOSE] JSON patch payload (per operation):");
                    var optionsPretty = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
                    for (int i = 0; i < operations.Count; i++)
                    {
                        try
                        {
                            var opJson = JsonSerializer.Serialize(operations[i], optionsPretty);
                            Console.WriteLine($"--- operation {i + 1} ---");
                            Console.WriteLine(opJson);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[VERBOSE] Failed to serialize operation {i + 1}: {ex.Message}");
                        }
                    }

                    Console.WriteLine("[VERBOSE] Full payload string:");
                    Console.WriteLine(content);

                    try
                    {
                        var tmpPath = "C:\\temp\\sdo_create_payload.json";
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(tmpPath)!);
                        System.IO.File.WriteAllText(tmpPath, content);
                        Console.WriteLine($"[VERBOSE] Wrote payload to {tmpPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[VERBOSE] Failed to write payload file: {ex.Message}");
                    }
                }

                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json-patch+json");

                if (verbose)
                    Console.WriteLine($"[VERBOSE] Sending PATCH request to {url}");

                var response = await _httpClient.PatchAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = ExtractErrorMessage(errorContent);
                    _lastError = $"Update failed: {response.StatusCode} {response.ReasonPhrase}\n{errorMessage}";
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
                if (verbose)
                {
                    Console.WriteLine("[VERBOSE] JSON patch payload (per operation):");
                    var optionsPretty = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
                    for (int i = 0; i < operations.Count; i++)
                    {
                        try
                        {
                            var opJson = JsonSerializer.Serialize(operations[i], optionsPretty);
                            Console.WriteLine($"--- operation {i + 1} ---");
                            Console.WriteLine(opJson);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[VERBOSE] Failed to serialize operation {i + 1}: {ex.Message}");
                        }
                    }

                    Console.WriteLine("[VERBOSE] Full payload string:");
                    Console.WriteLine(content);
                }

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
        /// Creates a new work item in the specified project.
        /// </summary>
        public async Task<Dictionary<string, object>?> CreateWorkItemAsync(
            string project,
            string workItemType,
            string title,
            string description,
            List<string>? acceptanceCriteria = null,
            string? assignee = null,
            string? areaPath = null,
            string? iterationPath = null,
            bool dryRun = false,
            bool verbose = false)
        {
            try
            {
                if (string.IsNullOrEmpty(project))
                {
                    _lastError = "Project must be specified for work item creation";
                    return null;
                }

                // Normalize work item type
                var encodedType = Uri.EscapeDataString(string.IsNullOrEmpty(workItemType) ? "PBI" : workItemType);

                // Format description: remove markdown headers (##, ###, etc.) and convert markdown to HTML
                var formattedDesc = description ?? string.Empty;
                // Remove markdown headers (##, #, etc.)
                formattedDesc = System.Text.RegularExpressions.Regex.Replace(formattedDesc, @"^#+\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
                // Convert **bold** to <strong>bold</strong>
                formattedDesc = System.Text.RegularExpressions.Regex.Replace(formattedDesc, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
                // Replace line breaks with <br/>
                formattedDesc = System.Text.RegularExpressions.Regex.Replace(formattedDesc, @"\r?\n", "<br/>");
                var repoStepsHtml = formattedDesc;
                
                var acceptanceCriteriaHtml = string.Empty;
                
                if (acceptanceCriteria != null && acceptanceCriteria.Any())
                {
                    acceptanceCriteriaHtml = "<ul>" + string.Join("\n", acceptanceCriteria.Select(ac => $"<li>{System.Net.WebUtility.HtmlEncode(ac)}</li>")) + "</ul>";
                }

                if (dryRun)
                {
                    Console.WriteLine("[dry-run] Would create Azure DevOps work item with:");
                    Console.WriteLine($"  Project: {project}");
                    Console.WriteLine($"  Type: {workItemType}");
                    Console.WriteLine($"  Title: {title}");
                    Console.WriteLine("  Description/Repro Steps:");
                    Console.WriteLine(repoStepsHtml);
                    if (!string.IsNullOrEmpty(assignee)) Console.WriteLine($"  Assignee: {assignee}");
                    if (acceptanceCriteria != null && acceptanceCriteria.Any())
                    {
                        Console.WriteLine($"  Acceptance Criteria: {acceptanceCriteria.Count} items");
                        if (verbose)
                        {
                            for (int i = 0; i < acceptanceCriteria.Count; i++) Console.WriteLine($"   {i + 1}. {acceptanceCriteria[i]}");
                        }
                    }

                    return new Dictionary<string, object> { { "dry_run", true }, { "title", title }, { "project", project } };
                }

                // Build JSON patch operations - write to form-displayed fields
                var operations = new List<object>
                {
                    new { op = "add", path = "/fields/System.Title", value = title },
                    new { op = "add", path = "/fields/Microsoft.VSTS.TCM.ReproSteps", value = repoStepsHtml }
                };

                // Add acceptance criteria to its own field (displayed in form)
                if (!string.IsNullOrEmpty(acceptanceCriteriaHtml))
                {
                    operations.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value = acceptanceCriteriaHtml });
                }

                // Only set AreaPath/IterationPath when explicitly provided
                if (!string.IsNullOrEmpty(areaPath))
                {
                    operations.Add(new { op = "add", path = "/fields/System.AreaPath", value = areaPath });
                }

                if (!string.IsNullOrEmpty(iterationPath))
                {
                    operations.Add(new { op = "add", path = "/fields/System.IterationPath", value = iterationPath });
                }

                if (!string.IsNullOrEmpty(assignee))
                {
                    operations.Add(new { op = "add", path = "/fields/System.AssignedTo", value = assignee });
                }

                var url = $"https://dev.azure.com/{_organization}/{project}/_apis/wit/workitems/${encodedType}?api-version=7.1";

                if (verbose)
                {
                    Console.WriteLine($"[VERBOSE] Creating work item via: {url}");
                    Console.WriteLine($"[VERBOSE] Operations: {operations.Count}");
                }

                var content = JsonSerializer.Serialize(operations);
                if (verbose)
                {
                    Console.WriteLine("[VERBOSE] JSON patch payload (per operation):");
                    var optionsPretty = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
                    for (int i = 0; i < operations.Count; i++)
                    {
                        try
                        {
                            var opJson = JsonSerializer.Serialize(operations[i], optionsPretty);
                            Console.WriteLine($"--- operation {i + 1} ---");
                            Console.WriteLine(opJson);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[VERBOSE] Failed to serialize operation {i + 1}: {ex.Message}");
                        }
                    }

                    Console.WriteLine("[VERBOSE] Full payload string:");
                    Console.WriteLine(content);
                }

                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json-patch+json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    var errorMessage = ExtractErrorMessage(error);
                    _lastError = $"Create failed: {response.StatusCode} {response.ReasonPhrase}\n{errorMessage}";
                    if (verbose) Console.WriteLine($"[VERBOSE] {_lastError}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<JsonElement>(responseBody, options);

                int? id = null;
                string? link = null;
                try
                {
                    if (doc.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number) id = idProp.GetInt32();
                    if (doc.TryGetProperty("_links", out var links) && links.ValueKind == JsonValueKind.Object && links.TryGetProperty("html", out var html) && html.TryGetProperty("href", out var href)) link = href.GetString();
                }
                catch { }

                if (id.HasValue)
                {
                    Console.WriteLine($"✅ Created {workItemType} #{id}: {title}");
                    if (!string.IsNullOrEmpty(link)) Console.WriteLine($"   URL: {link}");
                    var result = new Dictionary<string, object>();
                    result["id"] = id.Value;
                    if (!string.IsNullOrEmpty(link)) result["url"] = link;
                    result["type"] = workItemType;
                    result["title"] = title;
                    return result;
                }

                _lastError = "Failed to parse work item creation response";
                return null;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception creating work item: {ex.Message}";
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
                    Url = $"https://dev.azure.com/{_organization}/{projectId}/_git/{repositoryId}/pullrequest/{prData.PullRequestId}",
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
                    Url = $"https://dev.azure.com/{_organization}/{projectId}/_git/{repositoryId}/pullrequest/{prData.PullRequestId}",
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
                    Url = $"https://dev.azure.com/{_organization}/{projectId}/_git/{repositoryId}/pullrequest/{pr.PullRequestId}",
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

                // Construct a user-facing web URL instead of returning the API/REST URL
                var webUrl = $"https://dev.azure.com/{_organization}/{projectId}/_git/{repositoryId}/pullrequest/{prData.PullRequestId}";

                return new Models.PullRequest
                {
                    Number = prData.PullRequestId,
                    Title = prData.Title,
                    Description = prData.Description,
                    Status = prData.Status,
                    Url = webUrl,
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
        /// Extracts a user-friendly error message from Azure DevOps API error responses.
        /// </summary>
        /// <param name="errorJson">The full JSON error response from Azure DevOps API.</param>
        /// <returns>A concise error message suitable for display to users.</returns>
        private string ExtractErrorMessage(string errorJson)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<JsonElement>(errorJson, options);

                // Try to extract the main "message" field first
                if (doc.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                {
                    var msgValue = msg.GetString();
                    if (!string.IsNullOrEmpty(msgValue))
                        return msgValue;
                }

                // Try customProperties.ErrorMessage
                if (doc.TryGetProperty("customProperties", out var customProps) && 
                    customProps.TryGetProperty("ErrorMessage", out var errorMsg) &&
                    errorMsg.ValueKind == JsonValueKind.String)
                {
                    var errorMsgValue = errorMsg.GetString();
                    if (!string.IsNullOrEmpty(errorMsgValue))
                        return errorMsgValue;
                }

                // Try the first rule validation error message
                if (doc.TryGetProperty("customProperties", out var cp2) &&
                    cp2.TryGetProperty("RuleValidationErrors", out var ruleErrors) &&
                    ruleErrors.ValueKind == JsonValueKind.Array)
                {
                    var firstError = ruleErrors.EnumerateArray().FirstOrDefault();
                    if (firstError.TryGetProperty("errorMessage", out var firstErrorMsg) &&
                        firstErrorMsg.ValueKind == JsonValueKind.String)
                    {
                        var firstErrorMsgValue = firstErrorMsg.GetString();
                        if (!string.IsNullOrEmpty(firstErrorMsgValue))
                            return firstErrorMsgValue;
                    }
                }

                // Fallback: return truncated response
                return errorJson.Length > 300 ? errorJson.Substring(0, 300) + "..." : errorJson;
            }
            catch
            {
                // If JSON parsing fails, return original
                return errorJson;
            }
        }

        /// <summary>
        /// Creates a new build pipeline from a YAML definition.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="pipelineName">The name for the new pipeline.</param>
        /// <param name="yamlFilePath">The path to the pipeline YAML file.</param>
        /// <returns>The created build definition ID, or -1 if creation fails.</returns>
        public async Task<int> CreatePipelineAsync(string project, string pipelineName, string yamlFilePath)
        {
            try
            {
                // Read YAML content
                if (!File.Exists(yamlFilePath))
                {
                    _lastError = $"YAML file not found: {yamlFilePath}";
                    return -1;
                }

                var yamlContent = await File.ReadAllTextAsync(yamlFilePath);

                var url = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/definitions?api-version=7.0";

                // Create the build definition request body
                var createData = new
                {
                    name = pipelineName,
                    type = "build",
                    process = new
                    {
                        yamlFilename = yamlFilePath,
                        type = 2  // YAML process type
                    },
                    repository = new
                    {
                        type = "TfsGit"  // Azure DevOps Git repository
                    }
                };

                var content = JsonSerializer.Serialize(createData);
                var request = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Create pipeline failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return -1;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pipelineData = JsonSerializer.Deserialize<AzureDevOpsBuildDefinition>(responseContent, options);

                if (pipelineData == null)
                {
                    _lastError = "Failed to parse pipeline response";
                    return -1;
                }

                return pipelineData.Id;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception creating pipeline: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return -1;
            }
        }

        /// <summary>
        /// Lists build pipelines in a project.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <returns>List of Azure DevOps build pipelines, or null if request fails.</returns>
        public async Task<List<AzureDevOpsBuildDefinition>?> ListPipelinesAsync(string project)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/definitions?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Azure DevOps API error: {response.StatusCode} {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pipelinesResponse = JsonSerializer.Deserialize<AzureDevOpsBuildDefinitionListResponse>(content, options);

                return pipelinesResponse?.Value;
            }
            catch (Exception ex)
            {
                _lastError = $"Error fetching Azure DevOps pipelines: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Gets a specific pipeline definition by ID.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="pipelineId">The pipeline definition ID.</param>
        /// <returns>The pipeline definition, or null if request fails.</returns>
        public async Task<AzureDevOpsBuildDefinition?> GetPipelineAsync(string project, int pipelineId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/definitions/{pipelineId}?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Get pipeline failed ({response.StatusCode}): {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<AzureDevOpsBuildDefinition>(content, options);
            }
            catch (Exception ex)
            {
                _lastError = $"Error fetching Azure DevOps pipeline {pipelineId}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Gets a specific pipeline definition by ID or exact name.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="pipelineIdOrName">Pipeline ID (numeric) or name.</param>
        /// <returns>The pipeline definition, or null if not found.</returns>
        public async Task<AzureDevOpsBuildDefinition?> GetPipelineAsync(string project, string pipelineIdOrName)
        {
            if (string.IsNullOrWhiteSpace(pipelineIdOrName))
            {
                _lastError = "Pipeline ID or name is required.";
                return null;
            }

            if (int.TryParse(pipelineIdOrName, out var pipelineId))
            {
                return await GetPipelineAsync(project, pipelineId);
            }

            var pipelines = await ListPipelinesAsync(project);
            if (pipelines == null || pipelines.Count == 0)
            {
                if (string.IsNullOrEmpty(_lastError))
                {
                    _lastError = "No pipelines found in this project.";
                }
                return null;
            }

            var match = pipelines.FirstOrDefault(p =>
                string.Equals(p.Name, pipelineIdOrName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                _lastError = $"Pipeline not found by name: {pipelineIdOrName}";
            }

            return match;
        }

        /// <summary>
        /// Deletes a build definition (pipeline) by numeric ID.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="pipelineId">The build definition ID.</param>
        /// <returns>True if deletion succeeded (HTTP 204), false otherwise.</returns>
        public async Task<bool> DeletePipelineAsync(string project, int pipelineId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{project}/_apis/build/definitions/{pipelineId}?api-version=7.0";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Delete pipeline failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception deleting pipeline: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return false;
            }
        }

        /// <summary>
        /// Deletes a build definition (pipeline) by numeric ID or name.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="pipelineIdOrName">Pipeline ID (numeric) or name.</param>
        /// <returns>True if deletion succeeded, false otherwise.</returns>
        public async Task<bool> DeletePipelineAsync(string project, string pipelineIdOrName)
        {
            if (string.IsNullOrWhiteSpace(pipelineIdOrName))
            {
                _lastError = "Pipeline ID or name is required.";
                return false;
            }

            if (int.TryParse(pipelineIdOrName, out var pipelineId))
            {
                return await DeletePipelineAsync(project, pipelineId);
            }

            var pipeline = await GetPipelineAsync(project, pipelineIdOrName);
            if (pipeline == null)
            {
                if (string.IsNullOrEmpty(_lastError))
                {
                    _lastError = $"Pipeline not found by name: {pipelineIdOrName}";
                }
                return false;
            }

            return await DeletePipelineAsync(project, pipeline.Id);
        }

        /// <summary>
        /// Gets a specific build by ID.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="buildId">The build ID.</param>
        /// <returns>Build details, or null if request fails.</returns>
        public async Task<AzureDevOpsBuild?> GetBuildAsync(string project, int buildId)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/builds/{buildId}?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"Get build failed ({response.StatusCode}): {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<AzureDevOpsBuild>(content, options);
            }
            catch (Exception ex)
            {
                _lastError = $"Error fetching Azure DevOps build {buildId}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Lists builds in a project, optionally filtered by definition.
        /// </summary>
        /// <param name="project">The project name.</param>
        /// <param name="top">Maximum number of builds to return.</param>
        /// <param name="definitionId">Optional pipeline definition ID filter.</param>
        /// <returns>List of builds, or null if request fails.</returns>
        public async Task<List<AzureDevOpsBuild>?> ListBuildsAsync(string project, int top = 10, int? definitionId = null)
        {
            try
            {
                var safeTop = Math.Max(1, top);
                var url = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/builds?api-version=7.0&$top={safeTop}";
                if (definitionId.HasValue)
                {
                    url += $"&definitions={definitionId.Value}";
                }

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _lastError = $"List builds failed ({response.StatusCode}): {response.ReasonPhrase}";
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var buildsResponse = JsonSerializer.Deserialize<AzureDevOpsBuildListResponse>(content, options);
                return buildsResponse?.Value;
            }
            catch (Exception ex)
            {
                _lastError = $"Error listing Azure DevOps builds: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return null;
            }
        }

        /// <summary>
        /// Queues a new build for the specified pipeline definition.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <param name="definitionId">Pipeline definition ID.</param>
        /// <param name="branch">Branch to run (e.g. 'main' or 'refs/heads/main').</param>
        /// <param name="parameters">Optional parameters dictionary (will be serialized to JSON string).</param>
        /// <returns>New build ID on success, or -1 on failure.</returns>
        public async Task<int> RunPipelineAsync(string project, int definitionId, string? branch = null, Dictionary<string, string>? parameters = null)
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/builds?api-version=7.0";

                var payload = new Dictionary<string, object>
                {
                    ["definition"] = new { id = definitionId }
                };

                if (!string.IsNullOrWhiteSpace(branch))
                {
                    var refName = branch.StartsWith("refs/") ? branch : $"refs/heads/{branch}";
                    payload["sourceBranch"] = refName;
                }

                if (parameters != null && parameters.Any())
                {
                    // Azure DevOps expects parameters as a JSON string
                    payload["parameters"] = JsonSerializer.Serialize(parameters);
                }

                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _lastError = $"Queue build failed ({response.StatusCode}): {response.ReasonPhrase}\n{errorContent}";
                    return -1;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var build = JsonSerializer.Deserialize<AzureDevOpsBuild>(responseContent, options);

                return build?.Id ?? -1;
            }
            catch (Exception ex)
            {
                _lastError = $"Exception queuing build: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(_lastError);
                return -1;
            }
        }

        /// <summary>
        /// Retrieves and concatenates logs for a given build.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <param name="buildId">Build/run ID.</param>
        /// <returns>Concatenated log text, or null if failed.</returns>
        public async Task<string?> GetBuildLogsAsync(string project, int buildId)
        {
            try
            {
                var listUrl = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/builds/{buildId}/logs?api-version=7.0";
                var listResp = await _httpClient.GetAsync(listUrl);
                if (!listResp.IsSuccessStatusCode)
                {
                    var err = await listResp.Content.ReadAsStringAsync();
                    _lastError = $"Failed to list logs for build {buildId}: {listResp.StatusCode} {listResp.ReasonPhrase}\n{err}";
                    return null;
                }

                var listContent = await listResp.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<JsonElement>(listContent, options);

                if (!doc.TryGetProperty("value", out var arr) || arr.ValueKind != JsonValueKind.Array)
                {
                    _lastError = "No logs found for this build.";
                    return null;
                }

                var sb = new System.Text.StringBuilder();
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                    {
                        var id = idProp.GetInt32();
                        var logUrl = $"https://dev.azure.com/{_organization}/{System.Uri.EscapeDataString(project)}/_apis/build/builds/{buildId}/logs/{id}?api-version=7.0";
                        var logResp = await _httpClient.GetAsync(logUrl);
                        if (logResp.IsSuccessStatusCode)
                        {
                            var logText = await logResp.Content.ReadAsStringAsync();
                            sb.AppendLine($"===== Log {id} =====");
                            sb.AppendLine(logText);
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _lastError = $"Exception retrieving build logs: {ex.Message}";
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
}