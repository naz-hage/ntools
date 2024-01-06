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
        

        private static readonly List<string> Targets = [BuildStarter.BuildFileName, BuildStarter.CommonBuildFileName];

        static int Main(string[] args)
        {
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
            var buildResult = ResultHelper.New();
            var watch = Stopwatch.StartNew();
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
                        Console.WriteLine($"nbuild completed with '-1'");
                    return 0;
                }

                if (options != null && !string.IsNullOrEmpty(options.Command))
                {
                    switch (options.Command)
                    {
                        case var d when d == CmdTargets:
                            // project required
                            // find all *.targets files
                            string[] targetsFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.targets", SearchOption.TopDirectoryOnly);
                            

                            try
                            {
                                foreach (var targetsFile in targetsFiles)
                                {
                                    Console.WriteLine($"{targetsFile} Targets:");
                                    Console.WriteLine($"----------------------");
                                    foreach (var targetName in BuildStarter.GetTargets(Path.Combine(Environment.CurrentDirectory, targetsFile)))
                                    {
                                        Console.WriteLine(targetName);
                                    }
                                    Console.WriteLine();

                                    Console.WriteLine($"Imported Targets:");
                                    Console.WriteLine($"----------------------");
                                    foreach (var item in BuildStarter.GetImportAttributes(targetsFile, "Project"))
                                    {
                                        // replace $(ProgramFiles) with environment variable
                                        var importItem = item.Replace("$(ProgramFiles)", Environment.GetEnvironmentVariable("ProgramFiles"));
                                        Console.WriteLine($"{importItem} Targets:");
                                        Console.WriteLine($"----------------------");
                                        foreach (var targetName in BuildStarter.GetTargets(importItem))
                                        {
                                            Console.WriteLine(targetName);
                                        }
                                        Console.WriteLine();

                                    }
                                }

                                buildResult = ResultHelper.Success();
                            }
                            catch (Exception ex)
                            {
                                buildResult = ResultHelper.Fail(-1, ex.Message);
                            }
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
                watch.Stop();
                // display elapsed time in HH:MM:SS.MS format
                Console.WriteLine($"nbuild completed in {watch.Elapsed:hh\\:mm\\:ss\\.ff} (hh:mm:ss.ff) with {buildResult.Code}");

                Colorizer.WriteLine($"[{ConsoleColor.Green}!√ Build completed successfully.]");
            }
            else
            {
                watch.Stop();
                // display elapsed time in HH:MM:SS.MS format
                

                if (buildResult.Code == int.MaxValue)
                {
                    // Display Help
                    Parser.DisplayHelp<Cli>(HelpFormat.Full);
                }
                else
                {
                    Console.WriteLine($"nbuild completed in {watch.Elapsed:hh\\:mm\\:ss\\.ff} (hh:mm:ss.ff) with {buildResult.Code}");

                    if (buildResult.Output.Count > 0)
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}!X Build failed:\n X {buildResult.Output[0]}]");
                    }
                    else
                    {
                        Colorizer.WriteLine($"[{ConsoleColor.Red}!X Build failed]");
                    }
                }
            }

            return (int)buildResult.Code;
        }

        private static void DisplayGitInfo()
        {
            var parameters = new Parameters
            {
                FileName = "ngit.exe",
                Arguments = $"-git getbranch",
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