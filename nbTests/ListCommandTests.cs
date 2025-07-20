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
            // Create a valid temporary test.json file in the format of vs-code.json
            var jsonPath = "test.json";
            var jsonContent = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                    ""Name"": ""Visual Studio Code"",
                    ""Version"": ""1.100.1"",
                    ""AppFileName"": ""$(InstallPath)\\Code.exe"",
                    ""WebDownloadFile"": ""https://aka.ms/win32-x64-system-stable"",
                    ""DownloadedFile"": ""VSCodeSetup-x64-$(Version).exe"",
                    ""InstallCommand"": ""$(DownloadedFile)"",
                    ""InstallArgs"": ""/silent /mergetasks=!runcode,addcontextmenufiles,addcontextmenufolders"",
                    ""InstallPath"": ""$(ProgramFiles)\\Microsoft VS Code"",
                    ""UninstallCommand"": ""$(InstallPath)\\unins000.exe"",
                    ""UninstallArgs"": ""/SILENT"",
                    ""StoredHash"": null
                    }
                ]
                }";
                
            System.IO.File.WriteAllText(jsonPath, jsonContent);
            try
            {
                var exitCode = nb.Program.Main(new string[] { "list", "--json", jsonPath });
                Assert.Equal(0, exitCode);
            }
            finally
            {
                // Clean up
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
        }
    }
}
