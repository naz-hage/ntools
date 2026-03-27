// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub repository API response.
    /// </summary>
    public class GitHubRepositoryResponse
    {
        /// <summary>
        /// Gets or sets the repository ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the repository description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether the repository is private.
        /// </summary>
        public bool Private { get; set; }

        /// <summary>
        /// Gets or sets the default branch name.
        /// </summary>
        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the repository.
        /// </summary>
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public GitHubUser? Owner { get; set; }

        /// <summary>
        /// Gets or sets the number of stargazers.
        /// </summary>
        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of watchers.
        /// </summary>
        [JsonPropertyName("watchers_count")]
        public int WatchersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of forks.
        /// </summary>
        [JsonPropertyName("forks_count")]
        public int ForksCount { get; set; }

        /// <summary>
        /// Gets or sets the repository topics/tags.
        /// </summary>
        public List<string>? Topics { get; set; }
    }
}
