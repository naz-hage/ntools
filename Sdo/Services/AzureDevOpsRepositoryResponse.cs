using System;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps Git repository API response.
    /// </summary>
    public class AzureDevOpsRepositoryResponse
    {
        /// <summary>
        /// Gets or sets the repository ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the repository URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the repository web URL.
        /// </summary>
        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }

        /// <summary>
        /// Gets or sets the default branch name.
        /// </summary>
        [JsonPropertyName("defaultBranch")]
        public string? DefaultBranch { get; set; }

        /// <summary>
        /// Gets or sets the repository size in bytes.
        /// </summary>
        public long Size { get; set; }
    }
}