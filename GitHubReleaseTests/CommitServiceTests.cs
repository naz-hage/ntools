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

            var apiService = new ApiService();
            var commitService = new CommitService(apiService, Owner, Repo, Token);

            // Act
            var commits = await commitService.GetCommits(DefaultBranch, lastPublished);

            Assert.IsNotNull(commits);

            // Assert
            Assert.IsTrue(commits.Count() > 0);

            // console result count and first element
            Console.WriteLine($"Commits count: {commits.Count()}");
            Console.WriteLine("First commit:");

            Console.WriteLine(JsonSerializer.Serialize(commits.First(), Options));
        }

        [TestMethod()]
        public async Task GetPullRequestCommitsTestAsync()
        {
            // Arrange
            var apiService = new ApiService();
            var commitService = new CommitService(apiService, Owner, Repo, Token);

            // Act  Get all commits from pull requests
            var result = await commitService.GetPullRequestCommits();

            Assert.IsNotNull(result);

            // Assert
            //Assert.IsTrue(result.Count() > 0, "No PRs since last release");

            // console result count and first element
            Console.WriteLine($"Commits count: {result.Count()}");
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
            // Arrange
            var apiService = new ApiService();

            var commitService = new CommitService(apiService, Owner, Repo, Token);

            // Act
            var commits = await commitService.GetCommits(DefaultBranch);

            // Assert
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
            // Arrange
            var apiService = new ApiService();
            var commitService = new CommitService(apiService, Owner, Repo, Token);
            var releaseService = new ReleaseService(Owner, Repo);   

            var (sinceLastPublished, sinceTag) = await releaseService.GetLatestReleasePublishedAtAndTagAsync(Token, DefaultBranch);
            // Act
            var commits = await commitService.GetCommits(DefaultBranch, sinceLastPublished);

            // Assert
            Assert.IsNotNull(commits);
            //Assert.IsTrue(commits.Count() > 0, "No commits since last release");

            ConsolFirstAndLast(commits);

            ConsoleBranchAndDateForeachCommit(commits);
        }

        [TestMethod()]
        public async Task GetReleaseTagsTestAsync()
        {
            // Arrange
            var apiService = new ApiService();
            var commitService = new CommitService(apiService, Owner, Repo, Token);

            // Act
            var tags = await commitService.GetReleaseTags();

            // Assert
            Assert.IsNotNull(tags);

            Console.WriteLine($"Tags count: {tags.Count}");
            // console tags
            foreach (var tag in tags)
            {
                Console.WriteLine(tag);
            }
        }
    }
}