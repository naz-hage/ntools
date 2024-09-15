// This task is used to run a PowerShell script using pwsh (PowerShell Core) as MSBuild Task
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
        
        [Output]
        public int ExitCode { get; set; }

        private readonly string PowerShellExe = "pwsh";
        public override bool Execute()
        {
            if (!Path.IsPathRooted(ScriptPath))
            {
                Log.LogError($"{PowerShellExe} - Script Path '{ScriptPath}' is not rooted");
            }

            if (!File.Exists(ScriptPath))
            {
                Log.LogError($"{PowerShellExe}  - Script '{ScriptPath}' does not exist");
            }
            else
            {
                Log.LogMessage($"{PowerShellExe}  - '{ScriptPath} {Arguments}' in '{WorkingDirectory}'");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = PowerShellExe,
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
                ExitCode = process.ExitCode;
                Log.LogMessage(MessageImportance.High, output);
                if (process.ExitCode != 0)
                {
                    Log.LogError($"Exit Code: {process.ExitCode} \n{error}");
                    return false;
                }

                return true;
            }

            return !Log.HasLoggedErrors;
        }
    }
}
