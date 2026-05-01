using System;

namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps work item.
    /// </summary>
    public class AzureDevOpsWorkItem
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the work item type (PBI, Task, Bug, etc.).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the work item state (New, Approved, Committed, Done, etc.).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the work item description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last change date.
        /// </summary>
        public DateTime ChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the number of comments.
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// Gets or sets the work item URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the sprint/iteration name.
        /// </summary>
        public string? Sprint { get; set; }

        /// <summary>
        /// Gets or sets the area path.
        /// </summary>
        public string? Area { get; set; }

        /// <summary>
        /// Gets or sets the assigned to user name.
        /// </summary>
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the acceptance criteria.
        /// </summary>
        public string? AcceptanceCriteria { get; set; }

        /// <summary>
        /// Gets or sets the parent work item ID.
        /// </summary>
        public int? ParentId { get; set; }
    }
}