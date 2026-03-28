// PipelineRun.cs
// Neutral representation of a pipeline/workflow run.

using System;

namespace Sdo.Services
{
    /// <summary>
    /// Neutral representation of a pipeline/workflow run.
    /// </summary>
    public class PipelineRun : IPipeline
    {
        /// <summary>Platform-specific identifier (stringified).</summary>
        public string? PlatformId { get; set; }

        /// <summary>Friendly name or build number.</summary>
        public string? Name { get; set; }

        /// <summary>Branch or ref the run executed on.</summary>
        public string? Branch { get; set; }

        /// <summary>Run status (e.g., in_progress, completed).</summary>
        public string? Status { get; set; }

        /// <summary>Result/conclusion (e.g., success, failed).</summary>
        public string? Result { get; set; }

        /// <summary>When the run started.</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>When the run finished (or last updated).</summary>
        public DateTime? FinishedAt { get; set; }

        /// <summary>URL to the run details.</summary>
        public string? Url { get; set; }
    }
}
