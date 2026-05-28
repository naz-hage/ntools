using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nbuild;
using Nbuild.Services;
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

        [TestMethod]
        public async Task CreateRelease_DryRun_PrintsDryRunMessage()
        {
            var result = await Command.CreateRelease("repo", "v1.0.0", "main", "asset.zip", false, true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public async Task DownloadAsset_DryRun_PrintsDryRunMessage()
        {
            var result = await Command.DownloadAsset("repo", "v1.0.0", "C:\\Temp", true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public void Install_DryRun_PrintsDryRunMessage()
        {
            var result = Command.Install("{\"NbuildAppList\":[]}", false, true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public void Install_DryRun_WithName_PrintsDryRunMessage()
        {
            var result = Command.Install(null, "NonExistentApp", null, false, true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public void Install_DryRun_WithNameAndVersion_PrintsDryRunMessage()
        {
            var result = Command.Install(null, "NonExistentApp", "1.0.0", false, true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public void Install_WithoutJsonOrName_Fails()
        {
            var result = Command.Install(null, null, null, false, false);
            Assert.IsFalse(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("Either json file path or app name must be provided")));
        }

        [TestMethod]
        public void Uninstall_DryRun_PrintsDryRunMessage()
        {
            var result = Command.Uninstall("{\"NbuildAppList\":[]}", false, true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public async Task ListReleases_DryRun_PrintsDryRunMessageAndFetchesData()
        {
            // Use a real repository for testing since dry-run now performs read-only fetches
            var result = await Command.ListReleases("naz-hage/ntools", false, true);
            Assert.IsTrue(result.IsSuccess(), "Expected ListReleases to succeed in dry-run mode.");
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")), "Expected DRY-RUN message in output.");

            // In dry-run mode, list_release should still perform read-only network fetch per PBI-038 specs
            // This is acceptable behavior as documented: "reads allowed"
        }

        [TestMethod]
        public void Clone_DryRun_PrintsDryRunMessage()
        {
            var result = Command.Clone("https://github.com/naz-hage/getting-started", "c:\\temp", false, true);
            Assert.IsTrue(result.IsSuccess());
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")));
        }

        [TestMethod]
        public void DownloadManifestFile_DryRun_ShouldNotPerformDownload()
        {
            // Reproduce the user's reported command:
            // .\nb\bin\Release\nb.exe download --json ".\dev-setup\ntools.json" --dry-run
            var manifestPath = @"C:\source\ntools\dev-setup\ntools.json";
            var result = Command.Download(manifestPath, false, true);

            // Dry-run should be reported as successful and include a DRY-RUN marker
            Assert.IsTrue(result.IsSuccess(), "Expected Download to succeed in dry-run mode.");
            Assert.IsTrue(result.Output.Any(x => x.Contains("DRY-RUN")), "Expected DRY-RUN message in output.");

            // It should NOT report actual downloads (e.g., 'apps to download' table header)
            Assert.IsFalse(result.Output.Any(x => x.Contains("apps to download") || x.Contains("Downloaded file") || x.Contains("| App name")), "Dry-run should not list actual downloads or table output.");
        }

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
        [Ignore("Removed - ntools.json is no longer embedded as a resource. Use go/apps.json instead.")]
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

            ResourceHelper.ExtractEmbeddedResourceFromAssembly(assembly, "nb.ntools.json", json);

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
            var result = Command.Install(json, verbose: true, dryRun: false);

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
        public void GetAppsFromCurrentDirectory_FindsAppByName()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Create a test JSON file
                var jsonContent = @"{
                    ""Version"": ""1.2.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""testapp"",
                            ""Version"": ""1.0.0"",
                            ""AppFileName"": ""testapp.exe"",
                            ""WebDownloadFile"": ""https://example.com/testapp.zip"",
                            ""DownloadedFile"": ""testapp.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\Temp\\testapp"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContent);

                // Act
                var apps = Command.GetAppsFromCurrentDirectory("testapp", null, out var availableApps);

                // Assert
                Assert.AreEqual(1, apps.Count);
                Assert.AreEqual("testapp", apps[0].Name);
                Assert.AreEqual("1.0.0", apps[0].Version);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void GetAppsFromCurrentDirectory_FindsAppByNameAndVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Create test JSON file with app
                var jsonContent = @"{
                    ""Version"": ""1.2.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""testapp"",
                            ""Version"": ""1.0.0"",
                            ""AppFileName"": ""testapp.exe"",
                            ""WebDownloadFile"": ""https://example.com/testapp.zip"",
                            ""DownloadedFile"": ""testapp.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\Temp\\testapp"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContent);

                // Act - version parameter should override the JSON version
                var apps = Command.GetAppsFromCurrentDirectory("testapp", "2.0.0", out var availableApps);

                // Assert
                Assert.AreEqual(1, apps.Count);
                Assert.AreEqual("testapp", apps[0].Name);
                Assert.AreEqual("2.0.0", apps[0].Version); // Version should be overridden
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void GetAppsFromCurrentDirectory_ThrowsOnMultipleAppsWithSameName()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);
                // Create a single apps.json with both versions of the same app
                var jsonContentFinal = @"{
                    ""Version"": ""1.2.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""testapp"",
                            ""Version"": ""1.0.0"",
                            ""AppFileName"": ""testapp.exe"",
                            ""WebDownloadFile"": ""https://example.com/testapp.zip"",
                            ""DownloadedFile"": ""testapp.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\Temp\\testapp"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        },
                        {
                            ""Name"": ""testapp"",
                            ""Version"": ""2.0.0"",
                            ""AppFileName"": ""testapp.exe"",
                            ""WebDownloadFile"": ""https://example.com/testapp.zip"",
                            ""DownloadedFile"": ""testapp.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\Temp\\testapp"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContentFinal);

                // Act & Assert - should fail because multiple apps with same name exist without version specified
                var ex = Assert.ThrowsException<ArgumentException>(() =>
                    Command.GetAppsFromCurrentDirectory("testapp", null, out var availableApps));
                Assert.IsTrue(ex.Message.Contains("Multiple apps found with name 'testapp'"));
                Assert.IsTrue(ex.Message.Contains("Please specify a version"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void GetAppsFromCurrentDirectory_HandlesUnsupportedVersionGracefully()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Create apps.json with unsupported version - this file will be skipped
                var jsonContentBadVersion = @"{
                    ""Version"": ""99.0.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""badapp"",
                            ""Version"": ""1.0.0"",
                            ""AppFileName"": ""badapp.exe"",
                            ""WebDownloadFile"": ""https://example.com/badapp.zip"",
                            ""DownloadedFile"": ""badapp.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\Temp\\badapp"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContentBadVersion);

                // Act - when apps.json has unsupported version, it is skipped
                var apps = Command.GetAppsFromCurrentDirectory("anyapp", null, out var availableApps);

                // Assert - should return empty because the only file has unsupported version (gets skipped)
                Assert.AreEqual(0, apps.Count);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void GetAppsFromCurrentDirectory_ReturnsEmptyWhenNoMatch()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Create a test JSON file
                var jsonContent = @"{
                    ""Version"": ""1.2.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""testapp"",
                            ""Version"": ""1.0.0"",
                            ""AppFileName"": ""testapp.exe"",
                            ""WebDownloadFile"": ""https://example.com/testapp.zip"",
                            ""DownloadedFile"": ""testapp.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\Temp\\testapp"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContent);

                // Act
                var apps = Command.GetAppsFromCurrentDirectory("nonexistent", null, out var availableApps);

                // Assert
                Assert.AreEqual(0, apps.Count);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod()]
        public void GetAppsFromCurrentDirectory_PopulatesAvailableAppsWhenNoMatch()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Create test JSON files with multiple apps
                var jsonContent = @"{
                    ""Version"": ""1.2.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""nodejs"",
                            ""Version"": ""22.0.0"",
                            ""AppFileName"": ""node.exe"",
                            ""WebDownloadFile"": ""https://example.com/node.zip"",
                            ""DownloadedFile"": ""node.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\apps\\nodejs"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        },
                        {
                            ""Name"": ""python"",
                            ""Version"": ""3.14.0"",
                            ""AppFileName"": ""python.exe"",
                            ""WebDownloadFile"": ""https://example.com/python.zip"",
                            ""DownloadedFile"": ""python.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\apps\\python"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContent);

                // Act - Use a unique app name that definitely doesn't exist in any system-wide ntools.json
                var apps = Command.GetAppsFromCurrentDirectory("zzz-test-nonexistent-app-xyz", null, out var availableApps);

                // Assert
                Assert.AreEqual(0, apps.Count, "Should find no apps");
                Assert.IsTrue(availableApps.Count >= 2, "Should return list of available apps from current directory (may include program files apps)");
                Assert.IsTrue(availableApps.Any(x => x.Contains("nodejs")), "Available apps should contain nodejs");
                Assert.IsTrue(availableApps.Any(x => x.Contains("python")), "Available apps should contain python");
                Assert.IsTrue(availableApps.Any(x => x.Contains("22.0.0")), "Available apps should contain nodejs version");
                Assert.IsTrue(availableApps.Any(x => x.Contains("3.14.0")), "Available apps should contain python version");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod()]
        public void Install_WithNameNotFound_DisplaysAvailableApps()
        {
            // Skip test if not running in admin mode
            if (!CurrentProcess.IsElevated())
            {
                Assert.Inconclusive("Test skipped because it requires admin privileges.");
            }

            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var originalDir = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Create test JSON file
                var jsonContent = @"{
                    ""Version"": ""1.2.0"",
                    ""NbuildAppList"": [
                        {
                            ""Name"": ""nodejs"",
                            ""Version"": ""22.0.0"",
                            ""AppFileName"": ""node.exe"",
                            ""WebDownloadFile"": ""https://example.com/node.zip"",
                            ""DownloadedFile"": ""node.zip"",
                            ""InstallCommand"": ""echo"",
                            ""InstallArgs"": ""installed"",
                            ""InstallPath"": ""C:\\apps\\nodejs"",
                            ""UninstallCommand"": ""echo"",
                            ""UninstallArgs"": ""uninstalled""
                        }
                    ]
                }";
                File.WriteAllText("apps.json", jsonContent);

                // Act - Use dryRun=false to trigger the actual logic
                var result = Command.Install(null, "nonexistent-app", null, false, false);

                // Assert
                Assert.IsFalse(result.IsSuccess());
                var errorOutput = result.GetFirstOutput();
                Assert.IsTrue(errorOutput.Contains("No apps found matching 'nonexistent-app'"));
                Assert.IsTrue(errorOutput.Contains("Available applications"));
                Assert.IsTrue(errorOutput.Contains("nodejs"));
                Assert.IsTrue(errorOutput.Contains("22.0.0"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
                Directory.Delete(tempDir, true);
            }
        }
    }
}

