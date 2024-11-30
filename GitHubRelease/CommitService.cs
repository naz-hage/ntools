using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GitHubRelease
{
    public class Author
    {
        public string? Name { get; set; }
        // We're ignoring the 'email' and 'date' fields in the JSON
    }

    public class Commit
    {
        public Author? Author { get; set; }
        // We're ignoring other properties in the 'commit' object in the JSON
    }

    public class Root
    {
        public Commit? Commit { get; set; }
        // We're ignoring other properties in the root object in the JSON
    }

    /// <summary>
    /// Service for retrieving commit information and generating release notes.
    /// </summary>
    public class CommitService : Constants
    {
        
        private readonly ApiService ApiService;
        private readonly string Owner;
        private readonly string Repo;

        public CommitService(ApiService apiService, string owner, string repo, string token)
        {
            ApiService = apiService;
            Owner = owner;
            Repo = repo;
            apiService.GetClient().DefaultRequestHeaders.Clear();
            apiService.GetClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            apiService.GetClient().DefaultRequestHeaders.Add("User-Agent", "request");
            apiService.GetClient().DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        /// <summary>
        /// Retrieves the tag associated with a given commit SHA.
        /// </summary>
        /// <param name="commitSha">The SHA of the commit.</param>
        /// <returns>The tag associated with the commit, or null if not found.</returns>
        private async Task<string?> GetTagFromCommitAsync(string commitSha)
        {
            var uri = $"https://api.github.com/repos/{Owner}/{Repo}/tags";
            
            var response = await ApiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tags = JsonDocument.Parse(content).RootElement.EnumerateArray();
                foreach (var tag in tags)
                {
                    var commit = tag.GetProperty(CommitPropertyName);
                    if (commit.GetProperty(ShaPropertyName).GetString() == commitSha)
                    {
                        return tag.GetProperty("name").GetString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Generates the release notes based on the provided commits.
        /// </summary>
        /// <param name="commits">The array of commits.</param>
        /// <param name="sinceTag">The tag to compare against. Only include commits after this tag.</param>
        /// <param name="lastPublishedAt">The date of the last published commit.</param>
        /// <param name="tag">The tag associated with the release.</param>
        /// <returns>A StringBuilder containing the release notes.</returns>
        public async Task<StringBuilder> GetWhatsChangedAsync(List<JsonElement> commits, string? sinceTag, string lastPublishedAt, string tag)
        {
            StringBuilder releaseNotes = new();
            string? previousPublishedAt = "1970-01-01T00:00:00Z";
            string? publishedAt = string.Empty;
            foreach (var commit in commits)
            {
                //Console.WriteLine(JsonSerializer.Serialize(commit, jsonSerializerOptions));
                // How do I get the published_at date of the commit?
                publishedAt = commit.GetProperty(CommitPropertyName).GetProperty(AuthorPropertyName).GetProperty("date").GetString();
                // append only if the date is different from the previous commit
                if (DateTime.Parse(previousPublishedAt!).ToShortDateString() != DateTime.Parse(publishedAt!).ToShortDateString())
                {
                    previousPublishedAt = publishedAt;
                    releaseNotes.AppendLine($"<br>**{DateTime.Parse(publishedAt!).ToLocalTime().ToString("dd-MMM-yy")}**");
                }

                // If the commit is before the lastPublishedAt, skip it
                if (DateTime.Parse(publishedAt!) < DateTime.Parse(lastPublishedAt))
                {
                    continue;
                }

                var message = commit.GetProperty(CommitPropertyName).GetProperty("message").GetString() ?? string.Empty;

      
                var author = " ";
                try
                {
                    if (commit.TryGetProperty(CommitPropertyName, out var commitProperty) &&
                        commitProperty.ValueKind != JsonValueKind.Null &&
                        commitProperty.TryGetProperty(AuthorPropertyName, out var authorProperty) &&
                        authorProperty.ValueKind != JsonValueKind.Null &&
                        authorProperty.TryGetProperty("name", out var nameProperty) &&
                        nameProperty.ValueKind != JsonValueKind.Null)
                    {
                        author = nameProperty.GetString();
                    }
                    else
                    {
                        Console.WriteLine("Author or name property not found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    author = "null";
                    continue;
                }

                var sha = commit.GetProperty(ShaPropertyName).GetString();
                var commitTag = await GetTagFromCommitAsync(sha!);

                // If the commit is tagged and less than SinceTag, skip it
                if (commitTag != null && sinceTag != null && string.Compare(commitTag, sinceTag) <= 0)
                {
                    continue;
                }
                var prCommits = await GetPullRequestCommits(sha!);
                if (sha == null || prCommits.Count == 0)
                {
                    releaseNotes.AppendLine($"* {commitTag} - {message} by @{author}");
                }
                else
                {
                    var prNumber = prCommits[0].GetProperty("number").GetInt32().ToString();
                    releaseNotes.AppendLine($"* {message} by @{author} in https://github.com/{Owner}/{Repo}/pull/{prNumber}");
                }
            }

            return releaseNotes;
        }

        public static string? GetNextLink(string linkHeader)
        {
            var nextLink = Regex.Match(linkHeader, "<(.*)>; rel=\"next\"")?.Groups[1].Value;
            return nextLink;
        }

        public async Task<List<JsonElement>> GetCommits(string? branch = null, string? sinceLastPublished = null)
        {
            var uri = $"https://api.github.com/repos/{Owner}/{Repo}/commits";
            var queryParams = new List<string>();
            if (branch != null)
            {
                queryParams.Add($"sha={branch}");
            }
            if (sinceLastPublished != null)
            {
                queryParams.Add($"since={sinceLastPublished}");
            }
            if (queryParams.Any())
            {
                uri += "?" + string.Join("&", queryParams);
            }

            var commitsList = new List<JsonElement>();

            while (!string.IsNullOrEmpty(uri))
            {
                var response = await ApiService.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    commitsList.AddRange(await FormatResponseAsync(response));

                    // Check if the Link header is present
                    if (response.Headers.TryGetValues("Link", out var linkHeader))
                    {
                        // Get the next page URL
                        uri = GetNextLink(linkHeader.FirstOrDefault()!);
                    }
                    else
                    {
                        uri = null;
                    }
                }
                else
                {
                    // If the request fails, break the loop
                    break;
                }
            }

            return commitsList;
        }

        public async Task<List<string>> GetReleaseTags(string? branch = null)
        {
            var uri = $"https://api.github.com/repos/{Owner}/{Repo}/tags";
            var response = await ApiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tags = JsonDocument.Parse(content).RootElement.EnumerateArray();
                return tags.Select(tag => tag.GetProperty("name").GetString()!).ToList();
            }

            // Return an empty enumerator if the request fails
            return new List<string>();
        }

        public async Task<List<JsonElement>> GetPullRequestCommits(string? sha = null)
        {
            var uri = $"https://api.github.com/repos/{Owner}/{Repo}/commits/pulls";
            if (sha != null)
            {
                uri = $"https://api.github.com/repos/{Owner}/{Repo}/commits/{sha}/pulls";
            }

            var response = await ApiService.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                return await FormatResponseAsync(response);
            }

            // Return an empty enumerator if the request fails
            return new List<JsonElement>();
        }

        private async Task<string> GetShaFromBranch(string branch)
        {
            var branchUri = $"https://api.github.com/repos/{Owner}/{Repo}/branches/{branch}";
            var branchResponse = await ApiService.GetAsync(branchUri);
            if (branchResponse.IsSuccessStatusCode)
            {
                // Extract the sha from the response
                var content = await branchResponse.Content.ReadAsStringAsync();
                var branchSha = JsonDocument.Parse(content).RootElement.GetProperty(CommitPropertyName).GetProperty(ShaPropertyName).GetString();
                return branchSha ?? string.Empty;
            }

            return string.Empty;
        }

        private async Task<List<JsonElement>> FormatResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var commits = JsonDocument.Parse(content).RootElement.EnumerateArray();
            return ConvertToArray(commits);
        }

        private List<JsonElement> ConvertToArray(JsonElement.ArrayEnumerator enumerator)
        {
            var list = new List<JsonElement>();
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }
            return list;
        }

        public static string GetCommitMessage(JsonElement jsonElement)
        {
            // Navigate through the JsonElement to get the commit message
            if (jsonElement.TryGetProperty("commit", out JsonElement commitElement) &&
                commitElement.TryGetProperty("message", out JsonElement messageElement))
            {
                return messageElement.GetString() ?? string.Empty;
            }

            throw new InvalidOperationException("Invalid JSON structure");
        }
    }
}
