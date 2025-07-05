using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using NbuildTasks;
using Moq;

namespace NbuildTasksTests
{
    [TestClass]
    public class UpdateVersionsInDocsTests
    {
        private string _testDirectory;
        private string _devSetupPath;
        private string _docsPath;
        private UpdateVersionsInDocs _task;
        private Mock<IBuildEngine> _mockBuildEngine;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "UpdateVersionsInDocsTests", Guid.NewGuid().ToString());
            _devSetupPath = Path.Combine(_testDirectory, "dev-setup");
            _docsPath = Path.Combine(_testDirectory, "docs", "ntools.md");

            Directory.CreateDirectory(_devSetupPath);
            Directory.CreateDirectory(Path.GetDirectoryName(_docsPath));

            // Setup task with mock build engine
            _mockBuildEngine = new Mock<IBuildEngine>();
            _task = new UpdateVersionsInDocs
            {
                BuildEngine = _mockBuildEngine.Object,
                DevSetupPath = _devSetupPath,
                DocsPath = _docsPath
            };
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
        public void Execute_WithValidJsonAndMarkdown_UpdatesVersionsSuccessfully()
        {
            // Arrange
            CreateTestJsonFile("node.json", "Node.js", "22.12.0");
            CreateTestJsonFile("powershell.json", "Powershell", "7.5.2");
            CreateTestMarkdownFile();

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");

            var updatedContent = File.ReadAllText(_docsPath);
            Assert.IsTrue(updatedContent.Contains("22.12.0"), "Should contain updated Node.js version");
            Assert.IsTrue(updatedContent.Contains("7.5.2"), "Should contain updated PowerShell version");

            // Verify date was updated (should be today's date)
            var today = DateTime.Now.ToString("dd-MMM-yy");
            Assert.IsTrue(updatedContent.Contains(today), "Should contain today's date");
        }

        [TestMethod]
        public void Execute_WithInvalidJsonFile_ContinuesProcessingOtherFiles()
        {
            // Arrange
            CreateInvalidJsonFile("invalid.json");
            CreateTestJsonFile("node.json", "Node.js", "22.12.0");
            CreateTestMarkdownFile();

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully despite invalid JSON");

            var updatedContent = File.ReadAllText(_docsPath);
            Assert.IsTrue(updatedContent.Contains("22.12.0"), "Should still update valid entries");
        }

        [TestMethod]
        public void Execute_WithMissingMarkdownFile_ReturnsFalse()
        {
            // Arrange
            CreateTestJsonFile("node.json", "Node.js", "22.12.0");
            // Don't create markdown file

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsFalse(result, "Task should fail when markdown file is missing");
        }

        [TestMethod]
        public void Execute_WithEmptyDevSetupPath_ReturnsTrue()
        {
            // Arrange
            CreateTestMarkdownFile();
            // Don't create any JSON files

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should succeed even with no JSON files");
        }

        [TestMethod]
        public void Execute_WithJsonMissingRequiredProperties_SkipsFile()
        {
            // Arrange
            CreateJsonFileWithoutVersion("incomplete.json", "SomeTool");
            CreateTestJsonFile("node.json", "Node.js", "22.12.0");
            CreateTestMarkdownFile();

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should succeed");

            var updatedContent = File.ReadAllText(_docsPath);
            Assert.IsTrue(updatedContent.Contains("22.12.0"), "Should update complete entries");
        }

        [TestMethod]
        public void Execute_WithUnmatchedTool_DoesNotUpdateEntry()
        {
            // Arrange
            CreateTestJsonFile("unknown.json", "UnknownTool", "1.0.0");
            CreateTestMarkdownFile();

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should succeed");

            var updatedContent = File.ReadAllText(_docsPath);
            Assert.IsFalse(updatedContent.Contains("1.0.0"), "Should not update unmatched tools");
        }

        [TestMethod]
        public void Execute_UpdatesMultipleToolsCorrectly()
        {
            // Arrange
            CreateTestJsonFile("node.json", "Node.js", "22.12.0");
            CreateTestJsonFile("powershell.json", "Powershell", "7.5.2");
            CreateTestJsonFile("python.json", "Python", "3.13.3");
            CreateTestMarkdownFile();

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");

            var updatedContent = File.ReadAllText(_docsPath);
            Assert.IsTrue(updatedContent.Contains("22.12.0"), "Should contain Node.js version");
            Assert.IsTrue(updatedContent.Contains("7.5.2"), "Should contain PowerShell version");
            Assert.IsTrue(updatedContent.Contains("3.13.3"), "Should contain Python version");
        }

        [TestMethod]
        public void Execute_PreservesMarkdownFormatting()
        {
            // Arrange
            CreateTestJsonFile("node.json", "Node.js", "22.12.0");
            CreateTestMarkdownFile();
            var originalContent = File.ReadAllText(_docsPath);

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");

            var updatedContent = File.ReadAllText(_docsPath);

            // Verify table structure is preserved
            Assert.IsTrue(updatedContent.Contains("| Tool"), "Should preserve table header");
            Assert.IsTrue(updatedContent.Contains("| :---"), "Should preserve table alignment");
            Assert.IsTrue(updatedContent.Contains("[Node.js]"), "Should preserve tool links");
        }

        private void CreateTestJsonFile(string fileName, string toolName, string version)
        {
            var jsonData = new
            {
                NbuildAppList = new[]
                {
                    new
                    {
                        Name = toolName,
                        Version = version,
                        Description = $"{toolName} tool"
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(_devSetupPath, fileName), jsonString);
        }

        private void CreateInvalidJsonFile(string fileName)
        {
            File.WriteAllText(Path.Combine(_devSetupPath, fileName), "{ invalid json content");
        }

        private void CreateJsonFileWithoutVersion(string fileName, string toolName)
        {
            var jsonData = new
            {
                NbuildAppList = new[]
                {
                    new
                    {
                        Name = toolName,
                        Description = $"{toolName} tool"
                        // Missing Version property
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(_devSetupPath, fileName), jsonString);
        }

        private void CreateTestMarkdownFile()
        {
            var markdownContent = @"# Test Documentation

| Tool                                                                                                       | Version     | Last Checked on |
| :--------------------------------------------------------------------------------------------------------- | :---------- | :-------------- |
| [Node.js](https://nodejs.org/en/download/)                                                                | 20.0.0      | 01-Jan-24       |
| [PowerShell](https://github.com/PowerShell/PowerShell/releases)                                           | 7.4.0       | 01-Jan-24       |
| [Python](https://www.python.org/downloads/)                                                               | 3.12.0      | 01-Jan-24       |
| [Git for Windows](https://git-scm.com/downloads)                                                          | 2.40.0      | 01-Jan-24       |
";

            File.WriteAllText(_docsPath, markdownContent);
        }
    }
}
