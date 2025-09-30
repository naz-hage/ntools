using System.Text;
using wi;

[TestClass]
public class AzureDevOpsWorkItemHelperTests
{
    private const string TestOrganization = "https://dev.azure.com/testorg";
    private const string TestProject = "TestProject";
    private const string TestPat = "test-pat-token";

    [TestInitialize]
    public void Setup()
    {
        // Set the PAT environment variable for testing
        Environment.SetEnvironmentVariable("PAT", TestPat);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("PAT", null);
    }

    [TestMethod]
    public void Constructor_WhenPatNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PAT", null);

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => new AzureDevOpsWorkItemHelper(TestOrganization, TestProject));

        Assert.AreEqual("Personal Access Token (PAT) is required.", exception.Message);
    }

    [TestMethod]
    public void Constructor_WhenPatIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PAT", "");

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => new AzureDevOpsWorkItemHelper(TestOrganization, TestProject));

        Assert.AreEqual("Personal Access Token (PAT) is required.", exception.Message);
    }

    [TestMethod]
    public void Constructor_WhenPatIsWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PAT", "   ");

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => new AzureDevOpsWorkItemHelper(TestOrganization, TestProject));

        Assert.AreEqual("Personal Access Token (PAT) is required.", exception.Message);
    }

    [TestMethod]
    public void Constructor_WhenPatIsValid_CreatesInstance()
    {
        // Arrange & Act
        var helper = new AzureDevOpsWorkItemHelper(TestOrganization, TestProject);

        // Assert
        Assert.IsNotNull(helper);
    }

    [TestMethod]
    public void HttpClientCreation_SetsCorrectAuthorizationHeader()
    {
        // Arrange
        var helper = new TestableAzureDevOpsWorkItemHelper(TestOrganization, TestProject);

        // Act
        using var client = helper.CreateHttpClientForTesting();

        // Assert
        Assert.IsNotNull(client.DefaultRequestHeaders.Authorization);
        Assert.AreEqual("Basic", client.DefaultRequestHeaders.Authorization.Scheme);

        // Verify the PAT is encoded correctly
        var expectedToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{TestPat}"));
        Assert.AreEqual(expectedToken, client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [TestMethod]
    public void Interface_IsImplemented()
    {
        // Arrange & Act
        var helper = new AzureDevOpsWorkItemHelper(TestOrganization, TestProject);

        // Assert
        Assert.IsInstanceOfType(helper, typeof(IAzureDevOpsWorkItemHelper));
    }

    /// <summary>
    /// Testable version of AzureDevOpsWorkItemHelper that exposes protected methods for testing
    /// </summary>
    private class TestableAzureDevOpsWorkItemHelper : AzureDevOpsWorkItemHelper
    {
        public TestableAzureDevOpsWorkItemHelper(string organization, string project)
            : base(organization, project)
        {
        }

        public HttpClient CreateHttpClientForTesting()
        {
            return CreateHttpClient();
        }
    }
}
