using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a list of Azure DevOps pull requests API response.
    /// </summary>
    public class AzureDevOpsPullRequestListResponse
    {
        /// <summary>
        /// Gets or sets the list of pull requests.
        /// </summary>
        public List<AzureDevOpsPullRequestResponse>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }
}