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
        private const string NbuildAssemblyName = "nb.dll";
        private const string NbuildAppListJsonFile = "ntools.json";
        private const string LocalTest = "LOCAL_TEST";
        private const string VersionToTest = "1.10.0";
        // Local test mode flag
        private bool? LocalTestMode;


        // Resource location for test setup
        private readonly string ResourceLocation = "Nbuild.ntools.json"; //"Nbuild.resources.ntools.json";

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

        private string TestPath => "C:\\Temp\\nbuild2";
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
                ""Version"": ""1.6.0"",
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

        [TestMethod()]
        public void DownloadUriNotFoundTest()
        {
            // Arrange
            SetupTestModeFlag();
            // JSON string for test setup
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""nbuild"",
                ""Version"": ""0.0.0"",
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

            try
            {
                // Act
                var result = Command.Download(json, true);
            }
            catch (Exception ex)
            {
                // Assert
                Assert.IsTrue(ex.Message.Contains("(404)"));

                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }

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

            // Obtain the Assembly object representing the currently Nb.dll assembly
            Assembly nbAssembly = Assembly.LoadFrom(assembly);

            // Call GetManifestResourceNames on the executingAssembly object
            string[] resources = nbAssembly.GetManifestResourceNames();
            Console.WriteLine($"Resources in the assembly: {assembly}");

            foreach (string resource in resources)
            {
                Console.WriteLine(resource);
            }

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(assembly, ResourceLocation, json);

            // Replace C:\\Program Files\\Nbuild with C:\\Temp\\nbuild2
            string jsonContent = File.ReadAllText(json);
            jsonContent = jsonContent.Replace("$(ProgramFiles)\\\\Nbuild", "C:\\\\Temp");

            // print json content to console as indented json

            Console.WriteLine(jsonContent);
           

            // Act
            var result = Command.Install(jsonContent, true);

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
            SetupTestModeFlag();

            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                    {
                        ""Name"": ""nbuild"",
                        ""Version"": ""versionToTest"",
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
            // replace versionToTest with the actual version
            json = json.Replace("versionToTest", VersionToTest);

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
                        ""Version"": ""versionToTest"",
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

            // replace versionToTest with the actual version
            json = json.Replace("versionToTest", VersionToTest);

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
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

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
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

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
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

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
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

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
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

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
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

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

        [TestMethod]
        public void AddAppInstallPathToEnvironmentPath_AddsPath_WhenNotPresent()
        {
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = TestPath
            };
            var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("PATH", string.Empty, EnvironmentVariableTarget.User);

            // Act
            Command.AddAppInstallPathToEnvironmentPath(nbuildApp);
            var updatedPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            // Assert
            Assert.IsTrue(updatedPath!.Contains(nbuildApp.InstallPath));

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);
        }

        [TestMethod]
        public void AddAppInstallPathToEnvironmentPath_DoesNotAddPath_WhenAlreadyPresent()
        {
            // Skip test if not running in admin mode - NOTE: Now that we use User PATH, admin is not required
            // but keeping the pattern for potential future system-level operations
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = TestPath
            };
            var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            var originalPathCount = originalPath!.Split(';').Length;
            if (!Command.IsAppInstallPathInEnvironmentPath(nbuildApp))
            {
                Environment.SetEnvironmentVariable("PATH", $"{nbuildApp.InstallPath};{originalPath}", EnvironmentVariableTarget.User);
            }

            var updatedPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            // Act
            Command.AddAppInstallPathToEnvironmentPath(nbuildApp);
            updatedPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            // Assert
            var pathCount = updatedPath!.Split(';').Length;
            Assert.AreEqual(pathCount, originalPath!.Split(';').Length + 1);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);

            Command.RemoveAppInstallPathFromEnvironmentPath(nbuildApp);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddAppInstallPathToEnvironmentPath_ThrowsException_WhenInstallPathIsNull()
        {
            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = null
            };

            // Act
            Command.AddAppInstallPathToEnvironmentPath(nbuildApp);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        public void Download_PrivateAsset_NotFound()
        {
            // Arrange
            SetupTestModeFlag();
            var json = @"{
                ""Version"": ""1.2.0"",
                ""NbuildAppList"": [
                {
                ""Name"": ""private-app"",
                ""Version"": ""0.0.0"",
                ""AppFileName"": ""private.zip"",
                ""WebDownloadFile"": ""https://github.com/naz-hage/this-repo-does-not-exist/releases/download/0.0.0/0.0.0.zip"",
                ""DownloadedFile"": ""0.0.0.zip"",
                ""InstallCommand"": ""powershell.exe"",
                ""InstallArgs"": ""-Command Write-Output 'noop'"",
                ""InstallPath"": ""C:\\Temp\\nbuild2"",
                ""UninstallCommand"": ""powershell.exe"",
                ""UninstallArgs"": ""-Command Write-Output 'noop'""
                }
            ]
            }";

            // Act
            var result = Command.Download(json, true);

            // Assert - should return a failure result but not throw
            Assert.IsFalse(result.IsSuccess());

            // teardown
            TeardownTestModeFlag();
        }

        [TestMethod]
        public void RemoveAppInstallPathFromEnvironmentPath_RemovesPath_WhenPresent()
        {
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = TestPath
            };
            var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("PATH", $"{TestPath};{originalPath}", EnvironmentVariableTarget.User);

            // Act
            Command.RemoveAppInstallPathFromEnvironmentPath(nbuildApp);
            var updatedPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            // Assert
            Assert.IsFalse(updatedPath!.Contains(nbuildApp.InstallPath));

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);
        }

        [TestMethod]
        public void RemoveAppInstallPathFromEnvironmentPath_DoesNotRemovePath_WhenNotPresent()
        {
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = TestPath
            };

            if (Command.IsAppInstallPathInEnvironmentPath(nbuildApp))
            {
                Command.RemoveAppInstallPathFromEnvironmentPath(nbuildApp);
            }

            var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);

            // Act
            Command.RemoveAppInstallPathFromEnvironmentPath(nbuildApp);
            var updatedPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            // Assert
            Assert.AreEqual(originalPath, updatedPath);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveAppInstallPathFromEnvironmentPath_ThrowsException_WhenInstallPathIsNull()
        {
            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = null
            };

            // Act
            Command.RemoveAppInstallPathFromEnvironmentPath(nbuildApp);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        public void IsAppInstallPathInEnvironmentPath_ReturnsTrue_WhenPathIsPresent()
        {
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = "C:\\TestPath"
            };
            var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("PATH", $"C:\\TestPath;{originalPath}", EnvironmentVariableTarget.User);

            // Act
            var result = Command.IsAppInstallPathInEnvironmentPath(nbuildApp);

            // Assert
            Assert.IsTrue(result);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);
        }

        [TestMethod]
        public void IsAppInstallPathInEnvironmentPath_ReturnsFalse_WhenPathIsNotPresent()
        {
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = "C:\\TestPath"
            };
            var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);

            // Act
            var result = Command.IsAppInstallPathInEnvironmentPath(nbuildApp);

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.User);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsAppInstallPathInEnvironmentPath_ThrowsException_WhenInstallPathIsNull()
        {
            // Arrange
            var nbuildApp = new NbuildApp
            {
                InstallPath = null
            };

            // Act
            Command.IsAppInstallPathInEnvironmentPath(nbuildApp);

            // Assert is handled by ExpectedException
        }
    }
}

