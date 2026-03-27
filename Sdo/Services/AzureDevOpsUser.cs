namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps user.
    /// </summary>
    public class AzureDevOpsUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? UniqueName { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? PreferredEmail { get; set; }
    }
}