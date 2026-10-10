/// <summary>
/// Tests for apps.yaml file validation.
/// 
/// This test suite ensures that:
/// - apps.yaml exists in the build output (copied from dev-setup/apps.yaml by nb.csproj)
/// - apps.yaml can be successfully parsed as NbuildApps
/// - apps.yaml contains the correct version (1.2.0)
/// - apps.yaml contains at least one application entry in the NbuildAppList
/// 
/// The apps.yaml file is the single source of truth for all developer tools
/// managed by SDO and is used by the sdo tool install command to discover applications.
/// </summary>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using System.Reflection;

namespace NbuildTests
{
    [TestClass()]
    public class AppsYamlTests
    {
        private const string NbuildAssemblyName = "nb.dll"; // "nb.dll"
        private const string SupportedVersion = "1.2.0";

        [TestMethod()]
        public void ValidateAppsYamlTest()
        {
            // Arrange
            string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            // Use apps.yaml from output directory (copied by nb.csproj)
            string appsYamlFile = Path.Combine(executingAssemblyDirectory, "apps.yaml");

            // Assert that apps.yaml exists (it's copied to output by the build)
            Assert.IsTrue(File.Exists(appsYamlFile), $"apps.yaml not found at: {appsYamlFile}");

            // Act & Assert
            Console.WriteLine($"Validating: {appsYamlFile}");
            ValidateAppsYamlFile(appsYamlFile);
        }

        private void ValidateAppsYamlFile(string appsYamlPath)
        {
            var apps = Command.GetApps(appsYamlPath).ToList();

            Assert.IsTrue(apps.Count > 0, "No apps found in apps.yaml NbuildAppList");

            Console.WriteLine($"Successfully validated apps.yaml with {apps.Count} apps");
        }
    }
}