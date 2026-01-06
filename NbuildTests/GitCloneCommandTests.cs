using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild.Commands;
using Nbuild.Interfaces;
using System.CommandLine;
using Moq;
using System.IO;

namespace NbuildTests
{
    [TestClass]
    public class GitCloneCommandTests
    {
        private Mock<IGitCloneService>? _mockCloneService;
        private Option<bool>? _dryRunOption;
        private Option<bool>? _verboseOption;
        private RootCommand? _rootCommand;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockCloneService = new Mock<IGitCloneService>();
            _dryRunOption = new Option<bool>("--dry-run") { Description = "Perform a dry run" };
            _verboseOption = new Option<bool>("--verbose") { Description = "Verbose output" };
            _rootCommand = new RootCommand();
            
            // Add global options to root command so they can be inherited by subcommands
            _rootCommand!.Options.Add(_dryRunOption!);
            _rootCommand!.Options.Add(_verboseOption!);
        }

        [TestMethod]
        public void Register_AddsGitCloneCommandToRootCommand()
        {
            // Act
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);

            // Assert
            Assert.AreEqual(1, _rootCommand!.Subcommands.Count, "Should add one subcommand");
            var gitCloneCmd = _rootCommand!.Subcommands[0];
            Assert.AreEqual("git_clone", gitCloneCmd.Name, "Command name should be git_clone");
        }

        [TestMethod]
        public void GitCloneCommand_HasRequiredUrlOption()
        {
            // Act
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);

            // Assert
            var gitCloneCmd = _rootCommand!.Subcommands[0];
            var stringOptions = gitCloneCmd.Options.OfType<Option<string>>().ToList();
            var urlOption = stringOptions.FirstOrDefault(o => o.Name == "--url");
            Assert.IsNotNull(urlOption, "Should have url option");
        }

        [TestMethod]
        public void GitCloneCommand_HasOptionalPathOption()
        {
            // Act
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);

            // Assert
            var gitCloneCmd = _rootCommand!.Subcommands[0];
            var stringOptions = gitCloneCmd.Options.OfType<Option<string>>().ToList();
            var pathOption = stringOptions.FirstOrDefault(o => o.Name == "--path");
            Assert.IsNotNull(pathOption, "Should have path option");
        }

        [TestMethod]
        public void GitCloneCommand_GlobalVerboseOptionWorks()
        {
            // Act
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);

            // Assert - Global verbose option should be available at root level
            var globalVerboseOption = _rootCommand!.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            Assert.IsNotNull(globalVerboseOption, "Global verbose option should be available");
        }

        [TestMethod]
        public void GitCloneCommand_InvokesServiceWithCorrectParameters()
        {
            // Arrange
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);
            _mockCloneService!.Setup(s => s.Clone(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<TextWriter>()))
                            .Returns(0);

            // Act
            var result = _rootCommand!.Parse(new[] { "--verbose", "git_clone", "--url", "https://github.com/user/repo", "--path", "/tmp/repo" });
            var exitCode = result.Invoke();

            // Assert
            Assert.AreEqual(0, exitCode, "Command should succeed");
            _mockCloneService!.Verify(s => s.Clone("https://github.com/user/repo", "/tmp/repo", true, false, It.IsAny<TextWriter>()), Times.Once);
        }

        [TestMethod]
        public void GitCloneCommand_UsesDefaultPathWhenNotSpecified()
        {
            // Arrange
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);
            _mockCloneService!.Setup(s => s.Clone(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<TextWriter>()))
                            .Returns(0);

            // Act
            var result = _rootCommand!.Parse(new[] { "git_clone", "--url", "https://github.com/user/repo" });
            var exitCode = result.Invoke();

            // Assert
            Assert.AreEqual(0, exitCode, "Command should succeed");
            _mockCloneService!.Verify(s => s.Clone("https://github.com/user/repo", "", false, false, It.IsAny<TextWriter>()), Times.Once);
        }

        [TestMethod]
        public void GitCloneCommand_ReturnsServiceExitCode()
        {
            // Arrange
            GitCloneCommand.Register(_rootCommand!, _dryRunOption!, _verboseOption!, _mockCloneService!.Object);
            _mockCloneService!.Setup(s => s.Clone(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<TextWriter>()))
                            .Returns(42);

            // Act
            var result = _rootCommand!.Parse(new[] { "git_clone", "--url", "https://github.com/user/repo" });
            var exitCode = result.Invoke();

            // Assert
            Assert.AreEqual(42, exitCode, "Should return service exit code");
        }
    }
}