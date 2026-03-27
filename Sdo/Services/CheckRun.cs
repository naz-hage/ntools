// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub check run.
    /// </summary>
    public class CheckRun
    {
        /// <summary>
        /// Gets or sets the check run name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the check run status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the check run conclusion.
        /// </summary>
        public string? Conclusion { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the completion time.
        /// </summary>
        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the details URL.
        /// </summary>
        [JsonPropertyName("details_url")]
        public string? DetailsUrl { get; set; }
    }
}
