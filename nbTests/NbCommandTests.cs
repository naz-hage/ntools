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

        private string CreateTestJson(string fileName)
        {
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
            System.IO.File.WriteAllText(fileName, jsonContent);
            return fileName;
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
            var jsonPath = CreateTestJson("test.json");
            try
            {
                var exitCode = nb.Program.Main(new string[] { "list", "--json", jsonPath });
                Assert.Equal(0, exitCode);
            }
            finally
            {
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
        }

        [Fact]
        public void InstallCommand_WithJson_ReturnsSuccess()
        {
            var jsonPath = CreateTestJson("test_install.json");
            try
            {
                var exitCode = nb.Program.Main(new string[] { "install", "--json", jsonPath });
                Assert.Equal(0, exitCode);
            }
            finally
            {
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
        }

        [Fact]
        public void DownloadCommand_WithJson_ReturnsSuccess()
        {
            var jsonPath = CreateTestJson("test_download.json");
            try
            {
                var exitCode = nb.Program.Main(new string[] { "download", "--json", jsonPath });
                Assert.Equal(0, exitCode);
            }
            finally
            {
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
        }

        [Fact]
        public void ListCommand_WithJson_Verbose_ReturnsSuccessAndVerboseOutput()
        {
            var jsonPath = CreateTestJson("test_verbose.json");
            try
            {
                using (var sw = new System.IO.StringWriter())
                {
                    Console.SetOut(sw);
                    var exitCode = nb.Program.Main(new string[] { "list", "--json", jsonPath, "--verbose" });
                    var output = sw.ToString();
                    Assert.Equal(0, exitCode);
                    Assert.Contains("Visual Studio Code", output);
                    Assert.Contains("Version", output);
                }
            }
            finally
            {
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
        }

        [Fact]
        public void ListCommand_WithInvalidJson_ReturnsError()
        {
            var jsonPath = "invalid.json";
            System.IO.File.WriteAllText(jsonPath, "{ invalid json }");
            try
            {
                var exitCode = nb.Program.Main(new string[] { "list", "--json", jsonPath });
                Assert.NotEqual(0, exitCode);
            }
            finally
            {
                if (System.IO.File.Exists(jsonPath))
                    System.IO.File.Delete(jsonPath);
            }
        }
    }
}
