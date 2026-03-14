// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// IPlatformDetector.cs
//
// Interface for platform detection services that identify the DevOps platform
// (GitHub or Azure DevOps) from Git repository configuration.

namespace Sdo.Interfaces
{
    /// <summary>
    /// Represents the detected DevOps platform.
    /// </summary>
    public enum Platform
    {
        /// <summary>
        /// GitHub platform.
        /// </summary>
        GitHub,

        /// <summary>
        /// Azure DevOps platform.
        /// </summary>
        AzureDevOps,

        /// <summary>
        /// Unknown or unsupported platform.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Interface for detecting the DevOps platform from Git repository configuration.
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Detects the DevOps platform by analyzing Git remote URLs.
        /// </summary>
        /// <returns>The detected platform, or Platform.Unknown if detection fails.</returns>
        /// <exception cref="InvalidOperationException">Thrown when not in a Git repository or no supported remote is found.</exception>
        Platform DetectPlatform();

        /// <summary>
        /// Gets the organization name from the detected platform.
        /// </summary>
        /// <returns>The organization name, or null if not detected.</returns>
        string? GetOrganization();

        /// <summary>
        /// Gets the project/repository name from the detected platform.
        /// </summary>
        /// <returns>The project/repository name, or null if not detected.</returns>
        string? GetProject();
    }
}