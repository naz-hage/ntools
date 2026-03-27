using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a list of Azure DevOps build definitions API response.
    /// </summary>
    public class AzureDevOpsBuildDefinitionListResponse
    {
        /// <summary>
        /// Gets or sets the list of build definitions.
        /// </summary>
        public List<AzureDevOpsBuildDefinition>? Value { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
        /// </summary>
        [JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }
    }
}