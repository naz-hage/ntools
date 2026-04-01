using System;

namespace Sdo.Services
{
    /// <summary>
    /// Represents an Azure DevOps project API response.
    /// </summary>
    public class AzureDevOpsProjectResponse
    {
        /// <summary>
        /// Gets or sets the project ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the project description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the project URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the project state.
        /// </summary>
        public string? State { get; set; }
    }
}