// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// WorkItem.cs
//
// Model class for work items (issues, tasks, etc.) across different platforms.

namespace Sdo.Models
{
    /// <summary>
    /// Represents a work item (issue, task, bug, etc.) in a DevOps platform.
    /// </summary>
    public class WorkItem : BaseEntity
    {
        /// <summary>
        /// Gets or sets the title of the work item.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the work item.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the current state of the work item.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the type of the work item (Issue, Task, Bug, etc.).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the assigned user.
        /// </summary>
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the tags/labels associated with the work item.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the URL to the work item in the DevOps platform.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the platform-specific identifier.
        /// </summary>
        public string? PlatformId { get; set; }

        /// <summary>
        /// Validates the work item data.
        /// </summary>
        /// <returns>True if the work item is valid, false otherwise.</returns>
        public override bool IsValid()
        {
            return base.IsValid() &&
                   !string.IsNullOrEmpty(Title) &&
                   !string.IsNullOrEmpty(Type);
        }
    }
}