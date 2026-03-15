// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// PlatformService.cs
//
// Implementation of platform detection service that analyzes Git remote URLs
// to determine the DevOps platform (GitHub or Azure DevOps).

using Sdo.Interfaces;
using System.Diagnostics;

namespace Sdo.Services
{
    /// <summary>
    /// Implementation of platform detection that analyzes Git remote URLs.
    /// </summary>
    public class PlatformService : IPlatformService
    {
        private Platform _detectedPlatform = Platform.Unknown;
        private string? _organization;
        private string? _project;

        /// <summary>
        /// Detects the DevOps platform by analyzing Git remote URLs.
        /// </summary>
        /// <returns>The detected platform.</returns>
        /// <exception cref="InvalidOperationException">Thrown when not in a Git repository or no supported remote is found.</exception>
        public Platform DetectPlatform()
        {
            if (_detectedPlatform != Platform.Unknown)
            {
                return _detectedPlatform;
            }

            try
            {
                var remoteUrl = GetGitRemoteUrl();
                if (string.IsNullOrEmpty(remoteUrl))
                {
                    throw new InvalidOperationException("Not a Git repository.\nPlease ensure you're in an Azure or GitHub repository.");
                }

                _detectedPlatform = ParsePlatformFromUrl(remoteUrl);
                if (_detectedPlatform == Platform.Unknown)
                {
                    throw new InvalidOperationException("Not a Git repository.\nPlease ensure you're in an Azure or GitHub repository.");
                }

                ParseOrganizationAndProject(remoteUrl);

                return _detectedPlatform;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                throw new InvalidOperationException("Not a Git repository.\nPlease ensure you're in an Azure or GitHub repository.");
            }
        }

        /// <summary>
        /// Gets the organization name from the detected platform.
        /// </summary>
        /// <returns>The organization name, or null if not detected.</returns>
        public string? GetOrganization()
        {
            // Ensure platform is detected
            DetectPlatform();
            return _organization;
        }

        /// <summary>
        /// Gets the project/repository name from the detected platform.
        /// </summary>
        /// <returns>The project/repository name, or null if not detected.</returns>
        public string? GetProject()
        {
            // Ensure platform is detected
            DetectPlatform();
            return _project;
        }

        /// <summary>
        /// Gets the repository information for GitHub repositories.
        /// </summary>
        /// <returns>RepositoryInfo with owner and repo, or null if not a GitHub repository.</returns>
        public RepositoryInfo? GetRepositoryInfo()
        {
            if (DetectPlatform() != Platform.GitHub)
            {
                return null;
            }

            return new RepositoryInfo
            {
                Owner = _organization,
                Repo = _project
            };
        }

        /// <summary>
        /// Gets the Git remote URL from the current repository.
        /// </summary>
        /// <returns>The remote URL, or null if not found.</returns>
        private string? GetGitRemoteUrl()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "remote get-url origin",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output.Trim();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses the platform from the Git remote URL.
        /// </summary>
        /// <param name="url">The Git remote URL.</param>
        /// <returns>The detected platform.</returns>
        private Platform ParsePlatformFromUrl(string url)
        {
            var lowerUrl = url.ToLower();

            if (lowerUrl.Contains("github.com"))
            {
                return Platform.GitHub;
            }

            if (lowerUrl.Contains("dev.azure.com") || lowerUrl.Contains("visualstudio.com"))
            {
                return Platform.AzureDevOps;
            }

            return Platform.Unknown;
        }

        /// <summary>
        /// Parses the organization and project from the Git remote URL.
        /// </summary>
        /// <param name="url">The Git remote URL.</param>
        private void ParseOrganizationAndProject(string url)
        {
            try
            {
                // Remove protocol and credentials
                var cleanUrl = url
                    .Replace("https://", "")
                    .Replace("http://", "")
                    .Replace("git@", "")
                    .Replace("ssh://", "")
                    .Split('@').Last() // Remove user@ if present
                    .Split('?').First(); // Remove query parameters

                if (_detectedPlatform == Platform.GitHub)
                {
                    // GitHub format: github.com/organization/repository
                    var parts = cleanUrl.Split('/');
                    if (parts.Length >= 3 && parts[0].Contains("github.com"))
                    {
                        _organization = parts[1];
                        _project = parts[2].Replace(".git", "");
                    }
                }
                else if (_detectedPlatform == Platform.AzureDevOps)
                {
                    // Azure DevOps formats:
                    // dev.azure.com/organization/project/_git/repository
                    // organization.visualstudio.com/project/_git/repository
                    var parts = cleanUrl.Split('/');
                    if (parts.Length >= 5)
                    {
                        if (parts[0].Contains("dev.azure.com"))
                        {
                            _organization = parts[1];
                            _project = parts[2]; // Project is parts[2], repository is parts[4]
                        }
                        else if (parts[0].Contains("visualstudio.com"))
                        {
                            _organization = parts[0].Split('.').First();
                            _project = parts[1]; // Project is parts[1] for visualstudio.com format
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }
    }
}