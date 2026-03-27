// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub issue.
    /// </summary>
    public class GitHubIssue
    {
        /// <summary>
        /// Gets or sets the issue ID (using long to support large GitHub issue IDs).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the issue number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the issue title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the issue body/description.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the issue state (open, closed).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the number of comments.
        /// </summary>
        public int Comments { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update date.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the issue.
        /// </summary>
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the labels assigned to the issue.
        /// </summary>
        public List<GitHubLabel>? Labels { get; set; }

        /// <summary>
        /// Gets or sets the assignee of the issue.
        /// </summary>
        public GitHubUser? Assignee { get; set; }

        /// <summary>
        /// Gets or sets the pull request object (null for issues, non-null for PRs).
        /// Used to distinguish between issues and pull requests.
        /// </summary>
        [JsonPropertyName("pull_request")]
        public object? PullRequest { get; set; }
    }
}
