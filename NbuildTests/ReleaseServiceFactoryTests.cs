using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NbuildTests
{
    [TestClass]
    public class ReleaseServiceFactoryTests
    {
        [TestMethod]
        public void DownloadApp_UsesReleaseServiceFactory_OnAuthenticatedFallback_Success()
        {
            // Arrange - inject factory that returns a fake service which returns success
            Command.ReleaseServiceFactory = repo => new FakeReleaseService(HttpStatusCode.OK);

            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""private-app"",
                ""Version"": ""0.0.0"",
                ""AppFileName"": ""private.zip"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/this-repo-does-not-exist/releases/download/0.0.0/0.0.0.zip"",
                ""DownloadedFile"": ""0.0.0.zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Write-Output 'noop'"",
                ""InstallPath"": ""C:\\Temp\\nbuild2"",
                ""UninstallCommand"": ""powershell.exe"",
                ""UninstallArgs"": ""-Command Write-Output 'noop'""
                }
            ]
            }";

            // Ensure test mode and token are present so fallback is attempted
            System.Environment.SetEnvironmentVariable("LOCAL_TEST", "true", System.EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("API_GITHUB_KEY", "fake-token", System.EnvironmentVariableTarget.Process);

            // Act
            var result = Command.Download(json, true);

            // Assert - since fake returned OK, Command.Download should succeed
            Assert.IsTrue(result.IsSuccess());
        }

        [TestMethod]
        public void DownloadApp_UsesReleaseServiceFactory_OnAuthenticatedFallback_Failure()
        {
            // Arrange - inject factory that returns a fake service which returns NotFound
            Command.ReleaseServiceFactory = repo => new FakeReleaseService(HttpStatusCode.NotFound);

            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""private-app"",
                ""Version"": ""0.0.0"",
                ""AppFileName"": ""private.zip"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/this-repo-does-not-exist/releases/download/0.0.0/0.0.0.zip"",
                ""DownloadedFile"": ""0.0.0.zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Write-Output 'noop'"",
                ""InstallPath"": ""C:\\Temp\\nbuild2"",
                ""UninstallCommand"": ""powershell.exe"",
                ""UninstallArgs"": ""-Command Write-Output 'noop'""
                }
            ]
            }";

            // Ensure test mode and token are present so fallback is attempted
            System.Environment.SetEnvironmentVariable("LOCAL_TEST", "true", System.EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("API_GITHUB_KEY", "fake-token", System.EnvironmentVariableTarget.Process);

            // Act
            var result = Command.Download(json, true);

            // Assert - since fake returned NotFound, Command.Download should return failure (but not throw)
            Assert.IsFalse(result.IsSuccess());
        }

        // Simple fake implementation of IReleaseService
        private class FakeReleaseService : Nbuild.IReleaseService
        {
            private readonly HttpStatusCode _status;
            public FakeReleaseService(HttpStatusCode status)
            {
                _status = status;
            }

            public Task<HttpResponseMessage> DownloadAssetByName(string tagName, string assetName, string downloadPath)
            {
                var resp = new HttpResponseMessage(_status);
                return Task.FromResult(resp);
            }
        }
    }
}
