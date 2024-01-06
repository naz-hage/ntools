using CommandLine;
using NbuildTasks;
using OutputColorizer;
using static Ngit.Enums;

namespace Ngit
{
    public class Command
    {
        public const string GetBranchCommand = "getbranch";
        public const string GetTagCommand = "gettag";
        public const string SetTagCommand = "settag";
        public const string PushTagCommand = "pushtag";
        public const string AutoTagCommand = "autotag";   // Increment tag based on branch and build type but don't set it
        public const string SetAutoTagCommand = "setautotag";   // Increment tag based on branch and build type and set it
        public const string SetBranchCommand = "setbranch";
        public const string CreateBranchCommand = "createbranch";
        public const string DeleteTagCommand = "deletetag";
        public const string CloneCommand = "clone";
        public const string SetRemoteCommand = "setremote";

        private static readonly GitWrapper  GitWrapper= new();

        public static RetCode Process(Cli options)
        {
            if (GitWrapper == null) return RetCode.GitWrapperFailed;

            var retCode = options.GitCommand switch
            {
                GetBranchCommand => DisplayBranch(),
                GetTagCommand => DisplayTag(),
                AutoTagCommand => AutoTag(options) == string.Empty? RetCode.AutoTagFailed : RetCode.Success,
                SetTagCommand => SetTag(options),
                PushTagCommand => PushTag(options),
                SetBranchCommand => SetBranch(options),
                CreateBranchCommand => CreateBranch(options),
                SetAutoTagCommand => SetAutoTag(options),
                DeleteTagCommand => DeleteTag(options),
                CloneCommand => Clone(options),
                SetRemoteCommand => SetBranch(options),
                _ => Default(options)
            };

            DisplayResults(GitWrapper.Branch, GitWrapper.Tag);

            return retCode;
        }

        public static RetCode DisplayTag()
        {
            RetCode ReturnCode = RetCode.InvalidParameter;

            Colorizer.WriteLine($"[{ConsoleColor.Green}!{GitWrapper.Tag}]");
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
                var nextTag = GitWrapper.PushTag(GitWrapper.Branch, options.Tag);
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
            throw new NotImplementedException();
        }

        public static RetCode DeleteTag(Cli options)
        {
           var retCode = RetCode.InvalidParameter;

            
            if (!string.IsNullOrEmpty(options.Tag))
            {
                if (!GitWrapper.DeleteTag(options.Tag))
                    retCode = RetCode.DeleteTagFailed;
                else
                {
                    retCode = RetCode.Success;
                }
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

        public static RetCode CreateBranch(Cli options)
        {
            throw new NotImplementedException();
        }

        public static RetCode SetBranch(Cli options)
        {
            throw new NotImplementedException();
        }

        private static void DisplayResults(string branch, string tag)
        {
            var project = Path.GetFileName(Directory.GetCurrentDirectory());
            Colorizer.WriteLine($"[{ConsoleColor.DarkMagenta}!Project [{ConsoleColor.Yellow}!{project}] " +
                                                        $"Branch [{ConsoleColor.Yellow}!{branch}] " +
                                                        $"Tag [{ConsoleColor.Yellow}!{tag}]]");
        }

    }
}
