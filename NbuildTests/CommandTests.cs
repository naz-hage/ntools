using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using NbuildTasks;
using Ntools;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace NbuildTests
{
    [TestClass()]
    public class CommandTests
    {
        // Constants for test setup
        private const string NbuildAssemblyName = "Nb.dll";
        private const string NbuildAppListJsonFile = "app-ntools.json";
        private const string LocalTest = "LOCAL_TEST";

        // Local test mode flag
        private bool? LocalTestMode;

        // Resource location for test setup
        private readonly string ResourceLocation = "Nbuild.resources.app-ntools.json";

        // Method to teardown test mode flag
        private void TeardownTestModeFlag()
        {
            // If local test mode is set, unset it
            if (LocalTestMode.HasValue)
            {
                // tear test mode
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "REG.exe",
                        Arguments = $"delete HKCU\\Environment /F /V {LocalTest}",
                        WorkingDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                var result = process.LockStart(true);
                Assert.IsTrue(result.IsSuccess());
            }
        }

        // Method to setup test mode flag
        private void SetupTestModeFlag()
        {
            var githubActions = Environment.GetEnvironmentVariable(LocalTest, EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(githubActions))
            {
                // on local machine, Set GitHubActions to true
                LocalTestMode = true;

                // setup test mode
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "setx.exe",
                        Arguments = $"{LocalTest} true",
                        WorkingDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                // Get all environment variables
                IDictionary environmentVariables = Environment.GetEnvironmentVariables();

                // Iterate over the environment variables and print them
                foreach (DictionaryEntry entry in environmentVariables)
                {
                    Console.WriteLine($"{entry.Key}: {entry.Value}");
                }

                Assert.IsTrue(process.LockStart(true).IsSuccess());
            }
            LocalTestMode = true;
        }

        // Test method for download functionality
        [TestMethod()]
        public void DownloadTest()
        {
            // Arrange
            SetupTestModeFlag();
            // JSON string for test setup
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""nbuild"",
                ""Version"": ""1.2.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2"",
                ""UninstallCommand"": ""powershell.exe"",
                ""UninstallArgs"": ""-Command Remove-Item -Path '$(InstallPath)' -Recurse -Force""
                }
            ]
            }";

            // Act
            var result = Command.Download(json);

            // Assert
            Assert.IsTrue(result.IsSuccess());

            //teardown
            TeardownTestModeFlag();
        }

        // Test method for install from JSON file functionality
        [TestMethod()]
        public void InstallFromJsonFileTest()
        {
            SetupTestModeFlag();
            // Arrange read json from file from embedded resource

            string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.IsNotNull(executingAssemblyDirectory);

            string json = Path.Combine(executingAssemblyDirectory, NbuildAppListJsonFile);
            var assembly = Path.Combine(executingAssemblyDirectory, NbuildAssemblyName);

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(assembly, ResourceLocation, json);

            // Act
            var result = Command.Install(json);

            if (!result.IsSuccess() && result.Output.Count > 0)
            {

                Console.WriteLine(result.GetFirstOutput().Trim(' '));
            }

            var result2 = result.IsSuccess();

            // Assert
            Assert.IsTrue(result2);

            // teardown
            TeardownTestModeFlag();
        }

        // Test method for install functionality
        [TestMethod()]
        public void InstallTest()
        {
            // Arrange "C:\Program Files\7-Zip\7z.exe" x C:\Artifacts\ntools\Release\%1.zip -o"C:\Program Files\Nbuild" -y
            // var json = @"{
            //     ""Name"": ""nbuild"",
            //     ""Version"": ""1.2.0"",
            //     ""Url"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
            //     ""InstallFile"": ""$(Version).zip"",
            //     ""InstallCommand"": ""c:\\program files\\7-Zip\\7z.exe"",
            //     ""InstallArgs"": ""x $(Version).zip -o\""C:\\Temp\\nbuild2\"" -y""
            // }";
            // Use this json to test the install command in GitHub Actions because it doesn't have 7-Zip installed
            SetupTestModeFlag();

            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                        ""Name"": ""nbuild"",
                        ""Version"": ""1.2.35"",
                        ""AppFileName"": ""$(InstallPath)\\nb.exe"",
                        ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                        ""DownloadedFile"": ""$(Version).zip"",
                        ""InstallCommand"": ""powershell.exe"",
                        ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                        ""InstallPath"": ""C:\\Temp\\nbuild2"",
                        ""UninstallCommand"": ""powershell.exe"",
                        ""UninstallArgs"": ""-Command Remove-Item -Path '$(InstallPath)' -Recurse -Force""
                    }
                ]
            }";

            // Act
            var result = Command.Install(json);

            if (!result.IsSuccess() && result.Output.Count > 0)
            {

                Console.WriteLine(result.GetFirstOutput().Trim(' '));
            }

            var result2 = result.IsSuccess();

            // Assert
            Assert.IsTrue(result2);

            // teardown
            TeardownTestModeFlag();
        }

        // Test method for uninstall functionality
        [TestMethod()]
        public void UninstallTest()
        {
            // Arrange "C:\Program Files\7-Zip\7z.exe" x C:\Artifacts\ntools\Release\%1.zip -o"C:\Program Files\Nbuild" -y
            // var json = @"{
            //     ""Name"": ""nbuild"",
            //     ""Version"": ""1.2.0"",
            //     ""Url"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
            //     ""InstallFile"": ""$(Version).zip"",
            //     ""InstallCommand"": ""c:\\program files\\7-Zip\\7z.exe"",
            //     ""InstallArgs"": ""x $(Version).zip -o\""C:\\Temp\\nbuild2\"" -y""
            // }";
            // Use this json to test the install command in GitHub Actions because it doesn't have 7-Zip installed
            SetupTestModeFlag();

            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                        ""Name"": ""nbuild"",
                        ""Version"": ""1.2.35"",
                        ""AppFileName"": ""$(InstallPath)\\nb.exe"",
                        ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                        ""DownloadedFile"": ""$(Version).zip"",
                        ""InstallCommand"": ""powershell.exe"",
                        ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                        ""InstallPath"": ""C:\\Temp\\nbuild2"",
                        ""UninstallCommand"": ""powershell.exe"",
                        ""UninstallArgs"": ""-Command Remove-Item -Path '$(InstallPath)' -Recurse -Force""
                    }
                ]
            }";
            // Install the app first before uninstalling
            var result = Command.Install(json);
            Assert.IsTrue(result.IsSuccess());


            // Act
            result = Command.Uninstall(json, true);

            if (!result.IsSuccess() && result.Output.Count > 0)
            {

                Console.WriteLine(result.GetFirstOutput().Trim(' '));
            }

            var result2 = result.IsSuccess();

            // Assert
            Assert.IsTrue(result2);

            // teardown
            TeardownTestModeFlag();
        }


        // Test method for install exception when name is not defined
        [TestMethod()]
        public void InstallExceptionNameTest()
        {
            // Arrange with json and no name define ""Name"": ""nbuild"",
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                    ""Version"": ""1.2.0"",
                    ""AppFileName"": ""nb.exe"",
                    ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                    ""DownloadedFile"": ""$(Version).zip"",
                    ""InstallCommand"": ""powershell.exe"",
                    ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                    ""InstallPath"": ""C:\\Temp\\nbuild2""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert an failed json parsing is returned 

            Assert.AreEqual("Invalid json input: Name is required", result.GetFirstOutput().Trim(' '));
        }

        // Test method for install exception when AppFileName is not defined
        [TestMethod()]
        public void InstallExceptionAppFileNameTest()
        {
            // Arrange with json and no AppFileName defined
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                    ""Name"": ""nbuild"",
                    ""Version"": ""1.2.0"",
                    ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                    ""DownloadedFile"": ""$(Version).zip"",
                    ""InstallCommand"": ""powershell.exe"",
                    ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                    ""InstallPath"": ""C:\\Temp\\nbuild2"",
                    ""UninstallCommand"": ""powershell.exe"",
                    ""UninstallArgs"": ""-Command Remove-Item -Path '$(InstallPath)' -Recurse -Force""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(result.GetFirstOutput().Trim(' '), "Invalid json input: AppFileName is required");
        }

        // Test method for install exception when WebDownloadFile is not defined
        [TestMethod()]
        public void InstallExceptionWebDownloadFileTest()
        {
            // Arrange with json and no WebDownloadFile defined
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                    ""Name"": ""nbuild"",
                    ""Version"": ""1.2.0"",
                    ""AppFileName"": ""nb.exe"",
                    ""DownloadedFile"": ""$(Version).zip"",
                    ""InstallCommand"": ""powershell.exe"",
                    ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                    ""InstallPath"": ""C:\\Temp\\nbuild2""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(result.GetFirstOutput().Trim(' '), "Invalid json input: WebDownloadFile is required");
        }

        // Test method for install exception when DownloadedFile is not defined
        [TestMethod()]
        public void InstallExceptionDownloadedFileTest()
        {
            // Arrange with json and no DownloadedFile defined
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                    ""Name"": ""nbuild"",
                    ""Version"": ""1.2.0"",
                    ""AppFileName"": ""nb.exe"",
                    ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                    ""InstallCommand"": ""powershell.exe"",
                    ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                    ""InstallPath"": ""C:\\Temp\\nbuild2""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(result.GetFirstOutput().Trim(' '), "Invalid json input: DownloadedFile is required");
        }

        // Test method for install exception when InstallCommand is not defined
        [TestMethod()]
        public void InstallExceptionInstallCommandTest()
        {
            // Arrange with json and no InstallCommand defined
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""nbuild"",
                ""Version"": ""1.2.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: InstallCommand is required");
        }

        // Test method for install exception when InstallArgs is not defined
        [TestMethod()]
        public void InstallExceptionInstallArgsTest()
        {
            // Arrange with json and no InstallArgs defined
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""nbuild"",
                ""Version"": ""1.2.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(result.GetFirstOutput().Trim(' '), "Invalid json input: InstallArgs is required");
        }

        // Test method for install exception when InstallPath is not defined
        [TestMethod()]
        public void InstallExceptionInstallPathTest()
        {
            // Arrange with json and no InstallPath defined
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""nbuild"",
                ""Version"": ""1.2.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force""
                    }
                ]
            }";

            ResultHelper result;
            // Act
            try
            {
                result = Command.Install(json);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());

            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: InstallPath is required");
        }
    }
}