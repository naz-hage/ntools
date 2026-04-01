using System;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps build run.
    /// </summary>
    public class AzureDevOpsBuild
    {
        /// <summary>
        /// Gets or sets the build ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the build number.
        /// </summary>
        [JsonPropertyName("buildNumber")]
        public string? BuildNumber { get; set; }

        /// <summary>
        /// Gets or sets the build status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the build result.
        /// </summary>
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the source branch.
        /// </summary>
        [JsonPropertyName("sourceBranch")]
        public string? SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets queue time.
        /// </summary>
        [JsonPropertyName("queueTime")]
        public DateTime? QueueTime { get; set; }

        /// <summary>
        /// Gets or sets start time.
        /// </summary>
        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets finish time.
        /// </summary>
        [JsonPropertyName("finishTime")]
        public DateTime? FinishTime { get; set; }

        /// <summary>
        /// Gets or sets the build definition reference.
        /// </summary>
        public AzureDevOpsBuildDefinitionReference? Definition { get; set; }

        /// <summary>
        /// Gets or sets logs resource metadata.
        /// </summary>
        public AzureDevOpsResourceReference? Logs { get; set; }

        /// <summary>
        /// Gets or sets the build URL.
        /// </summary>
        public string? Url { get; set; }
    }
}