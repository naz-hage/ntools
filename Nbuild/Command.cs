﻿using Launcher;
using NbuildTasks;
using OutputColorizer;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace Nbuild
{
    public static class Command
    {
        private static readonly string DownloadsDirectory = $"{Environment.GetEnvironmentVariable("Temp")}\\nb"; // "C:\\NToolsDownloads" $"{Environment.GetEnvironmentVariable("Temp")}\\nb"
        private static bool Verbose = false;

        public static bool TestMode
        {
            get { return _testMode; }
            set
            {
                _testMode = IsTestMode() ? value : throw new InvalidOperationException("TestMode can only be set in test mode.");
            }
        }

        private static bool _testMode = IsTestMode();

        static Command()
        {
            // Examine this method when we implement the logic to require admin
            //PrepareDonloadsDirectory();
            if (!Directory.Exists(DownloadsDirectory))
            {
                Directory.CreateDirectory(DownloadsDirectory);
            }

        }

        private static bool IsTestMode()
        {
            // Add your logic here to determine if the application is running in test mode
            // For example, you can check an environment variable or a configuration setting
            // Return true if the application is running in test mode, false otherwise
            // return true until we implement the logic
            return true;
        }

        public static ResultHelper Install(string? json)
        {
            // check if admin
            if (!Launcher.CurrentProcess.IsElevated())
            {
                if (!TestMode)
                {
                    return ResultHelper.Fail(-1, $"You must run this command as an administrator");
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                return ResultHelper.Fail(-1, $"json cannot be null");
            }

            if (File.Exists(json))
            {
                json = File.ReadAllText(json);
            }

            try
            {
                if (json.Contains("NbuildAppList"))
                {
                    var appDataList = NbuildApp.FromMultiJson(json);
                    if (appDataList == null) return ResultHelper.Fail(-1, $"Invalid json input");

                    if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appDataList.Count()} apps to install.]");

                    foreach (var appData in appDataList)
                    {


                        var result = Install(appData);
                        if (!result.IsSuccess())
                        {
                            return result;
                        }
                    }

                    Console.WriteLine();
                    // all apps installed successfully
                    return ResultHelper.Success();
                }
                else
                {
                    var appData = NbuildApp.FromJson(json);

                    return Install(appData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                return ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");

            }
        }

        public static ResultHelper List(string? json)
        {
            try
            {
                var appDataList = NbuildApp.FromMultiJson(json);
                if (appDataList == null) return ResultHelper.Fail(-1, $"Invalid json input");

                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appDataList.Count()} apps to list.]");

                // print header
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}! App               | Target Version | Installed version]");
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}! ------------------|----------------|------------------]");
                foreach (var appData in appDataList)
                {
                    // display app and installed version
                    // InstalledAppFileVersionGreterOrEqual is true, print green, else print red
                    if (InstalledAppFileVersionGreterOrEqual(appData))
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}! {appData.Name,-17} | {appData.Version,-14} | {GetNbuildAppFileVersion(appData)}]");
                    }
                    else
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}! {appData.Name,-17} | {appData.Version,-14} | {GetNbuildAppFileVersion(appData)}]");
                    }
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                return ResultHelper.Fail(-1, ex.Message);
            }

            return ResultHelper.Success();
        }

        public static ResultHelper Download(string? json)
        {
            // check if admin
            if (!Launcher.CurrentProcess.IsElevated())
            {
                if (!Command.TestMode)
                {
                    return ResultHelper.Fail(-1, $"You must run this command as an administrator");
                }
            }
            var appDataList = NbuildApp.FromMultiJson(json);
            if (appDataList == null) return ResultHelper.Fail(-1, $"Invalid json input");

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appDataList.Count()} apps to download to {DownloadsDirectory}]");

            // print header
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! App                | Download File                  |]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! -------------------|--------------------------------|]");
            foreach (var appData in appDataList)
            {
                // download app
                var result = DownloadApp(appData);

                if (result.IsSuccess())
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Green}! {appData.Name,-18} | {appData.DownloadedFile,-30} |]");
                }
                else
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! Failed to download {appData.WebDownloadFile} to {appData.DownloadedFile}. {result.Output[0]}]");
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! {appData.Name,-18} | {appData.DownloadedFile,-30} |]");
                }
            }

            Console.WriteLine();

            return ResultHelper.Success();
        }

        private static ResultHelper DownloadApp(NbuildApp nbuildApp)
        {
            if (nbuildApp == null || string.IsNullOrEmpty(nbuildApp.WebDownloadFile))
            {
                return ResultHelper.Fail(-1, $"WebDownloadFile is invalid");
            }

            var fileName = $"{DownloadsDirectory}\\{nbuildApp.DownloadedFile}";
            var httpClient = new HttpClient();
            var result = Task.Run(async () => await httpClient.DownloadFileAsync(new Uri(nbuildApp.WebDownloadFile), fileName)).Result;

            return result;
        }

        private static ResultHelper Install(NbuildApp appData)
        {
            //PrepareDonloadsDirectory();

            if (string.IsNullOrEmpty(appData.DownloadedFile) ||
                string.IsNullOrEmpty(appData.WebDownloadFile) ||
                string.IsNullOrEmpty(appData.Version) ||
                string.IsNullOrEmpty(appData.Name) ||
                string.IsNullOrEmpty(appData.InstallCommand) ||
                string.IsNullOrEmpty(appData.InstallArgs) ||
                string.IsNullOrEmpty(appData.InstallPath) ||
                string.IsNullOrEmpty(appData.AppFileName)
                )
            {
                return ResultHelper.Fail(-1, $"Invalid json input");
            }

            if (!Directory.Exists(DownloadsDirectory)) Directory.CreateDirectory(DownloadsDirectory);

            if (InstalledAppFileVersionGreterOrEqual(appData))
            {
                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {appData.Name} {appData.Version} already installed.]");
                return ResultHelper.Success();
            }

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Downloading {appData.Name} {appData.Version}]");
            var result = DownloadApp(appData);

            if (result.IsSuccess())
            {
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appData.Name} {appData.Version} downloaded.]");

                // Install the downloaded file
                var parameters = new Launcher.Parameters
                {
                    FileName = appData.InstallCommand,
                    Arguments = appData.InstallArgs,
                    WorkingDir = DownloadsDirectory
                };

                Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Installing {appData.Name} {appData.Version}]");
                var resultInstall = Launcher.Launcher.Start(parameters);

                if (resultInstall.IsSuccess())
                {
                    // Check if the app was installed successfully
                    if (InstalledAppFileVersionGreterOrEqual(appData))
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {appData.Name} {appData.Version} installed.]");
                    }
                    else
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}!X {appData.Name} {appData.Version} failed to install]");
                    }
                    //resultInstall = Update(appData);
                }
                else
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}!X {appData.Name} {appData.Version} failed to install: {resultInstall.Output[0]}]");
                }

                return resultInstall;
            }
            else
            {
                return ResultHelper.Fail(-1, $"Failed to download {appData.WebDownloadFile} to {appData.DownloadedFile}. {result.Output[0]}");
            }
        }

        private static string? GetNbuildAppFileVersion(NbuildApp nbuildApp)
        {
            var appFile = $"{nbuildApp.InstallPath}\\{nbuildApp.AppFileName}";
            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(appFile);

                // return file Version as combined string of all parts : major, minor, build, patch
                return fileVersionInfo != null
                    ? $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}.{fileVersionInfo.FilePrivatePart}"
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool InstalledAppFileVersionGreterOrEqual(NbuildApp nbuildApp)
        {
            var currentVersion = GetNbuildAppFileVersion(nbuildApp);
            if (currentVersion == null)
            {
                return false;
            }
            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{nbuildApp.Name} {nbuildApp.Version} current version: {currentVersion}]");


            var result = Version.TryParse(currentVersion, out Version? currentVersionParsed);
            if (!result)
            {
                return false;
            }

            result = Version.TryParse(nbuildApp.Version, out Version? versionParsed);
            if (!result)
            {
                return false;
            }

            //if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{nbuildApp.Name} current version: {currentVersion} >=  {nbuildApp.Version}: {currentVersionParsed >= versionParsed}]");
            return currentVersionParsed >= versionParsed;
        }

        // Examine if this method is needed
        private static ResultHelper Update(NbuildApp nbuildApp)
        {
            var currentVersion = GetNbuildAppFileVersion(nbuildApp);
            if (currentVersion == null)
            {
                return ResultHelper.Fail(-1, $"Failed to get current version of {nbuildApp.Name}");
            }

            if (currentVersion == nbuildApp.Version)
            {
                return ResultHelper.Success();
            }

            var result = Install(nbuildApp);
            if (result.IsSuccess())
            {
                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {nbuildApp.Name} {nbuildApp.Version} updated.]");
            }
            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!X {nbuildApp.Name} {nbuildApp.Version} failed to update: {result.Output[0]}]");
            }

            return result;
        }

        private static void PrepareDonloadsDirectory()
        {

            if (!Directory.Exists(DownloadsDirectory))
            {
                Directory.CreateDirectory(DownloadsDirectory);
            }

            // Perform a runtime check for the Windows platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set access control for the download directory to administrators only
                var directorySecurity = new DirectorySecurity();
                directorySecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));

                var directoryInfo = new DirectoryInfo(DownloadsDirectory);
                directoryInfo.SetAccessControl(directorySecurity);
            }
        }
    }
}
