// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// Repository.cs
//
// Model class for repositories across different platforms.

namespace Sdo.Models
{
    /// <summary>
    /// Represents a repository in a DevOps platform.
    /// </summary>
    public class Repository : BaseEntity
    {
        /// <summary>
        /// Gets or sets the name of the repository.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the repository.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the owner/organization of the repository.
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// Gets or sets whether the repository is private.
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Gets or sets the URL to the repository.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the default branch name.
        /// </summary>
        public string? DefaultBranch { get; set; }

        /// <summary>
        /// Gets or sets the programming language of the repository.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the platform-specific identifier.
        /// </summary>
        public string? PlatformId { get; set; }

        /// <summary>
        /// Gets or sets the number of stargazers (GitHub only).
        /// </summary>
        public int StargazersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of watchers (GitHub only).
        /// </summary>
        public int WatchersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of forks (GitHub only).
        /// </summary>
        public int ForksCount { get; set; }

        /// <summary>
        /// Gets or sets the repository topics/tags (GitHub only).
        /// </summary>
        public List<string>? Topics { get; set; }

        /// <summary>
        /// Gets or sets the repository size in bytes (Azure DevOps only).
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the remote URL (Azure DevOps only).
        /// </summary>
        public string? RemoteUrl { get; set; }

        /// <summary>
        /// Gets or sets the project ID (Azure DevOps only).
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// Validates the repository data.
        /// </summary>
        /// <returns>True if the repository is valid, false otherwise.</returns>
        public override bool IsValid()
        {
            return base.IsValid() &&
                   !string.IsNullOrEmpty(Name) &&
                   !string.IsNullOrEmpty(Owner);
        }
    }
}