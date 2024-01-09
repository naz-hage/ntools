using CommandLine;
using Launcher;
using NbuildTasks;
using OutputColorizer;
using System.Diagnostics;
using System.Xml.Linq;

namespace Nbuild
{
    public class Program
    {
        private const string CmdTargets = "targets";
        private const string CmdHelp = "--help";
        private const string NgitAssemblyExe = "ng.exe";
        private static readonly List<string> Targets = [BuildStarter.BuildFileName, BuildStarter.CommonBuildFileName];
        private static readonly int linesToDisplay = 10;

        static int Main(string[] args)
        {
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
            var buildResult = ResultHelper.New();
            string? target = null;
            Cli options;
            

            if (args.Length == 0)
            {
                options = new Cli();
                buildResult = BuildStarter.Build(target, options.Verbose);
            }
            else if (args.Length == 1 && !args[0].Contains(CmdHelp, StringComparison.InvariantCultureIgnoreCase))
            {
                target = args[0];
                options = new Cli();
                buildResult = BuildStarter.Build(target, options.Verbose);
            }
            else
            {
                if (!Parser.TryParse(args, out options))
                {
                    if (!args[0].Equals("--help", StringComparison.CurrentCultureIgnoreCase))
                        Console.WriteLine($"build completed with '-1'");
                    return 0;
                }

                if (options != null && !string.IsNullOrEmpty(options.Command))
                {
                    switch (options.Command)
                    {
                        case var d when d == CmdTargets:
                            buildResult = BuildStarter.DisplayTargets();
                            break;
    
                        default:
                            buildResult = ResultHelper.Fail(-1, $"Invalid Command: '{options.Command}'");
                            break;
                    }
                }
            }

            DisplayGitInfo();

            if (buildResult.IsSuccess())
            {
                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ Build completed.]");
            }
            else
            {
                if (buildResult.Code == int.MaxValue)
                {
                    // Display Help
                    Parser.DisplayHelp<Cli>(HelpFormat.Full);
                }
                else
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}!X Last {linesToDisplay} lines from error]");
                    foreach (var item in buildResult.Output.TakeLast(linesToDisplay))
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Yellow}! {item}]");
                    }

                    Colorizer.WriteLine($"[{ConsoleColor.Red}!X Build failed with code: {buildResult.Code}]");
                }
            }

            return (int)buildResult.Code;
        }

        

        private static void DisplayGitInfo()
        {
            var parameters = new Parameters
            {
                FileName = NgitAssemblyExe,
                Arguments = $"-c branch",
                WorkingDir = Environment.CurrentDirectory,
                RedirectStandardOutput = false,
                Verbose = false,
            };

            var resultHelper = Launcher.Launcher.Start(parameters);
            if (!resultHelper.IsSuccess())
            {
                Console.WriteLine($"==> Failed tp display git info");
            }
        }

    

    }
}