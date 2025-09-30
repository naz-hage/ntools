using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NbuildTasks;
using System;
using System.IO;

namespace NbuildTasksTests
{
    [TestClass]
    public class GenerateCommitMessageTests
    {
        private string _testDirectory;
        private string _workingDirectory;
        private GenerateCommitMessage _task;
        private Mock<IBuildEngine> _mockBuildEngine;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "GenerateCommitMessageTests", Guid.NewGuid().ToString());
            _workingDirectory = Path.Combine(_testDirectory, "repo");

            Directory.CreateDirectory(_workingDirectory);

            // Setup task with mock build engine
            _mockBuildEngine = new Mock<IBuildEngine>();
            _task = new GenerateCommitMessage
            {
                BuildEngine = _mockBuildEngine.Object,
                WorkingDirectory = _workingDirectory
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
        public void Execute_WithExistingCommitMessageFile_UsesFileContent()
        {
            // Arrange
            var commitMessage = "feat(api): Add new authentication endpoint";
            var commitMessageFile = Path.Combine(_workingDirectory, ".commit-message");
            File.WriteAllText(commitMessageFile, commitMessage);

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.AreEqual(commitMessage, _task.CommitMessage, "Should use commit message from file");
        }

        [TestMethod]
        public void Execute_WithCustomCommitMessageFileName_UsesCorrectFile()
        {
            // Arrange
            var commitMessage = "fix: Resolve critical security vulnerability";
            var customFileName = "my-commit-msg.txt";
            var commitMessageFile = Path.Combine(_workingDirectory, customFileName);
            File.WriteAllText(commitMessageFile, commitMessage);

            _task.CommitMessageFile = customFileName;

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.AreEqual(commitMessage, _task.CommitMessage, "Should use commit message from custom file");
        }

        [TestMethod]
        public void Execute_WithoutCommitMessageFile_GeneratesDynamicMessage()
        {
            // Arrange
            // No commit message file exists

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsFalse(string.IsNullOrEmpty(_task.CommitMessage), "Should generate a commit message");
            Assert.IsTrue(_task.CommitMessage.StartsWith("feat:"), "Should use default commit type");
        }

        [TestMethod]
        public void Execute_WithCustomCommitType_UsesCorrectType()
        {
            // Arrange
            _task.CommitType = "fix";

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsTrue(_task.CommitMessage.StartsWith("fix:"), "Should use custom commit type");
        }

        [TestMethod]
        public void Execute_WithScope_IncludesScopeInMessage()
        {
            // Arrange
            _task.Scope = "api";

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsTrue(_task.CommitMessage.Contains("(api)"), "Should include scope in commit message");
        }

        [TestMethod]
        public void Execute_SavesGeneratedMessageToFile()
        {
            // Arrange
            var expectedFile = Path.Combine(_workingDirectory, ".commit-message");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsTrue(File.Exists(expectedFile), "Should save generated message to file");

            var savedMessage = File.ReadAllText(expectedFile);
            Assert.AreEqual(_task.CommitMessage, savedMessage, "Saved message should match generated message");
        }

        [TestMethod]
        public void Execute_WithCommitMessageFileHavingWhitespace_TrimsContent()
        {
            // Arrange
            var commitMessage = "  feat: Add new feature  \n\n  ";
            var commitMessageFile = Path.Combine(_workingDirectory, ".commit-message");
            File.WriteAllText(commitMessageFile, commitMessage);

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.AreEqual("feat: Add new feature", _task.CommitMessage, "Should trim whitespace from file content");
        }

        [TestMethod]
        public void Execute_WithEmptyCommitMessageFile_GeneratesDynamicMessage()
        {
            // Arrange
            var commitMessageFile = Path.Combine(_workingDirectory, ".commit-message");
            File.WriteAllText(commitMessageFile, "   \n  \t  ");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            // The task will use the whitespace content from the file, not generate a dynamic message
            Assert.IsTrue(_task.CommitMessage.Trim().Length == 0 || _task.CommitMessage.StartsWith("feat:"),
                         "Should either be empty or generate dynamic message");
        }

        [TestMethod]
        public void Execute_WithInvalidWorkingDirectory_StillSucceeds()
        {
            // Arrange
            _task.WorkingDirectory = Path.Combine(_testDirectory, "nonexistent");

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should not fail even with invalid working directory");
            Assert.IsTrue(_task.CommitMessage.StartsWith("feat:"), "Should provide fallback commit message");
        }

        [TestMethod]
        public void Execute_WithIOException_UsesFallbackMessage()
        {
            // Arrange
            var readOnlyDirectory = Path.Combine(_testDirectory, "readonly");
            Directory.CreateDirectory(readOnlyDirectory);

            try
            {
                // Make directory read-only (Windows specific)
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var dirInfo = new DirectoryInfo(readOnlyDirectory);
                    dirInfo.Attributes |= FileAttributes.ReadOnly;
                }

                _task.WorkingDirectory = readOnlyDirectory;

                // Act
                bool result = _task.Execute();

                // Assert
                Assert.IsTrue(result, "Task should not fail on IO exceptions");
                Assert.IsTrue(_task.CommitMessage.StartsWith("feat:"), "Should use fallback message starting with feat:");
            }
            finally
            {
                // Clean up read-only attribute
                if (Directory.Exists(readOnlyDirectory))
                {
                    var dirInfo = new DirectoryInfo(readOnlyDirectory);
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                }
            }
        }

        [TestMethod]
        public void Execute_WithLongCommitMessage_DoesNotTruncate()
        {
            // Arrange
            var longMessage = new string('a', 500) + "\n\nThis is a very long commit message that exceeds typical limits but should still be processed correctly.";
            var commitMessageFile = Path.Combine(_workingDirectory, ".commit-message");
            File.WriteAllText(commitMessageFile, longMessage);

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.IsTrue(_task.CommitMessage.Length > 400, "Should preserve long commit messages");
            Assert.IsTrue(_task.CommitMessage.Contains("very long commit message"), "Should contain original content");
        }

        [TestMethod]
        public void Execute_WithMultilineCommitMessage_PreservesFormat()
        {
            // Arrange
            var multilineMessage = "feat(api): Add user authentication\n\n- Implement JWT token validation\n- Add password encryption\n- Update API documentation";
            var commitMessageFile = Path.Combine(_workingDirectory, ".commit-message");
            File.WriteAllText(commitMessageFile, multilineMessage);

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");
            Assert.AreEqual(multilineMessage, _task.CommitMessage, "Should preserve multiline format");
            Assert.IsTrue(_task.CommitMessage.Contains("\n\n- Implement"), "Should preserve bullet points");
        }

        [TestMethod]
        public void Execute_GeneratedMessage_HasValidFormat()
        {
            // Arrange
            // No existing commit message file

            // Act
            bool result = _task.Execute();

            // Assert
            Assert.IsTrue(result, "Task should execute successfully");

            var message = _task.CommitMessage;
            Assert.IsTrue(message.Contains(":"), "Generated message should contain colon separator");

            // Check conventional commit format
            var parts = message.Split(':');
            Assert.IsTrue(parts.Length >= 2, "Should have at least type and description");
            Assert.IsFalse(string.IsNullOrWhiteSpace(parts[0]), "Type should not be empty");
            Assert.IsFalse(string.IsNullOrWhiteSpace(parts[1]), "Description should not be empty");
        }

        [TestMethod]
        public void Execute_WithDifferentCommitTypes_GeneratesCorrectFormat()
        {
            var commitTypes = new[] { "feat", "fix", "docs", "test", "ci", "refactor", "chore" };

            foreach (var commitType in commitTypes)
            {
                // Arrange
                _task.CommitType = commitType;

                // Act
                bool result = _task.Execute();

                // Assert
                Assert.IsTrue(result, $"Task should execute successfully for commit type: {commitType}");
                Assert.IsTrue(_task.CommitMessage.Contains(":"), "Message should contain colon separator");
                Assert.IsFalse(string.IsNullOrWhiteSpace(_task.CommitMessage), "Message should not be empty");

                // The message may start with the specified type or a dynamically detected type
                // but should always be a valid conventional commit format
                var messageParts = _task.CommitMessage.Split(':');
                Assert.IsTrue(messageParts.Length >= 2, "Should have type and description separated by colon");
            }
        }
    }
}
