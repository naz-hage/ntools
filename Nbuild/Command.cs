using Ntools;
using NbuildTasks;
using OutputColorizer;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            if (!TestMode || Ntools.CurrentProcess.IsElevated())

            {
                DownloadsDirectory = "C:\\NToolsDownloads";
            }
            else
            {
                DownloadsDirectory = $"{Environment.GetEnvironmentVariable("Temp")}\\nb";
            }

            if (!Directory.Exists(DownloadsDirectory)) Directory.CreateDirectory(DownloadsDirectory); 
        }

        private static bool IsTestMode()
        {
            // Check if running in GitHub Actions
            var githubActions = Environment.GetEnvironmentVariable("LOCAL_TEST", EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(githubActions) && githubActions.Equals("true", StringComparison.CurrentCultureIgnoreCase))
            {
                return true; // Running in GitHub Actions, in test mode
            }

            // defaut is not in test mode
            return false;
        }

        private static bool CanRunCommand(bool modifyAcls=true)
        {
            if (!Ntools.CurrentProcess.IsElevated())
            {
                if (!TestMode)
                {
                    return false;
                }
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && modifyAcls)
                {
                    var folder = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}";
                    var process = new Process
                    {
                        StartInfo =
                        {
                            WorkingDirectory = Environment.CurrentDirectory,
                            FileName = $"{folder}\\icacls.exe",
                            Arguments = $"{DownloadsDirectory} /grant Administrators:(OI)(CI)F /inheritance:r",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = false,
                            UseShellExecute = false
                        }
                    };
                    // update ACL on DownloadsDirectory
                    var resultInstall = process.LockStart(Verbose);
                    if (resultInstall.IsSuccess())
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {DownloadsDirectory} ACL updated.]");
                    }
                    else
                    {

                        Colorizer.WriteLine($"[{ConsoleColor.Red}!X {DownloadsDirectory} ACL failed to update: {resultInstall.Output[0]}]");
                        return false;
                    }
                }
            }

            if (!Directory.Exists(DownloadsDirectory)) Directory.CreateDirectory(DownloadsDirectory);

            // all good caller allowed to run this command
            return true;
        }

        public static ResultHelper Install(string json, bool verbose = false)
        {
            Verbose = verbose;
            // check if caller is admin or in test mode
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            try
            {
                var appDataList = NbuildApp.GetApps(json);
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
            catch (Exception ex)
            {
                Console.WriteLine();
                return ResultHelper.Fail(-1, $"Invalid json input: {ex.Message}");

            }
        }

        public static ResultHelper List(string json, bool verbose = false)
        {
            Verbose = verbose;
            try
            {
                var appDataList = NbuildApp.GetApps(json);
                if (appDataList == null) return ResultHelper.Fail(-1, $"Invalid json input");

                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appDataList.Count()} apps to list:]");

                // print header
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!|--------------------|----------------|-------------------|]");
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!| App name           | Target version | Installed version |]");
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!|--------------------|----------------|-------------------|]");
                foreach (var appData in appDataList)
                {
                    // display app and installed version
                    // InstalledAppFileVersionGreterOrEqual is true, print green, else print red
                    if (InstalledAppFileVersionGreterOrEqual(appData))
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}!| {appData.Name,-18} | {appData.Version,-14} | {GetNbuildAppFileVersion(appData), -18}|]");
                    }
                    else
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}!| {appData.Name,-18} | {appData.Version,-14} | {GetNbuildAppFileVersion(appData),-18}|]");
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

        public static ResultHelper Download(string json, bool verbose = false)
        {
            Verbose = verbose;
            // check if admin
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            var appDataList = NbuildApp.GetApps(json);
            if (appDataList == null) return ResultHelper.Fail(-1, $"Invalid json input");

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{appDataList.ToList().Count} apps to download to {DownloadsDirectory}]");

            // print header
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! |--------------------|--------------------------------|-----------------|]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! | App name           | Downloaded File                | (hh:mm:ss.ff)   |]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! |--------------------|--------------------------------|-----------------|]");
            foreach (var appData in appDataList)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                // download app
                var result = DownloadApp(appData);

                stopWatch.Stop();

                if (result.IsSuccess())
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Green}! | {appData.Name,-18} | {appData.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|]");
                }
                else
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! Failed to download {appData.WebDownloadFile} to {appData.DownloadedFile}. {result.GetFirstOutput()}]");
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! | {appData.Name,-18} | {appData.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|]");
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
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

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
                var process = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = DownloadsDirectory,
                        FileName = $"{DownloadsDirectory}\\{appData.InstallCommand}",
                        Arguments = appData.InstallArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                // Update the filename to the full path of executable in the PATH environment variable
                process.StartInfo.FileName = FileMappins.GetFullPathOfFile(process.StartInfo.FileName);

                Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Installing {appData.Name} {appData.Version}]");
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Working Directory: {process.StartInfo.WorkingDirectory}]");
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! FileName: {process.StartInfo.FileName}]");
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Arguments: {process.StartInfo.Arguments}]");
                // Install the downloaded file
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Calling process.LockStart(Verbose)]");
                var resultInstall = process.LockStart(Verbose);
                if (resultInstall.IsSuccess())

                {
                    // Check if the app was installed successfully
                    if (InstalledAppFileVersionGreterOrEqual(appData))
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {appData.Name} {appData.Version} installed.]");
                        return ResultHelper.Success();
                    }
                    else
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}!X {appData.Name} {appData.Version} failed to install]");
                        return ResultHelper.Fail(-1, $"Failed to install {appData.Name} {appData.Version}");
                    }
                }
                else
                {

                    //Colorizer.WriteLine($"[{ConsoleColor.Red}!X {appData.Name} {appData.Version} failed to install: {resultInstall.GetFirstOutput()}]");
                    Colorizer.WriteLine($"[{ConsoleColor.Red}!X {appData.Name} {appData.Version} failed to install: {process.ExitCode}]");
                    return ResultHelper.Fail(process.ExitCode, $"Failed to install {appData.Name} {appData.Version}");
                }
               
                //return resultInstall;
            }
            else
            {
                return ResultHelper.Fail(-1, $"Failed to download {appData.WebDownloadFile} to {appData.DownloadedFile}. {result.GetFirstOutput()}");
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
    }
}
