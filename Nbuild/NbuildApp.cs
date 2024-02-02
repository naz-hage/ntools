using CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nbuild
{
    public record NbuildApps(string Version, List<NbuildApp> NbuildAppList);

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
    }
}