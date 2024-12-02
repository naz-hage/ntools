using System.Net;
using System.Text;
using System.Text.Json;

namespace GitHubRelease
{
    /// <summary>
    /// Helper class for interacting with GitHub API.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="GitHubHelper"/> class.
    /// </remarks>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the GitHub repository.</param>
    public class GitHubHelper(string repo) : Constants
    {
        private readonly HttpClient Client = new();
        private readonly string Repo = repo;

        /// <summary>
        /// Gets the latest release information from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <returns>The latest release information as a <see cref="JsonDocument"/> object.
        /// null if no release is not found.
        /// </returns>
        public async Task<JsonDocument?> GetLatestReleaseRawAsync(string token)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            //_client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases/latest";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var latestRelease = JsonDocument.Parse(content);
                return latestRelease;
            }
            else
            {
                Console.WriteLine($"Latest release not found. Status code: {response.StatusCode}");
                // print response content to console
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            return null;
        }

        /// <summary>
        /// Gets the latest release notes from GitHub API.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="tag">The tag of the latest release.</param>
        /// <returns>The latest release notes as a string.</returns>
        public async Task<string?> GetLatestReleaseAsync(string token, string tag)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var latest = await GetLatestReleaseRawAsync(token);
            var latestReleaseTag = latest == null ? "" : latest.RootElement.GetProperty("tag_name").GetString();
            var sinceLastPublished = latest == null ? "1970-01-01T00:00:00Z" : latest.RootElement.GetProperty("published_at").GetString();

            DateTime date = DateTime.Parse(sinceLastPublished!);
            string iso8601DateSinceLastPublished = date.ToString("s") + "Z";

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/commits?since={iso8601DateSinceLastPublished}";

            // If there are no releases, get all commits
            if (latest == null)
            {
                uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/commits";
            }

            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var commits = JsonDocument.Parse(content).RootElement.EnumerateArray();
                if (commits.Count() > 0)
                {
                    // Need to find the tag of the previous release
                    //var sinceTag = await GetTagFromCommitAsync((string)commits[0]["sha"]);
                    var sinceTag = latestReleaseTag;

                    var releaseNotes = await FormatReleaseNotesAsync(commits, sinceTag, iso8601DateSinceLastPublished, tag);
                    return releaseNotes;
                }
            }
            return null;
        }

        /// <summary>
        /// Formats the release notes based on the commits.
        /// </summary>
        /// <param name="commits">The commits as a <see cref="JsonElement.ArrayEnumerator"/> object.</param>
        /// <param name="sinceTag">The tag of the previous release.</param>
        /// <param name="tag">The tag of the latest release.</param>
        /// <returns>The formatted release notes as a string.</returns>
        private async Task<string> FormatReleaseNotesAsync(JsonElement.ArrayEnumerator commits, string? sinceTag, string iso8601DateSinceLastPublished, string tag)
        {
            // Format the commit messages
            var releaseNotes = new StringBuilder();
            ///
            /// What's Changed
            ///
            releaseNotes.AppendLine("### What's Changed");
            var whatsChanged = await GetWhatsChangedAsync(commits, sinceTag, iso8601DateSinceLastPublished, tag);
            if (whatsChanged.Length > 0)
            {
                releaseNotes.AppendLine(whatsChanged.ToString());
            }


            ///
            /// New Contributors
            ///
            var newContributors = await GetNewContributorsAsync(commits);
            if (newContributors.Length > 0)
            {
                releaseNotes.AppendLine("\n\n### New Contributors");
                releaseNotes.AppendLine(newContributors.ToString());
            }

            ///
            /// Full Changelog
            /// 
            var fullChangelog = GetFullChangelog(sinceTag, tag);
            releaseNotes.AppendLine(fullChangelog.ToString());

            return releaseNotes.ToString();
        }

        private StringBuilder GetFullChangelog(string? sinceTag, string tag)
        {
            StringBuilder releaseNotes = new();
            if (string.IsNullOrEmpty(sinceTag))
            {
                releaseNotes.AppendLine($"\n\n**Full Changelog**: https://github.com/{Credentials.GetOwner()}/{Repo}/commits/{tag}");
            }
            else
            {
                releaseNotes.AppendLine($"\n\n**Full Changelog**: https://github.com/{Credentials.GetOwner()}/{Repo}/compare/{sinceTag}...{tag}");
            }

            return releaseNotes;
        }

        private async Task<List<string>> GetAllContributorsAsync()
        {
            var contributors = new List<string>();

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/contributors";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {

                var content = await response.Content.ReadAsStringAsync();
                var allContributors = JsonDocument.Parse(content).RootElement.EnumerateArray();
                foreach (var contributor in allContributors)
                {
                    contributors.Add(contributor.GetProperty("login").GetString() ?? string.Empty);
                }

                // remove duplicates and empty strings
                contributors = contributors.Distinct().Where(c => !string.IsNullOrEmpty(c)).ToList();
            }

            return contributors;
        }

        private async Task<StringBuilder> GetNewContributorsAsync(JsonElement.ArrayEnumerator commits)
        {
            StringBuilder releaseNotes = new();
            var contributors = await GetAllContributorsAsync();

            foreach (var commit in commits)
            {
                var author = commit.GetProperty("author").GetProperty("login").GetString();
                if (author != null && !contributors.Contains(author))
                {
                    var prUri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/commits/{commit.GetProperty("sha").GetString()}/pulls";
                    Console.WriteLine($"GET uri: {prUri}");
                    var prResponse = await Client.GetAsync(prUri);
                    if (prResponse.IsSuccessStatusCode)
                    {
                        var prContent = await prResponse.Content.ReadAsStringAsync();
                        var pulls = JsonDocument.Parse(prContent).RootElement.EnumerateArray();
                        if (pulls.Count() > 0)
                        {
                            var prNumber = pulls.ElementAt(0).GetProperty("number").GetString();
                            releaseNotes.AppendLine($"* @{author} made their first contribution in https://github.com/{Credentials.GetOwner()}/{Repo}/pull/{prNumber}");
                        }
                    }
                }
            }

            return releaseNotes;
        }

        private async Task<StringBuilder> GetWhatsChangedAsync(JsonElement.ArrayEnumerator commits, string? sinceTag, string lastPublishedAt, string tag)
        {
            StringBuilder releaseNotes = new();
            foreach (var commit in commits)
            {
                // How do I get the published_at date of the commit?
                var publishedAt = commit.GetProperty("commit").GetProperty("author").GetProperty("date").GetString();

                // If the commit is before the lastPublishedAt, skip it
                if (DateTime.Parse(publishedAt!) < DateTime.Parse(lastPublishedAt))
                {
                    continue;
                }



                var message = commit.GetProperty("commit").GetProperty("message").GetString() ?? string.Empty;
                var author = commit.GetProperty("author").GetProperty("login").GetString();
                var sha = commit.GetProperty("sha").GetString();
                var commitTag = await GetTagFromCommitAsync(sha!);

                // If the commit is tagged and less than SinceTag, skip it
                if (commitTag != null && sinceTag != null && string.Compare(commitTag, sinceTag) <= 0)
                {
                    continue;
                }

                var prUri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/commits/{sha}/pulls";
                Console.WriteLine($"GET uri: {prUri}");
                var prResponse = await Client.GetAsync(prUri);
                if (prResponse.IsSuccessStatusCode)
                {
                    var prContent = await prResponse.Content.ReadAsStringAsync();
                    var pulls = JsonDocument.Parse(prContent).RootElement.EnumerateArray();
                    if (pulls.Count() > 0)
                    {
                        try
                        {
                            var prNumber = pulls.ElementAt(0).GetProperty("number").GetInt32().ToString();
                            releaseNotes.AppendLine($"* {message} by @{author} in https://github.com/{Credentials.GetOwner()}/{Repo}/pull/{prNumber}");
                        }
                        catch (Exception ex)
                        {
                            // ignore and continue
                            Console.WriteLine($"Exception ignored: {ex.Message}");
                        }
                    }
                    else
                    {
                        releaseNotes.AppendLine($"* {commitTag} - {message} by @{author}");
                    }
                }
            }

            return releaseNotes;
        }

        /// Gets the tag name from a commit SHA.
        /// </summary>
        /// <param name="commitSha">The SHA of the commit.</param>
        /// <returns>The tag name as a string.</returns>

        private async Task<string?> GetTagFromCommitAsync(string commitSha)
        {
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/tags";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tags = JsonDocument.Parse(content).RootElement.EnumerateArray();
                foreach (var tag in tags)
                {
                    var commit = tag.GetProperty("commit");
                    if (commit.GetProperty("sha").GetString() == commitSha)
                    {
                        return tag.GetProperty("name").GetString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Creates and pushes a tag to the remote repository.
        /// </summary>
        /// <param name="tag">The tag name.</param>
        public static void CreateAndPushTag(string tag)
        {
            // get current branch
            var branch = RunGitCommand("rev-parse --abbrev-ref HEAD").Trim();
            // create a tag
            RunGitCommand($"tag {tag}");
            // push the tag to the remote
            RunGitCommand($"push origin {branch} {tag}");
        }

        private static string RunGitCommand(string arguments)
        {
            var process = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        /// Creates a new GitHub release and uploads an asset.
        /// </summary>
        /// <param name="token">The GitHub access token.</param>
        /// <param name="gitHubReleaseClass">The GitHub release information.</param>
        /// <param name="assetPath">The path to the asset file.</param>
        /// <returns>The response from creating the release and uploading the asset.</returns>
        public async Task<HttpResponseMessage> CreateGitHubRelease(string token, Release gitHubReleaseClass, string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                Console.WriteLine($"File {assetPath} does not exist");
                return new(HttpStatusCode.NotFound);
            }

            // Delete the release if it exist
            var releaseId = await GetReleaseByTagNameAsync(token, gitHubReleaseClass.TagName!);
            if (releaseId.HasValue)
            {
                var responseReleaseExist = await DeleteReleaseAsync(token, releaseId.Value);
                if (!responseReleaseExist.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to delete release {gitHubReleaseClass.TagName}. Status code: {responseReleaseExist.StatusCode}");
                    Console.WriteLine(await responseReleaseExist.Content.ReadAsStringAsync());
                    return responseReleaseExist;
                }
            }

            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var releaseNotes = await GetLatestReleaseAsync(token, gitHubReleaseClass.TagName!);
            gitHubReleaseClass.Body = releaseNotes ?? "No release notes available.";
            // trancate the body to num characters
            //var num = 9000;
            //if (gitHubReleaseClass.Body!.Length > num)
            //{
            //    gitHubReleaseClass.Body = gitHubReleaseClass.Body[..num];
            //}

            //var body = new
            //{
            //    tag_name = gitHubReleaseClass.TagName,
            //    name = gitHubReleaseClass.Name,
            //    body = gitHubReleaseClass.Body,
            //    draft = gitHubReleaseClass.Draft,
            //    prerelease = gitHubReleaseClass.Prerelease
            //};

            var context = new JsonContext();
            string jsonBody = JsonSerializer.Serialize(gitHubReleaseClass, context.Release);
            //var jsonBody = JsonSerializer.Serialize(body, typeof(JsonContext));

            // replace TargetCommitish with the bra

            // Send a POST request to create a new release on GitHub
            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases";
            Console.WriteLine($"POST uri: {uri}");
            var response = await Client.PostAsync(uri, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully created a release {gitHubReleaseClass.TagName}. uploading asset");
                response = await UploadAsset(token, gitHubReleaseClass, assetPath, response);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully uploaded the asset: {assetPath}.");
                }
                else
                {
                    Console.WriteLine($"Error: Could not upload the asset:{assetPath}.");
                    Console.WriteLine(response);
                    var additionalData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(additionalData);
                }
                return response;
            }
            else
            {
                Console.WriteLine($"Error: Could not create a release:{gitHubReleaseClass.TagName}.");
                Console.WriteLine(response);
                var additionalData = await response.Content.ReadAsStringAsync();
                Console.WriteLine(additionalData);
                return response;
            }

        }

        private async Task<HttpResponseMessage> UploadAsset(string token, Release gitHubReleaseClass, string assetPath, HttpResponseMessage response)
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

            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = System.Text.Json.JsonDocument.Parse(responseContent);
            var uploadUrlDynamic = responseObject.RootElement.GetProperty("upload_url").GetString();
            if (uploadUrlDynamic is string uploadUrl)
            {
                uploadUrl = uploadUrl.Replace("{?name,label}", $"?name={gitHubReleaseClass.Name}");
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
            var uploadResponse = await Client.PostAsync(uploadUrl, byteArrayContent);

            return uploadResponse;
        }


        public async Task<int?> GetReleaseByTagNameAsync(string token, string tagName)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases/tags/{tagName}";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // ...

                var release = JsonDocument.Parse(content);
                return (int)(release?.RootElement.GetProperty("id").GetInt32() ?? 0);
            }
            else
            {
                Console.WriteLine($"release by tag name {tagName} not found. Status code: {response.StatusCode}");
                return null;
            }
        }

        public async Task<HttpResponseMessage> DeleteReleaseAsync(string token, int releaseId)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases/{releaseId}";
            Console.WriteLine($"DELETE uri: {uri}");
            var response = await Client.DeleteAsync(uri);

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

        public async Task<HttpResponseMessage> DeleteTagAsync(string token, string tagName)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/git/refs/tags/{tagName}";
            Console.WriteLine($"DELETE uri: {uri}");
            var response = await Client.DeleteAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully deleted the tag: {tagName}.");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            else
            {
                Console.WriteLine($"Failed to delete the tag: {tagName}. Status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }

            return response;
        }

        public async Task<bool> TagExistsAsync(string token, string tagName)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/git/refs/tags/{tagName}";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);

            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> GetAllTagsAsync(string token)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/tags";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tags = JsonDocument.Parse(content).RootElement.EnumerateArray();
                return tags.Select(t => (string)(t.GetProperty("name").ToString() ?? "")).ToList();
            }
            else
            {
                Console.WriteLine($"Failed to get tags. Status code: {response.StatusCode}");
                return new List<string>();
            }
        }

        public async Task<HttpResponseMessage> DeleteAllTagsAsync(string token)
        {
            // Set up the response message to 200
            HttpResponseMessage response = new(HttpStatusCode.OK);
            var tags = await GetAllTagsAsync(token);
            Console.WriteLine($"tags: {tags.Count}");

            foreach (var tag in tags)
            {
                response = await DeleteTagAsync(token, tag);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to delete tag {tag}. Status code: {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return response;
                }
            }
            return response;
        }

        public async Task<List<int>> GetAllReleasesAsync(string token)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var uri = $"{Constants.GitHubApiPrefix}/{Credentials.GetOwner()}/{Repo}/releases";
            Console.WriteLine($"GET uri: {uri}");
            var response = await Client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var releases = JsonDocument.Parse(content).RootElement.EnumerateArray();
                return releases.Select(r => (int)(r.GetProperty("id").GetInt32())).ToList();
            }
            else
            {
                Console.WriteLine($"Failed to get releases. Status code: {response.StatusCode}");
                return [];
            }
        }

        public async Task<HttpResponseMessage> DeleteAllReleasesAsync(string token)
        {
            // Set up the response message to 200
            HttpResponseMessage response = new(HttpStatusCode.OK);

            var releases = await GetAllReleasesAsync(token);
            Console.WriteLine($"releases: {releases.Count}");

            foreach (var release in releases)
            {
                response = await DeleteReleaseAsync(token, release);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to delete release {release}. Status code: {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return response;
                }
            }

            return response;
        }
    }
}