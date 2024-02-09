using CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Nbuild
{
    public record NbuildApps(string Version, List<NbuildApp> NbuildAppList);

    public class NbuildApp
    {
        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }    // the name of the app to download
        [Required(ErrorMessage = "Version is required")]
        public string? Version { get; set; } // the version of the app to download
        [Required(ErrorMessage = "AppFileName is required")]
        public string? AppFileName { get; set; }     // name of File that hold the app version.  File should have file version info
        [Required(ErrorMessage = "WebDownloadFile is required")] 
        public string? WebDownloadFile { get; set; }     // the url to download the app from. can use $(Version) to substitute the version number
        [Required(ErrorMessage = "DownloadedFile is required")] 
        public string? DownloadedFile { get; set; }     // the downloaded file name, can use $(Version) to substitute the version number
        [Required(ErrorMessage = "InstallCommand is required")] 
        public string? InstallCommand { get; set; }  // install command to run to install the app. i.e. setup.exe
        [Required(ErrorMessage = "InstallArgs is required")] 
        public string? InstallArgs { get; set; }     // the arguments to pass to the install command. can use $(Version) and $(InstallPath) to substitute the version number
        [Required(ErrorMessage = "InstallPath is required")]
        public string? InstallPath { get; set; }     // The directory required to locate the AppFileName. path must be rooted. i.e. C:\Program Files\Nbuild can use $(Version) to substitute the version number
        [Required(ErrorMessage = "UninstallCommand is required")] 
        public string? UninstallCommand { get; set; }  // uninstall command to run to uninstall the app. i.e. setup.exe
        [Required(ErrorMessage = "UninstallArgs is required")] 
        public string? UninstallArgs { get; set; }     // the arguments to pass to the uninstall command. can use $(Version) and $(InstallPath) to substitute the version number
    }
}