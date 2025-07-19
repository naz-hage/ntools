using System;
using Xunit;
using nb;

namespace nbTests
{
    public class ListCommandTests
    {

        static ListCommandTests()
        {
            // Set LOCAL_TEST to true for the user to enable test mode
            Environment.SetEnvironmentVariable("LOCAL_TEST", "true", EnvironmentVariableTarget.User);
        }

        [Fact]
        public void ListCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "list", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ListCommand_WithoutJson_UsesDefaultAndReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "list" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ListCommand_WithJson_PrintsListAndReturnsSuccess()
        {
            // For now, just check that the command returns 0 when --json is provided
            // (You can expand this to check output if needed)
            var exitCode = nb.Program.Main(new string[] { "list", "--json", "test.json" });
            Assert.Equal(0, exitCode);
        }
    }
}
