// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Sdo.Services
{
    /// <summary>
    /// Represents a GitHub user.
    /// </summary>
    public class GitHubUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? Login { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? Email { get; set; }
    }
}
