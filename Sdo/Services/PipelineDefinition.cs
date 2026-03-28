// PipelineDefinition.cs
// Neutral representation of a pipeline/workflow definition.

using System;

namespace Sdo.Services
{
    /// <summary>
    /// Neutral representation of a pipeline/workflow definition.
    /// </summary>
    public class PipelineDefinition : IPipeline
    {
        /// <summary>Platform-specific identifier (stringified).</summary>
        public string? PlatformId { get; set; }

        /// <summary>Name of the pipeline/workflow.</summary>
        public string? Name { get; set; }

        /// <summary>Path to the definition file (if applicable).</summary>
        public string? Path { get; set; }

        /// <summary>Type or category (e.g., "build").</summary>
        public string? Type { get; set; }

        /// <summary>URL or HTML link to the definition.</summary>
        public string? Url { get; set; }

        /// <summary>State (e.g., active/disabled).</summary>
        public string? State { get; set; }

        /// <summary>Creation timestamp.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Last update timestamp.</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
