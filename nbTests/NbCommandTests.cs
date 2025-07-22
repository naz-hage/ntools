using System;
using Xunit;
using nb;

namespace nbTests
{
    public class NbCommandTests
    {
        static NbCommandTests()
        {
            // Set LOCAL_TEST to true for the user to enable test mode
            Environment.SetEnvironmentVariable("LOCAL_TEST", "true", EnvironmentVariableTarget.User);
        }

        private static string GetTestJsonContent()
        {
            return @"{
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
        }

        private void RunCommandWithJson(string command, string jsonPath)
        {
            System.IO.File.WriteAllText(jsonPath, GetTestJsonContent());
            try
            {
                var exitCode = nb.Program.Main(new string[] { command, "--json", jsonPath });
                Assert.Equal(0, exitCode);
            }
            finally
            {
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
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
            RunCommandWithJson("list", "test_list.json");
        }

        [Fact]
        public void InstallCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "install", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void InstallCommand_WithoutJson_UsesDefaultAndReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "install" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void InstallCommand_WithJson_InstallsAndReturnsSuccess()
        {
            RunCommandWithJson("install", "test_install.json");
        }

        [Fact]
        public void UninstallCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "uninstall", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void UninstallCommand_WithoutJson_UsesDefaultAndReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "uninstall" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void UninstallCommand_WithJson_UninstallsAndReturnsSuccess()
        {
            RunCommandWithJson("uninstall", "test_uninstall.json");
        }

        [Fact]
        public void DownloadCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "download", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DownloadCommand_WithoutJson_UsesDefaultAndReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "download" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DownloadCommand_WithJson_DownloadsAndReturnsSuccess()
        {
            RunCommandWithJson("download", "test_download.json");
        }
    }
}
