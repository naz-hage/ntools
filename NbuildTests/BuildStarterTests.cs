using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuildTasks;
using System.Reflection;

namespace Nbuild.Tests
{
    [TestClass()]
    public class BuildStarterTests
    {
        private const string NbuildAssemblyName = "Nb.dll"; // "Nbuild.dll"

        [TestMethod()]
        public void GetTargetsTest()
        {

            // Arrange
            List<string> expectedCommonTargets =
            [
                "PROPERTIES",
                "CLEAN",
                "INSTALL_DEP",
                "TELEMETRY_OPT_OUT",
                "DEV",
                "STAGE",
                "PROD",
                "GITHUB_RELEASE",
                "STAGE_DEPLOY",
                "PROD_DEPLOY",
                "SOLUTION",
                "SOLUTION_MSBUILD",
                "PACKAGE",
                "COPY_ARTIFACTS",
                "DEPLOY",
                "TEST",
                "TEST_DEBUG",
                "IS_ADMIN",
                "SingleProject",
                "HandleError",
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
                { "AUTOTAG_STAGE", true },
                { "SET_TAG", true },
                { "GIT_PULL", true },
                { "AUTOTAG_PROD", true },
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
                { "STAGE", true },
                { "PROD", true },
                { "STAGE_DEPLOY", true },
                { "PROD_DEPLOY", true },
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

        [TestMethod]
        public void TestSingleLineCommentAndTarget()
        {
            // Arrange
            string testFileName = "testFile.txt";
            File.WriteAllLines(testFileName, new string[]
            {
                    "<!-- Single-line comment -->",
                    "<Target Name=\"Target1\" />"
            });

            // Act
            var result = BuildStarter.GetTargetsAndComments(testFileName).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Target1             | Single-line comment", result[0]);

            // Cleanup
            File.Delete(testFileName);
        }

        [TestMethod]
        public void TestMultiLineCommentAndTarget()
        {
            // Arrange
            string testFileName = "testFile.txt";
            File.WriteAllLines(testFileName, new string[]
            {
            "<!-- Start of",
            "multi-line comment -->",
            "<Target Name=\"Target2\" />"
            });

            // Act
            var result = BuildStarter.GetTargetsAndComments(testFileName).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Target2             | Start of multi-line comment", result[0]);

            // Cleanup
            File.Delete(testFileName);
        }

        [TestMethod]
        public void TestMultipleTargets()
        {
            // Arrange
            string testFileName = "testFile.txt";
            File.WriteAllLines(testFileName, new string[]
            {
            "<!-- Comment for Target1 -->",
            "<Target Name=\"Target1\" />",
            "</Target>",
            "<!-- Comment for Target2 -->",
            "<Target Name=\"Target2\" />"
            });

            // Act
            var result = BuildStarter.GetTargetsAndComments(testFileName).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Target1             | Comment for Target1", result[0]);
            Assert.AreEqual("Target2             | Comment for Target2", result[1]);

            // Cleanup
            File.Delete(testFileName);
        }

        [TestMethod()]
        public void FindMsBuildPathTest()
        {
            // Arrange
            var buildStarter = new BuildStarter();

            // Act
            var msBuildPath = BuildStarter.FindMsBuild64BitPath(verbose:true);

            Console.WriteLine($"MSBuild Path: {msBuildPath}");

            // Assert not null and contains amd64
            Assert.IsNotNull(msBuildPath);
            Assert.IsTrue(msBuildPath.Contains("amd64"));
        }
    }
}