using System.Text;
using System.Text.Json;

namespace GitHubRelease
{
    public class ReleaseFormatter(ApiService apiService, string repo) : Constants
    {
        private readonly CommitService CommitService = new(apiService, repo);
        private readonly ContributorService ContributorService = new(apiService, repo);
         private readonly string Repo = repo;

        /// <summary>
        /// Formats the release notes based on the commits.
        /// </summary>
        /// <param name="commits">The commits as a <see cref="JsonElement.ArrayEnumerator"/> object.</param>
        /// <param name="sinceTag">The tag of the previous release.</param>
        /// <param name="tag">The tag of the latest release.</param>
        /// <returns>The formatted release notes as a string.</returns>
        public async Task<string> FormatAsync(List<JsonElement> commits, string? sinceTag, string iso8601DateSinceLastPublished, string tag)
        {
            // Format the commit messages
            var releaseNotes = new StringBuilder();
            ///
            /// What's Changed
            ///
            releaseNotes.AppendLine("### What's Changed");
            var whatsChanged = await CommitService.GetWhatsChangedAsync(commits, sinceTag, iso8601DateSinceLastPublished, tag);
            if (whatsChanged.Length > 0)
            {
                releaseNotes.AppendLine(whatsChanged.ToString());
            }

            ///
            /// New Contributors
            ///
            var newContributors = await ContributorService.GetNewContributorsAsync(commits);
            if (newContributors.Length > 0)
            {
                releaseNotes.AppendLine("\n\n### New Contributors");
                releaseNotes.AppendLine(newContributors.ToString());
            }

            ///
            /// Full Changelog
            /// 
            var fullChangelog = GetFullChangelog(sinceTag, tag);
            releaseNotes.AppendLine(fullChangelog.ToString());

            return releaseNotes.ToString();
        }

        private StringBuilder GetFullChangelog(string? sinceTag, string tag)
        {
            StringBuilder releaseNotes = new();
            if (string.IsNullOrEmpty(sinceTag))
            {
                releaseNotes.AppendLine($"\n\n**Full Changelog**: https://github.com/{Credentials.GetOwner()}/{Repo}/commits/{tag}");
            }
            else
            {
                releaseNotes.AppendLine($"\n\n**Full Changelog**: https://github.com/{Credentials.GetOwner()}/{Repo}/compare/{sinceTag}...{tag}");
            }

            return releaseNotes;
        }
    }
}
