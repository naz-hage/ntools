using CommandLine;
using NbuildTasks;
using OutputColorizer;
using static NbuildTasks.Enums;
using static Ngit.Cli;

namespace Ngit
{
    public class Command
    {
        private static readonly GitWrapper GitWrapper = new();

        public static RetCode Process(Cli options)
        {
            if (GitWrapper == null) return RetCode.GitWrapperFailed;

            if (!GitWrapper.IsGitRepository(Environment.CurrentDirectory)) return RetCode.NotAGitRepository;

            if (!GitWrapper.IsGitConfigured()) return RetCode.GitNotConfigured;

            GitWrapper.Verbose = options.Verbose;

            var retCode = options.Command switch
            {
                CommandType.branch => DisplayBranch(),
                CommandType.tag => DisplayTag(options),
                CommandType.autoTag => SetAutoTag(options),
                CommandType.setAutoTag => SetAutoTag(options, push:true),
                CommandType.setTag => SetTag(options),
                CommandType.pushTag => PushTag(options),
                CommandType.deleteTag => DeleteTag(options),
                CommandType.clone => Clone(options),
                _ => Default(options)
            };

            if (options.Verbose) Console.WriteLine($"Command.Process.StartInfo.WorkingDirectory: {GitWrapper.WorkingDirectory}");

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
                Colorizer.WriteLine($"[{ConsoleColor.Green}! PushTag: {nextTag}]");
                
                if (!nextTag)
                {
                    ReturnCode = RetCode.AutoTagFailed;
                }
                else
                {
                    ReturnCode = RetCode.Success;
                }
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
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Error: [{ConsoleColor.Yellow}!{options.Command} is an invalid Command]]");
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
                var solutionDir = GitWrapper.WorkingDirectory;
                GitWrapper.WorkingDirectory = solutionDir;

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

        /// <summary>
        /// Sets the auto tag based on the provided build type and optionally pushes the tag.
        /// </summary>
        /// <param name="options">The CLI options containing the build type and other parameters.</param>
        /// <param name="push">A boolean flag indicating whether to push the tag after setting it. Default is false.</param>
        /// <returns>Returns a RetCode indicating the result of the operation.</returns>
        public static RetCode SetAutoTag(Cli options, bool push = false)
        {
            var retCode = RetCode.InvalidParameter;

            if (!string.IsNullOrEmpty(options.BuildType))
            {
                string? nextTag = GitWrapper.AutoTag(options.BuildType);
                if (!string.IsNullOrEmpty(nextTag))
                {
                    options.Tag = nextTag;
                    options.Command = CommandType.setAutoTag;
                    retCode = SetTag(options);

                    if (retCode == RetCode.Success && push)
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Green}!new tag: {GitWrapper.Tag}]");
                        retCode = PushTag(options);
                    }
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
