namespace Sdo.Services
{
    /// <summary>
    /// Represents a work item reference from a query.
    /// </summary>
    public class WorkItemReference
    {
        /// <summary>
        /// Gets or sets the work item ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work item URL.
        /// </summary>
        public string? Url { get; set; }
    }
}