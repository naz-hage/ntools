using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;

namespace NbuildTests
{
    /// <summary>
    /// Tests for validating CLI option parsing and error handling in the main Program class.
    /// These tests ensure that unknown CLI options are properly detected and helpful error messages are displayed.
    /// </summary>
    [TestClass]
    public class ProgramCliValidationTests
    {
        private StringWriter? _consoleOutput;
        private StringWriter? _consoleError;
        private TextWriter? _originalOut;
        private TextWriter? _originalError;

        [TestInitialize]
        public void TestInitialize()
        {
            // Capture console output for assertions
            _originalOut = Console.Out;
            _originalError = Console.Error;
            _consoleOutput = new StringWriter();
            _consoleError = new StringWriter();
            Console.SetOut(_consoleOutput);
            Console.SetError(_consoleError);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore original console output
            if (_originalOut != null) Console.SetOut(_originalOut);
            if (_originalError != null) Console.SetError(_originalError);
            _consoleOutput?.Dispose();
            _consoleError?.Dispose();
        }

        private string GetErrorOutput()
        {
            return _consoleError?.ToString() ?? string.Empty;
        }

        [TestMethod]
        public void Main_WithCommonTypo_DrysRun_ShouldSuggestCorrection()
        {
            // Arrange
            var args = new[] { "list", "--json", "test.json", "--drys-run" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--drys-run"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("--dry-run"), "Error should suggest the correct option");
            Assert.IsTrue(errorOutput.Contains("Did you mean"), "Error should provide helpful suggestion");
        }

        [TestMethod]
        public void Main_WithCommonTypo_Verbos_ShouldSuggestCorrection()
        {
            // Arrange
            var args = new[] { "download", "--json", "test.json", "--verbos" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--verbos"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("--verbose"), "Error should suggest the correct option");
            Assert.IsTrue(errorOutput.Contains("Did you mean"), "Error should provide helpful suggestion");
        }

        [TestMethod]
        public void Main_WithUnknownOption_ShouldShowError()
        {
            // Arrange
            var args = new[] { "list", "--json", "test.json", "--unknown-option" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--unknown-option"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("Unknown option"), "Error should indicate it's an unknown option");
        }

        [TestMethod]
        public void Main_WithRandomInvalidOption_ShouldShowError()
        {
            // Arrange
            var args = new[] { "install", "--json", "test.json", "--themoon" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--themoon"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("Unknown option"), "Error should indicate it's an unknown option");
        }

        [TestMethod]
        public void Main_WithValidOptions_ShouldNotShowErrors()
        {
            // Arrange - using a simple command that should work
            var args = new[] { "--help" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(0, exitCode, "Should exit with success code 0");
            var errorOutput = GetErrorOutput();
            Assert.IsFalse(errorOutput.Contains("Unknown option"), "Should not show unknown option errors");
        }

        [TestMethod]
        public void Main_WithGitCloneInvalidOption_ShouldShowError()
        {
            // Arrange
            var args = new[] { "git_clone", "--url", "https://github.com/user/repo", "--invalid-param" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--invalid-param"), "Error should mention the invalid option");
        }

        [TestMethod]
        public void Main_WithReleaseCreateInvalidOption_ShouldShowError()
        {
            // Arrange
            var args = new[] { "release_create", "--repo", "test/repo", "--tag", "v1.0.0", "--branch", "main", "--file", "test.zip", "--badoption" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--badoption"), "Error should mention the invalid option");
        }

        [TestMethod]
        public void Main_WithTargetsInvalidOption_ShouldShowError()
        {
            // Arrange
            var args = new[] { "targets", "--invalidflag" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--invalidflag"), "Error should mention the invalid option");
        }

        [TestMethod]
        public void Main_WithBuildTargetName_ShouldNotTriggerValidation()
        {
            // Arrange - build target names don't start with -- so should not trigger validation
            var args = new[] { "nonexistenttarget" };

            // Act
            var exitCode = Program.Main(args);

            // Assert - This should not trigger option validation since it's not a --option
            // The target might not exist, but that's a different kind of error
            var errorOutput = GetErrorOutput();
            Assert.IsFalse(errorOutput.Contains("Unknown option"), "Build target names should not trigger option validation");
        }

        [TestMethod]
        public void Main_WithMultipleInvalidOptions_ShouldShowErrorForFirst()
        {
            // Arrange
            var args = new[] { "list", "--json", "test.json", "--invalid1", "--invalid2" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("Unknown option"), "Should show error for invalid options");
            // Should catch the first invalid option
            Assert.IsTrue(errorOutput.Contains("--invalid1") || errorOutput.Contains("--invalid2"),
                "Should mention at least one of the invalid options");
        }

        [TestMethod]
        public void Main_WithTypoInGlobalOption_ShouldSuggestCorrection()
        {
            // Arrange - test typo in global --dry-run option at root level
            var args = new[] { "--dryrun" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--dryrun"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("--dry-run"), "Error should suggest the correct option");
        }

        [TestMethod]
        public void Main_WithJSONTypo_ShouldSuggestCorrection()
        {
            // Arrange
            var args = new[] { "list", "--jsn", "test.json" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--jsn"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("--json"), "Error should suggest the correct option");
        }

        [TestMethod]
        public void Main_WithHelpTypo_ShouldSuggestCorrection()
        {
            // Arrange
            var args = new[] { "--hep" };

            // Act
            var exitCode = Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should exit with error code 1");
            var errorOutput = GetErrorOutput();
            Assert.IsTrue(errorOutput.Contains("--hep"), "Error should mention the invalid option");
            Assert.IsTrue(errorOutput.Contains("--help"), "Error should suggest the correct option");
        }
    }
}
