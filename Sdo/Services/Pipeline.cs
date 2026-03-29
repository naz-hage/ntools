// Pipeline.cs
// Shared base class for pipeline-like models.

using System;

namespace Sdo.Services
{
    /// <summary>
    /// Base pipeline model capturing common properties between definitions and runs.
    /// </summary>
    public abstract class Pipeline : IPipeline
    {
        /// <summary>Platform-specific identifier (stringified).</summary>
        public string? PlatformId { get; set; }

        /// <summary>Name of the pipeline or run.</summary>
        public string? Name { get; set; }

        /// <summary>URL or HTML link to the pipeline or run.</summary>
        public string? Url { get; set; }

        public override string ToString()
        {
            return Name ?? PlatformId ?? base.ToString()!;
        }
    }
}
