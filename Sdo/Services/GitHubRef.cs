// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub ref (branch) reference.
    /// </summary>
    public class GitHubRef
    {
        /// <summary>
        /// Gets or sets the ref name (branch name).
        /// </summary>
        [JsonPropertyName("ref")]
        public string? Ref { get; set; }

        /// <summary>
        /// Gets or sets the commit SHA.
        /// </summary>
        public string? Sha { get; set; }
    }
}
