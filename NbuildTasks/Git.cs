using Microsoft.Build.Framework;

namespace NbuildTasks
{
    public class Git : Microsoft.Build.Utilities.Task
    {
        private const string GetTagCommand = "GetTag";
        private const string GetBranchCommand = "GetBranch";

        private const string AutoTagCommand = "AutoTag";

        private const string SetTagCommand = "SetTag";
        private const string DeleteTagCommand = "DeleteTag";
        private const string PushTagCommand = "PushTag";

        [Required]
        public string Command { get; set; }

        // Only required for SetTag, DeleteTag, PushTag as tag parameter
        // and for AutoTag as build type parameter
        public string TaskParameter { get; set; }

        [Output]
        public string Output { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(Command))
            {
                Log.LogError("NbuildTask: Command is null or empty");
                return !Log.HasLoggedErrors;
            }
            Log.LogMessage($"NbuildTask: {Command}");
            var gitWrapper = new GitWrapper(project: null, verbose: false);
            if (!gitWrapper.IsGitConfigured())
            {
                Log.LogError("Git is not configured");
                return !Log.HasLoggedErrors;
            }

            switch (Command)
            {
                case GetTagCommand:
                    Output = gitWrapper.Tag;
                    Log.LogMessage($"NbuildTask: {Output}");
                    break;

                case GetBranchCommand:
                    Output = gitWrapper.Branch;
                    Log.LogMessage($"NbuildTask: {Output}");
                    break;

                case AutoTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter) &&
                        Enums.BuildType.TryParse<Enums.BuildType>(TaskParameter, true, out var buildType))
                    {
                        Log.LogMessage($"BuildType: {TaskParameter}");

                        Output = gitWrapper.AutoTag(buildType.ToString());
                    }
                    else
                    {
                        Log.LogError($"BuildType is null or empty");
                    }
                    break;

                case SetTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Log.LogMessage($"Tag: {TaskParameter}");
                    }

                    Output = gitWrapper.SetTag(TaskParameter) ? TaskParameter : "";
                    break;

                case DeleteTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Log.LogMessage($"Tag: {TaskParameter}");
                        Output = gitWrapper.DeleteTag(TaskParameter) ? "True" : "False";
                    }
                    else
                    {
                        Log.LogError($"Tag is null or empty");
                    }

                    break;

                case PushTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Log.LogMessage($"Tag: {TaskParameter}");
                        Output = gitWrapper.PushTag(TaskParameter) ? "True" : "False";
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

            Log.LogMessage($"Output: {Output}");

            return !Log.HasLoggedErrors; // Return true if no errors were logged
        }
    }
}
