using System;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps pull request API response.
    /// </summary>
    public class AzureDevOpsPullRequestResponse
    {
        /// <summary>
        /// Gets or sets the pull request ID.
        /// </summary>
        [JsonPropertyName("pullRequestId")]
        public int PullRequestId { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the pull request description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the pull request status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the source ref name (branch).
        /// </summary>
        [JsonPropertyName("sourceRefName")]
        public string? SourceRefName { get; set; }

        /// <summary>
        /// Gets or sets the target ref name (branch).
        /// </summary>
        [JsonPropertyName("targetRefName")]
        public string? TargetRefName { get; set; }

        /// <summary>
        /// Gets or sets the pull request URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets creation date.
        /// </summary>
        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the user who created the pull request.
        /// </summary>
        [JsonPropertyName("createdBy")]
        public AzureDevOpsUser? CreatedBy { get; set; }
    }
}