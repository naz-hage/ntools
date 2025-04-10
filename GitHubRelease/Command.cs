﻿using OutputColorizer;

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
        /// <returns>True if the release was created successfully, otherwise false.</returns>
        // <remarks>
        /// This method creates a new release in the specified repository using the provided tag, branch, and asset path.
        /// It utilizes the ReleaseService to interact with the GitHub API.
        /// If the release creation is successful, it returns true; otherwise, it logs the error and returns false.
        /// </remarks>
        public static async Task<bool> CreateRelease(string repo, string tag, string branch, string assetPath)
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
            var responseMessage = await releaseService.CreateRelease(release, assetPath);
            if (responseMessage.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                // read content
                Console.WriteLine($"Failed to create release: {responseMessage.StatusCode}");
                var content = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                return false; // This line will never be reached, but is required for compilation
            }
        }

        /// <summary>
        /// Downloads an asset from the specified release.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="assetPath">The path where the asset will be downloaded.</param>
        /// <returns>True if the download was successful, otherwise false.</returns>
        /// <remarks>
        /// This method ensures that the assetPath includes a file name and that the download directory exists before attempting to download the asset.
        /// </remarks>/// 
        public static async Task<bool> DownloadAsset(string repo, string tag, string assetPath)
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

            // download the asset
            var response = await releaseService.DownloadAssetByName(tag, $"{tag}.zip", assetPath);

            // Check the response
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to download the asset. Status code: {response.StatusCode}");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                return false;
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