using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitHubRelease;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var contributorService = new ContributorService(apiService,Owner, Repo);
            // Act

            //var result = contributorService.GetNewContributorsAsync(commits);

        }
    }
}