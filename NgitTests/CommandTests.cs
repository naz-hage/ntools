using NbuildTasks;
using Ngit;

namespace NgitTests
{
    [TestClass()]
    public class CommandTests
    {
        private static Cli Options { get; set; } = new Cli()
        {
            Url = "https://nazhage.visualstudio.com",
            Verbose = false
        };

        private static GitWrapper GitWrapper { get; set; } = new GitWrapper();

        public CommandTests()
        {
            Console.WriteLine($"Ctor: DevDrive: {GitWrapper.DevDrive}");
            Console.WriteLine($"Ctor: MainDir: {GitWrapper.MainDir}");
        }
        [TestMethod]
        public void DisplayTagTest()
        {
            // Arrange
            Options.Command = Cli.CommandType.tag;

            // Act
            var actual = Command.DisplayTag(Options);

            // Assert
            Assert.IsTrue(actual == Enums.RetCode.Success);
        }

        [TestMethod]
        public void DisplayTagVerboseTest()
        {
            // Arrange
            Options.Command = Cli.CommandType.tag;
            Options.Verbose = true;

            // Act
            var actual = Command.DisplayTag(Options);

            // Assert
            Assert.IsTrue(actual == Enums.RetCode.Success);
        }

        [TestMethod]
        public void DisplayBranchTest()
        {
            // Arrange
            Options.Command = Cli.CommandType.branch;

            // Act
            var actual = Command.DisplayBranch();

            // Assert
            Assert.IsTrue(actual == Enums.RetCode.Success);
        }
    }

    [TestClass]
    public class UnifiedGitCommandTests
    {
        [TestMethod]
        public void TestGitCommands()
        {
            // Simulate invoking git commands
            var result = Program.Main(new[] { "git", "tag" });
            Assert.AreEqual(0, result);

            result = Program.Main(new[] { "git", "branch" });
            Assert.AreEqual(0, result);
        }
    }
}
