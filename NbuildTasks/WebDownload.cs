using Microsoft.Build.Framework;
using Ntools;
using System.Threading.Tasks;

namespace NbuildTasks
{
    public class WebDownload : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string WebUri { get; set; }

        [Required]
        public string FileName { get; set; }

        public override bool Execute()
        {
            var result = Task.Run(async () => await Nfile.DownloadAsync(WebUri, FileName)).Result;

            if (result.IsSuccess())
            {
                Log.LogMessage($"Downloaded {WebUri} to {FileName}");
            }
            else
            {

                Log.LogError($"Failed to download {WebUri} to {FileName}: {result.GetFirstOutput()}");
            }

            return !Log.HasLoggedErrors;
        }
    }
}
