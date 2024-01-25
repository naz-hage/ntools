using Microsoft.Build.Framework;
using System;
using System.Net.Http;
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
            HttpClient httpClient = new HttpClient();
            var result = Task.Run(async () => await httpClient.DownloadFileAsync(new Uri(WebUri), FileName)).Result;

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
