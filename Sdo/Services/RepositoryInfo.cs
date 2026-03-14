// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PlatformDetector.cs
//
// Implementation of platform detection service that analyzes Git remote URLs
// to determine the DevOps platform (GitHub or Azure DevOps).

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