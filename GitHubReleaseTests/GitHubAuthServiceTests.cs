using GitHubRelease;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GitHubReleaseTests
{
    [TestClass]
    public class GitHubAuthServiceTests
    {
        private Mock<HttpMessageHandler>? _mockHttpMessageHandler;
        private HttpClient? _httpClient;
        private GitHubAuthService? _authService;
        private GitHubAuthService? _verboseAuthService;

        [TestInitialize]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _authService = new GitHubAuthService(false);
            _verboseAuthService = new GitHubAuthService(true);

            // Inject the mocked HttpClient using reflection (since it's private)
            var httpClientField = typeof(GitHubAuthService).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(_authService, _httpClient);
            httpClientField?.SetValue(_verboseAuthService, _httpClient);
        }

        [TestMethod]
        public async Task RequiresAuthenticationAsync_PublicRepo_ReadOperation_ReturnsFalse()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": false}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.RequiresAuthenticationAsync(repositoryUrl, GitHubOperation.Read);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task RequiresAuthenticationAsync_PublicRepo_WriteOperation_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": false}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.RequiresAuthenticationAsync(repositoryUrl, GitHubOperation.Write);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task RequiresAuthenticationAsync_PrivateRepo_ReadOperation_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": true}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.RequiresAuthenticationAsync(repositoryUrl, GitHubOperation.Read);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task RequiresAuthenticationAsync_PrivateRepo_WriteOperation_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": true}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.RequiresAuthenticationAsync(repositoryUrl, GitHubOperation.Write);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task RequiresAuthenticationAsync_UnknownVisibility_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("", HttpStatusCode.InternalServerError);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.RequiresAuthenticationAsync(repositoryUrl, GitHubOperation.Read);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RequiresAuthentication_PublicRepo_ReadOperation_ReturnsFalse()
        {
            // Arrange
            Assert.IsNotNull(_authService);

            // Act
            var result = _authService.RequiresAuthentication(RepositoryVisibility.Public, GitHubOperation.Read);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RequiresAuthentication_PublicRepo_WriteOperation_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);

            // Act
            var result = _authService.RequiresAuthentication(RepositoryVisibility.Public, GitHubOperation.Write);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RequiresAuthentication_PrivateRepo_ReadOperation_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);

            // Act
            var result = _authService.RequiresAuthentication(RepositoryVisibility.Private, GitHubOperation.Read);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RequiresAuthentication_PrivateRepo_WriteOperation_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);

            // Act
            var result = _authService.RequiresAuthentication(RepositoryVisibility.Private, GitHubOperation.Write);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RequiresAuthentication_UnknownVisibility_ReturnsTrue()
        {
            // Arrange
            Assert.IsNotNull(_authService);

            // Act
            var result = _authService.RequiresAuthentication(RepositoryVisibility.Unknown, GitHubOperation.Read);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task GetRepositoryVisibilityAsync_PublicRepo_ReturnsPublic()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": false}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetRepositoryVisibilityAsync(repositoryUrl);

            // Assert
            Assert.AreEqual(RepositoryVisibility.Public, result);
        }

        [TestMethod]
        public async Task GetRepositoryVisibilityAsync_PrivateRepo_ReturnsPrivate()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": true}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetRepositoryVisibilityAsync(repositoryUrl);

            // Assert
            Assert.AreEqual(RepositoryVisibility.Private, result);
        }

        [TestMethod]
        public async Task GetRepositoryVisibilityAsync_NotFound_ReturnsPrivate()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("", HttpStatusCode.NotFound);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetRepositoryVisibilityAsync(repositoryUrl);

            // Assert
            Assert.AreEqual(RepositoryVisibility.Private, result);
        }

        [TestMethod]
        public async Task GetRepositoryVisibilityAsync_Forbidden_ReturnsPrivate()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("", HttpStatusCode.Forbidden);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetRepositoryVisibilityAsync(repositoryUrl);

            // Assert
            Assert.AreEqual(RepositoryVisibility.Private, result);
        }

        [TestMethod]
        public async Task GetRepositoryVisibilityAsync_ServerError_ReturnsUnknown()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("", HttpStatusCode.InternalServerError);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetRepositoryVisibilityAsync(repositoryUrl);

            // Assert
            Assert.AreEqual(RepositoryVisibility.Unknown, result);
        }

        [TestMethod]
        public async Task GetRepositoryVisibilityAsync_InvalidUrl_ReturnsUnknown()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            var repositoryUrl = "invalid-url";

            // Act
            var result = await _authService.GetRepositoryVisibilityAsync(repositoryUrl);

            // Assert
            Assert.AreEqual(RepositoryVisibility.Unknown, result);
        }

        [TestMethod]
        public async Task GetAuthenticationErrorMessageAsync_PublicRepo_WriteOperation_ReturnsCorrectMessage()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": false}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetAuthenticationErrorMessageAsync(repositoryUrl, GitHubOperation.Write);

            // Assert
            StringAssert.Contains(result, "Authentication required for write operations on public repository");
            StringAssert.Contains(result, "owner/repo");
            StringAssert.Contains(result, "API_GITHUB_KEY");
        }

        [TestMethod]
        public async Task GetAuthenticationErrorMessageAsync_PrivateRepo_ReadOperation_ReturnsCorrectMessage()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": true}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetAuthenticationErrorMessageAsync(repositoryUrl, GitHubOperation.Read);

            // Assert
            StringAssert.Contains(result, "Authentication required for all operations on private repository");
            StringAssert.Contains(result, "owner/repo");
            StringAssert.Contains(result, "API_GITHUB_KEY");
        }

        [TestMethod]
        public async Task GetAuthenticationErrorMessageAsync_UnknownVisibility_ReturnsCorrectMessage()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("", HttpStatusCode.InternalServerError);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetAuthenticationErrorMessageAsync(repositoryUrl, GitHubOperation.Read);

            // Assert
            StringAssert.Contains(result, "Authentication required for repository");
            StringAssert.Contains(result, "visibility could not be determined");
            StringAssert.Contains(result, "owner/repo");
            StringAssert.Contains(result, "API_GITHUB_KEY");
        }

        [TestMethod]
        public async Task GetAuthenticationErrorMessageAsync_PublicRepo_ReadOperation_ReturnsUnexpectedMessage()
        {
            // Arrange
            Assert.IsNotNull(_authService);
            SetupMockResponse("{\"private\": false}", HttpStatusCode.OK);
            var repositoryUrl = "https://github.com/owner/repo";

            // Act
            var result = await _authService.GetAuthenticationErrorMessageAsync(repositoryUrl, GitHubOperation.Read);

            // Assert
            StringAssert.Contains(result, "Unexpected authentication requirement for read operation on public repository");
            StringAssert.Contains(result, "owner/repo");
        }

        [TestMethod]
        [DataRow("https://github.com/owner/repo", "owner", "repo")]
        [DataRow("http://github.com/owner/repo", "owner", "repo")]
        [DataRow("github.com/owner/repo", "owner", "repo")]
        [DataRow("owner/repo", "owner", "repo")]
        [DataRow("https://github.com/owner/repo/", "owner", "repo")]
        public void ParseRepositoryUrl_ValidUrls_ReturnsCorrectOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
        {
            // Act - Use reflection to access private method
            var method = typeof(GitHubAuthService).GetMethod("ParseRepositoryUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (ValueTuple<string?, string?>)method?.Invoke(_authService, new object[] { url })!;

            // Assert
            Assert.AreEqual(expectedOwner, result.Item1);
            Assert.AreEqual(expectedRepo, result.Item2);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("invalid")]
        [DataRow("invalid format")]  // Contains space
        [DataRow("invalid/format/extra")]  // Too many parts
        [DataRow("https://github.com/invalid")]  // Missing repo part
        public void ParseRepositoryUrl_InvalidUrls_ReturnsNull(string url)
        {
            // Act - Use reflection to access private method
            var method = typeof(GitHubAuthService).GetMethod("ParseRepositoryUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (ValueTuple<string?, string?>)method?.Invoke(_authService, new object[] { url })!;

            // Assert
            Assert.IsNull(result.Item1);
            Assert.IsNull(result.Item2);
        }

        private void SetupMockResponse(string content, HttpStatusCode statusCode)
        {
            if (_mockHttpMessageHandler == null)
            {
                throw new InvalidOperationException("Mock HTTP message handler is not initialized.");
            }

            var response = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }
    }
}