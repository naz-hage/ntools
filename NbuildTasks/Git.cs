using Launcher;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NbuildTasks
{
    public class Git : Microsoft.Build.Utilities.Task
    {
        private const string GitBinary = @"c:\program files\Git\cmd\git.exe";

        private readonly Parameters Parameters = new Launcher.Parameters
        {
            WorkingDir = Environment.CurrentDirectory,
            FileName = GitBinary,
            RedirectStandardOutput = true,
            Verbose = true
        };

        [Required]
        public string Command { get; set; }

        public string TaskParameter { get; set; }

        [Output]
        public string Output { get; set; }

        public override bool Execute()
        {
            if (Command == "GetTag")
            {
                Output = GetTag();

                Log.LogMessage($"In task: {Output}"); // Log the OutputTag property value
            }
            else if (Command == "GetBranch")
            {
                Output = GetBranch();

                Log.LogMessage($"In Task: {Output}"); // Log the OutputBranch property value
            }
            else
            {
                Log.LogError($"Unknown command: {Command}");
                return !Log.HasLoggedErrors; // Return true if no errors were logged
            }

            if (string.IsNullOrEmpty(Output))
            {
                Log.LogError($"No output from command: {Command}");
            }
            
            return !Log.HasLoggedErrors; // Return true if no errors were logged
        }

        private string GetBranch()
        {
            var branch = string.Empty;  
            Parameters.Arguments = $"branch";
            var result = Launcher.Launcher.Start(Parameters);
            if ((result.Code == 0) && (result.Output.Count > 0))
            {

                foreach (var line in result.Output)
                {
                    if (Parameters.Verbose) Console.WriteLine(line);

                    if (line.StartsWith("*"))
                    {
                        //branch = line[2..];
                        branch = line.Substring(2);
                        break;
                    }
                }
            }

            return branch;
        }

        public string GetTag()
        {
            Parameters.Arguments = $"describe --abbrev=0 --tags";
            var result = Launcher.Launcher.Start(Parameters);
            if ((result.Code == 0) && (result.Output.Count == 1))
            {
                if (CheckForErrorAndDisplayOutput(result.Output))
                {
                    return string.Empty;
                }
                else
                {
                    return result.Output[0];
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private static bool CheckForErrorAndDisplayOutput(List<string> lines)
        {
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("error") ||
                    line.ToLower().Contains("fatal"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

    }
}
