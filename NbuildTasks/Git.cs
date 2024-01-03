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
        private const string GetTagCommand = "GetTag";
        private const string GetBranchCommand = "GetBranch";
        private const string AutoTagCommand = "AutoTag";
        private const string StagingBuildType = "Staging";
        private const string ProductionBuildType = "Production";

        private const string SetTagCommand = "SetTag";
        private const string DeleteTagCommand = "DeleteTag";
        private const string PushTagCommand = "PushTag";

        private readonly Parameters Parameters = new Launcher.Parameters
        {
            WorkingDir = Environment.CurrentDirectory,
            FileName = GitBinary,
            RedirectStandardOutput = true,
            Verbose = true
        };

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

            switch (Command)
            {
                case GetTagCommand:
                    Output = GetTag();
                    Log.LogMessage($"In task: {Output}");
                    break;

                case GetBranchCommand:
                    Output = GetBranch();
                    Log.LogMessage($"In Task: {Output}");
                    break;

                case AutoTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter) &&
                        (TaskParameter == StagingBuildType || TaskParameter == ProductionBuildType))
                    {
                        BuildType = TaskParameter;
                        Console.WriteLine($"BuildType: {BuildType}");

                        Output = AutoTag(BuildType);
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

                    Output = SetTag(Tag) ? Tag : "";
                    break;

                case DeleteTagCommand:
                    if (!string.IsNullOrEmpty(TaskParameter))
                    {
                        Tag = TaskParameter;
                        Console.WriteLine($"Tag: {Tag}");
                        Output = DeleteTag(Tag) ? "True" : "False";
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
                        Output = PushTag(GetBranch(), Tag) ? "True" : "False";
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

        private string AutoTag(string buildType)
        {
            if (string.IsNullOrEmpty(buildType))
            {
                Log.LogError($"BuildType is null or empty");
                return string.Empty;
            }

            if (buildType.Equals("Staging", StringComparison.OrdinalIgnoreCase))
            {
                return AutoTagStaging();
            }
            else if (buildType.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                return AutoTagProduction();
            }
            else
            {
                Log.LogError($"Unknown buildType: {buildType}");
                return string.Empty;
            }

        }

        private string AutoTagProduction()
        {
            var currentTag = GetTag();

            return ProductionTag(currentTag);
        }

        /// <summary>
        /// Push new Tag to remote repo
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="newTag"></param>
        /// <returns>True if command is successful, otherwise False</returns>
        public bool PushTag(string branch, string newTag)
        {
            if (string.IsNullOrEmpty(branch) || string.IsNullOrEmpty(newTag))
            {
                return false;
            }

            Parameters.Arguments = $"push origin {branch} {newTag}";
            var result = Launcher.Launcher.Start(Parameters);
            if ((result.Code == 0) && (result.Output.Count >= 0))
            {
                foreach (var line in result.Output)
                {
                    if (Parameters.Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal"))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public bool SetTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag)) return false;

            if (!IsValidTag(newTag)) return false;

            bool resultSetTag = true;
            Parameters.Arguments = $"tag -a {newTag} HEAD -m \"Automated tag\"";
            var result = Launcher.Launcher.Start(Parameters);
            Console.WriteLine($"result.Code: {result.Code}, result.Output.Count: {result.Output.Count}");
            if ((result.Code == 0) && (result.Output.Count >= 0))
            {
                foreach (var line in result.Output)
                {
                    if (Parameters.Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal"))
                    {
                        resultSetTag = false;
                        break;
                    }
                    else
                    {
                        resultSetTag = true;
                    }
                }
            }

            //if (resultSetTag)
            //{
            //    resultSetTag = UpdateTagToVersionFiles(newTag);
            //}

            Console.WriteLine($"resultSetTag: {resultSetTag}");
            return resultSetTag;
        }
        private static bool IsValid4Tag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                return false;
            }

            string[] items = newTag.Split('.');
            if (items == null || items.Length != 4)
            {
                return false;
            }
            else
            {
                foreach (var item in items)
                {
                    if (!UInt32.TryParse(item, out _))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsValidTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                return false;
            }



            string[] items = newTag.Split('.');
            if (items == null || items.Length != 3)
            {
                return false;
            }
            else
            {
                foreach (var item in items)
                {
                    if (!UInt32.TryParse(item, out _))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Given a valid tag, get staging tag
        /// given a valid tag m.n.p.b, staging tag is m.n.p.b+1
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>return valid stagging tag, otherwise null if failed</returns>
        public static string StagingTag(string tag)
        {
            if (tag == null)
            {
                return null;
            }

            if (IsValid4Tag(tag))
            {
                string[] items4 = tag.Split('.');
                tag = $"{items4[0]}.{items4[1]}.{items4[2]}";
            }

            if (!IsValidTag(tag))
            {
                return null;
            }
            else
            {
                string[] version = tag.Split('.');
                version[2] = ((Int32.Parse(version.Last())) + 1).ToString();
                return string.Join(".", version);
            }
        }

        /// <summary>
        /// Given a valid tag, get tag
        /// given a valid tag m.n.p, production tag is m.n.p+1
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>return valid Production tag, otherwise null if failed</returns>
        public static string ProductionTag(string tag)
        {
            if (tag == null)
            {
                return null;
            }

            // for backward compatibility, convert 4 digits tag to 3 digits tag
            if (IsValid4Tag(tag))
            {
                string[] items4 = tag.Split('.');
                tag = $"{items4[0]}.{items4[1]}.{items4[2]}";
            }

            if (!IsValidTag(tag))
            {
                return null;
            }
            else
            {
                string[] version = tag.Split('.');
                version[1] = ((Int32.Parse(version[1])) + 1).ToString();
                version[2] = "0";
                try
                {
                    string productionTag = string.Join(".", version);
                    if (IsValidTag(productionTag))
                    {
                        return productionTag;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred when building a production tag: {ex.Message}");
                    return null;
                }
            }
        }

        private string AutoTagStaging()
        {
            var currentTag = GetTag();

            return StagingTag(currentTag);

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

        private bool DeleteTag(string tag)
        {
            Console.WriteLine($"DeleteTag Start");
            if (!IsValidTag(tag)) return false;
            bool resultDeleteTag = DeleteLocalTag(tag);
            if (resultDeleteTag)
            {
                resultDeleteTag = DeleteRemoteTag(tag);
            }

            Console.WriteLine($"DeleteTag End");
            return resultDeleteTag;
        }

        private bool DeleteLocalTag(string tag)
        {
            Parameters.Arguments = $"tag -d {tag}";
            var result = Launcher.Launcher.Start(Parameters);
            Console.WriteLine($"result.Code: {result.Code}, result.Output.Count: {result.Output.Count}");
            if (result.Code == 0 && result.Output.Count >= 1)
            {
                foreach (var line in result.Output)
                {
                    if (line.StartsWith($"Deleted tag '{tag}'"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DeleteRemoteTag(string tag)
        {
            Parameters.Arguments = $"push origin :refs/tags/{tag}";
            var result = Launcher.Launcher.Start(Parameters);
            if (result.Code == 0 && result.Output.Count >= 1)
            {
                foreach (var line in result.Output)
                {
                    if (line.Contains($"[deleted]") && line.Contains(tag))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
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
