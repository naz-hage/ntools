// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AzureDevOpsCredentials.cs
//
// Provides centralized methods for retrieving Azure DevOps credentials (Personal Access Token).

using System.Diagnostics;

namespace Sdo.Services
{
    /// <summary>
    /// Provides methods for retrieving Azure DevOps credentials, such as the Personal Access Token (PAT).
    /// </summary>
    public static class AzureDevOpsCredentials
    {
        /// <summary>
        /// Gets the Azure DevOps Personal Access Token.
        /// </summary>
        /// <returns>The Azure DevOps PAT.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the token cannot be retrieved from any source.</exception>
        public static string GetToken()
        {
            return GetTokenOrDefault() ?? throw new InvalidOperationException("Azure DevOps Personal Access Token could not be retrieved from environment variable or Azure CLI.");
        }

        /// <summary>
        /// Gets the Azure DevOps Personal Access Token, or null if not available.
        /// </summary>
        /// <returns>The Azure DevOps PAT, or null if not available.</returns>
        public static string? GetTokenOrDefault()
        {
            // Try environment variable first
            var token = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }

            // Try Azure CLI as fallback
            try
            {
                token = GetTokenFromAzureCli();
                if (!string.IsNullOrEmpty(token))
                {
                    return token;
                }
            }
            catch
            {
                // Azure CLI not available or not authenticated
            }

            return null;
        }

        /// <summary>
        /// Retrieves the Azure DevOps PAT from the Azure CLI.
        /// </summary>
        /// <returns>The PAT from Azure CLI, or null if not available.</returns>
        private static string? GetTokenFromAzureCli()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = "account get-access-token --query accessToken -o tsv",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return null;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output.Trim();
                }
            }
            catch
            {
                // Azure CLI not available or error occurred
            }

            return null;
        }

        /// <summary>
        /// Gets the Azure DevOps organization name.
        /// </summary>
        /// <returns>The Azure DevOps organization name.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the organization cannot be determined.</exception>
        public static string GetOrganization()
        {
            // Try environment variable
            var org = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG");
            if (!string.IsNullOrEmpty(org))
            {
                return org;
            }

            throw new InvalidOperationException("Environment variable 'AZURE_DEVOPS_ORG' is required for Azure DevOps operations.");
        }

        /// <summary>
        /// Gets the Azure DevOps organization name, or null if not available.
        /// </summary>
        /// <returns>The Azure DevOps organization name, or null if not available.</returns>
        public static string? GetOrganizationOrDefault()
        {
            return Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG");
        }
    }
}
