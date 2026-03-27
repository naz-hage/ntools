// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub issue label.
    /// </summary>
    public class GitHubLabel
    {
        /// <summary>
        /// Gets or sets the label name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the label description.
        /// </summary>
        public string? Description { get; set; }
    }
}
