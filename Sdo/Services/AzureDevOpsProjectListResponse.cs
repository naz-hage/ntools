using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a list of Azure DevOps projects API response.
    /// </summary>
    public class AzureDevOpsProjectListResponse
    {
        /// <summary>
        /// Gets or sets the list of projects.
        /// </summary>
        public List<AzureDevOpsProjectResponse>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }
}