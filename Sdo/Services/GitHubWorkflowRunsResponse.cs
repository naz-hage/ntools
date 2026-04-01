// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents the workflow runs response from GitHub API.
    /// </summary>
    public class GitHubWorkflowRunsResponse
    {
        /// <summary>
        /// Gets or sets the list of workflow runs.
        /// </summary>
        [JsonPropertyName("workflow_runs")]
        public List<GitHubWorkflowRun>? WorkflowRuns { get; set; }
    }
}
