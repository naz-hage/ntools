using Ntools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using NbuildTasks;
using OutputColorizer;
using System.Reflection;

namespace NbuildTests
{
    [TestClass()]
    public class CommandTests
    {
        // Constants for test setup
        private const string NbuildAssemblyName = "Nb.dll";
        private const string NbuildAppListJsonFile = "NbuildAppListTest.json";
        private const string GitHubActions = "LOCAL_TEST";

        // Local test mode flag
        private bool? LocalTestMode;

        // Resource location for test setup
        private readonly string ResourceLocation = "Nbuild.resources.NbuildAppListTest.json";

        // Method to teardown test mode flag
        private void TeardownTestModeFlag()
        {
            // If local test mode is set, unset it
            if (LocalTestMode.HasValue)
            {
                // tear test mode
                var parameters = new Parameters
                {
                    FileName = "REG",
                    Arguments = $"delete HKCU\\Environment /F /V {GitHubActions}",
                    WorkingDir = Environment.CurrentDirectory,
                    Verbose = true
                };

                Assert.IsTrue(Launcher.Start(parameters).IsSuccess());
            }
        }

        // Method to setup test mode flag
        private void SetupTestModeFlag()
        {
            var githubActions = Environment.GetEnvironmentVariable(GitHubActions, EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(githubActions))
            {
                // on local machine, Set GitHubActions to true
                LocalTestMode = true;

                // setup test mode
                var parameters = new Parameters
                {
                    FileName = "setx",
                    Arguments = $"{GitHubActions} true",
                    WorkingDir = Environment.CurrentDirectory,
                    Verbose = true
                };
                var resultInstall = Launcher.Start(parameters);
                Assert.IsTrue(resultInstall.IsSuccess());
            }
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
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
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
            ResultHelper result = Command.Install(json);

            if (!result.IsSuccess() && result.Output.Count > 0)
            {
                Console.WriteLine(result.GetFirstOutput());
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
            //     ""Version"": ""1.1.0"",
            //     ""Url"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
            //     ""InstallFile"": ""$(Version).zip"",
            //     ""InstallCommand"": ""c:\\program files\\7-Zip\\7z.exe"",
            //     ""InstallArgs"": ""x $(Version).zip -o\""C:\\Temp\\nbuild2\"" -y""
            // }";
            // Use this json to test the install command in GitHub Actions because it doesn't have 7-Zip installed
            SetupTestModeFlag();

            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            var appdata = NbuildApp.FromJson(json);

            // Act
            ResultHelper result = Command.Install(json);

            if (!result.IsSuccess() && result.Output.Count > 0)
            {
                Console.WriteLine(result.GetFirstOutput());
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

                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert an failed json parsing is returned 
            Assert.AreEqual("Invalid json input: Name is missing or empty", result.GetFirstOutput());
        }

        // Test method for install exception when AppFileName is not defined
        [TestMethod()]
        public void InstallExceptionAppFileNameTest()
        {
            // Arrange with json and no AppFileName defined
            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: AppFileName is missing or empty");
        }

        // Test method for install exception when WebDownloadFile is not defined
        [TestMethod()]
        public void InstallExceptionWebDownloadFileTest()
        {
            // Arrange with json and no WebDownloadFile defined
            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: WebDownloadFile is missing or empty");
        }

        // Test method for install exception when DownloadedFile is not defined
        [TestMethod()]
        public void InstallExceptionDownloadedFileTest()
        {
            // Arrange with json and no DownloadedFile defined
            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: DownloadedFile is missing or empty");
        }

        // Test method for install exception when InstallCommand is not defined
        [TestMethod()]
        public void InstallExceptionInstallCommandTest()
        {
            // Arrange with json and no InstallCommand defined
            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: InstallCommand is missing or empty");
        }

        // Test method for install exception when InstallArgs is not defined
        [TestMethod()]
        public void InstallExceptionInstallArgsTest()
        {
            // Arrange with json and no InstallArgs defined
            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallPath"": ""C:\\Temp\\nbuild2""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: InstallArgs is missing or empty");
        }

        // Test method for install exception when InstallPath is not defined
        [TestMethod()]
        public void InstallExceptionInstallPathTest()
        {
            // Arrange with json and no InstallPath defined
            var json = @"{
                ""Name"": ""nbuild"",
                ""Version"": ""1.1.0"",
                ""AppFileName"": ""nb.exe"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/ntools/releases/download/$(Version)/$(Version).zip"",
                ""DownloadedFile"": ""$(Version).zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Expand-Archive -Path $(Version).zip -DestinationPath $(InstallPath) -Force""
            }";

            // Act
            ResultHelper result = Command.Install(json);

            // Assert a failed json parsing is returned 
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual(result.GetFirstOutput(), "Invalid json input: InstallPath is missing or empty");
        }
    }
}