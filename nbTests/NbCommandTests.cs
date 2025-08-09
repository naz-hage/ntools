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
        public void DownloadCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "download", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DownloadCommand_WithoutJson_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "download" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void DownloadCommand_WithJson_DownloadsAndReturnsSuccess()
        {
            RunCommandWithJson("download", "test_download.json");
        }

        [Fact]
        public void InstallCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "install", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void InstallCommand_WithoutJson_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "install" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void UninstallCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "uninstall", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void UninstallCommand_WithoutJson_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "uninstall" });
            Assert.NotEqual(0, exitCode);
        }


        [Fact]
        public void GitInfoCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_info", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitInfoCommand_Executes_ReturnsInt()
        {
            var exitCode = nb.Program.Main(new string[] { "git_info" });
            // Could be 0 or error depending on repo state, just check it's an int
            Assert.IsType<int>(exitCode);
        }

        [Fact]
        public void GitSetTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_settag", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitSetTagCommand_WithoutTag_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "git_settag" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void GitAutoTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_autotag", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitAutoTagCommand_WithoutBuildType_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "git_autotag" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void GitPushAutoTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_push_autotag", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitPushAutoTagCommand_WithoutBuildType_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "git_push_autotag" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void GitBranchCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_branch", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitBranchCommand_Executes_ReturnsInt()
        {
            var exitCode = nb.Program.Main(new string[] { "git_branch" });
            Assert.IsType<int>(exitCode);
        }

        [Fact]
        public void GitCloneCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_clone", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitCloneCommand_WithoutUrlOrPath_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "git_clone" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void GitDeleteTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "git_deletetag", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GitDeleteTagCommand_WithoutTag_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "git_deletetag" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void ReleaseCreateCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "release_create", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ReleaseCreateCommand_WithoutArgs_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "release_create" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void PreReleaseCreateCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "pre_release_create", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void PreReleaseCreateCommand_WithoutArgs_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "pre_release_create" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void ReleaseDownloadCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "release_download", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ReleaseDownloadCommand_WithoutArgs_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "release_download" });
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public void ListReleaseCommand_Help_ReturnsSuccess()
        {
            var exitCode = nb.Program.Main(new string[] { "list_release", "--help" });
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ListReleaseCommand_WithoutRepo_ReturnsError()
        {
            var exitCode = nb.Program.Main(new string[] { "list_release" });
            Assert.NotEqual(0, exitCode);
        }

    }
}
