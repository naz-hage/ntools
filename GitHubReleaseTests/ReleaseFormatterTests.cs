
namespace GitHubRelease.Tests
{
    [TestClass()]
    public class ReleaseFormatterTests : GitHubSetup
    {
        [TestMethod()]
        public async Task FormatReleaseNotesAsyncTestAsync()
        {
            // Arrange
            var apiService = new ApiService();

            var commitService = new CommitService(apiService, Owner, Repo, Token);
            var releaseService = new ReleaseService(Owner, Repo);
            var releaseFormatter = new ReleaseFormatter(apiService, Owner, Repo, Token);

            var (sinceLastPublished, sinceTag) = await releaseService.GetLatestReleasePublishedAtAndTagAsync(Token, DefaultBranch);

            // Get commits since last release on Default branch
            var commits = await commitService.GetCommits(DefaultBranch, sinceLastPublished);

            // Act
            var releaseNotes = await releaseFormatter.FormatAsync(commits, sinceTag, sinceLastPublished, TagStagingRequested);

            // Assert
            Assert.IsNotNull(releaseNotes);

            // console output for debugging
            Console.WriteLine(releaseNotes.ToString());
        }
    }
}