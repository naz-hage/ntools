using Microsoft.Build.Framework;
using System.Diagnostics;
using System.IO;

namespace NbuildTasks
{
    public class Pwsh : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string ScriptPath { get; set; }

        public string Arguments { get; set; }

        public string WorkingDirectory { get; set; }

        public override bool Execute()
        {
            if (!Path.IsPathRooted(ScriptPath))
            {
                Log.LogError($"Task - Script Path '{ScriptPath}' is not rooted");
            }

            if (!File.Exists(ScriptPath))
            {
                Log.LogError($"Task - Script '{ScriptPath}' does not exist");
            }
            else
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pwsh",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{ScriptPath}\" {Arguments}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = WorkingDirectory // Set the working directory
                    }
                };

                process.Start();
                process.WaitForExit();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                Log.LogMessage(MessageImportance.High, output);
                if (process.ExitCode != 0)
                {
                    Log.LogError(error);
                    return false;
                }

                return true;
            }

            return !Log.HasLoggedErrors;
        }
    }
}
