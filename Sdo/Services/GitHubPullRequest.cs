// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub pull request API response.
    /// </summary>
    public class GitHubPullRequest
    {
        /// <summary>
        /// Gets or sets the pull request ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the pull request number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the pull request body/description.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the pull request state (open, closed).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets whether the pull request is a draft.
        /// </summary>
        public bool Draft { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update date.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the merge date (if merged).
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("merged_at")]
        public DateTime? MergedAt { get; set; }

        /// <summary>
        /// Gets or sets whether the pull request is merged.
        /// </summary>
        public bool Merged { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the pull request.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the pull request author/user.
        /// </summary>
        public GitHubUser? User { get; set; }

        /// <summary>
        /// Gets or sets the source branch information.
        /// </summary>
        public GitHubRef? Head { get; set; }

        /// <summary>
        /// Gets or sets the target branch information.
        /// </summary>
        public GitHubRef? Base { get; set; }
    }
}
