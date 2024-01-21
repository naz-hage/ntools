using Launcher;
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
        private const string NbuildAssemblyName = "Nb.dll";
        private const string NbuildAppListJsonFile = "NbuildAppListTest.json";
        private readonly string ResourceLocation = "Nbuild.resources.NbuildAppListTest.json";

        [TestMethod()]
        public void DownloadTest()
        {
            // Arrange with GITHUB_ACTIONS environment variable set to true
            //var parameters = new Launcher.Parameters
            //{
            //    FileName = "setx",
            //    Arguments = "GITHUB_ACTIONS true",
            //    WorkingDir = Environment.CurrentDirectory,
            //    Verbose = true
            //};
            //var resultInstall = Launcher.Launcher.Start(parameters);
            //Assert.IsTrue(resultInstall.IsSuccess());

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
            //parameters = new Launcher.Parameters
            //{
            //    //FileName = "setx",
            //    FileName = "REG",
            //    //Arguments = "GITHUB_ACTIONS \"\"",
            //    Arguments = "delete HKCU\\Environment /F /V GITHUB_ACTIONS",
            //    WorkingDir = Environment.CurrentDirectory,
            //    Verbose = true
            //};
            //resultInstall = Launcher.Launcher.Start(parameters);
            //Assert.IsTrue(resultInstall.IsSuccess());
        }

        [TestMethod()]
        public void InstallFromJsonFileTest()
        {
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
                Console.WriteLine(result.Output[0]);
            }

            var result2 = result.IsSuccess();

            // Assert
            Assert.IsTrue(result2);
        }

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
                Console.WriteLine(result.Output[0]);
            }

            var result2 = result.IsSuccess();

            // Assert
            Assert.IsTrue(result2);
        }

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
            Assert.AreEqual("Invalid json input: Name is missing or empty", result.Output[0]);
        }

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
            Assert.AreEqual(result.Output[0], "Invalid json input: AppFileName is missing or empty");
        }

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
            Assert.AreEqual(result.Output[0], "Invalid json input: WebDownloadFile is missing or empty");
        }

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
            Assert.AreEqual(result.Output[0], "Invalid json input: DownloadedFile is missing or empty");
        }

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
            Assert.AreEqual(result.Output[0], "Invalid json input: InstallCommand is missing or empty");
        }

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
            Assert.AreEqual(result.Output[0], "Invalid json input: InstallArgs is missing or empty");
        }

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
            Assert.AreEqual(result.Output[0], "Invalid json input: InstallPath is missing or empty");
        }
    }
}