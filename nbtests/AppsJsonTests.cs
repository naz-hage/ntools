/// <summary>
/// Tests for apps.json file validation.
/// 
/// This test suite ensures that:
/// - apps.json exists in the build output (copied from go/apps.json by nb.csproj)
/// - apps.json can be successfully parsed as NbuildApps
/// - apps.json contains the correct version (1.2.0)
/// - apps.json contains at least one application entry in the NbuildAppList
/// 
/// The apps.json file is the single source of truth for all developer tools
/// managed by nbuild and is used by the nb install command to discover applications.
/// </summary>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using System.Reflection;
using System.Text.Json;

namespace NbuildTests
{
    [TestClass()]
    public class AppsJsonTests
    {
        private const string NbuildAssemblyName = "nb.dll"; // "nb.dll"
        private const string SupportedVersion = "1.2.0";

        [TestMethod()]
        public void ValidateAppsJsonTest()
        {
            // Arrange
            string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            // Use apps.json from output directory (copied by nb.csproj)
            string appsJsonFile = Path.Combine(executingAssemblyDirectory, "apps.json");

            // Assert that apps.json exists (it's copied to output by the build)
            Assert.IsTrue(File.Exists(appsJsonFile), $"apps.json not found at: {appsJsonFile}");

            // Act & Assert
            Console.WriteLine($"Validating: {appsJsonFile}");
            ValidateAppsJsonFile(appsJsonFile);
        }

        private void ValidateAppsJsonFile(string appsJsonPath)
        {
            var json = File.ReadAllText(appsJsonPath);

            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            var listAppData = JsonSerializer.Deserialize<NbuildApps>(json) ?? throw new ArgumentException("Failed to parse apps.json to NbuildApps");

            // Verify version matches expected format
            if (listAppData.Version != SupportedVersion)
            {
                throw new ArgumentException($"Version {listAppData.Version} is not supported. Expected version {SupportedVersion}");
            }

            // Verify we have apps in the list
            if (listAppData.NbuildAppList == null || listAppData.NbuildAppList.Count == 0)
            {
                throw new ArgumentException("No apps found in apps.json NbuildAppList");
            }

            Console.WriteLine($"Successfully validated apps.json with {listAppData.NbuildAppList.Count} apps");
        }
    }
}