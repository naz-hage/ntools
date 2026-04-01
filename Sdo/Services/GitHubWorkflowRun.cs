// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub Actions workflow run.
    /// </summary>
    public class GitHubWorkflowRun
    {
        /// <summary>
        /// Gets or sets the workflow run ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the workflow run number.
        /// </summary>
        [JsonPropertyName("run_number")]
        public int RunNumber { get; set; }

        /// <summary>
        /// Gets or sets the workflow run name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the branch for the run.
        /// </summary>
        [JsonPropertyName("head_branch")]
        public string? HeadBranch { get; set; }

        /// <summary>
        /// Gets or sets the run event.
        /// </summary>
        public string? Event { get; set; }

        /// <summary>
        /// Gets or sets the run status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the run conclusion.
        /// </summary>
        public string? Conclusion { get; set; }

        /// <summary>
        /// Gets or sets when the run started.
        /// </summary>
        [JsonPropertyName("run_started_at")]
        public DateTime? RunStartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the run was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the run was last updated.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL for the run details page.
        /// </summary>
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the API URL for logs.
        /// </summary>
        [JsonPropertyName("logs_url")]
        public string? LogsUrl { get; set; }
    }
}
