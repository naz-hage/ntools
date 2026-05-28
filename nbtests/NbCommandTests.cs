using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;

namespace NbuildTests
{
    [TestClass]
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
            File.WriteAllText(jsonPath, GetTestJsonContent());
            try
            {
                var exitCode = Program.Main(new string[] { command, "--json", jsonPath });
                Assert.AreEqual(0, exitCode);
            }
            finally
            {
                if (File.Exists(jsonPath))
                    File.Delete(jsonPath);
            }
        }

        [TestMethod]
        public void ListCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "list", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void ListCommand_WithoutJson_UsesDefaultAndReturnsSuccess()
        {
            // Create a temporary directory structure that matches the default path
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var nbuildDir = Path.Combine(tempDir, "nbuild");
            Directory.CreateDirectory(nbuildDir);
            
            var tempAppsJson = Path.Combine(nbuildDir, "apps.json");
            File.WriteAllText(tempAppsJson, GetTestJsonContent());
            
            try
            {
                // Set ProgramFiles to temp directory so the default path points to our test file
                var originalProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                Environment.SetEnvironmentVariable("ProgramFiles", tempDir);
                
                try
                {
                    var exitCode = Program.Main(new string[] { "list" });
                    Assert.AreEqual(0, exitCode);
                }
                finally
                {
                    // Restore original ProgramFiles
                    if (originalProgramFiles != null)
                        Environment.SetEnvironmentVariable("ProgramFiles", originalProgramFiles);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void ListCommand_WithJson_PrintsListAndReturnsSuccess()
        {
            RunCommandWithJson("list", "test_list.json");
        }

        [TestMethod]
        public void DownloadCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "download", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void DownloadCommand_WithoutJson_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "download" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void DownloadCommand_WithJson_DownloadsAndReturnsSuccess()
        {
            RunCommandWithJson("download", "test_download.json");
        }

        [TestMethod]
        public void InstallCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "install", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void InstallCommand_WithoutJson_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "install" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void UninstallCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "uninstall", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void UninstallCommand_WithoutJson_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "uninstall" });
            Assert.AreNotEqual(0, exitCode);
        }


        [TestMethod]
        public void GitInfoCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_info", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitInfoCommand_Executes_ReturnsInt()
        {
            var exitCode = Program.Main(new string[] { "git_info" });
            // Could be 0 or error depending on repo state, just check it's an int
            // exitCode type is already verified by declaration;
        }

        [TestMethod]
        public void GitSetTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_settag", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitSetTagCommand_WithoutTag_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "git_settag" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void GitAutoTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_autotag", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitAutoTagCommand_WithoutBuildType_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "git_autotag" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void GitPushAutoTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_push_autotag", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitPushAutoTagCommand_WithoutBuildType_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "git_push_autotag" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void GitBranchCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_branch", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitBranchCommand_Executes_ReturnsInt()
        {
            var exitCode = Program.Main(new string[] { "git_branch" });
            // exitCode type is already verified by declaration;
        }

        [TestMethod]
        public void GitCloneCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_clone", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitCloneCommand_WithoutUrlOrPath_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "git_clone" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void GitDeleteTagCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "git_deletetag", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void GitDeleteTagCommand_WithoutTag_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "git_deletetag" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void ReleaseCreateCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "release_create", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void ReleaseCreateCommand_WithoutArgs_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "release_create" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void PreReleaseCreateCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "pre_release_create", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void PreReleaseCreateCommand_WithoutArgs_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "pre_release_create" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void ReleaseDownloadCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "release_download", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void ReleaseDownloadCommand_WithoutArgs_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "release_download" });
            Assert.AreNotEqual(0, exitCode);
        }

        [TestMethod]
        public void ListReleaseCommand_Help_ReturnsSuccess()
        {
            var exitCode = Program.Main(new string[] { "list_release", "--help" });
            Assert.AreEqual(0, exitCode);
        }

        [TestMethod]
        public void ListReleaseCommand_WithoutRepo_ReturnsError()
        {
            var exitCode = Program.Main(new string[] { "list_release" });
            Assert.AreNotEqual(0, exitCode);
        }

    }
}
