﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitHubRelease;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace GitHubRelease.Tests
{
    [TestClass()]
    public class ReleaseServiceTests : GitHubSetup
    {

        /// <summary>
        /// CreateGitHubReleaseAsyncTestAsync method tests the creation of a GitHub release using the ReleaseService class. 
        /// It sets up the necessary release information, calls the CreateRelease method, and verifies that the release 
        /// was created successfully by checking the result and status code. 
        /// The test is skipped if it is running in a GitHub Actions environment.
        /// </summary>
        /// <returns></returns>
        [TestMethod()]
        public async Task CreateGitHubReleaseAsyncTestAsync()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null)
            {
                Assert.Inconclusive();
            }

            // Arrange
            var releaseService = new ReleaseService(Repo);

            var release = new Release
            {
                TagName = TagStagingRequested,
                TargetCommitish = DefaultBranch,  // This usually is the branch name
                Name = TagStagingRequested,
                Body = "Description of the release",  // should be pulled from GetLatestReleaseAsync
                Draft = false,
                Prerelease = false
            };

            string assetPath = CreateAsset(TagStagingRequested);

            // Act
            var result = await releaseService.CreateRelease(release, assetPath);

            // Assert
            Assert.IsNotNull(result, "Create release is null");
            Assert.AreEqual(result.StatusCode.ToString(), "Created");

            // Skip Private repo Download the asset
            var assetName = $"{TagStagingRequested}.zip";
            string DownloadPath = @"c:\temp";
            var response = await releaseService.DownloadAssetByName(release.TagName, assetName, DownloadPath);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to download the asset.  status: {response.StatusCode}.");

        }

        [TestMethod()]
        public async Task GetReleasesAsyncTest()
        {
            // Arrange
            var releaseService = new ReleaseService(Repo);

            // Act
            var releases = await releaseService.GetReleasesAsync(DefaultBranch);

            // Assert
            Assert.IsNotNull(releases, "Latest release is null");

            if (releases.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var release in releases.RootElement.EnumerateArray())
                {
                    Console.WriteLine($"tag: {release.GetProperty("tag_name").GetString()}");
                    Console.WriteLine($"published_at: {DateTime.Parse(release.GetProperty("published_at").GetString()!).ToLocalTime()}");
                }
            }
            else if (releases.RootElement.ValueKind == JsonValueKind.Object)
            {
                var release = releases.RootElement;
                Console.WriteLine($"tag: {release.GetProperty("tag_name").GetString()}");
                Console.WriteLine($"published_at: {DateTime.Parse(release.GetProperty("published_at").GetString()!).ToLocalTime()}");
            }
        }


        [TestMethod()]
        public async Task GetLatestReleaseRawAsyncTestAsync()
        {
            // Arrange
            var releaseService = new ReleaseService(Repo);

            // Act
            var releases = await releaseService.GetReleasesAsync(DefaultBranch);

            // Assert
            Assert.IsNotNull(releases, "Latest release is null");

            if (releases.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var release in releases.RootElement.EnumerateArray())
                {
                    // Get the tag name of the latest release and print to console
                    Console.WriteLine($"tag: {release.GetProperty("tag_name").GetString()}");
                    // get the published date of the latest release and print to console
                    Console.WriteLine($"published_at: {release.GetProperty("published_at").GetString()}");
                }
            }
            else
            {
                Console.WriteLine("The JSON response is not an array.");
            }

            // print the result to console
            Console.WriteLine($"Latest release: \n{JsonSerializer.Serialize(releases, Options)}");
        }

        [TestMethod()]
        public async Task GetLastPublishedAndLastTagAsyncTest()
        {
            // Arrange
            var releaseService = new ReleaseService(Repo);

            // Act
            var (sinceLastPublished, sinceTag) = await releaseService.GetLatestReleasePublishedAtAndTagAsync(DefaultBranch);

            // Assert
            Assert.AreNotEqual("", sinceTag, "Latest release tag is null");

            // print the result to console
            Console.WriteLine($"Latest release tag: {sinceTag}");
            Console.WriteLine($"Latest release Date: {DateTime.Parse(sinceLastPublished).ToLocalTime()}");
        }

        [TestMethod()]
        public async Task GetReleaseIdsAsyncTestAsync()
        {
            // Arrange
            var releaseService = new ReleaseService(Repo);

            // Act
            var releaseIds = await releaseService.GetReleaseIdsAsync();

            // Assert
            Assert.IsNotNull(releaseIds, "Release Ids is null");

            // print count and the result to console
            Console.WriteLine($"Release Ids count: {releaseIds.Count}");
            foreach (var releaseId in releaseIds)
            {
                var release = await releaseService.GetReleaseAsync(releaseId);

                if (release == null)
                {
                    Console.WriteLine($"Release Id: {releaseId}, Type: Not Found");
                    continue;
                }   

                // Determine the type of the release
                string releaseType = release.Prerelease ? "Prerelease" : "Normal Release";

                Console.WriteLine($"Release Id: {releaseId}, Type: {releaseType}");
            }
        }

        [TestMethod()]
        public async Task GetReleaseTagsAsyncTestAsync()
        {
            // Arrange
            var releaseService = new ReleaseService(Repo);

            // Act
            var tags = await releaseService.GetReleaseTagsAsync();

            // Assert
            Assert.IsNotNull(tags, "Release tags is null");

            // print count and the result to console
            Console.WriteLine($"Tags count: {tags.Count}");
            foreach (var tag in tags)
            {
                Console.WriteLine($"Tag: {tag}");
            }
        }

        [TestMethod]
        public async Task DownloadPrivateAssetByName_ShouldDownloadAsset()
        {
            // Skip the test if running in GitHub Actions
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null)
            {
                Assert.Inconclusive();
            }

            // Arrange
            string tagName = "1.2.1";
            string assetName = $"{tagName}.zip";
            string DownloadPath = @"c:\temp";
            var assetFileName = Path.Combine(DownloadPath, assetName);
            //string owner = "naz-hage";
            //var downloadUrl = $"https://github.com/{owner}/{Repo}/releases/download/{tagName}/{assetName}";

            // Delete the file if it exists
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }

            var releaseService = new ReleaseService(Repo);

            // Act
            var response = await releaseService.DownloadAssetByName(tagName, assetName, DownloadPath);
            //var response = await releaseService.DownloadAssetFromUrl(downloadUrl, assetFileName);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to download the asset.  status: {response.StatusCode}.");

            Assert.IsTrue(File.Exists(assetFileName), "The asset file was not downloaded.");

            // Clean up
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
        }

        [TestMethod]
        public async Task DownloadPublicAssetByName_ShouldDownloadAsset()
        {
            // Arrange
            string tagName = "1.13.0";
            string assetName = $"{tagName}.zip";
            string DownloadPath = @"c:\temp";
            var assetFileName = Path.Combine(DownloadPath, assetName);
            var repo = "naz-hage/ntools";

            // write parameters to console
            Console.WriteLine($"repo: {repo}");
            Console.WriteLine($"tagName: {tagName}");
            Console.WriteLine($"assetName: {assetName}");
            Console.WriteLine($"DownloadPath: {DownloadPath}");
            Console.WriteLine($"assetFileName: {assetFileName}");



            // Delete the file if it exists
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }

            var releaseService = new ReleaseService(repo);

            // Act
            var response = await releaseService.DownloadAssetByName(tagName, assetName, DownloadPath);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to download the asset.  status: {response.StatusCode}.");

            Assert.IsTrue(File.Exists(assetFileName), "The asset file was not downloaded.");

            // Clean up
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
        }

        [TestMethod]
        public async Task DownloadAssetByName_ShouldFailForNonExistentAsset()
        {
            // Arrange
            string tagName = "9.9.9";
            string assetName = "nonexistent.zip";
            string downloadPath = @"c:\temp";
            var assetFileName = Path.Combine(downloadPath, assetName);

            // Delete the file if it exists
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }

            var releaseService = new ReleaseService(Repo);

            // Act
            var response = await releaseService.DownloadAssetByName(tagName, assetName, downloadPath);

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Expected NotFound status code.");
            Assert.IsFalse(File.Exists(assetFileName), "The asset file should not be created.");
        }

        //[TestMethod]
        //public async Task UploadAsset_ShouldUploadAssetSuccessfully()
        //{
        //    // Arrange
        //    var releaseService = new ReleaseService(Repo);
        //    string assetPath = "test-asset.zip";
        //    string uploadUrl = "https://uploads.github.com/repos/naz-hage/learn/releases/assets{?name,label}";
        //    var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created);

        //    // Create a dummy file to upload
        //    File.WriteAllText(assetPath, "dummy content");

        //    // Act
        //    var response = await releaseService.UploadAsset(assetPath!, uploadUrl!);

        //    // Assert
        //    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        //    // Clean up
        //    if (File.Exists(assetPath))
        //    {
        //        File.Delete(assetPath);
        //    }
        //}

        //[TestMethod]
        //public async Task UploadAsset_ShouldReturnNotFoundIfFileDoesNotExist()
        //{
        //    // Arrange
        //    var releaseService = new ReleaseService(Repo);
        //    string assetPath = "nonexistent-asset.zip";
        //    string uploadUrl = "https://uploads.github.com/repos/naz-hage/learn/releases/assets{?name,label}";

        //    // Act
        //    var response = await releaseService.UploadAsset(assetPath, uploadUrl);

        //    // Assert
        //    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        //}
    }
}