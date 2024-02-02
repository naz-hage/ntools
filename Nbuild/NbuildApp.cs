using CommandLine;
using System.Text.Json;

namespace Nbuild
{
    public class NbuildApp
    {
        public string? Name { get; set; }    // the name of the app to download
        public string? Version { get; set; } // the version of the app to download
        public string? AppFileName { get; set; }     // name of File that hold the app version.  File should have file version info
        public string? WebDownloadFile { get; set; }     // the url to download the app from. can use $(Version) to substitute the version number
        public string? DownloadedFile { get; set; }     // the downloaded file name, can use $(Version) to substitute the version number
        public string? InstallCommand { get; set; }  // install command to run to install the app. i.e. setup.exe
        public string? InstallArgs { get; set; }     // the arguments to pass to the install command. can use $(Version) and $(InstallPath) to substitute the version number
        public string? InstallPath { get; set; }     // The directory required to locate the AppFileName. path must be rooted. i.e. C:\Program Files\Nbuild can use $(Version) to substitute the version number

        public static IEnumerable<NbuildApp> GetApps(string json)
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

            var listAppData = JsonSerializer.Deserialize<NbuildApps>(json) ?? throw new ParserException("Failed to parse json to AppData object", null);

            // make sure version matches 1.2.0
            if (listAppData.Version != "1.2.0")
            {
                throw new ParserException($"Version {listAppData.Version} is not supported. Please use version 1.2.0", null);
            }

            foreach (var appData in listAppData.NbuildAppList)
            {
                yield return Validate(appData);
            }

        }

        private static NbuildApp Validate(NbuildApp appData)
        {
            if (string.IsNullOrEmpty(appData.Name))
            {
                throw new ParserException("Name is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.WebDownloadFile))
            {
                throw new ParserException("WebDownloadFile is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.DownloadedFile))
            {
                throw new ParserException("DownloadedFile is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.InstallCommand))
            {
                throw new ParserException("InstallCommand is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.InstallArgs))
            {
                throw new ParserException("InstallArgs is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.Version))
            {
                throw new ParserException("Version is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.InstallPath))
            {
                throw new ParserException("InstallPath is missing or empty", null);
            }
            if (string.IsNullOrEmpty(appData.AppFileName))
            {
                throw new ParserException("AppFileName is missing or empty", null);
            }


            // Replace $(Version) with the actual version in appData.Version
            // Replace $(InstallPath) with the actual version in appData.InstallPath
            appData.WebDownloadFile = appData.WebDownloadFile.Replace("$(Version)", appData.Version);

            appData.DownloadedFile = appData.DownloadedFile.Replace("$(Version)", appData.Version);
            appData.DownloadedFile = appData.DownloadedFile.Replace("$(AppFileName)", appData.AppFileName);

            appData.InstallCommand = appData.InstallCommand.Replace("$(Version)", appData.Version);
            appData.InstallCommand = appData.InstallCommand.Replace("$(DownloadedFile)", appData.DownloadedFile);

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            appData.InstallArgs = appData.InstallArgs.Replace("$(Version)", appData.Version);
            appData.InstallArgs = appData.InstallArgs.Replace("$(InstallPath)", appData.InstallPath);
            appData.InstallArgs = appData.InstallArgs.Replace("$(AppFileName)", appData.AppFileName);

            appData.InstallArgs = appData.InstallArgs.Replace("$(DownloadedFile)", appData.DownloadedFile);

            appData.InstallArgs = appData.InstallArgs.Replace("$(ProgramFiles)", programFiles);
            appData.InstallArgs = appData.InstallArgs.Replace("$(ProgramFilesX86)", programFilesX86);

            appData.InstallPath = appData.InstallPath.Replace("$(Version)", appData.Version);
            appData.InstallPath = appData.InstallPath.Replace("$(ProgramFiles)", programFiles);
            appData.InstallPath = appData.InstallPath.Replace("$(ProgramFilesX86)", programFilesX86);


            return !Path.IsPathRooted(appData.InstallPath)
                ? throw new ParserException($"App: {appData.Name}, InstallPath {appData.InstallPath} must be rooted. i.e. C:\\Program Files\\Nbuild", null)
                : appData;
        }
    }
}