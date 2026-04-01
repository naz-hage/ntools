using System.Collections.Generic;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a work item query result.
    /// </summary>
    public class WorkItemQueryResult
    {
        /// <summary>
        /// Gets or sets the list of work items.
        /// </summary>
        public List<WorkItemReference>? WorkItems { get; set; }
    }
}