namespace wi
{
    /// <summary>
    /// Interface for Azure DevOps work item operations.
    /// </summary>
    public interface IAzureDevOpsWorkItemHelper
    {
        /// <summary>
        /// Creates a Product Backlog Item (PBI) with the given title and parent ID.
        /// </summary>
        /// <param name="title">The title of the PBI.</param>
        /// <param name="parentId">The parent work item ID.</param>
        /// <returns>The new PBI ID, or null if creation failed.</returns>
        Task<int?> CreatePbiAsync(string title, int parentId);

        /// <summary>
        /// Creates a child Task work item with the same title as the specified PBI.
        /// </summary>
        /// <param name="pbiId">The parent PBI work item ID.</param>
        Task CreateChildTaskWithSameTitleAsync(int pbiId);
    }
}
