using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GitHubRelease
{
    /// <summary>
    /// Service class for creating GitHub releases and uploading assets.
    /// </summary>
    public class ReleaseService(string repo) : Constants
    {
        private readonly ApiService ApiService = new();
        public readonly string Repo = repo;
        public readonly string FirstDate = "1970-01-01T00:00:00Z";
        private const string NoneFound = "N/A";

        /// <summary>
        /// Creates a release with the specified release object, and asset path.
        /// </summary>
        /// <param name="token">The token for authentication.</param>
        /// <param name="release">The release object.</param>
        /// <param name="assetPath">The path to the asset.</param>
        /// <returns>The HTTP response message.</returns>
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

        private async Task<string?> GetBranchNameFromGitHubActions()
        {
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/branches";
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

        private async Task<string> GetCurrentCommitSha()
        {
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/commits/HEAD";
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
        /// Deletes an existing release.
        /// </summary>
        /// <param name="token">The authentication token.</param>
        /// <param name="release">The release to be deleted.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the deletion of the release fails.</exception>
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
        /// Deletes staging releases if the provided release is a production release.
        /// </summary>
        /// <param name="token">The authentication token.</param>
        /// <param name="release">The release to be checked.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the deletion of staging releases fails.</exception>
        private async Task DeleteStagingReleasesIfProduction(Release release)
        {
            if (IsProductionTag(release.TagName!))
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
        /// <param name="token">The authentication token.</param>
        /// <param name="tags">The list of tags to check for staging releases.</param>
        /// <returns>A Task representing the asynchronous operation, containing the HTTP response message of the last deletion operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the deletion of a staging release fails.</exception>
        public async Task<HttpResponseMessage> DeleteStagingReleases(List<string> tags)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            foreach (var tag in tags.Where(IsStagingTag))
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
        /// <param name="token">The authentication token.</param>
        /// <param name="release">The release whose notes are to be updated.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no commits are found since the last release.</exception>
        public async Task UpdateReleaseNotes(Release release)
        {
            Console.WriteLine("Updating release notes...");
            var (sinceLastPublished, sinceTag)  = await GetLatestReleasePublishedAtAndTagAsync(release.TargetCommitish!);
            Console.WriteLine($"sinceLastPublished: {sinceLastPublished}");
            Console.WriteLine($"sinceTag: {sinceTag}");

            var commitService = new CommitService(ApiService, Repo);

            Console.WriteLine("Getting commits since the last release...");
            var commits = await commitService.GetCommits(release.TargetCommitish!, sinceLastPublished);

            if (commits.Count <= 0)
            {
                //throw new InvalidOperationException("No commits found since the last release.");
            }

            var releaseFormatter = new ReleaseFormatter(ApiService, Repo);
            var releaseNotes = await releaseFormatter.FormatAsync(commits, sinceTag, sinceLastPublished, release.TagName!);
            release.Body = releaseNotes ?? "No release notes available.";
        }

        /// <summary>
        /// Creates a release and uploads an asset to it.
        /// </summary>
        /// <param name="token">The authentication token.</param>
        /// <param name="release">The release to be created.</param>
        /// <param name="assetPath">The path to the asset to be uploaded.</param>
        /// <returns>A Task representing the asynchronous operation, containing the HTTP response message.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the creation of the release or the upload of the asset fails.</exception>
        private async Task<HttpResponseMessage> CreateReleaseAndUploadAsset(Release release, string assetPath)
        {
            var context = new JsonContext();
            string jsonBody = JsonSerializer.Serialize(release, context.Release);

            // Log the JSON body for debugging
            Console.WriteLine($"Release JSON Body: {jsonBody}");

            HttpResponseMessage response = await UploadAsset(jsonBody);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Error: Could not create a release: {release.TagName}. Response: {responseContent}");
            }
            Console.WriteLine($"Successfully created a release {release.TagName}. uploading asset");
            response = await UploadAsset(assetPath, response);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Error: Could not upload the asset:{assetPath}.");
            }
            Console.WriteLine($"Successfully uploaded the asset: {assetPath}.");
            return response;
        }

        private async Task<HttpResponseMessage> UploadAsset(string jsonBody)
        {
            // Send a POST request to create a new release on GitHub
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases";

            var response = await ApiService.PostAsync(uri, new StringContent(jsonBody, Encoding.UTF8, "application/json"));
            return response;
        }

        /// <summary>
        /// Gets the releases information from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="branch">The branch name. Set to null if requesting release on any branch.</param>
        /// <returns>The latest release information as a <see cref="JsonDocument"/> object.
        /// null if no release is not found.
        /// </returns>
        public async Task<JsonDocument?> GetReleasesAsync(string branch)
        {
            ApiService.SetupHeaders();
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
        /// Gets the release tags from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <returns>The list of release tags.</returns>
        public async Task<List<string>> GetReleaseTagsAsync()
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases";
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

        public async Task<List<int>> GetReleaseIdsAsync()
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases";
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
        /// Gets the latest release information from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <returns>The latest release information as a <see cref="JsonDocument"/> object.
        /// null if no release is not found.
        /// </returns>
        private async Task<JsonDocument?> GetLatestReleaseRawAsync(string? branch = null)
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases";
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
        /// Gets the latest release publish date from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="branch">The branch name. Set to null if requesting release on any branch.</param>
        /// <returns>The latest release publish date as a string.</returns>
        //public async Task<string> GetLatestReleasePublishDateAsync(string branch)
        //{
        //    var latestRelease = await GetLatestReleaseAsync(branch);
        //    if (latestRelease != null && latestRelease.RootElement.GetArrayLength() > 0)
        //    {
        //        // get count
        //        Console.WriteLine($"Count: {latestRelease.RootElement.GetArrayLength()}");
        //        // Get the tag name of the latest release and print to console
        //        foreach (var release in latestRelease.RootElement.EnumerateArray())
        //        {
        //            // Get the tag name of the latest release and print to console
        //            Console.WriteLine($"tag: {release.GetProperty(PropertyNameTagName).GetString()}");
        //            // get the published date of the latest release and print to console
        //            Console.WriteLine($"published_at: {release.GetProperty("published_at").GetString()}");
        //        }
        //        Console.WriteLine($"published_at: {latestRelease.RootElement[0].GetProperty("published_at").GetString()}");
        //        return latestRelease.RootElement[0].GetProperty("published_at").GetString() ?? FirstDate;
        //    }
        //    return FirstDate;
        //}

        /// <summary>
        /// Gets the latest release published date and tag from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="branch">The branch name. Set to null if requesting release on any branch.</param>
        /// <returns>A tuple containing the latest release published date and tag.</returns>
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
        //public async Task<string> GetLatestReleaseTagAsync(string branch)
        //{
        //    var latestRelease = await GetLatestReleaseAsync(branch);
        //    if (latestRelease != null)
        //    {
        //        // get count
        //        Console.WriteLine($"Count: {latestRelease.RootElement.GetArrayLength()}");
        //        // Get the tag name of the latest release and print to console
        //        foreach (var release in latestRelease.RootElement.EnumerateArray())
        //        {
        //            // Get the tag name of the latest release and print to console
        //            Console.WriteLine($"tag: {release.GetProperty(PropertyNameTagName).GetString()}");
        //        }
        //        return latestRelease.RootElement[0].GetProperty(PropertyNameTagName).GetString() ?? "";
        //    }
        //    return "";
        //}
        /// <summary>
        /// Gets the latest release information from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <returns>The latest release information as a <see cref="JsonDocument"/> object.
        /// null if no release is not found.
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
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases/{releaseId}";
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
        /// Gets the release ID by tag name.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="tagName">The tag name of the release.</param>
        /// <returns>The release ID if found, otherwise null.</returns>
        public async Task<int?> GetReleaseByTagNameAsync(string tagName)
        {
            ApiService.SetupHeaders();

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases/tags/{tagName}";
            Console.WriteLine($"GET uri: {uri}");
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
        /// Uploads an asset to the GitHub release.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="release">The GitHub release information.</param>
        /// <param name="assetPath">The path to the asset file.</param>
        /// <param name="response">The response from creating the release.</param>
        /// <returns>The response from uploading the asset.</returns>
        private async Task<HttpResponseMessage> UploadAsset(string assetPath, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return response;
            }

            if (!File.Exists(assetPath))
            {
                Console.WriteLine($"File {assetPath} does not exist");
                return new(HttpStatusCode.NotFound);
            }

            ApiService.SetupHeaders();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = System.Text.Json.JsonDocument.Parse(responseContent);
            var uploadUrlDynamic = responseObject.RootElement.GetProperty("upload_url").GetString();
            if (uploadUrlDynamic is string uploadUrl)
            {
                uploadUrl = uploadUrl.Replace("{?name,label}", $"?name={Path.GetFileName(assetPath)}");
            }
            else
            {
                // Handle the case where uploadUrlDynamic is not a string.
                return new(HttpStatusCode.BadRequest);
            }

            var assetMimeType = "application/octet-stream";
            var assetContent = System.IO.File.ReadAllBytes(assetPath);

            // Create a ByteArrayContent object with the asset content
            var byteArrayContent = new ByteArrayContent(assetContent);

            // Set the Content-Type header to the specified assetMimeType
            byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(assetMimeType);

            // Make the POST request with the specified assetMimeType to upload the asset
            var uploadResponse = await ApiService.PostAsync(uploadUrl, byteArrayContent);

            return uploadResponse;
        }

        /// <summary>
        /// Gets the release information from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="releaseId">The ID of the release.</param>
        /// <returns>The release information as a <see cref="Release"/> object.</returns>
        public async Task<Release?> GetReleaseAsync(int releaseId)
        {
            ApiService.SetupHeaders();
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases/{releaseId}";
            Console.WriteLine($"GET uri: {uri}");
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
        /// A staging tag is a tag with a version number that does not with ".0".
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag is a staging tag, false otherwise.</returns>
        private static bool IsStagingTag(string? tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            // tag is x.y.z is staging if z is not 0
            var parts = tag.Split('.');
            return parts.Length == 3 && parts[2] != "0";
        }

        /// <summary>
        /// Determines whether the given tag is a producion tag.
        /// A production tag is a tag with a version number that ends with ".0".
        /// </summary>
        /// <param name="tag">The tag to check.</param>
        /// <returns>True if the tag is a production tag, false otherwise.</returns>
        private static bool IsProductionTag(string? tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            // tag is x.y.z production if z is 0
            var parts = tag.Split('.');
            return parts.Length == 3 && parts[2] == "0";
        }
    }
}
