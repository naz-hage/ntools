using CommandLine;
using NbuildTasks;
using Ntools;
using OutputColorizer;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Nbuild
{
    public static class Command
    {
        private static readonly string DownloadsDirectory = $"{Environment.GetEnvironmentVariable("Temp")}\\nb"; // "C:\\NToolsDownloads" $"{Environment.GetEnvironmentVariable("Temp")}\\nb"
        private static bool Verbose = false;
        private static bool ValidJson = false;

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
            DownloadsDirectory = !TestMode || Ntools.CurrentProcess.IsElevated() ? "C:\\NToolsDownloads" : $"{Environment.GetEnvironmentVariable("Temp")}\\nb";

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

        private static bool CanRunCommand(bool modifyAcls = true)
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
                if (!UpdateDownloadsDirectoryAcls(modifyAcls)) return false;
            }

            if (!Directory.Exists(DownloadsDirectory)) Directory.CreateDirectory(DownloadsDirectory);

            // all good caller allowed to run this command
            return true;
        }

        private static bool UpdateDownloadsDirectoryAcls(bool modifyAcls)
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
                    return true;
                }
                else
                {

                    Colorizer.WriteLine($"[{ConsoleColor.Red}!X {DownloadsDirectory} ACL failed to update: {resultInstall.Output[0]}]");
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static ResultHelper Install(string? json, bool verbose = false)
        {
            Verbose = verbose;
            ResultHelper result = ResultHelper.New();
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            var apps = GetApps(json);
            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{apps.Count()} apps to install.]");

            foreach (var app in apps)
            {
                result = Install(app);
                if (!result.IsSuccess())
                {
                    break;
                }
            }

            return result;
        }

        public static ResultHelper Uninstall(string? json, bool verbose = false)
        {
            Verbose = verbose;
            ResultHelper result = ResultHelper.New();
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            var apps = GetApps(json);
            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{apps.Count()} apps to Uninstall.]");

            foreach (var app in apps)
            {
                result = Uninstall(app);
                if (!result.IsSuccess())
                {
                    break;
                }
            }

            return result;
        }
        public static ResultHelper List(string? json, bool verbose = false)
        {
            Verbose = verbose;

            var apps = GetApps(json);

            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{apps.Count()} apps to list:]");

            // print header
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!|--------------------|----------------|-------------------|]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!| App name           | Target version | Installed version |]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!|--------------------|----------------|-------------------|]");
            foreach (var app in apps)
            {
                // display app and installed version
                // InstalledAppFileVersionGreterOrEqual is true, print green, else print red
                if (InstalledAppFileVersionGreaterOrEqual(app))
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Green}!| {app.Name,-18} | {app.Version,-14} | {GetNbuildAppFileVersion(app),-18}|]");
                }
                else
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}!| {app.Name,-18} | {app.Version,-14} | {GetNbuildAppFileVersion(app),-18}|]");
                }
            }

            Console.WriteLine();
            return ResultHelper.Success();
        }

        public static ResultHelper Download(string? json, bool verbose = false)
        {
            Verbose = verbose;
            
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            var apps = GetApps(json);

            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{apps.ToList().Count} apps to download to {DownloadsDirectory}]");

            // print header
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! |--------------------|--------------------------------|-----------------|]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! | App name           | Downloaded File                | (hh:mm:ss.ff)   |]");
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! |--------------------|--------------------------------|-----------------|]");
            foreach (var app in apps)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                // download app
                var result = DownloadApp(app);

                stopWatch.Stop();

                if (result.IsSuccess())
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Green}! | {app.Name,-18} | {app.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|]");
                }
                else
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! Failed to download {app.WebDownloadFile} to {app.DownloadedFile}. {result.GetFirstOutput()}]");
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! | {app.Name,-18} | {app.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|]");
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

        private static ResultHelper Install(NbuildApp nbuildApp)
        {
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            if (string.IsNullOrEmpty(nbuildApp.DownloadedFile) ||
                string.IsNullOrEmpty(nbuildApp.WebDownloadFile) ||
                string.IsNullOrEmpty(nbuildApp.Version) ||
                string.IsNullOrEmpty(nbuildApp.Name) ||
                string.IsNullOrEmpty(nbuildApp.InstallCommand) ||
                string.IsNullOrEmpty(nbuildApp.InstallArgs) ||
                string.IsNullOrEmpty(nbuildApp.InstallPath) ||
                string.IsNullOrEmpty(nbuildApp.AppFileName)
                )
            {
                return ResultHelper.Fail(-1, $"Invalid json input");
            }

            if (!Directory.Exists(DownloadsDirectory)) Directory.CreateDirectory(DownloadsDirectory);

            if (InstalledAppFileVersionGreaterOrEqual(nbuildApp))
            {
                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {nbuildApp.Name} {nbuildApp.Version} already installed.]");
                return ResultHelper.Success();
            }

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Downloading {nbuildApp.Name} {nbuildApp.Version}]");
            var result = DownloadApp(nbuildApp);

            if (result.IsSuccess())
            {
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{nbuildApp.Name} {nbuildApp.Version} downloaded.]");

                // Install the downloaded file
                var process = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = DownloadsDirectory,
                        FileName = $"{nbuildApp.InstallCommand}",
                        Arguments = nbuildApp.InstallArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                // Update the filename to the full path of executable in the PATH environment variable
                process.StartInfo.FileName = FileMappins.GetFullPathOfFile(process.StartInfo.FileName);

                Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Installing {nbuildApp.Name} {nbuildApp.Version}]");
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Working Directory: {process.StartInfo.WorkingDirectory}]");
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! FileName: {process.StartInfo.FileName}]");
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Arguments: {process.StartInfo.Arguments}]");
                // Install the downloaded file
                if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Calling process.LockStart(Verbose)]");
                var resultInstall = process.LockStart(Verbose);
                if (resultInstall.IsSuccess())

                {
                    // Check if the app was installed successfully
                    if (InstalledAppFileVersionGreaterOrEqual(nbuildApp))
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {nbuildApp.Name} {nbuildApp.Version} installed.]");
                        return ResultHelper.Success();
                    }
                    else
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}!X {nbuildApp.Name} {nbuildApp.Version} failed to install]");
                        return ResultHelper.Fail(-1, $"Failed to install {nbuildApp.Name} {nbuildApp.Version}");
                    }
                }
                else
                {

                    //Colorizer.WriteLine($"[{ConsoleColor.Red}!X {appData.Name} {appData.Version} failed to install: {resultInstall.GetFirstOutput()}]");
                    Colorizer.WriteLine($"[{ConsoleColor.Red}!X {nbuildApp.Name} {nbuildApp.Version} failed to install: {process.ExitCode}]");
                    return ResultHelper.Fail(process.ExitCode, $"Failed to install {nbuildApp.Name} {nbuildApp.Version}");
                }

                //return resultInstall;
            }
            else
            {
                return ResultHelper.Fail(-1, $"Failed to download {nbuildApp.WebDownloadFile} to {nbuildApp.DownloadedFile}. {result.GetFirstOutput()}");
            }
        }


        private static ResultHelper Uninstall(NbuildApp nbuildApp)
        {
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            if (string.IsNullOrEmpty(nbuildApp.DownloadedFile) ||
                string.IsNullOrEmpty(nbuildApp.WebDownloadFile) ||
                string.IsNullOrEmpty(nbuildApp.Version) ||
                string.IsNullOrEmpty(nbuildApp.Name) ||
                string.IsNullOrEmpty(nbuildApp.InstallCommand) ||
                string.IsNullOrEmpty(nbuildApp.InstallArgs) ||
                string.IsNullOrEmpty(nbuildApp.UninstallCommand) ||
                string.IsNullOrEmpty(nbuildApp.UninstallArgs) ||
                string.IsNullOrEmpty(nbuildApp.InstallPath) ||
                string.IsNullOrEmpty(nbuildApp.AppFileName)
                )
            {
                return ResultHelper.Fail(-1, $"Invalid json input");
            }

            // Uninstall the app
            var process = new Process
            {
                StartInfo =
                    {
                        WorkingDirectory = DownloadsDirectory,
                        FileName = $"{nbuildApp.UninstallCommand}",
                        Arguments = nbuildApp.UninstallArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
            };

            // Update the filename to the full path of executable in the PATH environment variable
            process.StartInfo.FileName = FileMappins.GetFullPathOfFile(process.StartInfo.FileName);

            Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Uninstalling {nbuildApp.Name} {nbuildApp.Version}]");
            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Working Directory: {process.StartInfo.WorkingDirectory}]");
            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! FileName: {process.StartInfo.FileName}]");
            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Arguments: {process.StartInfo.Arguments}]");

            if (Verbose) Colorizer.WriteLine($"[{ConsoleColor.Yellow}! Calling process.LockStart(Verbose)]");
            var resultInstall = process.LockStart(Verbose);
            if (resultInstall.IsSuccess())

            {
                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {nbuildApp.Name} {nbuildApp.Version} Uninstalled.]");
                return ResultHelper.Success();
            }
            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!X {nbuildApp.Name} {nbuildApp.Version} failed to Uninstall: {process.ExitCode}]");
                return ResultHelper.Fail(process.ExitCode, $"Failed to Uninstall {nbuildApp.Name} {nbuildApp.Version}");
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

        private static bool InstalledAppFileVersionGreaterOrEqual(NbuildApp nbuildApp)
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

        public static IEnumerable<NbuildApp> GetApps(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            // check if json is a file path
            if (File.Exists(json))
            {
                json = File.ReadAllText(json);

                if (string.IsNullOrEmpty(json))
                {
                    throw new ArgumentNullException(nameof(json));
                }
            }

            var listAppData = JsonSerializer.Deserialize<NbuildApps>(json) ?? throw new ParserException("Failed to parse json to list of objects", null);

            // make sure version matches 1.2.0
            if (listAppData.Version != "1.2.0")
            {
                throw new ParserException($"Version {listAppData.Version} is not supported. Please use version 1.2.0", null);
            }

            foreach (var appData in listAppData.NbuildAppList)
            {
                Validate(appData);
                UpdateEnvironmentVariables(appData);
                yield return appData;
            }
        }

        private static void Validate(NbuildApp nbuildApp)
        {
            if (string.IsNullOrEmpty(nbuildApp.Name))
            {
                throw new ParserException("Name is required", null);
            }
            if (string.IsNullOrEmpty(nbuildApp.WebDownloadFile))
            {
                throw new ParserException("WebDownloadFile is required", null);
            }
            if (string.IsNullOrEmpty(nbuildApp.DownloadedFile))
            {
                throw new ParserException("DownloadedFile is required", null);
            }
            if (string.IsNullOrEmpty(nbuildApp.InstallCommand))
            {
                throw new ParserException("InstallCommand is required", null);
            }
            if (string.IsNullOrEmpty(nbuildApp.InstallArgs))
            {
                throw new ParserException("InstallArgs is required", null);
            }
            if (string.IsNullOrEmpty(nbuildApp.Version))
            {
                throw new ParserException("Version is required", null);
            }
            if (string.IsNullOrEmpty(nbuildApp.InstallPath))
            {
                throw new ParserException("InstallPath is required", null);
            }

            // Perform validation
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(nbuildApp);
            bool isValid = Validator.TryValidateObject(nbuildApp, validationContext, validationResults, true);

            if (!isValid)
            {
                var sb = new StringBuilder();
                foreach (var validationResult in validationResults)
                {
                    sb.Append($"{validationResult.ErrorMessage} ");
                }
                throw new ParserException(sb.ToString(), null);
            }
            ValidJson = true;
            
        }

        // Update variables with $(...).  This should be called after validation of the appData
        private static void UpdateEnvironmentVariables(NbuildApp nbuildApp)
        {
            if (!ValidJson) throw new InvalidOperationException("Json is not valid");

            // Update variables with $(...)
            nbuildApp.WebDownloadFile = nbuildApp.WebDownloadFile!
                .Replace("$(Version)", nbuildApp.Version)
                .Replace("$(AppFileName)", nbuildApp.AppFileName);

            nbuildApp.DownloadedFile = nbuildApp.DownloadedFile!
                .Replace("$(Version)", nbuildApp.Version)
                .Replace("$(AppFileName)", nbuildApp.AppFileName);

            nbuildApp.InstallCommand = nbuildApp.InstallCommand!
                .Replace("$(Version)", nbuildApp.Version)
                .Replace("$(DownloadedFile)", nbuildApp.DownloadedFile);

            nbuildApp.UninstallCommand = nbuildApp.UninstallCommand!
                .Replace("$(Version)", nbuildApp.Version)
                .Replace("$(DownloadedFile)", nbuildApp.DownloadedFile);

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            nbuildApp.InstallArgs = nbuildApp
                .InstallArgs!.Replace("$(Version)", nbuildApp.Version)
                .Replace("$(InstallPath)", nbuildApp.InstallPath)
                .Replace("$(AppFileName)", nbuildApp.AppFileName)
                .Replace("$(DownloadedFile)", nbuildApp.DownloadedFile)
                .Replace("$(ProgramFiles)", programFiles)
                .Replace("$(ProgramFilesX86)", programFilesX86);

            nbuildApp.UninstallArgs = nbuildApp
                .UninstallArgs!.Replace("$(Version)", nbuildApp.Version)
                .Replace("$(InstallPath)", nbuildApp.InstallPath)
                .Replace("$(AppFileName)", nbuildApp.AppFileName)
                .Replace("$(DownloadedFile)", nbuildApp.DownloadedFile)
                .Replace("$(ProgramFiles)", programFiles)
                .Replace("$(ProgramFilesX86)", programFilesX86);

            nbuildApp.InstallPath = nbuildApp.InstallPath!
                .Replace("$(Version)", nbuildApp.Version)
                .Replace("$(ProgramFiles)", programFiles)
                .Replace("$(ProgramFilesX86)", programFilesX86);

            nbuildApp.UninstallCommand = nbuildApp.UninstallCommand!
                .Replace("$(InstallPath)", nbuildApp.InstallPath);

            if (!Path.IsPathRooted(nbuildApp.InstallPath)) throw new ParserException($"App: {nbuildApp.Name}, InstallPath {nbuildApp.InstallPath} must be rooted. i.e. C:\\Program Files\\Nbuild", null);
        }
    }
}
