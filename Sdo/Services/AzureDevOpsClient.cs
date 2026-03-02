// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// AzureDevOpsClient.cs
//
// Azure DevOps API client for authentication verification and basic operations.

using System.Net.Http.Headers;
using System.Text.Json;

namespace Sdo.Services
{
    /// <summary>
    /// Azure DevOps API client for authentication and basic operations.
    /// </summary>
    public class AzureDevOpsClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _pat;
        private readonly string _organization;

        /// <summary>
        /// Initializes a new instance of the AzureDevOpsClient class.
        /// </summary>
        /// <param name="pat">The Azure DevOps Personal Access Token.</param>
        /// <param name="organization">The Azure DevOps organization name.</param>
        public AzureDevOpsClient(string pat, string organization)
        {
            _pat = pat;
            _organization = organization;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{_pat}")));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Verifies the authentication token by making a request to the Azure DevOps API.
        /// </summary>
        /// <returns>True if authentication is successful, false otherwise.</returns>
        public async Task<bool> VerifyAuthenticationAsync()
        {
            try
            {
                // Use projects API to verify authentication, similar to Python SDO
                var url = $"https://dev.azure.com/{_organization}/_apis/projects?api-version=7.0";
                var response = await _httpClient.GetAsync(url);

                // Check for authentication/authorization failures
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    response.StatusCode == System.Net.HttpStatusCode.NonAuthoritativeInformation)
                {
                    Console.WriteLine($"Debug: API returned {response.StatusCode} - authentication/authorization failed");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Debug: Error response content: {errorContent}");
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Debug: API returned {response.StatusCode} - not successful");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Debug: Error response: {errorContent}");

                    // If we get a 500 error, it might be a service issue, not auth issue
                    // Let's try a different approach - check if the PAT format is correct by examining it
                    Console.WriteLine($"Debug: PAT length is {_pat?.Length ?? 0} characters");
                    if (_pat != null && _pat.Length < 50)
                    {
                        Console.WriteLine("Debug: Warning - PAT appears to be unusually short. Azure DevOps PATs are typically 52 characters.");
                    }

                    return false;
                }

                // If we get a successful response, authentication worked
                Console.WriteLine($"Debug: API returned {response.StatusCode} - authentication successful");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Debug: Exception during API call: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the authenticated user's information.
        /// </summary>
        /// <returns>The user information, or null if request fails.</returns>
        public async Task<AzureDevOpsUser?> GetUserAsync()
        {
            try
            {
                var url = $"https://dev.azure.com/{_organization}/_apis/connectionData?api-version=7.1";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var connectionData = JsonSerializer.Deserialize<ConnectionData>(content);
                return connectionData?.AuthenticatedUser;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Represents Azure DevOps connection data.
    /// </summary>
    public class ConnectionData
    {
        /// <summary>
        /// Gets or sets the authenticated user.
        /// </summary>
        public AzureDevOpsUser? AuthenticatedUser { get; set; }
    }

    /// <summary>
    /// Represents an Azure DevOps user.
    /// </summary>
    public class AzureDevOpsUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? UniqueName { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? PreferredEmail { get; set; }
    }
}