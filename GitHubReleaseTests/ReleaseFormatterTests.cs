
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

            var commitService = new CommitService(apiService, Repo);
            var releaseService = new ReleaseService(Repo);
            var releaseFormatter = new ReleaseFormatter(apiService, Repo);

            var (sinceLastPublished, sinceTag) = await releaseService.GetLatestReleasePublishedAtAndTagAsync(DefaultBranch);

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