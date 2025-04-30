using Ntools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static NbuildTasks.Enums;

namespace NbuildTasks
{
    public class GitWrapper : NtoolsEnvironmentVariables
    {
        private const string GitBinary = "git.exe";

        private static readonly Process Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = ShellUtility.GetFullPathOfFile(GitBinary),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = false,
                UseShellExecute = false,
            }
        };

        public bool Verbose = false;

        /// <summary>
        /// Initializes a new instance of the GitWrapper class.
        /// Once Successful, The MainDir and DevDrive are set by the base class NtoolsEnvironmentVariables
        /// </summary>
        /// <param name="project">The project directory.</param>
        /// <param name="verbose">A flag indicating whether to enable verbose output.</param>
        /// <param name="testMode">A flag indicating whether to enable test mode.</param>
        public GitWrapper(string project = null, bool verbose = false, bool testMode = false) : base(testMode)
        { 
            Verbose = verbose;

            Process.StartInfo.WorkingDirectory = project == null ? Environment.CurrentDirectory : $@"{DevDrive}\{MainDir}\{project}";

            // print Parameters
            if (verbose) Console.WriteLine($"GitWrapper.Process.StartInfo.WorkingDirectory: {Process.StartInfo.WorkingDirectory}");
        }

        public bool SetWorkingDir(string url)
        {
            var projectName = url.Split('/').Last().Split('.').First();
            if (string.IsNullOrEmpty(projectName))
            {
                return false;
            }

              // change to project directory
            var DevDir = $"{DevDrive}\\{MainDir}";

            var solutionDir = $@"{DevDir}\{projectName}";

            Process.StartInfo.WorkingDirectory = string.IsNullOrEmpty(projectName) ? Environment.CurrentDirectory : solutionDir;

            if (Verbose) Console.WriteLine($"GitWrapper.Process.StartInfo.WorkingDirectory: {Process.StartInfo.WorkingDirectory}");

            return true;
        }

        public string Branch => GetBranch();

        public string Tag => GetTag();

        public string WorkingDirectory
        {
            get { return Process.StartInfo.WorkingDirectory; }
            set { Process.StartInfo.WorkingDirectory = value; }
        }

        public string SetAutoTag(string buildType)
        {
            var nextTag = String.Empty;

            if (!string.IsNullOrEmpty(buildType))
            {
                nextTag = AutoTag(buildType);
                if (!string.IsNullOrEmpty(nextTag) && SetTag(nextTag))
                {
                    return nextTag;
                }
            }

            return nextTag;
        }

        /// <summary>
        /// Push new Tag to remote repo
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="newTag"></param>
        /// <returns>True if command is successful, otherwise False</returns>
        public bool PushTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                throw new ArgumentException("Tag cannot be null or empty", nameof(newTag));
            }

            if (string.IsNullOrEmpty(Branch))
            {
                throw new InvalidOperationException("Branch cannot be null or empty");
            }

            // Pull the latest changes from the remote branch
            Process.StartInfo.Arguments = $"pull origin {Branch}";
            var pullResult = Process.LockStart(Verbose);
            if (pullResult.Code != 0)
            {
                Console.WriteLine("Failed to pull the latest changes from the remote branch.");
                foreach (var line in pullResult.Output)
                {
                    if (Verbose) Console.WriteLine(line);
                }
                return false;
            }

            // Push the new tag to the remote repository
            Process.StartInfo.Arguments = $"push origin {Branch} {newTag}";
            var pushResult = Process.LockStart(Verbose);
            if (pushResult.Code == 0 && pushResult.Output.Count > 0)
            {
                foreach (var line in pushResult.Output)
                {
                    if (Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal") || line.Contains("error"))
                    {
                        Console.WriteLine($"Error pushing tag: {line}");
                        return false;
                    }
                }
                return true;
            }
            else
            {
                foreach (var line in pushResult.Output)
                {
                    if (Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal") || line.Contains("error"))
                    {
                        Console.WriteLine($"Error pushing tag: {line}");
                    }
                }
                return false;
            }
        }

        public bool SetTag(string newTag)
        {
            if (string.IsNullOrEmpty(newTag)) return false;

            if (!IsValidTag(newTag)) return false;

            if (LocalTagExists(newTag)) DeleteLocalTag(newTag);

            bool resultSetTag = true;
            for (int i = 0; i < 2; i++)
            {
                Process.StartInfo.Arguments = $"tag -a {newTag} HEAD -m \"Automated tag\"";

                var result = Process.LockStart(Verbose);
                if ((result.Code == 0) && (result.Output.Count >= 0))
                {
                    foreach (var line in result.Output)
                    {
                        if (Verbose) Console.WriteLine(line);
                        if (line.Contains("fatal"))
                        {
                            resultSetTag = false;
                            break;
                        }
                        else
                        {
                            resultSetTag = PushTag(newTag);
                        }
                    }
                }
                if (Verbose) Console.WriteLine($"SetTag try: {++i}");
                if (resultSetTag) break;
            }

            return resultSetTag;
        }

        /// <summary>
        /// Given a valid tag, get stage tag
        /// given a valid tag m.n.p.b, stage tag is m.n.p.b+1
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>return valid stage tag, otherwise null if failed</returns>
        private string GetBranch()
        {
            var branch = string.Empty;
            Process.StartInfo.Arguments = $"rev-parse --abbrev-ref HEAD";
            var result = Process.LockStart(Verbose);
            if ((result.Code == 0) && (result.Output.Count == 1))
            {
                branch = result.GetFirstOutput();
            }

            return branch;
        }

        private string GetTag()
        {
            Process.StartInfo.Arguments = $"describe --abbrev=0 --tags";

            var result = Process.LockStart(Verbose);
            return (result.Code == 0) && (result.Output.Count == 1)
                ? CheckForErrorAndDisplayOutput(result.Output) ? string.Empty : result.GetFirstOutput()
                : string.Empty;
        }

        public bool LocalTagExists(string tag)
        {
            return ListLocalTags().Any(x => x.Contains(tag));
        }

        public bool RemoteTagExists(string tag)
        {
            return ListRemoteTags().Any(x => x.Contains(tag));
        }

        public bool DeleteTag(string tag)
        {
            bool bResult = !ListLocalTags().Contains(tag) || DeleteLocalTag(tag);

            // check if tag exists

            var remoteTags = ListRemoteTags();
            if (bResult && remoteTags.Any(x => x.Contains(tag)))
                bResult = DeleteRemoteTag(tag);

            return bResult;
        }

        public List<string> ListLocalTags()
        {
            Process.StartInfo.Arguments = $"tag --list";

            var result = Process.LockStart(Verbose);
            return result.Code == 0 && result.Output.Count >= 1 ? result.Output.ToList() : new List<string>();
        }

        private List<string> DeleteRemoteTags(string url)
        {
            var tags = new List<string>();
            Process.StartInfo.Arguments = $"ls-remote --tags {url}";
            var result = Process.LockStart(Verbose);
            if (result.Code == 0 && result.Output.Count >= 1)
            {
                foreach (var line in result.Output)
                {
                    // extract tag from line
                    var items = line.Split('/');
                    var tag = items.Last();
                    Console.WriteLine($"{line} -deleting {tag}");
                    DeleteRemoteTag(tag);
                    tags.Add(line);
                }
            }

            return tags;
        }

        public List<string> ListRemoteTags()
        {
            var tags = new List<string>();
            Process.StartInfo.Arguments = $"ls-remote --tags";
            var result = Process.LockStart(Verbose);
            if (result.Code == 0 && result.Output.Count >= 1)
            {
                foreach (var line in result.Output)
                {
                    // extract tag from line
                    var items = line.Split('/');
                    var tag = items.Last();
                    if (IsValid4Tag(tag) || IsValidTag(tag))
                        tags.Add(tag);
                }
            }

            return tags;
        }

        private bool DeleteLocalTag(string tag)
        {
            if (!LocalTagExists(tag)) return true;

            Process.StartInfo.Arguments = $"tag -d {tag}";

            var result = Process.LockStart(Verbose);
            return result.Code == 0
                    && result.Output.Count >= 1
                    && result.Output.Exists(line => line.StartsWith($"Deleted tag '{tag}'"));
        }

        private bool DeleteRemoteTag(string tag)
        {
            if (!RemoteTagExists(tag)) return true;

            Process.StartInfo.Arguments = $"push origin :refs/tags/{tag}";
            var result = Process.LockStart(Verbose);
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

        public ResultHelper CloneProject(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return ResultHelper.Fail(ResultHelper.InvalidParameter, $"Invalid url: {url}");
            }

            // extract project name from url
            var projectName = url.Split('/').Last().Split('.').First();
            if (string.IsNullOrEmpty(projectName))
            {
                return ResultHelper.Fail(ResultHelper.InvalidParameter, $"Invalid url: {url}");
            }
            // check if dev drive exists
            if (!Directory.Exists(DevDrive))
            {
                return ResultHelper.Fail(ResultHelper.InvalidParameter, $"Invalid DevDrive: {DevDrive}");
            }

            ResultHelper result;
            // change to project directory
            var DevDir = $"{DevDrive}\\{MainDir}";

            var solutionDir = $@"{DevDir}\{projectName}";
            if (Verbose)
            {
                Console.WriteLine($"Clone path: {solutionDir}");
            }

            var dirExists = Directory.Exists(solutionDir);
            if (!dirExists)
            {
                if (!Directory.Exists(DevDir))
                {
                    Directory.CreateDirectory(DevDir);
                }

                Process.StartInfo.WorkingDirectory = DevDir;
                Process.StartInfo.Arguments = $"clone {url} ";

                result = Process.LockStart(Verbose);
                if ((result.Code == 0) && (result.Output.Count > 0))
                {
                    if (CheckForErrorAndDisplayOutput(result.Output))
                    {
                        return ResultHelper.Fail((int)RetCode.CloneProjectFailed, $"Failed to clone project: {projectName}");
                    }
                    // reset working directory so other dir commands can be executed
                    SetWorkingDir(url);
                }
            }
            else
            {
                return ResultHelper.Fail((int)RetCode.CloneProjectFailed, $"Project already exists: {solutionDir}");
            }
            // change to solution directory 
            Directory.SetCurrentDirectory(solutionDir);
            return result;
        }

        public bool CheckoutBranch(string branch, bool create = false)
        {
            ResultHelper result;
            if (create && !BranchExists(branch))
            {
                Process.StartInfo.Arguments = $"checkout -b {branch}";
                result = Process.LockStart(Verbose);
            }
            else
            {
                // checkout
                Process.StartInfo.Arguments = $"checkout {branch}";
                result = Process.LockStart(Verbose);
            }

            if (result.Code == 0 && result.Output.Count >= 1)
            {
                if (CheckForErrorAndDisplayOutput(result.Output))
                {
                    return false;
                }
            }

            return Branch == branch;
        }

        public bool BranchExists(string branch)
        {
            return !string.IsNullOrEmpty(branch) && ListBranches().Contains(branch);
        }

        public List<string> ListBranches()
        {
            var branches = new List<string>();

            Process.StartInfo.Arguments = $"branch --list";
            var result = Process.LockStart(Verbose);
            if (result.Code == 0 && result.Output.Count >= 1)
            {
                foreach (var line in result.Output)
                {
                    // branch names stars on the 3rd char.
                    // current branch starts with a '* '
                    // others start with '  '
                    branches.Add(line.Substring(2));  //branches.Add(line[2..])
                }
            }

            return branches;
        }

        private bool CheckForErrorAndDisplayOutput(List<string> lines)
        {
            foreach (var line in lines)
            {
                return line.ToLower().Contains("error") ||
                    line.ToLower().Contains("fatal");
            }
            return false;
        }

        public bool IsGitRepository(string currentDirectory)
        {
            Process.StartInfo.Arguments = "rev-parse --is-inside-work-tree";
            var result = Process.LockStart(Verbose);
            if ((result.Code == 0) && (result.Output.Count >= 0))
            {
                foreach (var line in result.Output)
                {
                    if (Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal: not a git repository"))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if git is Configured
        /// </summary>
        /// <param name="silent">If true, suppresses the console output for configuration instructions.</param>
        /// <returns>True if git is configured, otherwise False</returns>
        public bool IsGitConfigured(bool silent = false)
        {
            if (!string.IsNullOrEmpty(GetGitUserNameConfiguration()) && !string.IsNullOrEmpty(GetGitUserEmailConfiguration()))
            {
                return true;
            }

            if (!silent)
            {
                Console.WriteLine("Git is not configured. To configure git, run the commands below:\n");
                Console.WriteLine("git config --global user.name <YourName>");
                Console.WriteLine("git config --global user.email <your.email@example.com>");
            }

            return false;
        }
        
        /// <summary>
        /// Get git global user.name configuration.
        /// </summary>
        /// <returns>The git global user.name if configured, otherwise null.</returns>
        public string GetGitUserNameConfiguration()
        {
            Process.StartInfo.Arguments = "config --global user.name";
            var result = Process.LockStart(Verbose);
            if ((result.Code == 0) && (result.Output.Count >= 0))
            {
                foreach (var line in result.Output)
                {
                    if (Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal: unable to read config file"))
                    {
                        return null;
                    }
                    else
                    {
                        return line;
                    }
                }
                return null;
            }
            return null;
        }

        /// <summary>
        /// Get git global user.email configuration.
        /// </summary>
        /// <returns>The git global user.email if configured, otherwise null.</returns>
        public string GetGitUserEmailConfiguration()
        {
            Process.StartInfo.Arguments = "config --global user.email";
            var result = Process.LockStart(Verbose);
            if ((result.Code == 0) && (result.Output.Count >= 0))
            {
                foreach (var line in result.Output)
                {
                    if (Verbose) Console.WriteLine(line);
                    if (line.Contains("fatal: unable to read config file"))
                    {
                        return null;
                    }
                    else
                    {
                        return line;
                    }
                }
                return null;
            }
            return null;
        }

        #region tag related methods
        /// <summary>
        /// Validates if the provided tag is a valid 4-part tag.
        /// </summary>
        /// <param name="tag">The tag to be validated.</param>
        /// <returns><c>true</c> if the tag is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid4Tag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            string[] items = tag.Split('.');
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

        /// <summary>
        /// Determines whether the specified tag is a valid 3-part tag.
        /// </summary>
        /// <param name="tag">The tag to validate.</param>
        /// <returns><c>true</c> if the tag is valid; otherwise, <c>false</c>.</returns>
        public bool IsValidTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            string[] items = tag.Split('.');
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
        /// Automatically generates a tag based on the build type.
        /// </summary>
        /// <param name="buildType">The build type: `production` or `stage`.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>The generated tag if the buildType is `production` or `stage`.  Otherwise throw a message</returns>
        public string AutoTag(string buildType)
        {
            if (string.IsNullOrEmpty(buildType))
            {
                Console.WriteLine($"BuildType is null or empty");
                return string.Empty;
            }

            if (buildType.Equals(Enums.BuildType.STAGE.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return StageTag();
            }
            else if (buildType.Equals(Enums.BuildType.PROD.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return ProdTag();
            }
            else
            {
                Console.WriteLine($"Unknown buildType: {buildType}");
                throw new ArgumentException($"Unknown buildType: {buildType}");
            }
        }


        /// <summary>
        /// Returns the stage tag based on the provided tag.
        /// </summary>
        /// <param name="tag">The tag to generate the stage tag from.</param>
        /// <returns>The stage tag.</returns>
        public string StageTag()
        {
            var tag = Tag;
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
        /// Generates a production tag based on the provided tag.
        /// </summary>
        /// <param name="tag">The input tag.</param>
        /// <returns>The generated production tag, or null if the input tag is invalid.</returns>
        public string ProdTag()
        {
            var tag = Tag;
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
                    return IsValidTag(productionTag) ? productionTag : null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred when building a production tag: {ex.Message}");
                    return null;
                }
            }
        }
        #endregion
    }
}
