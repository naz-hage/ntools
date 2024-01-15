using Launcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static NbuildTasks.Enums;

namespace NbuildTasks
{
    public class GitWrapper
    {
        private const string GitBinary = @"C:\Program Files\Git\cmd\git.exe";
        public static readonly Parameters Parameters = new Launcher.Parameters
        {
            WorkingDir = Environment.CurrentDirectory,
            FileName = GitBinary,
            RedirectStandardOutput = true,
        };

        public  string DevDrive { get; set; } = "d:";  // This default was chosen because of GitHub Actions
        public string MainDir { get; set; } = "a";  // This default was chosen because of GitHub Actions

        public GitWrapper( string project = null, bool verbose = false)
        {
            Parameters.Verbose = verbose;

            // Read Environment variables DevDrive and MainDir and set Properties respectively
            
            var devDrive = Environment.GetEnvironmentVariable("DevDrive");
            if (!string.IsNullOrEmpty(devDrive))
            {
                DevDrive = devDrive;
            }
            

            var mainDir = Environment.GetEnvironmentVariable("MainDir");
            if (!string.IsNullOrEmpty(mainDir))
            {
                MainDir = mainDir;
            }
            // hard code for now to debug GitHub Actions failures
            //DevDrive = "d:";
            //MainDir = "a";

            if (project == null )
            {
                Parameters.WorkingDir = Environment.CurrentDirectory;
            }
            else
            {
                Parameters.WorkingDir = $@"{DevDrive}\{MainDir}\{project}";
            }

            // print Parameters
            if (verbose) Console.WriteLine($"GitWrapper.Parameters.WorkingDir: {Parameters.WorkingDir}");
        }

        public string GetWorkingDir()
        {
            if (Parameters.Verbose) Console.WriteLine($"GitWrapper.Parameters.WorkingDir: {Parameters.WorkingDir}");

            return Parameters.WorkingDir;
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

            if (string.IsNullOrEmpty(projectName))
            {
                Parameters.WorkingDir = Environment.CurrentDirectory;
            }
            else
            {
                Parameters.WorkingDir = solutionDir;
            }

            if (Parameters.Verbose) Console.WriteLine($"GitWrapper.Parameters.WorkingDir: {Parameters.WorkingDir}");

            return true;
        }

        public string Branch { get { return GetBranch(); } }

        public string Tag { get { return GetTag(); } }

        public string AutoTag(string buildType)
        {
            if (string.IsNullOrEmpty(buildType))
            {
                Console.WriteLine($"BuildType is null or empty");
                return string.Empty;
            }

            if (buildType.Equals("Staging", StringComparison.OrdinalIgnoreCase))
            {
                return StagingTag();
            }
            else if (buildType.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                return ProductionTag();
            }
            else
            {
                Console.WriteLine($"Unknown buildType: {buildType}");
                return string.Empty;
            }

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

        //private string AutoTagProduction()
        //{
        //    var currentTag = GetTag();

        //    return ProductionTag(currentTag);
        //}

        //private string AutoTagStaging()
        //{
        //    var currentTag = GetTag();

        //    return StagingTag(currentTag);

        //}

        public string StagingTag()
        {
            var tag = GetTag();
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
        public string ProductionTag()
        {
            var tag = GetTag();
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

        /// <summary>
        /// Push new Tag to remote repo
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="newTag"></param>
        /// <returns>True if command is successful, otherwise False</returns>
        public bool PushTag(string newTag)
        {
            Parameters.Arguments = $"push origin {Branch} {newTag}";
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

            if (LocalTagExists(newTag)) DeleteTag(newTag);

            bool resultSetTag = true;
            for (int i = 0; i < 2; i++)
            {
                // delete tag if exists
                if (TagExist(newTag)) DeleteTag(newTag);

                Parameters.Arguments = $"tag -a {newTag} HEAD -m \"Automated tag\"";

                var result = Launcher.Launcher.Start(Parameters);
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
                            resultSetTag = PushTag(newTag);
                        }
                    }
                }
                if (Parameters.Verbose) Console.WriteLine($"SetTag try: {++i}");
                if (resultSetTag) break;
            }

            return resultSetTag;
        }

        private bool TagExist(string newTag)
        {
            return LocalTagExists(newTag) || RemoteTagExists(newTag);
        }

        private bool IsValid4Tag(string newTag)
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

        public bool IsValidTag(string newTag)
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
                        branch = line.Substring(2); // //branch = line[2..]
                        break;
                    }
                }
            }

            return branch;
        }

        private string GetTag()
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
            bool bResult = false;

            // check if tag exists
            if (ListLocalTags().Contains(tag))
                bResult = DeleteLocalTag(tag);
            else
                bResult = true;
            
            var remoteTags = ListRemoteTags();
            if (bResult && remoteTags.Any (x => x.Contains(tag))) 
                bResult = DeleteRemoteTag(tag);

            return bResult;
        }

        public List<string> ListLocalTags()
        {
            Parameters.Arguments = $"tag --list";
            var result = Launcher.Launcher.Start(Parameters);
            if (result.Code == 0 && result.Output.Count >= 1)
            {
                return result.Output.ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        private List<string> DeleteRemoteTags(string url)
        {
            var tags = new List<string>();
            Parameters.Arguments = $"ls-remote --tags {url}";
            var result = Launcher.Launcher.Start(Parameters);
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
            Parameters.Arguments = $"ls-remote --tags";
            var result = Launcher.Launcher.Start(Parameters);
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

            Parameters.Arguments = $"tag -d {tag}";
            var result = Launcher.Launcher.Start(Parameters);
            if (result.Code == 0 
                    && result.Output.Count >= 1 
                    && result.Output.Exists(line => line.StartsWith($"Deleted tag '{tag}'")))
            {
                return true;
            }

            return false;
        }

        private bool DeleteRemoteTag(string tag)
        {
            if (!RemoteTagExists(tag)) return true;

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
            
            var result = ResultHelper.New();
            // change to project directory
            var DevDir = $"{DevDrive}\\{MainDir}";
        
            var solutionDir = $@"{DevDir}\{projectName}";
            var dirExists = Directory.Exists(solutionDir);
            if (!dirExists)
            {
                if (!Directory.Exists(DevDir))
                {
                    Directory.CreateDirectory(DevDir);
                }

                Parameters.WorkingDir = DevDir;
                Parameters.Arguments = $"clone {url} ";
                result = Launcher.Launcher.Start(Parameters);
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
                Parameters.Arguments = $"checkout -b {branch}";
                result = Launcher.Launcher.Start(Parameters);
            }
            else
            {
                // checkout
                Parameters.Arguments = $"checkout {branch}";
                result = Launcher.Launcher.Start(Parameters);
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
            if (string.IsNullOrEmpty(branch)) return false;
            return ListBranches().Contains(branch);
        }

        public List<string> ListBranches()
        {
            var branches = new List<string>();
            
            Parameters.Arguments = $"branch --list";
            var result = Launcher.Launcher.Start(Parameters);
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
