// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// GitHubWorkflow.cs
//
// GitHub API client for authentication verification and basic operations.
// Reuses GitHubRelease authentication logic for token retrieval.


using Nbuild;
using Nbuild.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO.Compression;
using System.Text;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub Actions workflow.
    /// </summary>
    public class GitHubWorkflow
    {
        /// <summary>
        /// Gets or sets the workflow ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the workflow node ID.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("node_id")]
        public string? NodeId { get; set; }

        /// <summary>
        /// Gets or sets the workflow name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the path to the workflow file in the repository (e.g. .github/workflows/ci.yml).
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the workflow state (e.g. "active" or "disabled").
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets when the workflow was created.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the workflow was last updated.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the workflow.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("badge_url")]
        public string? BadgeUrl { get; set; }
    }
}