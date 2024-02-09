using CommandLine;
using NbuildTasks;
using Ntools;
using OutputColorizer;
using System.Diagnostics;

namespace Nbuild;

public class Program
{
    private const string CmdTargets = "targets";
    private const string CmdInstall = "install";
    private const string CmdUninstall = "uninstall";
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

            try
            {
                if (options != null && !string.IsNullOrEmpty(options.Command))
                {
                    options.Json = UpdateJsonOption(options);

                    result = options.Command switch
                    {
                        var d when d == CmdTargets => BuildStarter.DisplayTargets(Environment.CurrentDirectory),
                        var d when d == CmdInstall => Command.Install(options.Json, options.Verbose),
                        var d when d == CmdUninstall => Command.Uninstall(options.Json, options.Verbose),
                        var d when d == CmdList => Command.List(options.Json, options.Verbose),
                        var d when d == CmdDownload => Command.Download(options.Json, options.Verbose),
                        _ => ResultHelper.Fail(-1, $"Invalid Command: '{options.Command}'"),
                    };
                }
            }
            catch (Exception ex)
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!X Error occurred: {ex.Message}.]");
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

    private static string? UpdateJsonOption(Cli options)
    {
        if ((string.IsNullOrEmpty(options.Json) && !string.IsNullOrEmpty(options.Command)) &&
                    (options.Command.Equals(CmdInstall, StringComparison.InvariantCultureIgnoreCase) ||
                    options.Command.Equals(CmdUninstall, StringComparison.InvariantCultureIgnoreCase) ||
                    options.Command.Equals(CmdList, StringComparison.InvariantCultureIgnoreCase) ||
                    options.Command.Equals(CmdDownload, StringComparison.InvariantCultureIgnoreCase)))
        {
            options.Json = $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Nbuild\\ntools.json";
            if (File.Exists(options.Json))
            {
                return options.Json;
            }
        }

        return options.Json;
    }

    private static void DisplayGitInfo()
    {
        var process = new Process
        {
            StartInfo =
            {
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = ShellUtility.GetFullPathOfFile(NgitAssemblyExe),
                Arguments = $"-c branch",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
            },
        };

        var resultHelper = process.LockStart(false);
        if (!resultHelper.IsSuccess())
        {
            Console.WriteLine($"==> Failed to display git info:{resultHelper.GetFirstOutput()}");
        }
    }
}