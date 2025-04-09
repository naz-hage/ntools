using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GitHubRelease.Tests
{
    [TestClass]
    public class CliTests
    {
        [TestMethod]
        public void GetCommandType_ValidCommand_ShouldReturnCorrectCommandType()
        {
            // Arrange
            var cli = new Cli { Command = Cli.CommandType.create };

            // Act
            var commandType = cli.GetCommandType();

            // Assert
            Assert.AreEqual(Cli.CommandType.create, commandType);
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
                Command = Cli.CommandType.create,
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
                Command = Cli.CommandType.create,
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
                Command = Cli.CommandType.create,
                Repo = "invalidRepoFormat",
                Tag = "v1.0.0",
                Branch = "main",
                AssetFileName = "file.zip"
            };

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
                Command = Cli.CommandType.create,
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
                Command = Cli.CommandType.create,
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
        [ExpectedException(typeof(ArgumentException))]
        public async Task ValidateRepositoryExists_InvalidRepo_ShouldThrowException()
        {
            // Arrange
            var cli = new Cli()
            {
                Repo = "invalid/repo"
            };

            // Act
            await cli.ValidateRepositoryExists();
        }
    }
}
