using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuildTasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NbuildTasksTests
{
    [TestClass()]
    public class DownloadFileTests
    {
        [TestMethod()]
        public async Task DownloadFileTaskAsyncTestAsync()
        {
            // Arrange
            var httpClient = new HttpClient();
            Uri uri = new("https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe");
            string fileName = "Docker.Desktop.Installer.exe";

            // setup file name to download to temp folder because devtools is protected
            fileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileName));
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            // Act
            var result = await httpClient.DownloadFileAsync(uri, fileName);

            // Assert
            Assert.IsTrue(File.Exists(fileName));
            Assert.IsTrue(result.IsSuccess());
        }

        [TestMethod()]
        public async Task DownloadFileTaskAsyncValidateParametersTestAsync()
        {
            // Arrange
            var expectedFail = new Dictionary<Uri, string>
            {
                {   new("https://desktop.docker.com/win/main/am@d64/Docker%20Desktop%20Installer.exe"),"Docker.Desktop.Installer.exe" },
                {   new("http://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe"), "Docker.Desktop.Installer.exe" },
                {   new("https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe"), "Docker.Desk>top.Installer.exe" },
            };

            var httpClient = new HttpClient();

            foreach (var item in expectedFail)
            {
                // setup file name to download to temp folder because devtools is protected
                var fileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(item.Value));
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                // Act
                var result = await httpClient.DownloadFileAsync(item.Key, fileName);

                Console.WriteLine($"output: {result.Output[0]}");

                // Assert
                Assert.IsFalse(result.IsSuccess());
            }
        }
    }
}