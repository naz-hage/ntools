﻿namespace GitHubRelease
{
    /// <summary>
    /// Represents an API service for interacting with GitHub.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient Client;
        private readonly bool Verbose;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiService"/> class.
        /// </summary>
        public ApiService(bool verbose = false)
        {
            Verbose = verbose;
            Client = new HttpClient();
        }

        /// <summary>
        /// Gets the HTTP client instance.
        /// </summary>
        /// <returns>The HTTP client instance.</returns>
        public HttpClient GetClient()
        {
            return Client;
        }

        /// <summary>
        /// Sets up the headers for making API requests.
        /// </summary>
        /// <param name="token">The access token for authentication.</param>
        public void SetupHeaders()
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Credentials.GetToken()}");
            Client.DefaultRequestHeaders.Add("User-Agent", "request");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        /// <summary>
        /// Sends a GET request to the specified URI.
        /// </summary>
        /// <param name="uri">The URI to send the request to.</param>
        /// <returns>The HTTP response message.</returns>
        public async Task<HttpResponseMessage> GetAsync(string uri)
        {
            if (Verbose) Console.WriteLine($"GET uri: {uri}");
            return await Client.GetAsync(uri);
        }

        /// <summary>
        /// Sends a POST request to the specified URI with the given content.
        /// </summary>
        /// <param name="uri">The URI to send the request to.</param>
        /// <param name="content">The HTTP content to send with the request.</param>
        /// <returns>The HTTP response message.</returns>
        public async Task<HttpResponseMessage> PostAsync(string uri, HttpContent content)
        {
            if (Verbose) Console.WriteLine($"POST uri: {uri}");
            return await Client.PostAsync(uri, content);
        }

        /// <summary>
        /// Sends a DELETE request to the specified URI.
        /// </summary>
        /// <param name="uri">The URI to send the request to.</param>
        /// <returns>The HTTP response message.</returns>
        public async Task<HttpResponseMessage> DeleteAsync(string uri)
        {
            if (Verbose) Console.WriteLine($"DELETE uri: {uri}");
            return await Client.DeleteAsync(uri);
        }
    }
}
