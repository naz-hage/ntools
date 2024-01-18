using Launcher;
using NbuildTasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace Nbuild
{
    public static class Command
    {
        private static readonly string DownloadsDirectory = $"{Environment.GetEnvironmentVariable("Temp")}\\nb"; // "C:\\NToolsDownloads";

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
                    foreach (var appData in appDataList)
                    {
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

            var fileName = $"{DownloadsDirectory}\\{appData.DownloadedFile}";
            var httpClient = new HttpClient();
            var result = Task.Run(async () => await httpClient.DownloadFileAsync(new Uri(appData.WebDownloadFile), fileName)).Result;

            if (result.IsSuccess())
            {
                // Install the downloaded file
                var parameters = new Launcher.Parameters
                {
                    FileName = appData.InstallCommand,
                    Arguments = appData.InstallArgs,
                    WorkingDir = DownloadsDirectory
                };

                var result2 = Launcher.Launcher.Start(parameters);
                return result2;
            }
            else
            {
                return ResultHelper.Fail(-1, $"Failed to download {appData.WebDownloadFile} to {appData.DownloadedFile}: {result.Output[0]}");
            }
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
