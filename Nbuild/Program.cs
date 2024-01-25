using CommandLine;
using Launcher;
using NbuildTasks;
using OutputColorizer;

namespace Nbuild;

public class Program
{
    private const string CmdTargets = "targets";
    private const string CmdInstall = "install";
    private const string CmdList = "list";
    private const string CmdDownload = "download";
    private const string CmdHelp = "--help";
    private const string NgitAssemblyExe = "ng.exe";
    private static readonly int linesToDisplay = 10;

    static int Main(string[] args)
    {
        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
        var result = ResultHelper.New();
        string? target = null;
        Cli options;

        if (args.Length == 0)
        {
            options = new Cli() { Verbose = true };
            result = BuildStarter.Build(target, options.Verbose);
        }
        else if (args.Length == 1 && !args[0].Contains(CmdHelp, StringComparison.InvariantCultureIgnoreCase))
        {
            target = args[0];
            options = new Cli() { Verbose = true };
            result = BuildStarter.Build(target, options.Verbose);
        }
        else
        {
            if (!Parser.TryParse(args, out options))
            {
                if (!args[0].Equals("--help", StringComparison.CurrentCultureIgnoreCase))
                    Console.WriteLine($"build completed with '-1'");
                return 0;
            }

            var currentDirectory = Environment.CurrentDirectory;

            if (options != null && !string.IsNullOrEmpty(options.Command))
            {
                result = options.Command switch
                {
                    var d when d == CmdTargets => BuildStarter.DisplayTargets(Environment.CurrentDirectory),
                    var d when d == CmdInstall => Command.Install(options.Json),
                    var d when d == CmdList => Command.List(options.Json),
                    var d when d == CmdDownload => Command.Download(options.Json),
                    _ => ResultHelper.Fail(-1, $"Invalid Command: '{options.Command}'"),
                };
            }

            // return to current directory because the command might have changed it
            Environment.CurrentDirectory = currentDirectory;
        }



        if (result.IsSuccess())
        {
            Colorizer.WriteLine($"[{ConsoleColor.Green}!√ Build completed.]");
        }
        else
        {
            if (result.Code == int.MaxValue)
            {
                // Display Help
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }
            else
            {
                foreach (var item in result.Output.TakeLast(linesToDisplay))
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! {item}]");
                }

                Colorizer.WriteLine($"[{ConsoleColor.Red}!X Build failed!]");
            }
        }

        DisplayGitInfo();

        return (int)result.Code;
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
            Console.WriteLine($"==> Failed to display git info");
        }
    }
}