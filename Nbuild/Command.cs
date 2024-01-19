using Launcher;
using NbuildTasks;
using OutputColorizer;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace Nbuild
{
    public static class Command
    {
        private static readonly string DownloadsDirectory = $"{Environment.GetEnvironmentVariable("Temp")}\\nb"; // "C:\\NToolsDownloads";
        private static bool Verbose = false;

        public static ResultHelper Install(string? json)
        {
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
                        if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appData.Name} {appData.Version} to install.]");

                        var result = Install(appData);
                        if (!result.IsSuccess())
                        {
                            return result;
                        }
                    }
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
                return ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");
            }
        }

        public static ResultHelper List (string? json)
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

            return ResultHelper.Success();
        }

        private static string? Download (NbuildApp nbuildApp)
        {
            if (nbuildApp == null || string.IsNullOrEmpty(nbuildApp.WebDownloadFile))
            {
                return null;
            }

            var fileName = $"{DownloadsDirectory}\\{nbuildApp.DownloadedFile}";
            var httpClient = new HttpClient();
            var result = Task.Run(async () => await httpClient.DownloadFileAsync(new Uri(nbuildApp.WebDownloadFile), fileName)).Result;

            if (result.IsSuccess() && File.Exists(fileName))
            {
                return fileName;
            }
            else
            {
                return null;
            }
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

            var fileName = Download(appData);

            if (fileName != null)
            {
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appData.Name} {appData.Version} downloaded.]");

                // Install the downloaded file
                var parameters = new Launcher.Parameters
                {
                    FileName = appData.InstallCommand,
                    Arguments = appData.InstallArgs,
                    WorkingDir = DownloadsDirectory
                };

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
                return ResultHelper.Fail(-1, $"Failed to download {appData.WebDownloadFile} to {appData.DownloadedFile}");
            }
        }

        private static string? GetNbuildAppFileVersion(NbuildApp nbuildApp)
        {
            var appFile = $"{nbuildApp.InstallPath}\\{nbuildApp.AppFileName}";
            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(appFile);
                return fileVersionInfo.FileVersion;
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
