namespace Sdo.Services
{
    /// <summary>
    /// Represents Azure DevOps connection data.
    /// </summary>
    public class ConnectionData
    {
        /// <summary>
        /// Gets or sets the authenticated user.
        /// </summary>
        public AzureDevOpsUser? AuthenticatedUser { get; set; }
    }
}