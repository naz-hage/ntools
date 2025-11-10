using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GitHubRelease
{
    /// <summary>
    /// Represents an asset in a GitHub release.
    /// </summary>
    public class Asset
    {
        public string? Name { get; set; }
        public int Size { get; set; }
        public string? BrowserDownloadUrl { get; set; }
        public string? Uploader { get; set; }
    }
    /// <summary>
    /// Service class for creating GitHub releases and uploading assets.
    /// </summary>
    public class ReleaseService : Constants
    {
        private readonly ApiService ApiService;
        private readonly GitHubAuthService? AuthService;
        public readonly string Repo;
        public readonly string FirstDate = "1970-01-01T00:00:00Z";
        private const string NoneFound = "N/A";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReleaseService"/> class.
        /// </summary>
        public ReleaseService(string repo)
        {
            Repo = repo;
            AuthService = null;
            ApiService = new ApiService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReleaseService"/> class with authentication service.
        /// </summary>
        public ReleaseService(string repo, GitHubAuthService authService)
        {
            Repo = repo;
            AuthService = authService;
            ApiService = new ApiService(authService);
        }

        /// <summary>
        /// Creates a release with the specified release object and asset path.
        /// </summary>
        /// <param name="release">The release object to create.</param>
        /// <param name="assetPath">The path to the asset file to upload.</param>
        /// <returns>The HTTP response message from the release creation or asset upload operation.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the tag name or target commitish is missing or invalid, or if the branch name cannot be determined in a detached HEAD state.
        /// </exception>
        public async Task<HttpResponseMessage> CreateRelease(Release release, string assetPath)
        {
            ValidateAssetPath(assetPath);
            await DeleteExistingRelease(release);
            await DeleteStagingReleasesIfProduction(release);
            await UpdateReleaseNotes(release);

            // Log the release object for debugging
            Console.WriteLine($"Creating release with tag: {release.TagName}, target commitish: {release.TargetCommitish}");

            // Validate tag_name and target_commitish
            if (string.IsNullOrEmpty(release.TagName) || string.IsNullOrEmpty(release.TargetCommitish))
            {
                throw new InvalidOperationException("Tag name and target commitish must be provided and valid.");
            }

            // Handle detached HEAD in GitHub Actions
            if (release.TargetCommitish.Contains("HEAD"))
            {
                var branch = await GetBranchNameFromGitHubActions();
                if (!string.IsNullOrEmpty(branch))
                {
                    release.TargetCommitish = branch;
                }
                else
                {
                    throw new InvalidOperationException("Could not determine the branch name from GitHub Actions.");
                }
            }

            return await CreateReleaseAndUploadAsset(release, assetPath);
        }

        /// <summary>
        /// Retrieves the name of the branch from GitHub that matches the current commit SHA.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a result of the branch name as a <see cref="string"/> if found; otherwise, <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method queries the GitHub API for all branches in the specified repository and compares each branch's commit SHA
        /// with the current commit SHA. If a match is found, the corresponding branch name is returned.
        /// </remarks>
        private async Task<string?> GetBranchNameFromGitHubActions()
        {
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/branches";
            var response = await ApiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var branches = JsonDocument.Parse(content).RootElement.EnumerateArray();
                foreach (var branch in branches)
                {
                    var commitSha = await GetCurrentCommitSha();
                    if (branch.GetProperty("commit").GetProperty("sha").GetString() == commitSha)
                    {
                        var branchInGitHubActions = branch.GetProperty("name").GetString();
                        Console.WriteLine($"branch In GitHubActions: {branchInGitHubActions}");
                        Console.WriteLine($"CommitSha In GitHubActions: {commitSha}");
                        return branchInGitHubActions;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Asynchronously retrieves the SHA of the current commit (HEAD) from the GitHub repository.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous operation, containing the SHA string of the current commit.
        /// Returns an empty string if the request is unsuccessful or the SHA cannot be retrieved.
        /// </returns>
        private async Task<string> GetCurrentCommitSha()
        {
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/commits/HEAD";
            var response = await ApiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content).RootElement.GetProperty("sha").GetString() ?? string.Empty;
            }
            return string.Empty;
        }


        /// <summary>
        /// Validates the asset path.
        /// </summary>
        /// <param name="assetPath">The path to the asset.</param>
        /// <exception cref="FileNotFoundException">Thrown when the file at the specified path does not exist.</exception>
        private void ValidateAssetPath(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                throw new FileNotFoundException($"File {assetPath} does not exist");
            }
        }

        /// <summary>
        /// Deletes an existing release with the same tag name as the provided release, if it exists.
        /// </summary>
        /// <param name="release">The release whose tag name is used to find and delete an existing release.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the deletion of the existing release fails.
        /// </exception>
        private async Task DeleteExistingRelease(Release release)
        {
            var releaseId = await GetReleaseByTagNameAsync(release.TagName!);
            if (releaseId.HasValue)
            {
                var responseReleaseExist = await DeleteReleaseAsync(releaseId.Value);
                if (!responseReleaseExist.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Failed to delete release {release.TagName}. Status code: {responseReleaseExist.StatusCode}");
                }
            }
        }

        /// <summary>
        /// Deletes all staging releases if the provided release is a production release.
        /// </summary>
        /// <param name="release">The release to be checked.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the deletion of staging releases fails.</exception>
        private async Task DeleteStagingReleasesIfProduction(Release release)
        {
            if (IsProdTag(release.TagName!))
            {
                Console.WriteLine("Production release");
                var tags = await GetReleaseTagsAsync();
                var responseMessage = await DeleteStagingReleases(tags);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Failed to delete staging releases");
                }
            }
            else
            {
                Console.WriteLine("Not a production release");
            }
        }

        /// <summary>
        /// Deletes all staging releases from the provided list of tags.
        /// </summary>
        /// <param name="tags">The list of tags to check for staging releases.</param>
        /// <returns>
        /// A <see cref="Task{HttpResponseMessage}"/> representing the asynchronous operation, containing the HTTP response message of the last deletion operation.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the deletion of a staging release fails.
        /// </exception>
        public async Task<HttpResponseMessage> DeleteStagingReleases(List<string> tags)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            foreach (var tag in tags.Where(IsStageTag))
            {
                var releaseId = await GetReleaseByTagNameAsync(tag);
                if (releaseId.HasValue)
                {
                    response = await DeleteReleaseAsync(releaseId.Value);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException($"Failed to delete staging release with tag {tag}. Status code: {response.StatusCode}");
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Updates the release notes of the provided release.
        /// </summary>
        /// <param name="release">The release whose notes are to be updated.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no commits are found since the last release.</exception>
        public async Task UpdateReleaseNotes(Release release)
        {
            Console.WriteLine("Updating release notes...");
            var (sinceLastPublished, sinceTag) = await GetLatestReleasePublishedAtAndTagAsync(release.TargetCommitish!);
            Console.WriteLine($"sinceLastPublished: {sinceLastPublished}");
            Console.WriteLine($"sinceTag: {sinceTag}");

            var commitService = new CommitService(ApiService, Repo);

            Console.WriteLine("Getting commits since the last release...");
            var commits = await commitService.GetCommits(release.TargetCommitish!, sinceLastPublished);
            Console.WriteLine($"commits: {commits.Count}");

            if (commits.Count <= 0)
            {
                //throw new InvalidOperationException("No commits found since the last release.");
            }

            var releaseFormatter = new ReleaseFormatter(ApiService, Repo);
            var releaseNotes = await releaseFormatter.FormatAsync(commits, sinceTag, sinceLastPublished, release.TagName!);
            release.Body = releaseNotes ?? "No release notes available.";
        }

        /// <summary>
        /// Creates a release on GitHub and uploads an asset to it.
        /// </summary>
        /// <param name="release">The <see cref="Release"/> object containing release details to be created.</param>
        /// <param name="assetPath">The file path of the asset to upload to the created release.</param>
        /// <returns>
        /// A <see cref="Task{HttpResponseMessage}"/> representing the asynchronous operation, containing the HTTP response message from the asset upload.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the release creation or asset upload fails, or if the upload URL cannot be extracted from the GitHub API response.
        /// </exception>
        private async Task<HttpResponseMessage> CreateReleaseAndUploadAsset(Release release, string assetPath)
        {
            var context = new JsonContext();
            string jsonBody = JsonSerializer.Serialize(release, context.Release);

            // Verbose logging: show all key elements
            Console.WriteLine("[VERBOSE] --- CreateReleaseAndUploadAsset ---");
            Console.WriteLine($"[VERBOSE] Repo: {Repo}");
            Console.WriteLine($"[VERBOSE] Release Tag: {release.TagName}");
            Console.WriteLine($"[VERBOSE] Target Commitish: {release.TargetCommitish}");
            Console.WriteLine($"[VERBOSE] Asset Path: {assetPath}");
            Console.WriteLine($"[VERBOSE] Release JSON Body: {jsonBody}");

            // Send a POST request to create a new release on GitHub
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases";
            Console.WriteLine($"[VERBOSE] POST URI: {uri}");
            var response = await ApiService.PostAsync(uri, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[VERBOSE] Release creation response status: {response.StatusCode}");
            Console.WriteLine($"[VERBOSE] Release creation response body: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Error: Could not create a release: {release.TagName}. Response: {responseContent}");
            }

            Console.WriteLine($"[VERBOSE] Successfully created a release {release.TagName}. Uploading asset...");

            // Extract the upload URL from the response
            var responseObject = JsonDocument.Parse(responseContent);
            var uploadUrlDynamic = responseObject.RootElement.GetProperty("upload_url").GetString();
            if (uploadUrlDynamic is string uploadUrl)
            {
                uploadUrl = uploadUrl.Replace("{?name,label}", $"?name={Path.GetFileName(assetPath)}");
                Console.WriteLine($"[VERBOSE] Asset upload URL: {uploadUrl}");
            }
            else
            {
                Console.WriteLine("[VERBOSE] Failed to extract upload URL from the response.");
                throw new InvalidOperationException("Failed to extract upload URL from the response.");
            }

            // Upload the asset
            response = await UploadAsset(assetPath, uploadUrl);
            var assetUploadContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[VERBOSE] Asset upload response status: {response.StatusCode}");
            Console.WriteLine($"[VERBOSE] Asset upload response body: {assetUploadContent}");
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Error: Could not upload the asset: {assetPath}. Response: {assetUploadContent}");
            }

            Console.WriteLine($"[VERBOSE] Successfully uploaded the asset: {assetPath}.");
            return response;
        }

        /// <summary>
        /// Retrieves release information from the GitHub API for the specified branch.
        /// </summary>
        /// <param name="branch">The branch name to filter releases by. If <c>null</c>, returns all releases.</param>
        /// <returns>
        /// A <see cref="Task{JsonDocument}"/> representing the asynchronous operation, containing the release information as a <see cref="JsonDocument"/> object.
        /// Returns <c>null</c> if no releases are found.
        /// </returns>
        public async Task<JsonDocument?> GetReleasesAsync(string branch)
        {
            if (AuthService != null)
            {
                await ApiService.SetupHeadersAsync($"https://github.com/{Repo}", GitHubOperation.Read);
            }
            else
            {
                ApiService.SetupHeaders();
            }
            var releases = await GetLatestReleaseRawAsync(branch);
            if (releases == null)
            {
                Console.WriteLine($"No releases found on {branch}");
                // Get the latest release on the main branch
                releases = await GetLatestReleaseRawAsync("main");
            }

            if (releases != null)
            {
                Debug.WriteLine($"Releases JSON: {releases.RootElement}");
            }

            return releases;
        }

        /// <summary>
        /// Retrieves the list of release tags from the GitHub API for the current repository.
        /// </summary>
        /// <remarks>
        /// This method sends a GET request to the GitHub API to fetch all releases for the specified repository.
        /// It parses the response JSON to extract the tag names and returns them as a list of strings.
        /// If the request fails, it logs the error details and returns an empty list.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{List{string}}"/> representing the asynchronous operation, containing the list of release tag names.
        /// </returns>
        public async Task<List<string>> GetReleaseTagsAsync()
        {
            if (AuthService != null)
            {
                await ApiService.SetupHeadersAsync($"https://github.com/{Repo}", GitHubOperation.Read);
            }
            else
            {
                ApiService.SetupHeaders();
            }
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var releases = JsonDocument.Parse(content).RootElement.EnumerateArray();

                var tags = new List<string>();
                foreach (var release in releases)
                {
                    var tagName = release.GetProperty(PropertyNameTagName).GetString();
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        tags.Add(tagName);
                    }
                }

                return tags;
            }
            else
            {
                Console.WriteLine($"Failed to retrieve tags. Status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return new List<string>();
            }
        }

        /// <summary>
        /// Retrieves the IDs of all releases from the GitHub API for the current repository.
        /// </summary>
        /// <remarks>
        /// This method sends a GET request to the GitHub API to fetch all releases for the specified repository.
        /// It parses the response JSON to extract the release IDs and returns them as a list of integers.
        /// If the request fails, it logs the error details and returns an empty list.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{List{int}}"/> representing the asynchronous operation, containing the list of release IDs.
        /// </returns>
        public async Task<List<int>> GetReleaseIdsAsync()
        {
            if (AuthService != null)
            {
                await ApiService.SetupHeadersAsync($"https://github.com/{Repo}", GitHubOperation.Read);
            }
            else
            {
                ApiService.SetupHeaders();
            }
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var releases = JsonDocument.Parse(content).RootElement.EnumerateArray().ToList();

                var releaseIds = releases.Select(r => r.GetProperty("id").GetInt32()).ToList();
                return releaseIds;
            }
            else
            {
                Console.WriteLine($"Failed to retrieve releases. Status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return new List<int>();
            }
        }

        /// <summary>
        /// Retrieves the latest release information from the GitHub API.
        /// </summary>
        /// <param name="branch">The branch name to filter releases by. If <c>null</c>, returns all releases.</param>
        /// <returns>
        /// A <see cref="Task{JsonDocument}"/> representing the asynchronous operation, containing the latest release information as a <see cref="JsonDocument"/> object.
        /// Returns <c>null</c> if no release is found.
        /// </returns>
        private async Task<JsonDocument?> GetLatestReleaseRawAsync(string? branch = null)
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases";
            var response = await ApiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Releases JSON Content: {content}");

                // return all releases on all branches
                if (branch == null)
                {
                    return await FormatResponse(response);
                }

                var releases = JsonDocument.Parse(content).RootElement.EnumerateArray().ToList();

                var releaseOnBranch = releases
                    .OrderByDescending(r => r.GetProperty("created_at").GetString())
                    .FirstOrDefault(r => r.GetProperty("target_commitish").GetString() == branch);

                if (releaseOnBranch.ValueKind != JsonValueKind.Undefined)
                {
                    // Console the published_at date of the latest release
                    var publishedAt = releases[0].GetProperty("published_at").GetString();
                    Console.WriteLine($"published_at: {publishedAt}");
                    // return the latest release on the specified branch as a JsonDocument object
                    return JsonDocument.Parse(releaseOnBranch.GetRawText());
                }
            }
            else
            {
                Console.WriteLine($"Releases not found. Status code: {response.StatusCode}");
                // print response content to console for debugging
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            return null;
        }


        /// <summary>
        /// Retrieves the published date and tag name of the latest release from the GitHub API.
        /// </summary>
        /// <param name="branch">The branch name to filter releases by. If <c>null</c>, retrieves the latest release on any branch.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing a tuple with:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <c>string</c>: The published date of the latest release, or a default value if not found.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>string</c>: The tag name of the latest release, or a default value if not found.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the latest release cannot be retrieved or parsed.</exception>
        public async Task<(string, string)> GetLatestReleasePublishedAtAndTagAsync(string branch)
        {
            var releases = await GetReleasesAsync(branch);
            if (releases == null)
            {
                return (FirstDate, NoneFound);
            }

            try
            {
                var jsonDocument = JsonDocument.Parse(releases.RootElement.GetRawText());
                var root = jsonDocument.RootElement;

                string publishedAt = root.GetProperty("published_at").GetString() ?? NoneFound;
                string tagName = root.GetProperty("tag_name").GetString() ?? NoneFound;

                Console.WriteLine($"Published At: {publishedAt}");
                Console.WriteLine($"Tag Name: {tagName}");

                return (publishedAt, tagName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get the latest release. Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses the HTTP response from the GitHub API and returns the content as a <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> received from the GitHub API.</param>
        /// <returns>
        /// A <see cref="Task{JsonDocument}"/> representing the asynchronous operation, containing the parsed JSON document if successful; otherwise, <c>null</c> if the content is empty or cannot be parsed.
        /// </returns>
        private async Task<JsonDocument?> FormatResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            try
            {
                var jsonDocument = JsonDocument.Parse(content);
                return jsonDocument;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse response content to JSON. Exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the specified release from GitHub.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="releaseId">The ID of the release to delete.</param>
        /// <returns>The response from deleting the release.</returns>
        public async Task<HttpResponseMessage> DeleteReleaseAsync(int releaseId)
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases/{releaseId}";
            Console.WriteLine($"DELETE uri: {uri}");
            var response = await ApiService.DeleteAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully deleted the release: {releaseId}.");
            }
            else
            {
                Console.WriteLine($"Failed to delete the release: {releaseId}. Status code: {response.StatusCode}");
            }

            return response;
        }

        /// <summary>
        /// Retrieves the release ID for a given tag name from the GitHub API.
        /// </summary>
        /// <param name="tagName">The tag name of the release.</param>
        /// <returns>
        /// A <see cref="Task{Nullable{Int32}}"/> representing the asynchronous operation, containing the release ID if found; otherwise, <c>null</c>.
        /// </returns>
        public async Task<int?> GetReleaseByTagNameAsync(string tagName)
        {
            ApiService.SetupHeaders();

            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases/tags/{tagName}";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // ...

                var release = JsonDocument.Parse(content);
                return (release?.RootElement.GetProperty("id").GetInt32() ?? 0);
            }
            else
            {
                Console.WriteLine($"release by tag name {tagName} not found. Status code: {response.StatusCode}");
                return null;
            }
        }

        /// <summary>
        /// Uploads an asset to a GitHub release.
        /// </summary>
        /// <param name="assetPath">The file path of the asset to upload.</param>
        /// <param name="uploadUrl">The upload URL provided by the GitHub API (should include query parameters for name/label).</param>
        /// <returns>
        /// A <see cref="Task{HttpResponseMessage}"/> representing the asynchronous operation, containing the HTTP response message from the upload operation.
        /// Returns <see cref="HttpStatusCode.NotFound"/> if the file does not exist.
        /// </returns>
        /// <remarks>
        /// This method uploads a file as a release asset to the specified GitHub release upload URL.
        /// The asset is read from <paramref name="assetPath"/> and sent as a POST request to <paramref name="uploadUrl"/>.
        /// The content type is set to <c>application/octet-stream</c>.
        /// The method logs details about the upload and the response.
        /// </remarks>
        private async Task<HttpResponseMessage> UploadAsset(string assetPath, string uploadUrl)
        {
            if (!File.Exists(assetPath))
            {
                Console.WriteLine($"File {assetPath} does not exist");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            ApiService.SetupHeaders();

            var assetMimeType = "application/octet-stream";
            var assetContent = File.ReadAllBytes(assetPath);

            // Create a ByteArrayContent object with the asset content
            var byteArrayContent = new ByteArrayContent(assetContent);

            // Set the Content-Type header to the specified assetMimeType
            byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(assetMimeType);

            // Log the request details
            Console.WriteLine($"Uploading asset to URL: {uploadUrl}");
            Console.WriteLine($"Asset Path: {assetPath}");
            Console.WriteLine($"Asset MIME Type: {assetMimeType}");
            Console.WriteLine($"Asset Content Length: {assetContent.Length} bytes");

            // Make the POST request with the specified assetMimeType to upload the asset
            var uploadResponse = await ApiService.PostAsync(uploadUrl, byteArrayContent);

            // Log the response details
            Console.WriteLine($"Response Status Code: {uploadResponse.StatusCode}");
            var responseContent = await uploadResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {responseContent}");
            return uploadResponse;
        }
        /// <summary>
        /// Retrieves the release information for a specific release ID from the GitHub API.
        /// </summary>
        /// <param name="releaseId">The ID of the release to retrieve.</param>
        /// <returns>
        /// A <see cref="Task{Release}"/> representing the asynchronous operation, containing the <see cref="Release"/> object if found; otherwise, <c>null</c>.
        /// </returns>
        public async Task<Release?> GetReleaseAsync(int releaseId)
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases/{releaseId}";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var context = new JsonContext();
                var release = JsonSerializer.Deserialize<Release>(content, context.Release);

                if (release != null)
                {
                    if (release.Draft)
                    {
                        Console.WriteLine("The release is a draft.");
                    }
                    else if (release.Prerelease)
                    {
                        Console.WriteLine("The release is a pre-release.");
                    }
                    else
                    {
                        Console.WriteLine("The release is a normal release.");
                    }
                }
                return release;
            }
            else
            {
                Console.WriteLine($"Failed to retrieve release {releaseId}. Status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return null;
            }
        }

        /// <summary>
        /// Determines whether the given tag is a staging tag.
        /// A staging tag is a tag with a version number that does not end with ".0".
        /// For example, "1.2.3" is a staging tag, while "1.2.0" is not.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag is a staging tag, false otherwise.</returns>
        private static bool IsStageTag(string? tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            // tag is x.y.z is staging if z is not 0
            var parts = tag.Split('.');
            return parts.Length == 3 && parts[2] != "0";
        }

        /// <summary>
        /// Determines whether the given tag is a production tag.
        /// A production tag is a tag with a version number that ends with ".0".
        /// For example, "1.2.0" is a production tag, while "1.2.3" is not.
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag is a production tag, false otherwise.</returns>
        private static bool IsProdTag(string? tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            // tag is x.y.z production if z is 0
            var parts = tag.Split('.');
            return parts.Length == 3 && parts[2] == "0";
        }

        #region Download Release

        /// <summary>
        /// Downloads a release asset by its asset ID and saves it to the specified file.
        /// </summary>
        /// <param name="assetId">The ID of the asset to download.</param>
        /// <param name="assetFileName">The full path where the asset will be saved.</param>
        /// <returns>
        /// A <see cref="Task{HttpResponseMessage}"/> representing the asynchronous operation, containing the HTTP response message from the download operation.
        /// </returns>
        /// <remarks>
        /// This method downloads a release asset from GitHub using its asset ID and saves it to the specified file path.
        /// </remarks>
        public async Task<HttpResponseMessage> DownloadAsset(int assetId, string assetFileName)
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases/assets/{assetId}";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var assetContent = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(assetFileName, assetContent);
                Console.WriteLine($"Successfully downloaded the asset to: {assetFileName}");
            }
            else
            {
                Console.WriteLine($"Error: Could not download the asset. Status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }

            return response;
        }


        /// <summary>
        /// Downloads an asset by its name from a specific release tag.
        /// </summary>
        /// <param name="tagName">The tag name of the release.</param>
        /// <param name="assetName">The name of the asset to download.</param>
        /// <param name="downloadPath">The path where the asset will be downloaded.</param>
        /// <returns>
        /// A <see cref="Task{HttpResponseMessage}"/> representing the asynchronous operation, containing the HTTP response message from the download operation.
        /// </returns>
        /// <remarks>
        /// This method retrieves the release ID associated with the specified tag name and fetches the list of assets for that release.
        /// It then searches for the asset by its name and downloads it to the specified path.
        /// If the release or asset is not found, an appropriate HTTP response with a status code of <see cref="HttpStatusCode.NotFound"/> is returned.
        /// </remarks>
        public async Task<HttpResponseMessage> DownloadAssetByName(string tagName, string assetName, string downloadPath)
        {
            var releaseId = await GetReleaseByTagNameAsync(tagName);
            if (!releaseId.HasValue)
            {
                Console.WriteLine($"Release with tag {tagName} not found.");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases/{releaseId}/assets";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var assets = JsonDocument.Parse(content).RootElement.EnumerateArray();
                var asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == assetName);

                if (asset.ValueKind != JsonValueKind.Undefined)
                {
                    // Use the "url" property to get the download URL. This is essential for downloading assets, including those from private repositories, as it provides the authenticated endpoint for the asset.
                    var downloadUrl = asset.GetProperty("url").GetString();
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        // Build full filename with path
                        var assetFileName = Path.Combine(downloadPath, assetName);
                        response = await DownloadAssetFromUrl(downloadUrl, assetFileName);
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Successfully downloaded the asset to: {assetFileName}");
                            return response;
                        }
                        else
                        {
                            Console.WriteLine($"Error: Could not download the asset. Status code: {response.StatusCode}");
                            var contentError = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(contentError);
                            return response;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Download URL for asset {assetName} not found.");
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    Console.WriteLine($"Asset with name {assetName} not found in release {tagName}.");
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
            else
            {
                Console.WriteLine($"Failed to get assets for release {tagName}. Status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return response;
            }
        }

        /// <summary>
        /// Checks the permissions (scopes) of the current GitHub token by making a request to the GitHub API.
        /// </summary>
        /// <remarks>
        /// This method sends a GET request to the GitHub API user endpoint to retrieve the token's scopes from the response headers.
        /// It logs the Authorization header, the token scopes, and any errors encountered.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task CheckTokenPermissions()
        {
            // Setup headers with Authorization
            ApiService.SetupHeaders();

            // Log the Authorization header
            var authorizationHeader = ApiService.GetClient().DefaultRequestHeaders.Authorization;
            if (authorizationHeader == null)
            {
                Console.WriteLine("Authorization header is not set.");
                throw new InvalidOperationException("Authorization header is not set.");
            }

            // Make a request to the GitHub API to check token permissions
            var uri = "https://api.github.com/user";
            var response = await ApiService.GetAsync(uri);

            // Log the response headers
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.IsSuccessStatusCode)
            {
                // Log the scopes
                if (response.Headers.TryGetValues("X-OAuth-Scopes", out var scopes))
                {
                    Console.WriteLine($"Token scopes: {string.Join(", ", scopes)}");
                }
                else
                {
                    Console.WriteLine("Token scopes not found in the response headers.");
                }
            }
            else
            {
                Console.WriteLine($"Error: Could not retrieve token permissions. Status code: {response.StatusCode}");
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {responseContent}");
            }
        }

        /// <summary>
        /// Downloads an asset from a specified URL and saves it to a file.
        /// </summary>
        /// <param name="downloadUrl">The URL of the asset to download.</param>
        /// <param name="assetFileName">The full path where the asset will be saved.</param>
        /// <returns>
        /// A <see cref="Task{HttpResponseMessage}"/> representing the asynchronous operation, containing the HTTP response message from the download operation.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="ApiService"/> to set up headers and perform the download operation.
        /// If the download is successful, the asset is saved to the specified file path.
        /// For larger files, a streaming approach is used to avoid memory issues.
        /// </remarks>
        public async Task<HttpResponseMessage> DownloadAssetFromUrl(string downloadUrl, string assetFileName)
        {
            ApiService.SetupHeaders(download: true);

            var response = await ApiService.GetClient().GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var assetContent = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(assetFileName, assetContent);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"An error occurred while downloading the asset from URL: {downloadUrl}. Exception: {ex.Message}", ex);
                }
            }
            else
            {
                Console.WriteLine($"Error: Could not download the asset. Status code: {response.StatusCode}");
            }

            return response;
        }


        /// <summary>
        /// Verifies whether a specific asset ID exists in a release identified by its tag name.
        /// </summary>
        /// <param name="tagName">The tag name of the release.</param>
        /// <param name="assetId">The ID of the asset to verify.</param>
        /// <returns>
        /// A <see cref="Task{bool}"/> representing the asynchronous operation, containing <c>true</c> if the asset ID exists in the release; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> VerifyAssetId(string tagName, int assetId)
        {
            var releaseId = await GetReleaseByTagNameAsync(tagName);
            if (!releaseId.HasValue)
            {
                Console.WriteLine($"Release with tag {tagName} not found.");
                return false;
            }

            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases/{releaseId}/assets";
            var response = await ApiService.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var assets = JsonDocument.Parse(content).RootElement.EnumerateArray();
                foreach (var asset in assets)
                {
                    if (asset.GetProperty("id").GetInt32() == assetId)
                    {
                        return true;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Failed to get assets for release {tagName}. Status code: {response.StatusCode}");
            }

            return false;
        }

        /// <summary>
        /// Lists latest 3 releases for the specified repository (and latest pre-release if newer).
        /// </summary>
        /// <param name="verbose">If true, includes additional details for each release.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<Release>> ListReleasesAsync(bool verbose)
        {
            await ApiService.SetupHeadersAsync($"https://github.com/{Repo}", GitHubOperation.Read);
            var uri = $"{Constants.GitHubApiPrefix}/{Repo}/releases";
            var response = await ApiService.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponse(response);
                return new List<Release>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var releases = JsonDocument.Parse(content).RootElement.EnumerateArray().ToList();

            if (!releases.Any())
            {
                Console.WriteLine("No releases found.");
                return new List<Release>();
            }

            var finalReleases = SelectLatestReleases(releases);
            return ParseReleases(finalReleases, verbose);
        }

        private async Task HandleErrorResponse(HttpResponseMessage response)
        {
            Console.WriteLine($"Failed to retrieve releases. Status code: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
        }

        private List<JsonElement> SelectLatestReleases(List<JsonElement> releases)
        {
            var stableReleases = releases
                .Where(r => !r.GetProperty("prerelease").GetBoolean())
                .Take(3)
                .ToList();

            var preReleases = releases
                .Where(r => r.GetProperty("prerelease").GetBoolean())
                .ToList();

            var latestStable = stableReleases.FirstOrDefault();
            var latestPreRelease = preReleases.FirstOrDefault();

            if (IsPreReleaseNewer(latestPreRelease, latestStable))
            {
                stableReleases.Insert(0, latestPreRelease);
            }

            return stableReleases;
        }

        private bool IsPreReleaseNewer(JsonElement pre, JsonElement stable)
        {
            if (pre.ValueKind == JsonValueKind.Undefined || stable.ValueKind == JsonValueKind.Undefined)
                return false;

            var preDateStr = pre.GetProperty("published_at").GetString();
            var stableDateStr = stable.GetProperty("published_at").GetString();

            return DateTime.TryParse(preDateStr, out var preDate) &&
                   DateTime.TryParse(stableDateStr, out var stableDate) &&
                   preDate > stableDate;
        }

        private List<Release> ParseReleases(List<JsonElement> elements, bool verbose)
        {
            var result = new List<Release>();

            foreach (var release in elements)
            {
                var releaseObj = new Release
                {
                    TagName = release.GetProperty("tag_name").GetString(),
                    Name = release.GetProperty("name").GetString() ?? "Unnamed release",
                    Prerelease = release.GetProperty("prerelease").GetBoolean(),
                    PublishedAt = release.GetProperty("published_at").GetString(),
                    Body = release.GetProperty("body").GetString() ?? "No description available.",
                    Assets = verbose ? ParseAssets(release) : new List<Asset>()
                };

                result.Add(releaseObj);
            }

            return result;
        }

        private List<Asset> ParseAssets(JsonElement release)
        {
            var assets = new List<Asset>();

            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                assets.Add(new Asset
                {
                    Name = asset.GetProperty("name").GetString(),
                    Size = asset.GetProperty("size").GetInt32(),
                    BrowserDownloadUrl = asset.GetProperty("browser_download_url").GetString(),
                    // replace Uploader = asset.GetProperty("uploader").GetProperty("login").GetString()
                    // to avoid an exception if "uploader" is not found
                    Uploader = asset.TryGetProperty("uploader", out var uploader) &&
                              uploader.TryGetProperty("login", out var login)
                              ? login.GetString() : null
                });
            }

            return assets;
        }
        #endregion
    }
}
