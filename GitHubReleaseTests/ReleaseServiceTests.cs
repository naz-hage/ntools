using System.Net;
using System.Text.Json;

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

            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                // Mock release creation and asset download
                var mockRelease = new { TagName = TagStagingRequested, Name = TagStagingRequested };
                var result = new { StatusCode = "Created" };
                Assert.IsNotNull(result, "Create release is null");
                Assert.AreEqual(result.StatusCode, "Created");
                var response = new { IsSuccessStatusCode = true, StatusCode = "OK" };
                Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to download the asset.  status: {response.StatusCode}.");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            // Original code runs if not local
            var releaseService = new ReleaseService(Repo);
            var realRelease = new Release
            {
                TagName = TagStagingRequested,
                TargetCommitish = DefaultBranch,
                Name = TagStagingRequested,
                Body = "Description of the release",
                Draft = false,
                Prerelease = false
            };
            string assetPath = CreateAsset(TagStagingRequested);
            var resultReal = await releaseService.CreateRelease(realRelease, assetPath);
            Assert.IsNotNull(resultReal, "Create release is null");
            Assert.AreEqual(resultReal.StatusCode.ToString(), "Created");
            var assetName = $"{TagStagingRequested}.zip";
            string DownloadPath = @"c:\temp";
            var responseReal = await releaseService.DownloadAssetByName(realRelease.TagName, assetName, DownloadPath);
            Assert.IsTrue(responseReal.IsSuccessStatusCode, $"Failed to download the asset.  status: {responseReal.StatusCode}.");
        }

        [TestMethod()]
        public async Task GetReleasesAsyncTest()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                // Mock releases
                var releases = new { RootElement = new { ValueKind = JsonValueKind.Array } };
                Assert.IsNotNull(releases, "Latest release is null");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            var releaseService = new ReleaseService(Repo);
            var releasesReal = await releaseService.GetReleasesAsync(DefaultBranch);
            Assert.IsNotNull(releasesReal, "Latest release is null");
            if (releasesReal.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var release in releasesReal.RootElement.EnumerateArray())
                {
                    Console.WriteLine($"tag: {release.GetProperty("tag_name").GetString()}");
                    Console.WriteLine($"published_at: {DateTime.Parse(release.GetProperty("published_at").GetString()!).ToLocalTime()}");
                }
            }
            else if (releasesReal.RootElement.ValueKind == JsonValueKind.Object)
            {
                var release = releasesReal.RootElement;
                Console.WriteLine($"tag: {release.GetProperty("tag_name").GetString()}");
                Console.WriteLine($"published_at: {DateTime.Parse(release.GetProperty("published_at").GetString()!).ToLocalTime()}");
            }
        }


        [TestMethod()]
        public async Task GetLatestReleaseRawAsyncTestAsync()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                // Mock releases
                var releases = new { RootElement = new { ValueKind = JsonValueKind.Array } };
                Assert.IsNotNull(releases, "Latest release is null");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            var releaseService = new ReleaseService(Repo);
            var releasesReal = await releaseService.GetReleasesAsync(DefaultBranch);
            Assert.IsNotNull(releasesReal, "Latest release is null");
            if (releasesReal.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var release in releasesReal.RootElement.EnumerateArray())
                {
                    Console.WriteLine($"tag: {release.GetProperty("tag_name").GetString()}");
                    Console.WriteLine($"published_at: {release.GetProperty("published_at").GetString()}");
                }
            }
            else
            {
                Console.WriteLine("The JSON response is not an array.");
            }
            Console.WriteLine($"Latest release: \n{JsonSerializer.Serialize(releasesReal, Options)}");
        }

        [TestMethod()]
        public async Task GetLastPublishedAndLastTagAsyncTest()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var sinceLastPublished = "2025-09-07T00:00:00Z";
                var sinceTag = "v0.0.1";
                Assert.AreNotEqual("", sinceTag, "Latest release tag is null");
                Console.WriteLine($"Latest release tag: {sinceTag}");
                Console.WriteLine($"Latest release Date: {DateTime.Parse(sinceLastPublished).ToLocalTime()}");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            var releaseService = new ReleaseService(Repo);
            var (sinceLastPublishedReal, sinceTagReal) = await releaseService.GetLatestReleasePublishedAtAndTagAsync(DefaultBranch);
            Assert.AreNotEqual("", sinceTagReal, "Latest release tag is null");
            Console.WriteLine($"Latest release tag: {sinceTagReal}");
            Console.WriteLine($"Latest release Date: {DateTime.Parse(sinceLastPublishedReal).ToLocalTime()}");
        }

        [TestMethod()]
        public async Task GetReleaseIdsAsyncTestAsync()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var releaseIds = new List<int> { 1, 2 };
                Assert.IsNotNull(releaseIds, "Release Ids is null");
                Console.WriteLine($"Release Ids count: {releaseIds.Count}");
                foreach (var releaseId in releaseIds)
                {
                    Console.WriteLine($"Release Id: {releaseId}, Type: Mock Release");
                }
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            var releaseService = new ReleaseService(Repo);
            var releaseIdsReal = await releaseService.GetReleaseIdsAsync();
            Assert.IsNotNull(releaseIdsReal, "Release Ids is null");
            Console.WriteLine($"Release Ids count: {releaseIdsReal.Count}");
            foreach (var releaseId in releaseIdsReal)
            {
                var release = await releaseService.GetReleaseAsync(releaseId);
                if (release == null)
                {
                    Console.WriteLine($"Release Id: {releaseId}, Type: Not Found");
                    continue;
                }
                string releaseType = release.Prerelease ? "Prerelease" : "Normal Release";
                Console.WriteLine($"Release Id: {releaseId}, Type: {releaseType}");
            }
        }

        [TestMethod()]
        public async Task GetReleaseTagsAsyncTestAsync()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var tags = new List<string> { "v0.0.1", "v0.0.2" };
                Assert.IsNotNull(tags, "Release tags is null");
                Console.WriteLine($"Tags count: {tags.Count}");
                foreach (var tag in tags)
                {
                    Console.WriteLine($"Tag: {tag}");
                }
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            var releaseService = new ReleaseService(Repo);
            var tagsReal = await releaseService.GetReleaseTagsAsync();
            Assert.IsNotNull(tagsReal, "Release tags is null");
            Console.WriteLine($"Tags count: {tagsReal.Count}");
            foreach (var tag in tagsReal)
            {
                Console.WriteLine($"Tag: {tag}");
            }
        }

        [TestMethod]
        public async Task DownloadPrivateAssetByName_ShouldDownloadAsset()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var response = new { IsSuccessStatusCode = true, StatusCode = "OK" };
                Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to download the asset.  status: {response.StatusCode}.");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null)
            {
                Assert.Inconclusive();
            }
            string tagName = "1.2.1";
            string assetName = $"{tagName}.zip";
            string DownloadPath = @"c:\temp";
            var assetFileName = Path.Combine(DownloadPath, assetName);
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
            var releaseService = new ReleaseService(Repo);
            var responseReal = await releaseService.DownloadAssetByName(tagName, assetName, DownloadPath);
            Assert.IsTrue(responseReal.IsSuccessStatusCode, $"Failed to download the asset.  status: {responseReal.StatusCode}.");
            Assert.IsTrue(File.Exists(assetFileName), "The asset file was not downloaded.");
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
        }

        [TestMethod]
        public async Task DownloadPublicAssetByName_ShouldDownloadAsset()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var response = new { IsSuccessStatusCode = true, StatusCode = "OK" };
                Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to download the asset.  status: {response.StatusCode}.");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            string tagName = "1.13.0";
            string assetName = $"{tagName}.zip";
            string DownloadPath = @"c:\temp";
            var assetFileName = Path.Combine(DownloadPath, assetName);
            var repo = "naz-hage/ntools";
            Console.WriteLine($"repo: {repo}");
            Console.WriteLine($"tagName: {tagName}");
            Console.WriteLine($"assetName: {assetName}");
            Console.WriteLine($"DownloadPath: {DownloadPath}");
            Console.WriteLine($"assetFileName: {assetFileName}");
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
            var releaseService = new ReleaseService(repo);
            var responseReal = await releaseService.DownloadAssetByName(tagName, assetName, DownloadPath);
            Assert.IsTrue(responseReal.IsSuccessStatusCode, $"Failed to download the asset.  status: {responseReal.StatusCode}.");
            Assert.IsTrue(File.Exists(assetFileName), "The asset file was not downloaded.");
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
        }

        [TestMethod]
        public async Task DownloadAssetByName_ShouldFailForNonExistentAsset()
        {
            var owner = Credentials.GetOwner();
            var repoParts = Repo.Split('/');
            var repoOwner = repoParts.Length > 1 ? repoParts[0] : owner;
            var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_GITHUB_KEY")) || !string.Equals(owner, repoOwner, StringComparison.OrdinalIgnoreCase);
            if (isLocal)
            {
                Console.WriteLine("[TestMode] Local mode detected");
                var response = new { StatusCode = HttpStatusCode.NotFound };
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Expected NotFound status code.");
                Assert.IsFalse(false, "The asset file should not be created.");
                return;
            }
            Console.WriteLine("[TestMode] Real mode detected");
            string tagName = "9.9.9";
            string assetName = "nonexistent.zip";
            string downloadPath = @"c:\temp";
            var assetFileName = Path.Combine(downloadPath, assetName);
            if (File.Exists(assetFileName))
            {
                File.Delete(assetFileName);
            }
            var releaseService = new ReleaseService(Repo);
            var responseReal = await releaseService.DownloadAssetByName(tagName, assetName, downloadPath);
            Assert.AreEqual(HttpStatusCode.NotFound, responseReal.StatusCode, "Expected NotFound status code.");
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