// IPipeline.cs
// Lightweight interface for pipeline-like models.

using System;

namespace Sdo.Services
{
    /// <summary>
    /// Lightweight interface representing common pipeline properties.
    /// </summary>
    public interface IPipeline
    {
        /// <summary>Platform-specific identifier (stringified).</summary>
        string? PlatformId { get; set; }

        /// <summary>Name of the pipeline or run.</summary>
        string? Name { get; set; }

        /// <summary>URL or HTML link to the pipeline or run.</summary>
        string? Url { get; set; }
    }
}
