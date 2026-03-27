using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents the batch API response for multiple work items.
    /// </summary>
    public class BatchWorkItemsResponse
    {
        /// <summary>
        /// Gets or sets the list of work item responses.
        /// </summary>
        [JsonPropertyName("value")]
        public List<WorkItemResponse>? Value { get; set; }
    }
}