using System.Collections.Generic;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a list of Azure DevOps builds API response.
    /// </summary>
    public class AzureDevOpsBuildListResponse
    {
        /// <summary>
        /// Gets or sets the list of builds.
        /// </summary>
        public List<AzureDevOpsBuild>? Value { get; set; }
    }
}