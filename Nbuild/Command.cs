﻿using CommandLine;
using GitHubRelease;
using NbuildTasks;
using Ntools;
using OutputColorizer;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nbuild
{
    public static class Command
    {
        private const string SupportedVersion = "1.2.0";
        private const int MsiReturnCodeRestartRequired = 1603;
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

            // default is not in test mode
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
                    ConsoleHelper.WriteLine($"√ {DownloadsDirectory} ACL updated.", ConsoleColor.Green);
                    return true;
                }
                else
                {

                    ConsoleHelper.WriteLine($"X {DownloadsDirectory} ACL failed to update: {resultInstall.Output[0]}", ConsoleColor.Red);
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

            if (Verbose) ConsoleHelper.WriteLine($"{apps.Count()} apps to install.", ConsoleColor.Yellow);

            foreach (var app in apps)
            {
                result = Install(app, verbose);
                if (!result.IsSuccess())
                {
                    break;
                }

                // Print the stored hash of the app file name
                if (!string.IsNullOrEmpty(app.StoredHash))
                {
                    ConsoleHelper.WriteLine($"Stored hash for {app.AppFileName}: {app.StoredHash}", ConsoleColor.Yellow);
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

            if (Verbose) ConsoleHelper.WriteLine($"{apps.Count()} apps to Uninstall.", ConsoleColor.Yellow);

            foreach (var app in apps)
            {
                result = Uninstall(app);
                if (!result.IsSuccess())
                {
                    // display error message and continue to next app
                    ConsoleHelper.WriteLine($"{result.GetFirstOutput()}", ConsoleColor.Red);
                }
            }

            return result;
        }
        
        public static ResultHelper List(string? json, bool verbose = false)
        {
            Verbose = verbose;

            var apps = GetApps(json);

            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            ConsoleHelper.WriteLine($"{apps.Count()} apps to list:", ConsoleColor.Yellow);

            // print header
            ConsoleHelper.WriteLine("|--------------------|----------------|-------------------|", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine("| App name           | Target version | Installed version |", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine("|--------------------|----------------|-------------------|", ConsoleColor.Yellow);
            foreach (var app in apps)
            {
                // display app and installed version
                // InstalledAppFileVersionGreterOrEqual is true, print green, else print red
                if (IsAppVersionEqual(app))
                {
                    ConsoleHelper.WriteLine($"| {app.Name,-18} | {app.Version,-14} | {GetAppFileVersion(app),-18}|", ConsoleColor.Green);
                }
                else if (IsAppVersionGreaterOrEqual(app))
                {
                    ConsoleHelper.WriteLine($"| {app.Name,-18} | {app.Version,-14} | {GetAppFileVersion(app),-18}|", ConsoleColor.Cyan);
                }
                else
                {
                    ConsoleHelper.WriteLine($"| {app.Name,-18} | {app.Version,-14} | {GetAppFileVersion(app),-18}|", ConsoleColor.Red);
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

            ConsoleHelper.WriteLine($"{apps.ToList().Count} apps to download to {DownloadsDirectory}", ConsoleColor.Yellow);

            // print header
            ConsoleHelper.WriteLine(" |--------------------|--------------------------------|-----------------|", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine(" | App name           | Downloaded file                | (hh:mm:ss.ff)   |", ConsoleColor.Yellow);
            ConsoleHelper.WriteLine(" |--------------------|--------------------------------|-----------------|", ConsoleColor.Yellow);

            string webDownloadedFile = string.Empty;
            try
            {
                foreach (var app in apps)
                {
                    webDownloadedFile = app.WebDownloadFile!;

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    // download app
                    var result = DownloadApp(app);

                    stopWatch.Stop();

                    if (result.IsSuccess())
                    {
                        ConsoleHelper.WriteLine($" | {app.Name,-18} | {app.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|", ConsoleColor.Green);
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($" Failed to download {app.WebDownloadFile} to {app.DownloadedFile}", ConsoleColor.Red);
                        Console.WriteLine($"Return: {result.GetFirstOutput()}");
                        ConsoleHelper.WriteLine($" | {app.Name,-18} | {app.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|", ConsoleColor.Red);
                    }

                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to download {webDownloadedFile} to {DownloadsDirectory}. {ex.Message}";
                Console.WriteLine(errorMessage);
                return ResultHelper.Fail(-1, errorMessage);
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
            
            // *** Important **
            // Set trusted Host and extension.  This assumes that due diligence has been done to ensure the file is safe to download
            Nfile.SetTrustedHosts([new Uri(nbuildApp.WebDownloadFile).Host]);
            var extension = Path.GetExtension(new Uri(nbuildApp.WebDownloadFile).AbsolutePath);
            Nfile.SetAllowedExtensions([extension]);

            var result = Task.Run(async () => await Nfile.DownloadAsync(nbuildApp.WebDownloadFile, fileName)).Result;

            if (Verbose)
            {
                // display download file signature and size
                //result.DisplayCertificate();
                if (result.DigitallySigned)
                {
                    ConsoleHelper.WriteLine($" {fileName} is signed", ConsoleColor.Yellow);
                }
                else
                {
                    ConsoleHelper.WriteLine($" {fileName} is not signed", ConsoleColor.Yellow);
                }
            }

            return result;
        }

        private static ResultHelper Install(NbuildApp nbuildApp, bool verbose=false)
        {
            Verbose = verbose;
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

            if (IsAppVersionGreaterOrEqual(nbuildApp))
            {
                ConsoleHelper.WriteLine($"√ {nbuildApp.Name} {GetAppFileVersion(nbuildApp)} already installed.", ConsoleColor.Yellow);
                return ResultHelper.Success();
            }

            ConsoleHelper.WriteLine($" Downloading {nbuildApp.Name} {nbuildApp.Version}", ConsoleColor.Yellow);
            var result = DownloadApp(nbuildApp);

            if (result.IsSuccess())
            {
                ConsoleHelper.WriteLine($"{nbuildApp.Name} {nbuildApp.Version} downloaded.", ConsoleColor.Yellow);

                // Install the Downloaded file
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

                ConsoleHelper.WriteLine($" Installing {nbuildApp.Name} {nbuildApp.Version}", ConsoleColor.Yellow);
                if (Verbose)
                {
                    ConsoleHelper.WriteLine($" Working Directory: {process.StartInfo.WorkingDirectory}", ConsoleColor.Yellow);
                    ConsoleHelper.WriteLine($" FileName: {process.StartInfo.FileName}", ConsoleColor.Yellow);
                    ConsoleHelper.WriteLine($" Arguments: {process.StartInfo.Arguments}", ConsoleColor.Yellow);

                    ConsoleHelper.WriteLine($" Calling process.LockStart(Verbose)", ConsoleColor.Yellow);
                }

                var resultInstall = process.LockStart(Verbose);
                if (resultInstall.IsSuccess())
                {
                    if (nbuildApp.AddToPath == true)
                    {
                        AddAppInstallPathToEnvironmentPath(nbuildApp);
                    }

                    // Check if the app was installed successfully
                    return SuccessfullInstall(nbuildApp, result);
                }
                else
                {
                    
                    //ConsoleHelper.WriteLine($"X {appData.Name} {appData.Version} failed to install: {resultInstall.GetFirstOutput()}", ConsoleColor.Red);
                    ConsoleHelper.WriteLine($"X {nbuildApp.Name} {nbuildApp.Version} failed to install: {process.ExitCode}", ConsoleColor.Red);
                    if (Verbose) DisplayCodeAndOutput(result);
                    // print resultInstall.Output
                    foreach (var item in resultInstall.Output)
                    {
                        ConsoleHelper.WriteLine(item.ToString());
                    }
                    return ResultHelper.Fail(process.ExitCode, $"Failed to install {nbuildApp.Name} {nbuildApp.Version}");
                }
            }
            else
            {
                return ResultHelper.Fail(-1, $"Failed to download {nbuildApp.WebDownloadFile} to {nbuildApp.DownloadedFile}. {result.GetFirstOutput()}");
            }
        }

        /// <summary>
        /// Adds the application's install path to the system PATH environment variable.
        /// </summary>
        /// <param name="nbuildApp">The application details containing the install path.</param>
        /// <exception cref="ArgumentNullException">Thrown when the install path is null or empty.</exception>
        /// <remarks>
        /// This method checks if the install path is already present in the system PATH environment variable.
        /// If not, it adds the install path to the PATH. It also logs the action using the Colorizer.
        /// </remarks>
        public static void AddAppInstallPathToEnvironmentPath(NbuildApp nbuildApp)
        {
            if (string.IsNullOrEmpty(nbuildApp.InstallPath))
            {
                throw new ArgumentNullException(nameof(nbuildApp.InstallPath));
            }

            if (IsAppInstallPathInEnvironmentPath(nbuildApp))
            {
                ConsoleHelper.WriteLine($"{nbuildApp.InstallPath} is already in PATH.", ConsoleColor.Yellow);
                return;
            }
            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            var pathCount = path.Split(";").Length;
            if (!path.Split(';').Contains(nbuildApp.InstallPath, StringComparer.OrdinalIgnoreCase))
            {
                if (!path.Split(';').Contains(nbuildApp.InstallPath, StringComparer.OrdinalIgnoreCase))
                {
                    path = $"{nbuildApp.InstallPath};{path}";
                }
                pathCount = path.Split(";").Length;
                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Machine);
                ConsoleHelper.WriteLine($"√ {nbuildApp.InstallPath} added to PATH.", ConsoleColor.Green);
            }
            else
            {
                ConsoleHelper.WriteLine($"{nbuildApp.InstallPath} is already in PATH.", ConsoleColor.Yellow);
            }
        }


        private static bool IsFileHashEqual(string? filePath, string? storedHash)
        {
            // return false if file does not exist
            if (!File.Exists(filePath))
            {
                return false;
            }
            var computedHashString = FileHashString(filePath);
            return computedHashString.Equals(storedHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string FileHashString(string? filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath!);
            var computedHash = sha256.ComputeHash(stream);
            return Convert.ToHexStringLower(computedHash);
        }

        private static ResultHelper SuccessfullInstall(NbuildApp nbuildApp, ResultHelper result)
        {
            if (IsAppVersionGreaterOrEqual(nbuildApp))
            {
                ConsoleHelper.WriteLine($"√ {nbuildApp.Name} {GetAppFileVersion(nbuildApp)} installed.", ConsoleColor.Green);
                return ResultHelper.Success();
            }
            else
            {
                if (result.Code == MsiReturnCodeRestartRequired)
                {
                    ConsoleHelper.WriteLine($"√ {nbuildApp.Name} {nbuildApp.Version} installed.  Restart Required", ConsoleColor.Yellow);
                    result.Code = 0;
                    return result;
                }

                // Print the stored hash of the app file name
                if (!string.IsNullOrEmpty(nbuildApp.StoredHash))
                {
                    ConsoleHelper.WriteLine($"Stored hash for {nbuildApp.AppFileName}: {FileHashString(nbuildApp.AppFileName)}", ConsoleColor.Yellow);
                }


                if (IsFileHashEqual(nbuildApp.AppFileName, nbuildApp.StoredHash))
                {
                    ConsoleHelper.WriteLine($"√ {nbuildApp.Name} {nbuildApp.Version} installed.", ConsoleColor.Green);
                    return ResultHelper.Success();
                }

                ConsoleHelper.WriteLine($"X {nbuildApp.Name} {nbuildApp.Version} failed to install", ConsoleColor.Red);
                // print out ResultHelper code and output
                DisplayCodeAndOutput(result);
                return ResultHelper.Fail(-1, $"Failed to install {nbuildApp.Name} {nbuildApp.Version}");
            }
        }

        private static void DisplayCodeAndOutput(ResultHelper result)
        {
            ConsoleHelper.WriteLine($"X Code: {result.Code}", ConsoleColor.Yellow);
            foreach (var output in result.Output)
            {
                ConsoleHelper.WriteLine($"X Output: {output}", ConsoleColor.Yellow);
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

            // if app is not installed, return success with app not installed message
            if (!IsAppVersionGreaterOrEqual(nbuildApp))
            {
                ConsoleHelper.WriteLine($"√ {nbuildApp.Name} {nbuildApp.Version} not installed.", ConsoleColor.Yellow);
                return ResultHelper.Success();
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

            ConsoleHelper.WriteLine($"Uninstalling {nbuildApp.Name} {nbuildApp.Version}", ConsoleColor.Yellow);
            if (Verbose) ConsoleHelper.WriteLine($"Working Directory: {process.StartInfo.WorkingDirectory}", ConsoleColor.Yellow);
            if (Verbose) ConsoleHelper.WriteLine($"FileName: {process.StartInfo.FileName}", ConsoleColor.Yellow);
            if (Verbose) ConsoleHelper.WriteLine($"Arguments: {process.StartInfo.Arguments}", ConsoleColor.Yellow);

            if (Verbose) ConsoleHelper.WriteLine($"Calling process.LockStart(Verbose)", ConsoleColor.Yellow);
            var result = process.LockStart(Verbose);
            if (result.IsSuccess())

            {
                // remove the app install path from the system PATH environment variable
                if (nbuildApp.AddToPath == true)
                {
                    RemoveAppInstallPathFromEnvironmentPath(nbuildApp);
                }

                ConsoleHelper.WriteLine($"√ {nbuildApp.Name} {nbuildApp.Version} Uninstalled.", ConsoleColor.Green);
                return ResultHelper.Success();
            }
            else
            {
                ConsoleHelper.WriteLine($"X {nbuildApp.Name} {nbuildApp.Version} failed to Uninstall: {process.ExitCode}", ConsoleColor.Red);
                DisplayCodeAndOutput(result);
                return ResultHelper.Fail(process.ExitCode, $"Failed to Uninstall {nbuildApp.Name} {nbuildApp.Version}");
            }
        }

        private static string? GetAppFileVersion(NbuildApp nbuildApp)
        {
            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(nbuildApp.AppFileName!);

                // return file Version as combined string of all parts : major, minor, build, patch
                return fileVersionInfo != null
                    ? $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}.{fileVersionInfo.FilePrivatePart}"
                    : null;
            }
            catch (Exception ex)
            {
                if (Verbose) ConsoleHelper.WriteLine($"X {nbuildApp.Name} {nbuildApp.Version} failed to get file version: {ex.Message}", ConsoleColor.Red);
                return null;
            }
        }

        private static bool IsAppVersionGreaterOrEqual(NbuildApp nbuildApp, bool equal = false)
        {
            var currentVersion = GetAppFileVersion(nbuildApp);
            var versionGreater = false;
            var hashMatch = IsFileHashEqual(nbuildApp.AppFileName, nbuildApp.StoredHash);


            if (currentVersion == null)
            {
                versionGreater = false;
            }
            else
            {
                if (Verbose) ConsoleHelper.WriteLine($"{nbuildApp.Name} {nbuildApp.Version} current version: {currentVersion}", ConsoleColor.Yellow);

                if (!Version.TryParse(currentVersion, out Version? currentVersionParsed)) return false;

                if (!Version.TryParse(nbuildApp.Version, out Version? versionParsed)) return false;
                versionGreater = currentVersionParsed >= versionParsed;
            }

            return versionGreater || hashMatch;
            //return currentVersionParsed >= versionParsed;
        }

        private static bool IsAppVersionEqual(NbuildApp nbuildApp)
        {
            var currentVersion = GetAppFileVersion(nbuildApp);
            if (currentVersion == null)
            {
                return false;
            }

            if (Verbose) ConsoleHelper.WriteLine($"{nbuildApp.Name} {nbuildApp.Version} current version: {currentVersion}", ConsoleColor.Yellow);

            if (!Version.TryParse(currentVersion, out Version? currentVersionParsed)) return false;

            if (!Version.TryParse(nbuildApp.Version, out Version? versionParsed)) return false;

            return currentVersionParsed == versionParsed;
        }

        public static IEnumerable<NbuildApp> GetApps(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            // Strip quotes if they are present around the json parameter
            if (json.StartsWith('"') && json.EndsWith('"') && json.Length > 1)
            {
                json = json.Substring(1, json.Length - 2);
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

            NbuildApps listAppData;
            try
            {
                listAppData = JsonSerializer.Deserialize<NbuildApps>(json) ?? throw new ParserException("Failed to parse json to list of objects", null);
            }
            catch (JsonException ex)
            {
                throw new ParserException($"Invalid JSON format: {ex.Message}. Please check the JSON file for proper escaping of backslashes and quotes.", ex);
            }

            // make sure version matches supported version
            if (listAppData.Version != SupportedVersion)
            {
                throw new ParserException($"Json Version {listAppData.Version} is not supported. Please use version {SupportedVersion}", null);
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

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            // Update variables with $(...)
            nbuildApp.InstallPath = nbuildApp.InstallPath!
                .Replace("$(Version)", nbuildApp.Version)
                .Replace("$(ProgramFiles)", programFiles)
                .Replace("$(ProgramFilesX86)", programFilesX86);

            nbuildApp.AppFileName = nbuildApp.AppFileName!
                .Replace("$(InstallPath)", nbuildApp.InstallPath);

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
                .Replace("$(DownloadedFile)", nbuildApp.DownloadedFile)
                .Replace("$(InstallPath)", nbuildApp.InstallPath)
                .Replace("$(ProgramFiles)", programFiles)
                .Replace("$(ProgramFilesX86)", programFilesX86);

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


            if (!Path.IsPathRooted(nbuildApp.InstallPath)) throw new ParserException($"App: {nbuildApp.Name}, InstallPath {nbuildApp.InstallPath} must be rooted. i.e. C:\\Program Files\\Nbuild", null);
        }
        /// <summary>
        /// Removes the application's install path from the system PATH environment variable.
        /// </summary>
        /// <param name="nbuildApp">The application details containing the install path.</param>
        /// <exception cref="ArgumentNullException">Thrown when the install path is null or empty.</exception>
        /// <remarks>
        /// This method checks if the install path is present in the system PATH environment variable.
        /// If it is, it removes the install path from the PATH. It also logs the action using the Colorizer.
        /// </remarks>
        public static void RemoveAppInstallPathFromEnvironmentPath(NbuildApp nbuildApp)
        {
            if (string.IsNullOrEmpty(nbuildApp.InstallPath))
            {
                throw new ArgumentNullException(nameof(nbuildApp.InstallPath));
            }

            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            List<string> pathSegments = PathToSegments(path);

            if (pathSegments.Contains(nbuildApp.InstallPath, StringComparer.OrdinalIgnoreCase))
            {
                pathSegments.RemoveAll(p => p.Equals(nbuildApp.InstallPath, StringComparison.OrdinalIgnoreCase));
                var updatedPath = string.Join(';', pathSegments);
                Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.Machine);
                ConsoleHelper.WriteLine($"√ {nbuildApp.InstallPath} removed from PATH.", ConsoleColor.Green);
            }
            else
            {
                ConsoleHelper.WriteLine($"{nbuildApp.InstallPath} is not in PATH.", ConsoleColor.Yellow);
            }
        }

        /// <summary>
        /// Converts a PATH environment variable string into a list of individual path segments.
        /// </summary>
        /// <param name="path">The PATH environment variable string.</param>
        /// <returns>A list of individual path segments.</returns>
        /// <remarks>
        /// This method splits the PATH environment variable string into individual segments
        /// using the semicolon (';') as a delimiter. It also removes any empty entries from the result.
        /// The use of the coalescing operator ensures that the method handles null or empty input gracefully.
        /// </remarks>
        private static List<string> PathToSegments(string path)
        {
            return [.. path.Split(';', StringSplitOptions.RemoveEmptyEntries)];
        }

        /// <summary>
        /// Checks if the application's install path is present in the system PATH environment variable.
        /// </summary>
        /// <param name="nbuildApp">The application details containing the install path.</param>
        /// <returns>True if the install path is present in the PATH, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the install path is null or empty.</exception>
        /// <remarks>
        /// This method checks if the install path is present in the system PATH environment variable.
        /// It returns true if the path is found, otherwise false.
        /// </remarks>
        public static bool IsAppInstallPathInEnvironmentPath(NbuildApp nbuildApp)
        {
            if (string.IsNullOrEmpty(nbuildApp.InstallPath))
            {
                throw new ArgumentNullException(nameof(nbuildApp.InstallPath));
            }

            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            var pathSegments = path.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            return pathSegments.Contains(nbuildApp.InstallPath, StringComparer.OrdinalIgnoreCase);
        }

        internal static ResultHelper DisplayPathSegments()
        {
            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            var pathSegments = RemoveDuplicatePathSegments(path);
            ConsoleHelper.WriteLine($"PATH Segments:", ConsoleColor.Yellow);
            foreach (var segment in pathSegments)
            {
                Console.WriteLine($" '{segment}'");

            }
            return ResultHelper.Success();
        }

        /// <summary>
        /// removes any duplicate entries, and returns a list of unique segments.
        /// </remarks>
        /// <example>
        /// <code>
        /// var uniqueSegments = Command.RemoveDuplicatePathSegments(Environment.GetEnvironmentVariable("PATH"));
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Thrown when the path is null or empty.</exception>
        public static List<string> RemoveDuplicatePathSegments(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var pathSegments = PathToSegments(path);
            var uniqueSegments = pathSegments.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Add the renewed segments to the PATH environment variable
            var renewedPath = string.Join(';', uniqueSegments);
            Environment.SetEnvironmentVariable("PATH", renewedPath, EnvironmentVariableTarget.Machine);
            return uniqueSegments;
        }

        /// <summary>
        /// Displays git information if git is configured and folder is git repository.
        /// </summary>
        public static ResultHelper DisplayGitInfo()
        {
            var project = Path.GetFileName(Directory.GetCurrentDirectory());
            var gitWrapper = new GitWrapper();
            if (string.IsNullOrEmpty(gitWrapper.Branch))
            {
                ConsoleHelper.WriteLine($"Error: [{project}] directory is not a git repository", ConsoleColor.Red);
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                return ResultHelper.Fail(-1, "Not a git repository");
            }
            ConsoleHelper.WriteLine($"Project [{project}] Branch [{gitWrapper.Branch}] Tag [{gitWrapper.Tag}]", ConsoleColor.DarkMagenta);
            return ResultHelper.Success();
        }

        /// <summary>
        /// Sets a tag in the current git repository.
        /// </summary>
        /// <param name="tag">The string representing the tag to set.</param>    
        public static ResultHelper SetTag(string? tag)
        {
            var gitWrapper = new GitWrapper();
            // Project and branch required
            if (string.IsNullOrEmpty(tag))
            {
                ConsoleHelper.WriteLine($"Error: valid tag is required", ConsoleColor.Red);
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                return ResultHelper.Fail(-1, "Tag is required");
            }

            var result = gitWrapper.SetTag(tag) == true ? ResultHelper.Success() : ResultHelper.Fail(-1, "Set tag failed");
            if (result.IsSuccess())
            {
                DisplayGitInfo();
            }
            return result;
        }

        /// <summary>
        /// Sets the auto tag based on the provided build type and optionally pushes the tag.
        /// </summary>
        /// <param name="buildType">The build type (string): STAGE | PROD.</param>
        /// <param name="push">A boolean flag indicating whether to push the tag after setting it. Default is false.</param>
        public static ResultHelper SetAutoTag(string? buildType, bool push = false)
        {
            var gitWrapper = new GitWrapper();

            if (string.IsNullOrEmpty(buildType))
            {
                ConsoleHelper.WriteLine($"Error: valid build type is required", ConsoleColor.Red);
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                return ResultHelper.Fail(-1, "Build type is required");
            }
            string? nextTag = gitWrapper.AutoTag(buildType);
            if (string.IsNullOrEmpty(nextTag))
            {
                return ResultHelper.Fail(-1, "AutoTag failed");
            }

            var result = gitWrapper.SetTag(nextTag) == true ? ResultHelper.Success() : ResultHelper.Fail(-1, "SetTag failed");
            if (result.IsSuccess() && push)
            {
                ConsoleHelper.WriteLine($"new tag: {gitWrapper.Tag}", ConsoleColor.Green);
                gitWrapper.PushTag(nextTag);
                DisplayGitInfo();
            }
            else if (result.IsSuccess())
            {
                DisplayGitInfo();
            }
            return result;
        }

        /// <summary>
        /// Displays the current git branch in the local repository.
        /// </summary>
        public static ResultHelper DisplayGitBranch()
        {
            var gitWrapper = new GitWrapper();
            if (string.IsNullOrEmpty(gitWrapper.Branch))
            {
                ConsoleHelper.WriteLine($"Error: Not a git repository", ConsoleColor.Red);
                return ResultHelper.Fail(-1, "Not a git repository");
            }
            ConsoleHelper.WriteLine($"Current branch: {gitWrapper.Branch}", ConsoleColor.Green);
            DisplayGitInfo();
            return ResultHelper.Success();
        }

        /// <summary>
        /// Clones a Git repository to the specified path.
        /// </summary>
        /// <param name="options">The CLI options containing the repository URL and target path.</param>
        /// <returns>
        /// A <see cref="ResultHelper"/> indicating the success or failure of the operation.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="GitWrapper"/> class to clone a Git repository. 
        /// If the URL is not provided in the options, an error message is displayed, and the operation fails.
        /// Upon successful cloning, the working directory is switched to the cloned repository's directory.
        /// </remarks>
        public static ResultHelper Clone(string? url, string? path, bool verbose = false)
        {
            var gitWrapper = new GitWrapper(verbose: verbose);
            if (string.IsNullOrEmpty(url))
            {
                ConsoleHelper.WriteLine($"Error: valid url is required", ConsoleColor.Red);
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                return ResultHelper.Fail(-1, "Valid url is required");
            }

            if (string.IsNullOrEmpty(path))
            {
                path = Environment.CurrentDirectory;
            }

            var result = gitWrapper.CloneProject(url, path);
            if (result.IsSuccess())
            {
                ConsoleHelper.WriteLine($"√ Project cloned successfully to {path.TrimEnd('\\')}\\{GitWrapper.ProjectNameFromUrl(url)}", ConsoleColor.Green);
                return ResultHelper.Success();
            }
            else
            {
                ConsoleHelper.WriteLine($"X {result.GetFirstOutput()}", ConsoleColor.Red);
                return ResultHelper.Fail(-1, "Clone failed");
            }
        }
        /// <summary>
        /// Deletes the specified tag.
        /// </summary>
        /// <param name="tag">The string representing the tag to delete.</param>    
        public static ResultHelper DeleteTag(string? tag)
        {
            var gitWrapper = new GitWrapper();

            if (string.IsNullOrEmpty(tag))
            {
                ConsoleHelper.WriteLine($"Error: valid tag is required", ConsoleColor.Red);
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                return ResultHelper.Fail(-1, "Tag is required");
            }
            var result = gitWrapper.DeleteTag(tag) == true ? ResultHelper.Success() : ResultHelper.Fail(-1, "Delete tag failed");
            if (result.IsSuccess())
            {
                DisplayGitInfo();
            }
            return result;
        }

        /// <summary>
        /// Creates a new release in the specified repository.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="branch">The branch name for the release.</param>
        /// <param name="assetFileName">The path to the asset to be included in the release.</param>
        /// <returns>True if the release was created successfully, otherwise false.</returns>
        // <remarks>
        /// This method creates a new release in the specified repository using the provided tag, branch, and asset path.
        /// It utilizes the ReleaseService to interact with the GitHub API.
        /// If the release creation is successful, it returns true; otherwise, it logs the error and returns false.
        /// </remarks>
        public static async Task<ResultHelper> CreateRelease(string repo, string tag, string branch, string assetFileName, bool preRelease = false)
        {
            var releaseService = new ReleaseService(repo);

            var release = new Release
            {
                TagName = tag,
                TargetCommitish = branch,  // This usually is the branch name
                Name = tag,
                Body = "Description of the release",  // should be pulled from GetLatestReleaseAsync
                Draft = false,
                Prerelease = preRelease,
            };

            // Create a release
            var responseMessage = await releaseService.CreateRelease(release, assetFileName);
            if (responseMessage.IsSuccessStatusCode)
            {
                return ResultHelper.Success();
            }
            else
            {
                // read content
                Console.WriteLine($"Failed to create release: {responseMessage.StatusCode}");
                var content = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                return ResultHelper.Fail(-1, $"Failed to create release: {responseMessage.StatusCode} - {content}");
            }
        }

        /// <summary>
        /// Downloads an asset from the specified release.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="assetPath">The path where the asset will be downloaded.</param>
        /// <returns>True if the download was successful, otherwise false.</returns>
        /// <remarks>
        /// This method ensures that the assetPath includes a file name and that the download directory exists before attempting to download the asset.
        /// </remarks>/// 
        public static async Task<ResultHelper> DownloadAsset(string repo, string tag, string assetPath)
        {

            // Ensure the assetPath is a directory
            if (!Directory.Exists(assetPath))
            {
                throw new DirectoryNotFoundException($"The specified path is not a valid directory or does not exist: {assetPath}");
            }

            // Check if we have write access to the assetPath
            try
            {
                // remove '\' from end of path if present
                if (assetPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    assetPath = assetPath.TrimEnd(Path.DirectorySeparatorChar);
                }
                // Attempt to create a temporary file in the directory
                string tempFilePath = Path.Combine(assetPath, Path.GetRandomFileName());
                using FileStream fs = File.Create(tempFilePath, 1, FileOptions.DeleteOnClose);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"No write access to the path: {assetPath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while checking write access to the path: {assetPath}", ex);
            }

            var releaseService = new ReleaseService(repo);


            // download the asset
            var response = await releaseService.DownloadAssetByName(tag, $"{tag}.zip", assetPath);

            if (response.IsSuccessStatusCode)
            {
                return ResultHelper.Success();
            }
            else
            {
                Console.WriteLine($"Failed to download the asset. Status code: {response.StatusCode}");
                return ResultHelper.Fail(-1, $"Failed to download the asset. Status code: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Uploads an asset to the specified release.
        /// </summary>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name for the release.</param>
        /// <param name="branch">The branch name for the release.</param>
        /// <param name="assetPath">The path to the asset to be uploaded.</param>
        public static async Task UploadAsset(string repo, string tag, string branch, string assetPath)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public static async Task<ResultHelper> ListReleases(string repo, bool verbose = false)
        {
            if (verbose)
            {
                ConsoleHelper.WriteLine($"Verbose mode enabled", ConsoleColor.Yellow);
            }

            var releaseService = new ReleaseService(repo);
            var releases = await releaseService.ListReleasesAsync(verbose);

            if (releases == null || !releases.Any())
            {
                ConsoleHelper.WriteLine($"No releases found for repository: {repo}", ConsoleColor.Yellow);
                return ResultHelper.Fail(-1, "No releases found");
            }

            ConsoleHelper.WriteLine($"Releases for repository: {repo}", ConsoleColor.Green);
            foreach (var release in releases)
            {
                ConsoleHelper.WriteLine($"----------------------------------------", ConsoleColor.Yellow);

                ConsoleHelper.WriteLine($"Tag: {release.TagName}", ConsoleColor.Yellow);
                ConsoleHelper.WriteLine($"Name: {release.Name}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLine($"Pre-release: {(release.Prerelease ? "Yes" : "No")}", ConsoleColor.Cyan);
                ConsoleHelper.WriteLine($"Published: {release.PublishedAt}", ConsoleColor.Cyan);
                if (verbose && !string.IsNullOrEmpty(release.Body))
                {
                    ConsoleHelper.WriteLine($"Description: {release.Body}", ConsoleColor.Magenta);
                }

                if (verbose && release.Assets != null && release.Assets.Any())
                {
                    ConsoleHelper.WriteLine($"Assets:", ConsoleColor.Cyan);
                    foreach (var asset in release.Assets)
                    {
                        ConsoleHelper.WriteLine($"  Name: {asset.Name}", ConsoleColor.Cyan);
                        ConsoleHelper.WriteLine($"  Size: {asset.Size} bytes", ConsoleColor.Cyan);
                        ConsoleHelper.WriteLine($"  Download URL: {asset.BrowserDownloadUrl}", ConsoleColor.Cyan);
                    }
                }

                if (verbose && release.Author != null)
                {
                    ConsoleHelper.WriteLine($"Author: {release.Author}", ConsoleColor.Cyan);
                }
            }
            ConsoleHelper.WriteLine($"----------------------------------------", ConsoleColor.Yellow);
            return ResultHelper.Success();
        }
    }
}
