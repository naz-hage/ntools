using GitHubRelease;
using Nbuild.Helpers;
using Nbuild.Services;
using NbuildTasks;
using Ntools;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
        public static readonly string DefaultAppsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nbuild", "apps.json");
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
        // Factory for creating IReleaseService instances. Can be overridden in tests.
        public static ReleaseServiceFactory? ReleaseServiceFactory { get; set; } = repo => new ReleaseServiceAdapter(repo);

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
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }
                };
                // update ACL on DownloadsDirectory
                var resultInstall = process.LockStart(Verbose);
                if (resultInstall.IsSuccess())
                {
                    ConsoleHelper.WriteSuccess($"{DownloadsDirectory} ACL updated.");
                    return true;
                }
                else
                {

                    ConsoleHelper.WriteError($"{DownloadsDirectory} ACL failed to update: {resultInstall.Output[0]}");
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static ResultHelper Install(string? json, bool verbose = false, bool dryRun = false)
        {
            return Install(json, null, null, verbose, dryRun);
        }

        public static ResultHelper Install(string? json, string? name, string? version, bool verbose = false, bool dryRun = false)
        {
            Verbose = verbose;
            ResultHelper result = ResultHelper.New();

            if (dryRun)
            {
                string msg;
                if (!string.IsNullOrEmpty(json))
                {
                    msg = $"DRY-RUN: would install apps from json: {json}";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // Even in dry-run, search to check if app exists and list available if not found
                    try
                    {
                        var foundApps = GetAppsFromCurrentDirectory(name, version, out var availableApps);
                        if (!foundApps.Any())
                        {
                            // No matching app found - show message but still succeed (it's dry-run)
                            ConsoleHelper.WriteError($"No apps found matching '{name}'");
                            if (availableApps.Any())
                            {
                                ConsoleHelper.WriteLine("Available applications found in:");
                                ConsoleHelper.WriteLine($"  - {Directory.GetCurrentDirectory()}");
                                ConsoleHelper.WriteLine($"  - {Path.GetDirectoryName(DefaultAppsFile)}");
                                ConsoleHelper.WriteLine("Applications:");
                                foreach (var app in availableApps.OrderBy(a => a))
                                {
                                    ConsoleHelper.WriteLine($"  - {app}");
                                }
                            }
                            msg = $"DRY-RUN: No apps found matching '{name}'";
                            return ResultHelper.Success(msg);
                        }

                        // Found apps - show details in dry-run
                        msg = $"DRY-RUN: would install app '{name}' from current directory (JSON discovery)";
                        ConsoleHelper.WriteWarning(msg);
                        
                        // Show version details of found apps
                        foreach (var app in foundApps)
                        {
                            ConsoleHelper.WriteWarning($"  - {app.Name} - Version: {app.Version}");
                        }
                        return ResultHelper.Success(msg);
                    }
                    catch (Exception ex)
                    {
                        msg = $"DRY-RUN: Error searching for apps: {ex.Message}";
                        return ResultHelper.Success(msg);
                    }
                }
                else
                {
                    msg = "DRY-RUN: would install apps from json: <default>";
                }
                ConsoleHelper.WriteWarning(msg);
                return ResultHelper.Success(msg);
            }

            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            IEnumerable<NbuildApp> apps;

            if (!string.IsNullOrEmpty(json))
            {
                // Original behavior: use provided JSON file
                apps = GetApps(json);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                // New behavior: search current directory for app by name/version
                apps = GetAppsFromCurrentDirectory(name, version, out var availableApps);
                // Materialize the enumerable to avoid multiple enumeration
                var appsList = apps.ToList();

                if (!appsList.Any())
                {
                    string errorMsg = $"No apps found matching '{name}'";
                    if (availableApps.Any())
                    {
                        errorMsg += "\nAvailable applications found in:";
                        errorMsg += $"\n  - {Directory.GetCurrentDirectory()}";
                        errorMsg += $"\n  - {Path.GetDirectoryName(DefaultAppsFile)}";
                        errorMsg += "\nApplications:";
                        foreach (var app in availableApps.OrderBy(a => a))
                        {
                            errorMsg += $"\n  - {app}";
                        }
                    }
                    return ResultHelper.Fail(-1, errorMsg);
                }

                foreach (var app in appsList)
                {
                    result = Install(app, verbose);
                    if (!result.IsSuccess())
                    {
                        break;
                    }

                    // Print the stored hash of the app file name
                    if (!string.IsNullOrEmpty(app.StoredHash))
                    {
                        ConsoleHelper.WriteWarning($"Stored hash for {app.AppFileName}: {app.StoredHash}");
                    }
                }

                return result;
            }
            else
            {
                return ResultHelper.Fail(-1, "Either json file path or app name must be provided");
            }

            // This code path is for JSON-based installs only
            var jsonAppsList = apps.ToList();

            if (!jsonAppsList.Any()) return ResultHelper.Fail(-1, $"No apps found to install");

            if (Verbose) ConsoleHelper.WriteWarning($"{jsonAppsList.Count} apps to install.");

            foreach (var app in jsonAppsList)
            {
                result = Install(app, verbose);
                if (!result.IsSuccess())
                {
                    break;
                }

                // Print the stored hash of the app file name
                if (!string.IsNullOrEmpty(app.StoredHash))
                {
                    ConsoleHelper.WriteWarning($"Stored hash for {app.AppFileName}: {app.StoredHash}");
                }
            }

            return result;
        }

        public static ResultHelper Uninstall(string? json, bool verbose = false, bool dryRun = false)
        {
            Verbose = verbose;
            ResultHelper result = ResultHelper.New();
            if (dryRun)
            {
                var msg = $"DRY-RUN: would uninstall apps from json: {json ?? "<default>"}";
                ConsoleHelper.WriteWarning(msg);
                return ResultHelper.Success(msg);
            }
            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            var apps = GetApps(json);
            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            if (Verbose) ConsoleHelper.WriteWarning($"{apps.Count()} apps to Uninstall.");

            foreach (var app in apps)
            {
                result = Uninstall(app);
                if (!result.IsSuccess())
                {
                    // display error message and continue to next app
                    ConsoleHelper.WriteError($"{result.GetFirstOutput()}");
                }
            }

            return result;
        }

        public static ResultHelper List(string? json, bool verbose = false)
        {
            Verbose = verbose;

            var apps = GetApps(json);

            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");
            ConsoleHelper.WriteWarning($"{apps.Count()} apps to list:");

            // print header
            ConsoleHelper.WriteWarning("|--------------------|----------------|-------------------|");
            ConsoleHelper.WriteWarning("| App name           | Target version | Installed version |");
            ConsoleHelper.WriteWarning("|--------------------|----------------|-------------------|");
            foreach (var app in apps)
            {
                // display app and installed version
                // InstalledAppFileVersionGreterOrEqual is true, print green, else print red
                if (IsAppVersionEqual(app))
                {
                    ConsoleHelper.WriteSuccess($"| {app.Name,-18} | {app.Version,-14} | {GetAppFileVersion(app),-18}|");
                }
                else if (IsAppVersionGreaterOrEqual(app))
                {
                    ConsoleHelper.WriteWarning($"| {app.Name,-18} | {app.Version,-14} | {GetAppFileVersion(app),-18}|");
                }
                else
                {
                    ConsoleHelper.WriteError($"| {app.Name,-18} | {app.Version,-14} | {GetAppFileVersion(app),-18}|");
                }
            }

            Console.WriteLine();
            return ResultHelper.Success();
        }

        public static ResultHelper Download(string? json, bool verbose = false, bool dryRun = false)
        {
            Verbose = verbose;

            // Respect dry-run: do not perform downloads or print the downloads table.
            if (dryRun)
            {
                var msg = $"DRY-RUN: would download after processing from {json ?? "<default>"}";
                ConsoleHelper.WriteWarning(msg);
                return ResultHelper.Success(msg);
            }

            if (!CanRunCommand()) return ResultHelper.Fail(-1, $"You must run this command as an administrator");

            var apps = GetApps(json);

            if (apps == null) return ResultHelper.Fail(-1, $"Invalid json input");

            ConsoleHelper.WriteWarning($"{apps.ToList().Count} apps to download to {DownloadsDirectory}");

            // print header
            ConsoleHelper.WriteWarning(" |--------------------|--------------------------------|-----------------|");
            ConsoleHelper.WriteWarning(" | App name           | Downloaded file                | (hh:mm:ss.ff)   |");
            ConsoleHelper.WriteWarning(" |--------------------|--------------------------------|-----------------|");

            string webDownloadedFile = string.Empty;
            ResultHelper lastResult = ResultHelper.Success();
            try
            {
                foreach (var app in apps)
                {
                    webDownloadedFile = app.WebDownloadFile!;

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    // download app
                    var result = DownloadApp(app);
                    lastResult = result;

                    stopWatch.Stop();

                    if (result.IsSuccess())
                    {
                        ConsoleHelper.WriteSuccess($" | {app.Name,-18} | {app.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|");
                    }
                    else
                    {
                        ConsoleHelper.WriteError($" Failed to download {app.WebDownloadFile} to {app.DownloadedFile}");
                        Console.WriteLine($"Return: {result.GetFirstOutput()}");
                        ConsoleHelper.WriteError($" | {app.Name,-18} | {app.DownloadedFile,-30} | {stopWatch.Elapsed,-16:hh\\:mm\\:ss\\.ff}|");
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

            // If any download failed, return the last failure result
            if (lastResult != null && !lastResult.IsSuccess())
            {
                return lastResult;
            }

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
            Nfile.SetTrustedHosts(new List<string> { new Uri(nbuildApp.WebDownloadFile).Host });
            var extension = Path.GetExtension(new Uri(nbuildApp.WebDownloadFile).AbsolutePath);
            Nfile.SetAllowedExtensions(new List<string> { extension });

            if (Verbose)
            {
                ConsoleHelper.WriteVerbose($"Downloading URL: {nbuildApp.WebDownloadFile} -> {fileName}");
            }

            try
            {
                var result = Task.Run(async () => await Nfile.DownloadAsync(nbuildApp.WebDownloadFile, fileName)).Result;

                if (Verbose)
                {
                    // display download file signature and size
                    //result.DisplayCertificate();
                    try
                    {
                        if (result.DigitallySigned)
                        {
                            ConsoleHelper.WriteWarning($" {fileName} is signed");
                        }
                        else
                        {
                            ConsoleHelper.WriteWarning($" {fileName} is not signed");
                        }
                    }
                    catch (Exception)
                    {
                        // result may not expose DigitallySigned in some implementations; ignore safely
                    }
                }

                // If the unauthenticated download failed, attempt GitHub authenticated fallback
                if (!result.IsSuccess())
                {
                    try
                    {
                        var uri = new Uri(nbuildApp.WebDownloadFile);
                        if (uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = Environment.GetEnvironmentVariable("API_GITHUB_KEY") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                            if (!string.IsNullOrEmpty(token))
                            {
                                var parts = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 5)
                                {
                                    var owner = parts[0];
                                    var repoName = parts[1];
                                    var releasesIdx = Array.IndexOf(parts, "releases");
                                    if (releasesIdx >= 0 && parts.Length > releasesIdx + 2)
                                    {
                                        var tag = parts[releasesIdx + 2];
                                        var assetName = (parts.Length > releasesIdx + 3) ? parts[releasesIdx + 3] : Path.GetFileName(uri.AbsolutePath);
                                        var repo = $"{owner}/{repoName}";
                                        var downloadFolder = Path.GetDirectoryName(fileName) ?? DownloadsDirectory;

                                        var factory = ReleaseServiceFactory ?? (r => new ReleaseServiceAdapter(r));
                                        var releaseService = factory(repo);

                                        var response = Task.Run(async () => await releaseService.DownloadAssetByName(tag, assetName, downloadFolder)).Result;
                                        if (response != null && response.IsSuccessStatusCode)
                                        {
                                            if (Verbose) ConsoleHelper.WriteVerbose($"Authenticated download saved to: {fileName}");
                                            return ResultHelper.Success();
                                        }
                                        else
                                        {
                                            if (Verbose)
                                            {
                                                var status = response == null ? "no response" : response.StatusCode.ToString();
                                                ConsoleHelper.WriteVerbose($"Authenticated download failed: {status}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (Verbose) ConsoleHelper.WriteVerbose($"Authenticated fallback failed: {e}");
                    }
                }

                return result;
            }
            catch (AggregateException aggEx)
            {
                var ex = aggEx.Flatten().InnerException ?? aggEx;
                
                // Try GitHub authenticated fallback first (before checking for 404)
                var uri = new Uri(nbuildApp.WebDownloadFile);
                var fb = TryGithubFallback(uri, fileName, ex);
                if (fb != null) return fb;
                
                // Check if this is a 404 (version not found) error
                if (ex.Message.Contains("404") || ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleHelper.WriteError($"Installation of {nbuildApp.Name} version {nbuildApp.Version} is not found");
                    if (Verbose) ConsoleHelper.WriteVerbose($"404 - Version not available: {nbuildApp.Version}");
                    return ResultHelper.Fail(-1, $"Installation of {nbuildApp.Name} version {nbuildApp.Version} is not found");
                }

                if (Verbose)
                {
                    ConsoleHelper.WriteVerbose($"Download failed for URL: {nbuildApp.WebDownloadFile}");
                    ConsoleHelper.WriteVerbose($"Exception: {ex}");
                }
                return ResultHelper.Fail(-1, ex.Message);
            }
            catch (Exception ex)
            {
                // Try GitHub authenticated fallback first (before checking for 404)
                var uri = new Uri(nbuildApp.WebDownloadFile);
                var fb = TryGithubFallback(uri, fileName, ex);
                if (fb != null) return fb;
                
                // Check if this is a 404 (version not found) error
                if (ex.Message.Contains("404") || ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleHelper.WriteError($"Installation of {nbuildApp.Name} version {nbuildApp.Version} is not found");
                    if (Verbose) ConsoleHelper.WriteVerbose($"404 - Version not available: {nbuildApp.Version}");
                    return ResultHelper.Fail(-1, $"Installation of {nbuildApp.Name} version {nbuildApp.Version} is not found");
                }

                if (Verbose)
                {
                    ConsoleHelper.WriteVerbose($"Download failed for URL: {nbuildApp.WebDownloadFile}");
                    ConsoleHelper.WriteVerbose($"Exception: {ex}");
                }
                return ResultHelper.Fail(-1, ex.Message);
            }

            // local fallback function: if URL is github release and token exists, try authenticated download via GitHubRelease library
            ResultHelper? TryGithubFallback(Uri uri, string destFile, Exception originalEx)
            {
                try
                {
                    if (!uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase)) return null;

                    // quick token check - ReleaseService will also use the central Credentials logic
                    var token = Environment.GetEnvironmentVariable("API_GITHUB_KEY") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                    if (string.IsNullOrEmpty(token)) return null;

                    // parse owner, repo, tag and asset name from the releases/download URL
                    var parts = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5) return null; // expected: owner/repo/releases/download/tag/asset
                    var owner = parts[0];
                    var repoName = parts[1];
                    var releasesIdx = Array.IndexOf(parts, "releases");
                    if (releasesIdx < 0 || parts.Length <= releasesIdx + 2) return null;
                    var tag = parts[releasesIdx + 2];
                    var assetName = (parts.Length > releasesIdx + 3) ? parts[releasesIdx + 3] : Path.GetFileName(uri.AbsolutePath);

                    if (Verbose) ConsoleHelper.WriteVerbose($"Attempting authenticated GitHub API download (via ReleaseService) for {owner}/{repoName} tag {tag} asset {assetName}");

                    var repo = $"{owner}/{repoName}";
                    var downloadFolder = Path.GetDirectoryName(destFile) ?? DownloadsDirectory;

                    // create the release service via factory (tests can override)
                    var factory = ReleaseServiceFactory ?? (r => new ReleaseServiceAdapter(r));
                    var releaseService = factory(repo);

                    var response = Task.Run(async () => await releaseService.DownloadAssetByName(tag, assetName, downloadFolder)).Result;

                    if (response != null && response.IsSuccessStatusCode)
                    {
                        if (Verbose) ConsoleHelper.WriteVerbose($"Authenticated download saved to: {destFile}");
                        return ResultHelper.Success();
                    }
                    else
                    {
                        if (Verbose)
                        {
                            var status = response == null ? "no response" : response.StatusCode.ToString();
                            ConsoleHelper.WriteVerbose($"Authenticated download failed: {status}");
                        }
                        return null;
                    }
                }
                catch (Exception e)
                {
                    if (Verbose) ConsoleHelper.WriteVerbose($"Authenticated fallback failed: {e}");
                    return null;
                }
            }

            // end of DownloadApp try/catch - result already returned or failure returned above
        }

        private static ResultHelper Install(NbuildApp nbuildApp, bool verbose = false)
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
                ConsoleHelper.WriteWarning($"{nbuildApp.Name} {GetAppFileVersion(nbuildApp)} already installed.");
                return ResultHelper.Success();
            }

            ConsoleHelper.WriteWarning($" Downloading {nbuildApp.Name} {nbuildApp.Version}");
            var result = DownloadApp(nbuildApp);

            if (result.IsSuccess())
            {
                ConsoleHelper.WriteWarning($"{nbuildApp.Name} {nbuildApp.Version} downloaded.");

                // Install the Downloaded file
                ConsoleHelper.WriteWarning($" Installing {nbuildApp.Name} {nbuildApp.Version}");
                
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

                if (Verbose)
                {
                    ConsoleHelper.WriteVerbose($" Working Directory: {process.StartInfo.WorkingDirectory}");
                    ConsoleHelper.WriteVerbose($" FileName: {process.StartInfo.FileName}");
                    ConsoleHelper.WriteVerbose($" Arguments: {process.StartInfo.Arguments}");

                    ConsoleHelper.WriteVerbose($" Calling process.LockStart(Verbose)");
                }

                var resultInstall = process.LockStart(Verbose);
                if (resultInstall.IsSuccess())
                {
                    if (nbuildApp.AddToPath == true && !TestMode)
                    {
                        PathManager.AddAppInstallPathToEnvironmentPath(nbuildApp.InstallPath);
                    }

                    // Check if the app was installed successfully
                    return SuccessfullInstall(nbuildApp, resultInstall);
                }
                else
                {
                    // installer failed
                    ConsoleHelper.WriteError($"{nbuildApp.Name} {nbuildApp.Version} failed to install: {resultInstall.Code}");
                    if (Verbose) DisplayCodeAndOutput(resultInstall);
                    // print resultInstall.Output
                    foreach (var item in resultInstall.Output)
                    {
                        ConsoleHelper.WriteLine(item.ToString());
                    }
                    return ResultHelper.Fail(resultInstall.Code, $"Failed to install {nbuildApp.Name} {nbuildApp.Version}");
                }
            }
            else
            {
                return ResultHelper.Fail(-1, $"Failed to download {nbuildApp.WebDownloadFile} to {nbuildApp.DownloadedFile}. {result.GetFirstOutput()}");
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
                ConsoleHelper.WriteSuccess($"{nbuildApp.Name} {GetAppFileVersion(nbuildApp)} installed.");
                return ResultHelper.Success();
            }
            else
            {
                if (result.Code == MsiReturnCodeRestartRequired)
                {
                    ConsoleHelper.WriteWarning($"{nbuildApp.Name} {nbuildApp.Version} installed.  Restart Required");
                    result.Code = 0;
                    return result;
                }

                // Print the stored hash of the app file name
                if (!string.IsNullOrEmpty(nbuildApp.StoredHash))
                {
                    ConsoleHelper.WriteWarning($"Stored hash for {nbuildApp.AppFileName}: {FileHashString(nbuildApp.AppFileName)}");

                    // Only compare file hash when a StoredHash is provided
                    if (IsFileHashEqual(nbuildApp.AppFileName, nbuildApp.StoredHash))
                    {
                        ConsoleHelper.WriteSuccess($"{nbuildApp.Name} {nbuildApp.Version} installed.");
                        return ResultHelper.Success();
                    }
                    else
                    {
                        ConsoleHelper.WriteError($"{nbuildApp.Name} {nbuildApp.Version} installed, but file hash does not match.");
                        return ResultHelper.Fail(-1, $"File hash does not match for {nbuildApp.AppFileName}");
                    }
                }
            }

            // If no explicit success/failure was determined above, return the provided installer result
            return result;
        }

        private static void DisplayCodeAndOutput(ResultHelper result)
        {
            ConsoleHelper.WriteWarning($"Code: {result.Code}");
            foreach (var output in result.Output)
            {
                ConsoleHelper.WriteWarning($"Output: {output}");
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
                ConsoleHelper.WriteWarning($"{nbuildApp.Name} {nbuildApp.Version} not installed.");
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

            ConsoleHelper.WriteWarning($"Uninstalling {nbuildApp.Name} {nbuildApp.Version}");
            if (Verbose) ConsoleHelper.WriteVerbose($"Working Directory: {process.StartInfo.WorkingDirectory}");
            if (Verbose) ConsoleHelper.WriteVerbose($"FileName: {process.StartInfo.FileName}");
            if (Verbose) ConsoleHelper.WriteVerbose($"Arguments: {process.StartInfo.Arguments}");

            if (Verbose) ConsoleHelper.WriteVerbose($"Calling process.LockStart(Verbose)");
            var result = process.LockStart(Verbose);
            if (result.IsSuccess())

            {
                // remove the app install path from the system PATH environment variable
                if (nbuildApp.AddToPath == true)
                {
                    PathManager.RemovePath(nbuildApp.InstallPath!);
                }

                ConsoleHelper.WriteSuccess($"{nbuildApp.Name} {nbuildApp.Version} Uninstalled.");
                return ResultHelper.Success();
            }
            else
            {
                ConsoleHelper.WriteError($"{nbuildApp.Name} {nbuildApp.Version} failed to Uninstall: {result.Code}");
                DisplayCodeAndOutput(result);
                return ResultHelper.Fail(result.Code, $"Failed to Uninstall {nbuildApp.Name} {nbuildApp.Version}");
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
                if (Verbose) ConsoleHelper.WriteError($"{nbuildApp.Name} {nbuildApp.Version} failed to get file version: {ex.Message}");
                return null;
            }
        }

        private static bool IsAppVersionGreaterOrEqual(NbuildApp nbuildApp, bool equal = false)
        {
            var currentVersion = GetAppFileVersion(nbuildApp);
            var versionGreater = false;
            var hashMatch = IsFileHashEqual(nbuildApp.AppFileName, nbuildApp.StoredHash);

            if (Verbose)
            {
                ConsoleHelper.WriteVerbose($"[VERSION CHECK] App: {nbuildApp.Name}, Requested Version: {nbuildApp.Version}, Installed: {currentVersion}");
            }

            if (currentVersion == null)
            {
                versionGreater = false;
            }
            else
            {
                if (Verbose) ConsoleHelper.WriteVerbose($"{nbuildApp.Name} {nbuildApp.Version} current version: {currentVersion}");

                if (!Version.TryParse(currentVersion, out Version? currentVersionParsed)) return false;

                if (!Version.TryParse(nbuildApp.Version, out Version? versionParsed)) return false;
                versionGreater = currentVersionParsed >= versionParsed;
                
                if (Verbose)
                {
                    ConsoleHelper.WriteVerbose($"[VERSION COMPARE] {currentVersionParsed} >= {versionParsed} = {versionGreater}");
                }
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

            if (Verbose) ConsoleHelper.WriteVerbose($"{nbuildApp.Name} {nbuildApp.Version} current version: {currentVersion}");

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
                listAppData = JsonSerializer.Deserialize<NbuildApps>(json) ?? throw new ArgumentException("Failed to parse json to list of objects");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}. Please check the JSON file for proper escaping of backslashes and quotes.", ex);
            }

            // make sure version matches supported version
            if (listAppData.Version != SupportedVersion)
            {
                throw new ArgumentException($"Json Version {listAppData.Version} is not supported. Please use version {SupportedVersion}");
            }

            foreach (var appData in listAppData.NbuildAppList)
            {
                Validate(appData);
                UpdateEnvironmentVariables(appData);
                yield return appData;
            }
        }

        /// <summary>
        /// Searches for an application by name in the current working directory and returns matching apps.
        /// </summary>
        /// <param name="name">The name of the application to search for.</param>
        /// <param name="version">Optional version override. If specified, overrides the version in the JSON file.</param>
        /// <param name="availableApps">Output parameter that returns a list of available apps found during the search (name and version).</param>
        /// <returns>A list of matching NbuildApp objects. If multiple apps share the same name and no version is specified, an exception is thrown.</returns>
        public static List<NbuildApp> GetAppsFromCurrentDirectory(string name, string? version, out List<string> availableApps)
        {
            var availableAppsSet = new HashSet<string>();
            var result = new List<NbuildApp>();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            NbuildApp? foundApp = null;

            // Search for apps.json in order: current directory first, then default program files directory
            var searchFilePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "apps.json"),
                DefaultAppsFile
            };

            var appsFileFound = false;

            foreach (var appsFilePath in searchFilePaths)
            {
                if (!File.Exists(appsFilePath))
                {
                    if (Verbose)
                    {
                        ConsoleHelper.WriteVerbose($"apps.json file does not exist: {appsFilePath}");
                    }
                    continue;
                }

                appsFileFound = true;
                
                if (Verbose)
                {
                    ConsoleHelper.WriteVerbose($"Found apps.json: {appsFilePath}");
                }

                try
                {
                    var jsonContent = File.ReadAllText(appsFilePath);

                    // Parse JSON directly without calling GetApps to avoid premature template processing
                    NbuildApps? listAppData;
                    try
                    {
                        listAppData = JsonSerializer.Deserialize<NbuildApps>(jsonContent);
                    }
                    catch (JsonException ex)
                    {
                        throw new ArgumentException($"Invalid JSON format in {appsFilePath}: {ex.Message}. Please check the JSON file for proper escaping of backslashes and quotes.", ex);
                    }

                    if (listAppData == null || listAppData.NbuildAppList == null)
                    {
                        continue;
                    }

                    // Make sure version matches supported version
                    if (listAppData.Version != SupportedVersion)
                    {
                        // Log warning but continue searching other files
                        ConsoleHelper.WriteWarning($"Warning: Skipping {Path.GetFileName(appsFilePath)} - unsupported version {listAppData.Version}. Supported version: {SupportedVersion}");
                        continue;
                    }

                    foreach (var appData in listAppData.NbuildAppList)
                    {
                        availableAppsSet.Add($"{appData.Name} - Version: {appData.Version}");

                        // Match by name only - version parameter is for override, not filtering
                        if (string.Equals(appData.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            // No version specified: if multiple configs share the same name, fail explicitly
                            if (string.IsNullOrEmpty(version))
                            {
                                if (foundApp == null)
                                {
                                    foundApp = appData;
                                }
                                else
                                {
                                    throw new ArgumentException(
                                        $"Multiple apps found with name '{name}'. Please specify a version. " +
                                        $"Examples: '{name}' version '{foundApp.Version}', '{name}' version '{appData.Version}'.");
                                }
                            }
                            else
                            {
                                // Version specified: allow multiple, take the first one (will be overridden anyway)
                                if (foundApp == null)
                                {
                                    foundApp = appData;
                                }
                            }
                        }
                    }

                    // If we found a match in a higher-priority file (current directory) and no version specified, stop searching
                    if (foundApp != null && string.IsNullOrEmpty(version))
                    {
                        break; // Use the first match found (current directory takes precedence)
                    }
                }
                catch (Exception ex) when (ex is not ArgumentException)
                {
                    // Log warning but continue searching other files
                    ConsoleHelper.WriteWarning($"Warning: Failed to parse {Path.GetFileName(appsFilePath)}: {ex.Message}");
                }
            }

            if (!appsFileFound)
            {
                throw new FileNotFoundException($"No apps.json file found in search paths: {string.Join(", ", searchFilePaths)}");
            }

            if (foundApp != null)
            {
                if (Verbose)
                {
                    ConsoleHelper.WriteVerbose($"Matched app: {foundApp.Name} (Version: {foundApp.Version})");
                }
                
                // Apply version override if specified BEFORE processing templates
                if (!string.IsNullOrEmpty(version))
                {
                    if (Verbose)
                    {
                        ConsoleHelper.WriteVerbose($"Overriding version: {foundApp.Version} -> {version}");
                    }
                    foundApp.Version = version;
                }

                // Now validate and update environment variables with the correct version
                Validate(foundApp);
                UpdateEnvironmentVariables(foundApp);

                result.Add(foundApp);
            }

            // Convert HashSet to List for out parameter (ensures no duplicates)
            availableApps = availableAppsSet.ToList();

            return result;
        }

        private static void Validate(NbuildApp nbuildApp)
        {
            if (string.IsNullOrEmpty(nbuildApp.Name))
            {
                throw new ArgumentException("Name is required");
            }
            if (string.IsNullOrEmpty(nbuildApp.WebDownloadFile))
            {
                throw new ArgumentException("WebDownloadFile is required");
            }
            if (string.IsNullOrEmpty(nbuildApp.DownloadedFile))
            {
                throw new ArgumentException("DownloadedFile is required");
            }
            if (string.IsNullOrEmpty(nbuildApp.InstallCommand))
            {
                throw new ArgumentException("InstallCommand is required");
            }
            if (string.IsNullOrEmpty(nbuildApp.InstallArgs))
            {
                throw new ArgumentException("InstallArgs is required");
            }
            if (string.IsNullOrEmpty(nbuildApp.Version))
            {
                throw new ArgumentException("Version is required");
            }
            if (string.IsNullOrEmpty(nbuildApp.InstallPath))
            {
                throw new ArgumentException("InstallPath is required");
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
                throw new ArgumentException(sb.ToString());
            }
            ValidJson = true;

        }

        /// <summary>  
        /// Updates the properties of the given NbuildApp instance by replacing placeholder variables  
        /// (e.g., $(Version), $(ProgramFiles), $(AppFileName), etc.) with their actual values.  
        /// This prepares the app configuration for use in the build process.  
        /// </summary> 
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


            if (!Path.IsPathRooted(nbuildApp.InstallPath)) throw new ArgumentException($"App: {nbuildApp.Name}, InstallPath {nbuildApp.InstallPath} must be rooted. i.e. C:\\Program Files\\Nbuild");
        }

        /// <summary>
        /// Displays git information if git is configured and folder is git repository.
        /// </summary>
        /// <param name="verbose">Whether to display verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        public static ResultHelper DisplayGitInfo(bool verbose = false, bool dryRun = false)
        {
            if (dryRun)
            {
                ConsoleHelper.WriteVerbose("DRY-RUN: Displaying git repository information (read-only operation).");
            }

            var project = Path.GetFileName(Directory.GetCurrentDirectory());
            var gitWrapper = new GitWrapper();
            if (string.IsNullOrEmpty(gitWrapper.Branch))
            {
                ConsoleHelper.WriteError($"Error: [{project}] directory is not a git repository");
                return ResultHelper.Fail(-1, "Not a git repository");
            }
            ConsoleHelper.WriteVerbose($"Project [{project}] Branch [{gitWrapper.Branch}] Tag [{gitWrapper.Tag}]");
            return ResultHelper.Success();
        }

        /// <summary>
        /// Sets a tag in the current git repository.
        /// </summary>
        /// <param name="tag">The string representing the tag to set.</param>    
        /// <param name="verbose">Whether to display verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making actual changes.</param>
        public static ResultHelper SetTag(string? tag, bool verbose = false, bool dryRun = false)
        {
            var gitWrapper = new GitWrapper();
            // Project and branch required
            if (string.IsNullOrEmpty(tag))
            {
                ConsoleHelper.WriteError($"Error: valid tag is required");
                return ResultHelper.Fail(-1, "Tag is required");
            }

            if (dryRun)
            {
                ConsoleHelper.WriteVerbose($"DRY-RUN: Would set git tag: {tag}");
                ConsoleHelper.WriteVerbose($"DRY-RUN: No actual changes will be made to the repository.");
                return ResultHelper.Success();
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
        /// <param name="verbose">Whether to display verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making actual changes.</param>
        public static ResultHelper SetAutoTag(string? buildType, bool push = false, bool verbose = false, bool dryRun = false)
        {
            var gitWrapper = new GitWrapper();

            if (string.IsNullOrEmpty(buildType))
            {
                ConsoleHelper.WriteError($"Error: valid build type is required");
                return ResultHelper.Fail(-1, "Build type is required");
            }

            string? nextTag = gitWrapper.AutoTag(buildType);
            if (string.IsNullOrEmpty(nextTag))
            {
                return ResultHelper.Fail(-1, "AutoTag failed");
            }

            if (dryRun)
            {
                ConsoleHelper.WriteVerbose($"DRY-RUN: Would compute and set git tag: {nextTag} (build type: {buildType})");
                if (push)
                {
                    ConsoleHelper.WriteVerbose($"DRY-RUN: Would push tag {nextTag} to remote repository");
                }
                ConsoleHelper.WriteVerbose("DRY-RUN: No actual changes will be made to the repository or remote.");
                return ResultHelper.Success();
            }

            var result = gitWrapper.SetTag(nextTag) == true ? ResultHelper.Success() : ResultHelper.Fail(-1, "SetTag failed");
            if (result.IsSuccess() && push)
            {
                ConsoleHelper.WriteVerbose($"new tag: {gitWrapper.Tag}");
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
                ConsoleHelper.WriteError($"Error: Not a git repository");
                return ResultHelper.Fail(-1, "Not a git repository");
            }
            ConsoleHelper.WriteVerbose($"Current branch: {gitWrapper.Branch}");
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
        public static ResultHelper Clone(string? url, string? path, bool verbose = false, bool dryRun = false)
        {
            if (dryRun)
            {
                var msg = $"DRY-RUN: would clone {url} to {path ?? Environment.CurrentDirectory}";
                ConsoleHelper.WriteVerbose(msg);
                return ResultHelper.Success(msg);
            }
            var gitWrapper = new GitWrapper(verbose: verbose);
            if (string.IsNullOrEmpty(url))
            {
                ConsoleHelper.WriteError($"Error: valid url is required");
                return ResultHelper.Fail(-1, "Valid url is required");
            }

            if (string.IsNullOrEmpty(path))
            {
                path = Environment.CurrentDirectory;
            }

            var result = gitWrapper.CloneProject(url, path);
            if (result.IsSuccess())
            {
                ConsoleHelper.WriteVerbose($"Project cloned successfully to {path.TrimEnd('\\')}\\{GitWrapper.ProjectNameFromUrl(url)}");
                return ResultHelper.Success();
            }
            else
            {
                ConsoleHelper.WriteError($"{result.GetFirstOutput()}");
                return ResultHelper.Fail(-1, "Clone failed");
            }
        }
        /// <summary>
        /// Deletes the specified tag.
        /// </summary>
        /// <param name="tag">The string representing the tag to delete.</param>
        /// <param name="verbose">Whether to display verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run without making actual changes.</param>
        public static ResultHelper DeleteTag(string? tag, bool verbose = false, bool dryRun = false)
        {
            var gitWrapper = new GitWrapper();

            if (string.IsNullOrEmpty(tag))
            {
                ConsoleHelper.WriteError($"Error: valid tag is required");
                return ResultHelper.Fail(-1, "Tag is required");
            }

            if (dryRun)
            {
                // Check if tag exists locally or remotely to give accurate dry-run message
                bool localExists = gitWrapper.LocalTagExists(tag);
                bool remoteExists = gitWrapper.RemoteTagExists(tag);

                if (localExists && remoteExists)
                {
                    ConsoleHelper.WriteVerbose($"DRY-RUN: Would delete tag '{tag}' from both local and remote repository");
                }
                else if (localExists)
                {
                    ConsoleHelper.WriteVerbose($"DRY-RUN: Would delete tag '{tag}' from local repository");
                }
                else if (remoteExists)
                {
                    ConsoleHelper.WriteVerbose($"DRY-RUN: Would delete tag '{tag}' from remote repository");
                }
                else
                {
                    ConsoleHelper.WriteVerbose($"DRY-RUN: Tag '{tag}' does not exist locally or remotely - no action needed");
                }

                ConsoleHelper.WriteVerbose("DRY-RUN: No actual changes will be made to the repository or remote.");
                return ResultHelper.Success();
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
        /// <remarks>
        /// This method creates a new release in the specified repository using the provided tag, branch, and asset path.
        /// It utilizes the ReleaseService to interact with the GitHub API.
        /// If the release creation is successful, it returns true; otherwise, it logs the error and returns false.
        /// </remarks>
        public static async Task<ResultHelper> CreateRelease(string repo, string tag, string branch, string assetFileName, bool preRelease = false, bool dryRun = false, bool verbose = false)
        {
            if (dryRun)
            {
                var msg = $"DRY-RUN: would create {(preRelease ? "pre-release" : "release")} for {repo} with tag {tag} and asset {assetFileName}";
                ConsoleHelper.WriteVerbose(msg);
                return ResultHelper.Success(msg);
            }
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
            var responseMessage = await releaseService.CreateRelease(release, assetFileName, verbose);
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
        public static async Task<ResultHelper> DownloadAsset(string repo, string tag, string assetPath, bool dryRun = false)
        {
            if (dryRun)
            {
                var msg = $"DRY-RUN: would download asset for {repo} tag {tag} to path {assetPath}";
                ConsoleHelper.WriteVerbose(msg);
                return ResultHelper.Success(msg);
            }

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

        public static async Task<ResultHelper> ListReleases(string repo, bool verbose = false, bool dryRun = false)
        {
            if (dryRun)
            {
                ConsoleHelper.WriteVerbose($"DRY-RUN: performing read-only fetch for repository: {repo}");
                ConsoleHelper.WriteVerbose($"DRY-RUN: no state-changing operations will be performed.");
            }

            var authService = new GitHubRelease.GitHubAuthService(verbose);
            var releaseService = new ReleaseService(repo, authService);
            var releases = await releaseService.ListReleasesAsync(verbose);

            if (releases == null || !releases.Any())
            {
                ConsoleHelper.WriteVerbose($"No releases found for repository: {repo}");
                return ResultHelper.Fail(-1, "No releases found");
            }

            ConsoleHelper.WriteVerbose($"Releases for repository: {repo}");
            foreach (var release in releases)
            {
                ConsoleHelper.WriteVerbose($"----------------------------------------");

                ConsoleHelper.WriteWarning($"Tag: {release.TagName}");
                ConsoleHelper.WriteInfo($"Name: {release.Name}");
                ConsoleHelper.WriteInfo($"Pre-release: {(release.Prerelease ? "Yes" : "No")}");
                ConsoleHelper.WriteInfo($"Published: {release.PublishedAt}");
                if (verbose && !string.IsNullOrEmpty(release.Body))
                {
                    ConsoleHelper.WriteVerbose($"Description: {release.Body}");
                }

                if (verbose && release.Assets != null && release.Assets.Any())
                {
                    ConsoleHelper.WriteVerbose($"Assets:");
                    foreach (var asset in release.Assets)
                    {
                        ConsoleHelper.WriteVerbose($"  Name: {asset.Name}");
                        ConsoleHelper.WriteVerbose($"  Size: {asset.Size} bytes");
                        ConsoleHelper.WriteVerbose($"  Download URL: {asset.BrowserDownloadUrl}");
                    }
                }

                if (verbose && release.Author != null)
                {
                    ConsoleHelper.WriteVerbose($"Author: {release.Author}");
                }
            }
            ConsoleHelper.WriteVerbose($"----------------------------------------");

            var successMessage = dryRun
                ? "DRY-RUN: successfully performed read-only fetch of releases"
                : "Successfully listed releases";

            return ResultHelper.Success(successMessage);
        }
    }
}
