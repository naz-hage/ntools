using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Build.Framework;
using System;
using System.IO;
using NbuildTasks;
using Moq;

namespace NbuildTasksTests
{
    [TestClass]
    public class SetupPreCommitHooksTests
    {
        private string _testDirectory;
        private string _gitDirectory;
        private string _hooksSourceDirectory;
        private string _hooksTargetDirectory;
        private SetupPreCommitHooks _task;
        private Mock<IBuildEngine> _mockBuildEngine;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "SetupPreCommitHooksTests", Guid.NewGuid().ToString());
            _gitDirectory = Path.Combine(_testDirectory, ".git");
            _hooksSourceDirectory = Path.Combine(_testDirectory, "dev-setup", "hooks");
            _hooksTargetDirectory = Path.Combine(_gitDirectory, "hooks");

            Directory.CreateDirectory(_gitDirectory);
            Directory.CreateDirectory(_hooksSourceDirectory);

            // Setup task with mock build engine
            _mockBuildEngine = new Mock<IBuildEngine>();
            _task = new SetupPreCommitHooks
            {
                BuildEngine = _mockBuildEngine.Object,
                GitDirectory = _gitDirectory,
                HooksSourceDirectory = _hooksSourceDirectory
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
        public void Execute_WithValidDirectories_CopiesHooksSuccessfully()
        {
            // Arrange
            CreateTestHookFile("pre-commit", "#!/bin/bash\necho 'Pre-commit hook'");
            CreateTestHookFile("pre-push", "#!/bin/bash\necho 'Pre-push hook'");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsTrue(Directory.Exists(_hooksTargetDirectory), "Hooks directory should be created");
            Assert.IsTrue(File.Exists(Path.Combine(_hooksTargetDirectory, "pre-commit")), "pre-commit hook should be copied");
            Assert.IsTrue(File.Exists(Path.Combine(_hooksTargetDirectory, "pre-push")), "pre-push hook should be copied");
        }

        [TestMethod]
        public void Execute_WithExistingHooksDirectory_OverwritesExistingFiles()
        {
            // Arrange
            Directory.CreateDirectory(_hooksTargetDirectory);
            File.WriteAllText(Path.Combine(_hooksTargetDirectory, "pre-commit"), "old content");

            CreateTestHookFile("pre-commit", "#!/bin/bash\necho 'New pre-commit hook'");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            var content = File.ReadAllText(Path.Combine(_hooksTargetDirectory, "pre-commit"));
            Assert.IsTrue(content.Contains("New pre-commit hook"), "Should overwrite existing hook with new content");
        }

        [TestMethod]
        public void Execute_WithEmptySourceDirectory_ReturnsTrue()
        {
            // Arrange
            // Source directory exists but is empty

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should succeed even with empty source directory");
            Assert.IsTrue(Directory.Exists(_hooksTargetDirectory), "Hooks directory should still be created");
        }

        [TestMethod]
        public void Execute_WithNonExistentSourceDirectory_ReturnsFalse()
        {
            // Arrange
            _task.HooksSourceDirectory = Path.Combine(_testDirectory, "nonexistent");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsFalse(result, "Task should fail when source directory doesn't exist");
        }

        [TestMethod]
        public void Execute_WithInvalidGitDirectory_ReturnsFalse()
        {
            // Arrange
            _task.GitDirectory = Path.Combine(_testDirectory, "invalid\0path");
            CreateTestHookFile("pre-commit", "#!/bin/bash\necho 'Pre-commit hook'");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsFalse(result, "Task should fail with invalid git directory path");
        }

        [TestMethod]
        public void Execute_WithMultipleHookFiles_CopiesAllFiles()
        {
            // Arrange
            var hookFiles = new[]
            {
                "pre-commit",
                "pre-push",
                "post-commit",
                "prepare-commit-msg"
            };

            foreach (var hookFile in hookFiles)
            {
                CreateTestHookFile(hookFile, $"#!/bin/bash\necho '{hookFile} hook'");
            }

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");

            foreach (var hookFile in hookFiles)
            {
                var targetPath = Path.Combine(_hooksTargetDirectory, hookFile);
                Assert.IsTrue(File.Exists(targetPath), $"{hookFile} should be copied to hooks directory");

                var content = File.ReadAllText(targetPath);
                Assert.IsTrue(content.Contains($"{hookFile} hook"), $"{hookFile} should have correct content");
            }
        }

        [TestMethod]
        public void Execute_CreatesHooksDirectoryIfNotExists()
        {
            // Arrange
            CreateTestHookFile("pre-commit", "#!/bin/bash\necho 'Pre-commit hook'");

            // Ensure hooks directory doesn't exist
            if (Directory.Exists(_hooksTargetDirectory))
            {
                Directory.Delete(_hooksTargetDirectory, true);
            }

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsTrue(Directory.Exists(_hooksTargetDirectory), "Should create hooks directory");
            Assert.IsTrue(File.Exists(Path.Combine(_hooksTargetDirectory, "pre-commit")), "Should copy hook file");
        }

        [TestMethod]
        public void Execute_WithReadOnlySourceFile_StillCopiesFile()
        {
            // Arrange
            var sourceFile = CreateTestHookFile("pre-commit", "#!/bin/bash\necho 'Pre-commit hook'");

            // Make source file read-only
            File.SetAttributes(sourceFile, FileAttributes.ReadOnly);

            try
            {
                // Act
                bool result = _task.Execute();

                // Assert
                Assert.IsTrue(result, "Task should execute successfully even with read-only source");
                Assert.IsTrue(File.Exists(Path.Combine(_hooksTargetDirectory, "pre-commit")), "Should copy read-only file");
            }
            finally
            {
                // Clean up read-only attribute
                if (File.Exists(sourceFile))
                {
                    File.SetAttributes(sourceFile, FileAttributes.Normal);
                }

                // Clean up copied file
                var copiedFile = Path.Combine(_hooksTargetDirectory, "pre-commit");
                if (File.Exists(copiedFile))
                {
                    File.SetAttributes(copiedFile, FileAttributes.Normal);
                }
            }
        }

        private string CreateTestHookFile(string fileName, string content)
        {
            var filePath = Path.Combine(_hooksSourceDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
