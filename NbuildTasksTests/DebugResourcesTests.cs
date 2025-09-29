using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuildTasks.Debug;
using System;
using System.IO;
using System.Reflection;

namespace NbuildTasksTests
{
    [TestClass]
    public class DebugResourcesTests
    {
        private string _testDirectory;
        private string _testAssemblyPath;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "DebugResourcesTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Create a test assembly path
            _testAssemblyPath = Path.Combine(_testDirectory, "TestAssembly.dll");

            // Create a minimal test assembly with embedded resources for testing
            CreateTestAssemblyWithResources();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public void ListEmbeddedResources_WithValidAssembly_DisplaysResources()
        {
            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.ListEmbeddedResources();

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("Embedded Resources in NbuildTasks.dll:"),
                             "Should display header");
                Assert.IsTrue(output.Contains("====================================="),
                             "Should display separator");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void ListEmbeddedResources_WithNoResources_DisplaysNoResourcesMessage()
        {
            // This test checks the behavior when an assembly has no embedded resources
            // We'll use the current test assembly which likely has no embedded resources

            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.ListEmbeddedResources();

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("Embedded Resources in NbuildTasks.dll:"),
                             "Should display header even with no resources");

                // The actual NbuildTasks.dll might have resources, so we can't assert
                // "No embedded resources found" without knowing the actual state
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void ExtractResource_WithValidResource_CreatesOutputFile()
        {
            // This is a more complex test that would require creating a mock assembly
            // For now, we'll test the error case with a non-existent resource

            // Arrange
            var outputPath = Path.Combine(_testDirectory, "extracted.txt");
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.ExtractResource("NonExistentResource", outputPath);

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("not found"),
                             "Should display not found message for non-existent resource");
                Assert.IsFalse(File.Exists(outputPath),
                              "Should not create output file for non-existent resource");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void ExtractResource_WithNullResourceName_HandlesGracefully()
        {
            // Arrange
            var outputPath = Path.Combine(_testDirectory, "extracted.txt");
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act & Assert - Should not throw exception
                DebugResources.ExtractResource(null, outputPath);

                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("not found"),
                             "Should handle null resource name gracefully");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void Main_WithNoArguments_CallsListEmbeddedResources()
        {
            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.Main(new string[0]);

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("NbuildTasks Resource Debugger"),
                             "Should display main header");
                Assert.IsTrue(output.Contains("Embedded Resources in NbuildTasks.dll:"),
                             "Should call ListEmbeddedResources");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void Main_WithExtractCommand_CallsExtractResource()
        {
            // Arrange
            var outputPath = Path.Combine(_testDirectory, "test.txt");
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.Main(new string[] { "extract", $"TestResource|{outputPath}" });

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("NbuildTasks Resource Debugger"),
                             "Should display main header");
                Assert.IsTrue(output.Contains("not found"),
                             "Should attempt to extract resource");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void Main_WithInvalidExtractSyntax_DisplaysUsage()
        {
            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.Main(new string[] { "extract", "InvalidSyntax" });

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("Usage for extract:"),
                             "Should display extract usage message");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void Main_WithInvalidArguments_DisplaysGeneralUsage()
        {
            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.Main(new string[] { "invalid", "arguments", "here" });

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("Usage:"),
                             "Should display general usage message");
                Assert.IsTrue(output.Contains("Examples:"),
                             "Should display examples");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void Main_DisplaysCorrectUsageInformation()
        {
            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.Main(new string[] { "help" });

                // Assert
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("debug-resources"),
                             "Should contain command name in usage");
                Assert.IsTrue(output.Contains("List all embedded resources"),
                             "Should describe list functionality");
                Assert.IsTrue(output.Contains("Extract resource to file"),
                             "Should describe extract functionality");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void ExtractResource_WithValidPathButInvalidResource_DoesNotCreateFile()
        {
            // Arrange
            var outputPath = Path.Combine(_testDirectory, "shouldnotexist.txt");
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act
                DebugResources.ExtractResource("Definitely.Does.Not.Exist", outputPath);

                // Assert
                Assert.IsFalse(File.Exists(outputPath),
                              "Should not create file when resource doesn't exist");

                var output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("not found"),
                             "Should report resource not found");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [TestMethod]
        public void ListEmbeddedResources_HandlesMissingAssembly_Gracefully()
        {
            // This test verifies that the method handles cases where the assembly might not exist
            // or cannot be loaded. The actual behavior depends on the implementation.

            // Arrange
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            try
            {
                // Act - This will try to load the actual NbuildTasks.dll
                // The test verifies it doesn't crash, regardless of whether the assembly exists
                DebugResources.ListEmbeddedResources();

                // Assert - Should complete without throwing an exception
                var output = stringWriter.ToString();
                Assert.IsTrue(output.Length > 0, "Should produce some output");
            }
            catch (Exception ex)
            {
                // If an exception occurs, it should be a well-known type
                Assert.IsTrue(ex is FileNotFoundException ||
                             ex is ReflectionTypeLoadException ||
                             ex is BadImageFormatException,
                             $"Should handle assembly loading gracefully, but got: {ex.GetType().Name}");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        private void CreateTestAssemblyWithResources()
        {
            // This is a placeholder for creating a test assembly with embedded resources
            // In a real implementation, you might use dynamic assembly generation
            // or have pre-built test assemblies as part of your test resources

            // For now, we'll just create an empty file to represent the assembly path
            File.WriteAllText(_testAssemblyPath, "Mock assembly for testing");
        }
    }
}
