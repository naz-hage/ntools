// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents the check runs response from GitHub API.
    /// </summary>
    public class CheckRunsResponse
    {
        /// <summary>
        /// Gets or sets the list of check runs.
        /// </summary>
        [JsonPropertyName("check_runs")]
        public List<CheckRun>? CheckRuns { get; set; }
    }
}
