using Launcher;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NbuildTasks
{
    public class Git : Microsoft.Build.Utilities.Task
    {
        
        private const string GetTagCommand = "GetTag";
        private const string GetBranchCommand = "GetBranch";
        private const string AutoTagCommand = "AutoTag";
        private const string StagingBuildType = "Staging";
        private const string ProductionBuildType = "Production";

        private const string SetTagCommand = "SetTag";
        private const string DeleteTagCommand = "DeleteTag";
        private const string PushTagCommand = "PushTag";

        

        [Required]
        public string Command { get; set; }

        public string BuildType { get; set; }

        public string Tag { get; set; }

        public string TaskParameter { get; set; }

        [Output]
        public string Output { get; set; }

        public override bool Execute()
        {
            if (!string.IsNullOrEmpty(Command))
            {
                Console.WriteLine($"Command: {Command}");
            }
            else
            {
                Log.LogError("Command is null or empty");
                return !Log.HasLoggedErrors;
            }
            var gitWrapper = new GitWrapper();
            switch (Command)
            {
                case GetTagCommand:
                    Output = gitWrapper.GetTag();
                    Log.LogMessage($"In task: {Output}");
                    break;

                case GetBranchCommand:
                    Output = gitWrapper.GetBranch();
                    Log.LogMessage($"In Task: {Output}");
                    break;

                case AutoTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter) &&
                        (TaskParameter == StagingBuildType || TaskParameter == ProductionBuildType))
                    {
                        BuildType = TaskParameter;
                        Console.WriteLine($"BuildType: {BuildType}");

                        Output = gitWrapper.AutoTag(BuildType);
                    }
                    else
                    {
                        Log.LogError($"BuildType is null or empty");
                    }
                    break;

                case SetTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Tag = TaskParameter;
                        Console.WriteLine($"Tag: {Tag}");
                    }

                    Output = gitWrapper.SetTag(Tag) ? Tag : "";
                    break;

                case DeleteTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Tag = TaskParameter;
                        Console.WriteLine($"Tag: {Tag}");
                        Output = gitWrapper.DeleteTag(Tag) ? "True" : "False";
                    }
                    else
                    {
                        Log.LogError($"Tag is null or empty");
                    }

                    break;

                case PushTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Tag = TaskParameter;
                        Console.WriteLine($"Tag: {Tag}");
                        Output = gitWrapper.PushTag(gitWrapper.GetBranch(), Tag) ? "True" : "False";
                    }
                    else
                    {
                        Log.LogError($"Tag is null or empty");
                    }

                    break;

                default:
                    Log.LogError($"Unknown command: {Command}");
                    return !Log.HasLoggedErrors;
            }

            if (string.IsNullOrEmpty(Output))
            {
                Log.LogError($"No output from command: {Command}");
            }
            
            return !Log.HasLoggedErrors; // Return true if no errors were logged
        }



    }
}
