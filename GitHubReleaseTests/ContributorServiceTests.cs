namespace GitHubRelease.Tests
{
    [TestClass()]
    public class ContributorServiceTests : GitHubSetup
    {

        [TestMethod()]
        public void GetNewContributorsAsyncTest()
        {
            // Arrange
            var apiService = new ApiService();
            var contributorService = new ContributorService(apiService, Repo);
            // Act

            //var result = contributorService.GetNewContributorsAsync(commits);

        }
    }
}