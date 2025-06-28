using System.Net;
using wi;

[TestClass]
public class HttpClientExtensionsTests
{
    [TestMethod]
    public async Task PatchAsync_WhenCalled_CreatesCorrectRequestMessage()
    {
        // Arrange
        using var httpClient = new HttpClient(new TestMessageHandler());
        var requestUri = "https://api.example.com/test";
        var content = new StringContent("test content");

        // Act
        var response = await httpClient.PatchAsync(requestUri, content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task PatchAsync_WhenContentIsNull_DoesNotThrow()
    {
        // Arrange
        using var httpClient = new HttpClient(new TestMessageHandler());
        var requestUri = "https://api.example.com/test";
        HttpContent? content = null;

        // Act & Assert (should not throw)
        var response = await httpClient.PatchAsync(requestUri, content!);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task PatchAsync_WithJsonContent_WorksCorrectly()
    {
        // Arrange
        using var httpClient = new HttpClient(new TestMessageHandler());
        var requestUri = "https://api.example.com/test";
        var jsonContent = new StringContent("{\"test\": \"value\"}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PatchAsync(requestUri, jsonContent);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test message handler that always returns OK
    /// </summary>
    private class TestMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Verify this is a PATCH request
            Assert.AreEqual("PATCH", request.Method.Method);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
