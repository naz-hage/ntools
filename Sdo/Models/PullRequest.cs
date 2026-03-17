// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PullRequest.cs
//
// Model representing a pull request from GitHub or Azure DevOps.

using System;
using System.Collections.Generic;

namespace Sdo.Models
{
    /// <summary>
    /// Model representing a pull request across GitHub and Azure DevOps platforms.
    /// </summary>
    public class PullRequest : BaseEntity
    {
        /// <summary>
        /// Gets or sets the pull request number/ID.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the pull request description/body.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the pull request status.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the pull request URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the author of the pull request.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the source branch.
        /// </summary>
        public string? SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the target branch.
        /// </summary>
        public string? TargetBranch { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public new DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets whether the PR is a draft (GitHub-specific).
        /// </summary>
        public bool? IsDraft { get; set; }

        /// <summary>
        /// Gets or sets the number of approvals (Azure DevOps-specific).
        /// </summary>
        public int? ApprovalCount { get; set; }

        /// <summary>
        /// Gets or sets reviewer names (comma-separated).
        /// </summary>
        public string? Reviewers { get; set; }

        /// <summary>
        /// Gets or sets the merged timestamp (if merged).
        /// </summary>
        public DateTime? MergedAt { get; set; }

        /// <summary>
        /// Gets or sets whether the PR is merged.
        /// </summary>
        public bool? IsMerged { get; set; }
    }
}
