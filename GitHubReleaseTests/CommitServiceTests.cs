using System.Text.Json;

namespace GitHubRelease.Tests
{
    [TestClass()]
    public class CommitServiceTests : GitHubSetup
    {
        [TestMethod()]
        public async Task GetCommitsTestAsync()
        {
            // Arrange
            string? lastPublished = null;

            // Use mock data when running locally
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            List<JsonElement> commits;
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                // Create a dummy commit as JsonElement
                var dummyCommitJson = "{" +
                    "\"sha\": \"dummysha\"," +
                    "\"commit\": {" +
                        "\"author\": {\"date\": \"2025-09-07T00:00:00Z\"}" +
                    "}," +
                    "\"message\": \"Test commit\"" +
                "}";
                var doc = JsonDocument.Parse(dummyCommitJson);
                commits = new List<JsonElement> { doc.RootElement.Clone() };
            }
            else
            {
                Console.WriteLine("[TestMode] Real mode detected");
                var apiService = new ApiService();
                var commitService = new CommitService(apiService, Repo);
                commits = await commitService.GetCommits(DefaultBranch, lastPublished);
            }

            Assert.IsNotNull(commits);

            // Assert
            Assert.IsTrue(commits.Count > 0);

            // console result count and first element
            Console.WriteLine($"Commits count: {commits.Count}");
            Console.WriteLine("First commit:");

            Console.WriteLine(JsonSerializer.Serialize(commits.First(), Options));
        }

        [TestMethod()]
        public async Task GetPullRequestCommitsTestAsync()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            List<JsonElement> result;
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var dummyCommitJson = "{" +
                    "\"sha\": \"dummysha\"," +
                    "\"commit\": {" +
                        "\"author\": {\"date\": \"2025-09-07T00:00:00Z\"}" +
                    "}," +
                    "\"message\": \"Test PR commit\"" +
                "}";
                var doc = JsonDocument.Parse(dummyCommitJson);
                result = new List<JsonElement> { doc.RootElement.Clone() };
            }
            else
            {
                Console.WriteLine("[TestMode] Real mode detected");
                var apiService = new ApiService();
                var commitService = new CommitService(apiService, Repo);
                result = await commitService.GetPullRequestCommits();
            }
            Assert.IsNotNull(result);
            Console.WriteLine($"Commits count: {result.Count}");
            if (result.Any())
            {
                Console.WriteLine("First commit:");
                Console.WriteLine(JsonSerializer.Serialize(result.First(), Options));
            }
            else
            {
                Console.WriteLine("No commits found.");
            }
        }

        [TestMethod()]
        public async Task GetAllCommitsTestAsync()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            List<JsonElement> commits;
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var dummyCommitJson = "{" +
                    "\"sha\": \"dummysha\"," +
                    "\"commit\": {" +
                        "\"author\": {\"date\": \"2025-09-07T00:00:00Z\"}" +
                    "}," +
                    "\"message\": \"Test commit\"" +
                "}";
                var doc = JsonDocument.Parse(dummyCommitJson);
                commits = new List<JsonElement> { doc.RootElement.Clone() };
            }
            else
            {
                Console.WriteLine("[TestMode] Real mode detected");
                var apiService = new ApiService();
                var commitService = new CommitService(apiService, Repo);
                commits = await commitService.GetCommits(DefaultBranch);
            }
            Assert.IsNotNull(commits);
            ConsolFirstAndLast(commits);
            ConsoleBranchAndDateForeachCommit(commits);
            Console.WriteLine($"First commit:\n {JsonSerializer.Serialize(commits.First(), Options)}");
        }

        private static void ConsoleBranchAndDateForeachCommit(List<JsonElement> commits)
        {
            Console.WriteLine($"Branch | Date");
            // print the published date and commit branch of all the commits
            var branchName = "not-found";
            var commitMessage = "not-found";
            foreach (var commit in commits)
            {
                // Check the "ref" property is missing or not a string
                if (commit.TryGetProperty("ref", out var refProperty) && refProperty.ValueKind == JsonValueKind.String)
                {
                    branchName = refProperty.GetString();
                    // Use the branchName variable here
                }
                else
                {
                    // Handle the case when the "ref" property is missing or not a string
                }
                var commitId = commit.GetProperty("sha").GetString();

                // Check the "message" property is missing or not a string
                if (commit.TryGetProperty("message", out var messageProperty) && messageProperty.ValueKind == JsonValueKind.String)
                {
                    commitMessage = commit.GetProperty("message").GetString();
                }
                else
                {
                    commitMessage = CommitService.GetCommitMessage(commit);
                }
                var commitDate = commit.GetProperty("commit").GetProperty("author").GetProperty("date").GetString();    

                Console.WriteLine($"{branchName} | {commitDate}");
            }
        }

        private static void ConsolFirstAndLast(List<JsonElement> commits)
        {
            // console result count and first element
            Console.WriteLine($"Commits count: {commits.Count()}");

            if (commits.Any())
            {
                // get published date of the first commit
                var firstCommit = $"{commits.First().GetProperty("commit").GetProperty("author").GetProperty("date").GetString()}";
                Console.WriteLine($"First commit date: {firstCommit}");
                // Get the published date of the last commit
                var lastCommit = $"{commits.Last().GetProperty("commit").GetProperty("author").GetProperty("date").GetString()}";
                Console.WriteLine($"Last commit date: {lastCommit}");
            }
            else
            {
                Console.WriteLine("No commits found.");
            }
        }

        [TestMethod()]
        public async Task GetCommitsSinceLastPublishedTest()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            List<JsonElement> commits;
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var dummyCommitJson = "{" +
                    "\"sha\": \"dummysha\"," +
                    "\"commit\": {" +
                        "\"author\": {\"date\": \"2025-09-07T00:00:00Z\"}" +
                    "}," +
                    "\"message\": \"Test commit\"" +
                "}";
                var doc = JsonDocument.Parse(dummyCommitJson);
                commits = new List<JsonElement> { doc.RootElement.Clone() };
            }
            else
            {
                Console.WriteLine("[TestMode] Real mode detected");
                var apiService = new ApiService();
                var commitService = new CommitService(apiService,Repo);
                var releaseService = new ReleaseService(Repo);   
                var (sinceLastPublished, sinceTag) = await releaseService.GetLatestReleasePublishedAtAndTagAsync(DefaultBranch);
                commits = await commitService.GetCommits(DefaultBranch, sinceLastPublished);
            }
            Assert.IsNotNull(commits);
            ConsolFirstAndLast(commits);
            ConsoleBranchAndDateForeachCommit(commits);
        }

        [TestMethod()]
        public async Task GetReleaseTagsTestAsync()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            List<string> tags;
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                tags = new List<string> { "v0.0.1", "v0.0.2" };
            }
            else
            {
                Console.WriteLine("[TestMode] Real mode detected");
                var apiService = new ApiService();
                var commitService = new CommitService(apiService,Repo);
                tags = await commitService.GetReleaseTags();
            }
            Assert.IsNotNull(tags);
            Console.WriteLine($"Tags count: {tags.Count}");
            foreach (var tag in tags)
            {
                Console.WriteLine(tag);
            }
        }
    }
}