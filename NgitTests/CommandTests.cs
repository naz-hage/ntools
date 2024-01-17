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

        private static GitWrapper Git { get; set; } = new GitWrapper();

        [TestMethod]
        public void DisplayTagTest()
        {
            // Arrange
            Options.GitCommand = Command.GetTagCommand;

            // Act
            var actual = Command.DisplayTag(Options);

            // Assert
            Assert.IsTrue(actual == Enums.RetCode.Success);
        }

        [TestMethod]
        public void DisplayTagVerboseTest()
        {
            // Arrange
            Options.GitCommand = Command.GetTagCommand;
            Options.Verbose = true;

            // Act
            var actual = Command.DisplayTag(Options);

            // Assert
            Assert.IsTrue(actual == Enums.RetCode.Success);
        }
    }
}
