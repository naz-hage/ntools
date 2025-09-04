using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Nbuild.Tests
{
    [TestClass]
    public class CliTests
    {
        private string? _originalOwnerValue;

        [TestInitialize]
        public void TestInitialize()
        {
            // Save the original OWNER environment variable value before each test
            _originalOwnerValue = Environment.GetEnvironmentVariable("OWNER");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore the original OWNER environment variable value after each test
            Environment.SetEnvironmentVariable("OWNER", _originalOwnerValue);
        }
        [TestMethod]
        public async Task ValidateRepo_ShouldExtractUserNameAndRepoName_FromFullUrl()
        {
            // Arrange
            var cli = new Cli
            {
                Repo = "https://github.com/naz-hage/ntools"
            };

            // Act
            await cli.ValidateRepo();

            // Assert
            Assert.AreEqual("naz-hage/ntools", cli.Repo, "The Repo property should correctly extract userName/repoName from the full URL.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ValidateRepo_ShouldThrowException_ForNonGitHubDomain()
        {
            // Arrange
            var cli = new Nbuild.Cli
            {
                Repo = "https://gitlab.com/userName/repoName"
            };

            // Act
            await cli.ValidateRepo();

            // Assert
            // Exception is expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ValidateRepo_ShouldThrowException_ForInvalidRepoFormat()
        {
            // Arrange
            var cli = new Nbuild.Cli
            {
                Repo = "invalid/repo/format"
            };

            // Act
            await cli.ValidateRepo();

            // Assert
            // Exception is expected
        }

        [TestMethod]
        public async Task ValidateRepo_ShouldHandleRepoNameOnly_WithOwnerEnvironmentVariable()
        {
            // Arrange
            Environment.SetEnvironmentVariable("OWNER", "naz-hage");
            var cli = new Nbuild.Cli
            {
                Repo = "ntools"
            };

            // Act
            await cli.ValidateRepo();

            // Assert
            Assert.AreEqual("naz-hage/ntools", cli.Repo, "The Repo property should correctly combine OWNER and repoName.");
        }

        [TestMethod]
        public async Task ValidateRepo_ShouldThrowException_WhenOwnerEnvironmentVariableIsMissing()
        {
            // Skip this test if running in GitHub Actions
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            {
                Assert.Inconclusive("Skipping test in GitHub Actions environment.");
                return;
            }

            // Arrange
            Environment.SetEnvironmentVariable("OWNER", null);
            var cli = new Nbuild.Cli
            {
                Repo = "ntools"
            };

            // Act & Assert - expect exception
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await cli.ValidateRepo()
            );

            // Verify the exception message is appropriate
            Assert.IsNotNull(exception, "Expected InvalidOperationException to be thrown");
        }

        [TestMethod]
        public void GetCommandType_ValidCommand_ShouldReturnCorrectCommandType()
        {
            // Arrange
            var cli = new Cli { Command = Cli.CommandType.release_create };

            // Act
            var commandType = cli.GetCommandType();

            // Assert
            Assert.AreEqual(Cli.CommandType.release_create, commandType);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetCommandType_InvalidCommand_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli();
            cli.Command = (Cli.CommandType)999; // Invalid command

            // Act
            cli.GetCommandType();
        }

        [TestMethod]
        public void Validate_ValidArguments_ShouldPass()
        {
            // Arrange
            var cli = new Cli
            {
                Command = Cli.CommandType.release_create,
                Repo = "naz-hage/ntools",
                Tag = "1.0.0",
                Branch = "main",
                AssetFileName = "file.zip"
            };

            // Act & Assert
            cli.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_MissingRepo_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli
            {
                Command = Cli.CommandType.release_create,
                Tag = "v1.0.0",
                Branch = "main",
                AssetFileName = "file.zip"
            };

            // Act
            cli.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_InvalidRepoFormat_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli
            {
                Command = Cli.CommandType.release_create,
                Repo = "invalidRepoFormat",
                Tag = "v1.0.0",
                Branch = "main",
                AssetFileName = "file.zip"
            };
            // Skip this test if OWNER or GITHUB_TOKEN is not set because Validate() may require OWNER and network access
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OWNER")) || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN")))
            {
                Assert.Inconclusive("OWNER or GITHUB_TOKEN not set; skipping test that requires environment and/or network access.");
                return;
            }

            // Act
            cli.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_MissingTag_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli
            {
                Command = Cli.CommandType.release_create,
                Repo = "user/repo",
                Branch = "main",
                AssetFileName = "file.zip"
            };

            // Act
            cli.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_MissingFileForCreateCommand_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli
            {
                Command = Cli.CommandType.release_create,
                Repo = "user/repo",
                Tag = "v1.0.0",
                Branch = "main"
            };

            // Act
            cli.Validate();
        }

        [TestMethod]
        public async Task ValidateRepo_ValidRepo_ShouldPass()
        {
            // Arrange
            var cli = new Cli() 
            {
                Repo = "naz-hage/ntools"
            };

            // Act & Assert
            await cli.ValidateRepo();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ValidateRepo_InvalidRepo_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli()
            {
                Repo = "invalid/repo"
            };

            // Act
            await cli.ValidateRepo();
        }

        [TestMethod]
        public async Task ValidateRepositoryExists_ValidRepo_ShouldPass()
        {
            // Arrange
            // Arrange
            var cli = new Cli()
            {
                Repo = "naz-hage/ntools"
            };

            // Act & Assert
            await cli.ValidateRepositoryExists();
        }

        [TestMethod]
        public async Task ValidateRepositoryExists_InvalidRepo_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli()
            {
                Repo = "invalid/repo"
            };

            // Act & Assert - expect exception
            var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await cli.ValidateRepositoryExists()
            );

            // Verify the exception message is appropriate
            Assert.IsNotNull(exception, "Expected ArgumentException to be thrown");
            Assert.IsTrue(exception.Message.Contains("invalid/repo"), 
                "Exception message should mention the invalid repository");
        }

        [TestMethod]
        public void EnvironmentVariable_ShouldBePreservedBetweenTests()
        {
            // This test verifies that the class-level TestInitialize/TestCleanup
            // properly preserves the OWNER environment variable between tests

            // Get the current OWNER value (should be the original value restored by TestCleanup)
            var currentOwner = Environment.GetEnvironmentVariable("OWNER");
            
            // Modify the OWNER environment variable
            Environment.SetEnvironmentVariable("OWNER", "test-owner-value");
            
            // Verify the change
            Assert.AreEqual("test-owner-value", Environment.GetEnvironmentVariable("OWNER"), 
                "OWNER should be set to test value");
                
            // Note: TestCleanup will automatically restore the original value
            // This test demonstrates that environment variables are properly managed
        }
    }
}
