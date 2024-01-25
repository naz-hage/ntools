using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using NbuildTasks;
using System.Reflection;

namespace NbuildTests
{
    [TestClass()]
    public class BuildStarterTests
    {
        private const string NbuildAssemblyName = "Nb.dll"; // "Nbuild.dll"

        [TestMethod()]
        public void GetTargetsTest()
        {
            List<string> expectedNgitTargets =
            [
                "GIT_STATUS",
                "AUTOTAG_STAGING",
                "SET_TAG",
                "GIT_PULL",
                "AUTOTAG_PRODUCTION",
                "TAG",
                "PUSH_TAG",
                "GIT_BRANCH"
            ];

            // Arrange
            List<string> expectedCommonTargets =
            [
                "PROPERTIES",
                "CLEAN",
                "INSTALL_DEP",
                "TELEMETRY_OPT_OUT",
                "STAGING",
                "PRODUCTION",
                "STAGING_DEPLOY",
                "PRODUCTION_DEPLOY",
                "SOLUTION",
                "SOLUTION_MSBUILD",
                "PACKAGE",
                "COPY_ARTIFACTS",
                "DEPLOY",
                "TEST",
                "TEST_DEBUG",
                "IS_ADMIN",
                "SingleProject",
                "HandleError"
            ];

            // Act
            string targetFileName = ExtractCommonTargetsFile();

            // Assert
            Console.WriteLine($"ResourcePath: {targetFileName}");
            Assert.IsTrue(File.Exists(targetFileName));
            var actual = BuildStarter.GetTargets(targetFileName).ToArray();

            Console.WriteLine($"Actual Targets:");
            Console.WriteLine($"{string.Join(",\n", actual.Select(a => $"\"{a}\""))}");

            // assert that arrays actual and expected are equal
            CollectionAssert.AreEqual(expectedCommonTargets, actual, StringComparer.OrdinalIgnoreCase);
        }

        private static string ExtractCommonTargetsFile()
        {
            var executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            string resourcePath = Path.Combine(executingAssemblyDirectory, NbuildAssemblyName);
            string targetFileName = Path.Combine(executingAssemblyDirectory, "commom.targets");

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(resourcePath, "Nbuild.resources.common.targets", targetFileName);
            return targetFileName;
        }

        [TestMethod()]
        public void GetImportAttributesTest()
        {
            // Arrange
            var executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            string resourcePath = Path.Combine(executingAssemblyDirectory, NbuildAssemblyName);
            string targetFileName = Path.Combine(executingAssemblyDirectory, "nbuild.targets");

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(resourcePath, "Nbuild.resources.nbuild.targets", targetFileName);

            // Act
            var fileNames = BuildStarter.GetImportAttributes(targetFileName, "Project");

            // display file names
            Console.WriteLine($"FileNames:");
            Console.WriteLine($"{string.Join(",\n", fileNames.Select(a => $"\"{a}\""))}");

            // Assert
            Assert.IsTrue(fileNames.Count() > 0);
        }

        [TestMethod()]
        public void ExtractCommentsFromTargets()
        {
            // Arrange
            string targetFileName = ExtractCommonTargetsFile();

            // Act
            var result = BuildStarter.GetTargetsAndComments(targetFileName);

            // Assert
            Assert.AreNotEqual(0, result.Count());
        }

        [TestMethod()]
        public void ValidTargetTest()
        {
            Dictionary<string, bool> ngitTargets = new()
            {
                { "GIT_STATUS", true },
                { "AUTOTAG_STAGING", true },
                { "SET_TAG", true },
                { "GIT_PULL", true },
                { "AUTOTAG_PRODUCTION", true },
                { "TAG", true },
                { "PUSH_TAG", true },
                { "GIT_BRANCH", true },
            };
            // Arrange
            string targetFileName = ExtractCommonTargetsFile();
            Dictionary<string, bool> commonTargets = new()
            {
                { "PROPERTIES", true },
                { "CLEAN", true },
                { "INSTALL_DEP", true },
                { "TELEMETRY_OPT_OUT", true },
                { "STAGING", true },
                { "PRODUCTION", true },
                { "STAGING_DEPLOY", true },
                { "PRODUCTION_DEPLOY", true },
                { "SOLUTION", true },
                { "SOLUTION_MSBUILD", true },
                { "PACKAGE", true },
                { "COPY_ARTIFACTS", true },
                { "DEPLOY", true },
                { "TEST", true },
                { "TEST_DEBUG", true },
                { "IS_ADMIN", true },
                { "JUNK_TARGET", false },
                { "SingleProject", true },
                { "HandleError", true },
            };

            // Act

            foreach (var target in commonTargets)
            {
                // Act
                bool isValid = BuildStarter.ValidTarget(targetFileName, target.Key);

                // Assert
                Assert.AreEqual(target.Value, isValid);
            }
        }
    }
}