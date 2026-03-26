// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// RepositoryInfo.cs
//
// Defines a data model that represents Git repository information,
// including the repository owner and name.

namespace Sdo.Services
{
    /// <summary>
    /// Represents GitHub or Azure DevOps repository information.
    /// </summary>
    public class RepositoryInfo
    {
        /// <summary>
        /// Gets or sets the repository owner (GitHub) or organization (Azure DevOps).
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// Gets or sets the repository name (GitHub) or repository name (Azure DevOps).
        /// </summary>
        public string? Repo { get; set; }

        /// <summary>
        /// Gets or sets the Azure DevOps organization name.
        /// </summary>
        public string? Organization { get; set; }

        /// <summary>
        /// Gets or sets the Azure DevOps project name.
        /// </summary>
        public string? Project { get; set; }

        /// <summary>
        /// Gets or sets the Azure DevOps repository name.
        /// </summary>
        public string? Repository { get; set; }
    }
}