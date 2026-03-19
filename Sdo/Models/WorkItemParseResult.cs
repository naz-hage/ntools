// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Sdo.Models
{
    /// <summary>
    /// Parse result for work items extracted from markdown files.
    /// </summary>
    public class WorkItemParseResult
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AcceptanceCriteria { get; set; } = new List<string>();
        public string ReproSteps { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    }
}
