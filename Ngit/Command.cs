using CommandLine;
using NbuildTasks;
using OutputColorizer;
using static NbuildTasks.Enums;

namespace Ngit
{
    public class Command
    {
        public const string GetBranchCommand = "branch";
        public const string GetTagCommand = "tag";
        public const string SetTagCommand = "settag";
        public const string PushTagCommand = "pushtag";
        public const string AutoTagCommand = "autotag";
        public const string DeleteTagCommand = "deletetag";
        public const string CloneCommand = "clone";

        private static readonly GitWrapper GitWrapper = new();

        public static RetCode Process(Cli options)
        {
            if (GitWrapper == null) return RetCode.GitWrapperFailed;

            GitWrapper.Verbose = options.Verbose;

            var retCode = options.GitCommand switch
            {
                GetBranchCommand => DisplayBranch(),
                GetTagCommand => DisplayTag(options),
                AutoTagCommand => SetAutoTag(options),
                SetTagCommand => SetTag(options),
                PushTagCommand => PushTag(options),
                DeleteTagCommand => DeleteTag(options),
                CloneCommand => Clone(options),
                _ => Default(options)
            };

            if (options.Verbose) Console.WriteLine($"Command.Process.StartInfo.WorkingDirectory: {GitWrapper.Process.StartInfo.WorkingDirectory}");

            if (retCode == RetCode.Success) DisplayResults(GitWrapper.Branch, GitWrapper.Tag);

            return retCode;
        }

        public static RetCode DisplayTag(Cli options)
        {
            RetCode ReturnCode = RetCode.InvalidParameter;

            if (options.Verbose) Colorizer.WriteLine($"[{ConsoleColor.Green}!{GitWrapper.Tag}]");

            if (!string.IsNullOrEmpty(GitWrapper.Branch))
            {
                ReturnCode = RetCode.Success;
            }

            return ReturnCode;
        }

        public static RetCode DisplayBranch()
        {
            RetCode ReturnCode = RetCode.InvalidParameter;

            if (!string.IsNullOrEmpty(GitWrapper.Branch))
            {
                ReturnCode = RetCode.Success;
            }

            return ReturnCode;
        }

        public static string AutoTag(Cli options)
        {
            var nextTag = string.Empty;

            if (!string.IsNullOrEmpty(options.BuildType))
            {
                nextTag = GitWrapper.AutoTag(options.BuildType);
                Colorizer.WriteLine($"[{ConsoleColor.Green}!{nextTag}]");
            }
            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: valid build type is required]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }

            return nextTag;
        }

        public static RetCode SetTag(Cli options)
        {
            RetCode returnCode;
            // Project and branch required
            if (!string.IsNullOrEmpty(options.Tag))
            {
                // write the tag to the file version.txt . use file.writealltext
                //File.WriteAllText("version.txt", options.Tag);

                returnCode = GitWrapper.SetTag(options.Tag) == true ? RetCode.Success : RetCode.SetTagFailed;
            }
            else
            {
                returnCode = RetCode.InvalidParameter;
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: valid tag is required]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }

            return returnCode;
        }

        public static RetCode PushTag(Cli options)
        {
            RetCode ReturnCode;

            if (!string.IsNullOrEmpty(options.Tag))
            {
                var nextTag = GitWrapper.PushTag(options.Tag);
                //Colorizer.WriteLine($"[{ConsoleColor.Green}!{nextTag}]");
                ReturnCode = RetCode.Success;
            }
            else
            {
                ReturnCode = RetCode.InvalidParameter;
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: valid tag is required]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }

            return ReturnCode;
        }

        public static RetCode Default(Cli options)
        {
            RetCode ReturnCode = RetCode.InvalidParameter;
            if (options.Verbose)
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: [{ConsoleColor.Yellow}!{options.GitCommand} is an invalid Command]]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }
            return ReturnCode;
        }

        public static RetCode Clone(Cli options)
        {
            if (string.IsNullOrEmpty(options.Url))
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: valid url is required]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                return RetCode.InvalidParameter;
            }

            var result = GitWrapper.CloneProject(options.Url); ;
            if (result.IsSuccess())
            {
                // reset Process.StartInfo.WorkingDirectory
                var solutionDir = GitWrapper.Process.StartInfo.WorkingDirectory;
                GitWrapper.Process.StartInfo.WorkingDirectory = solutionDir;

                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ Project cloned to `{solutionDir}`.]");
            }
            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!X {result.GetFirstOutput()}]");
            }

            return (RetCode)result.Code;
        }

        public static RetCode DeleteTag(Cli options)
        {
            var retCode = RetCode.InvalidParameter;

            if (!string.IsNullOrEmpty(options.Tag))
            {
                retCode = !GitWrapper.DeleteTag(options.Tag) ? RetCode.DeleteTagFailed : RetCode.Success;
            }
            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: valid tag is required]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                retCode = RetCode.InvalidParameter;
            }

            return retCode;


        }

        public static RetCode SetAutoTag(Cli options)
        {
            var retCode = RetCode.InvalidParameter;

            if (!string.IsNullOrEmpty(options.BuildType))
            {
                string? nextTag = GitWrapper.AutoTag(options.BuildType);
                if (!string.IsNullOrEmpty(nextTag))
                {
                    options.Tag = nextTag;
                    options.GitCommand = SetTagCommand;
                    retCode = SetTag(options);
                }
                else
                    retCode = RetCode.SetAutoTagFailed;
            }

            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: valid build type is required]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
                retCode = RetCode.InvalidParameter;
            }

            return retCode;
        }

        private static void DisplayResults(string branch, string tag)
        {
            var project = Path.GetFileName(Directory.GetCurrentDirectory());
            if (!string.IsNullOrEmpty(branch))
            {
                Colorizer.WriteLine($"[{ConsoleColor.DarkMagenta}!Project [{ConsoleColor.Yellow}!{project}] " +
                                                        $"Branch [{ConsoleColor.Yellow}!{branch}] " +
                                                        $"Tag [{ConsoleColor.Yellow}!{tag}]]");
            }
            else
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: [{ConsoleColor.Yellow}!{project}] directory is not a git repository]");
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }
        }

    }
}
