using Microsoft.Build.Framework;
using System.Diagnostics;
using System.IO;

namespace NbuildTasks
{
    public class FileVersion : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string Name { get; set; }

        [Output]
        public string Output { get; set; }

        public override bool Execute()
        {
            if (!Path.IsPathRooted(Name))
            {
                Log.LogError($"Task - Path is not rooted: {Name}");
            }

            if (!File.Exists(Name))
            {
                Log.LogError($"Task - File does not exist: {Name}");
            }
            else
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Name);
                Output = fileVersionInfo.FileVersion;
                if (string.IsNullOrEmpty(Output))
                {
                    Log.LogError($"Task - Failed to get file version for {Name}");
                }
                else
                {
                    Log.LogMessage($"Version: {Output,-20}  File: {Name} ");
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
