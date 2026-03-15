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
    /// Represents GitHub repository information.
    /// </summary>
    public class RepositoryInfo
    {
        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string? Repo { get; set; }
    }
}