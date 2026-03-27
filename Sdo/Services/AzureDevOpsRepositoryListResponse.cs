using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a list of Azure DevOps Git repositories API response.
    /// </summary>
    public class AzureDevOpsRepositoryListResponse
    {
        /// <summary>
        /// Gets or sets the list of repositories.
        /// </summary>
        public List<AzureDevOpsRepositoryResponse>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }
}