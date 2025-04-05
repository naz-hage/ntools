namespace GitHubRelease
{
    /// <summary>
    /// Provides methods to interact with GitHub releases.
    /// </summary>
    internal static class Command
    {
        /// <summary>
        /// Creates a new release in the specified repository.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="branch">The branch name for the release.</param>
        /// <param name="assetPath">The path to the asset to be included in the release.</param>
        public static async Task CreateRelease(string repo, string tag, string branch, string assetPath)
        {
            var releaseService = new ReleaseService(repo);

            var release = new Release
            {
                TagName = tag,
                TargetCommitish = branch,  // This usually is the branch name
                Name = tag,
                Body = "Description of the release",  // should be pulled from GetLatestReleaseAsync
                Draft = false,
                Prerelease = false
            };

            // Create a release
            await releaseService.CreateRelease(release, assetPath);
            // In debug mode, the release will not be created
            //var responseMessage = await releaseService.CreateRelease(token, release, assetPath);
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK); // for testing debugging
            if (responseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine(responseMessage);
            }
            else
            {
                // read content
                Console.WriteLine($"Failed to create release: {responseMessage.StatusCode}");
                var content = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Downloads an asset from the specified release.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="assetPath">The path where the asset will be downloaded.</param>
        public static async Task DownloadAsset(string repo, string tag, string assetPath)
        {
            var releaseService = new ReleaseService(repo);

            // Ensure the assetPath includes a file name
            if (string.IsNullOrEmpty(Path.GetFileName(assetPath)))
            {
                assetPath = Path.Combine(assetPath, $"{tag}.zip");
            }

            // Ensure the download directory exists
            var directoryPath = Path.GetDirectoryName(assetPath);
            if (directoryPath != null)
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Act
            var response = await releaseService.DownloadAssetByName(tag, $"{tag}.zip", assetPath);

            // Check the response
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully downloaded the asset to: {assetPath}");
            }
            else
            {
                Console.WriteLine($"Failed to download the asset. Status code: {response.StatusCode}");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Uploads an asset to the specified release.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="branch">The branch name for the release.</param>
        /// <param name="assetPath">The path to the asset to be uploaded.</param>
        public static async Task UploadAsset(string repo, string tag, string branch, string assetPath)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }
    }
}