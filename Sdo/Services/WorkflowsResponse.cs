// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Sdo.Services
{
    /// <summary>
    /// Represents the workflows list response from GitHub API.
    /// </summary>
    public class WorkflowsResponse
    {
        /// <summary>
        /// Gets or sets the list of workflows.
        /// </summary>
        public List<GitHubWorkflow>? Workflows { get; set; }
    }
}
