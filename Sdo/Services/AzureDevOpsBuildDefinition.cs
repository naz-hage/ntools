using System;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps build definition (pipeline).
    /// </summary>
    public class AzureDevOpsBuildDefinition
    {
        /// <summary>
        /// Gets or sets the pipeline ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the pipeline name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the pipeline folder path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the pipeline type (e.g., "build").
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the URL to the pipeline definition.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the revision number.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// Gets or sets the quality level.
        /// </summary>
        public string? Quality { get; set; }

        /// <summary>
        /// Gets or sets the queue status (e.g., "enabled").
        /// </summary>
        [JsonPropertyName("queueStatus")]
        public string? QueueStatus { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        [JsonPropertyName("modifiedDate")]
        public DateTime? ModifiedDate { get; set; }
    }
}