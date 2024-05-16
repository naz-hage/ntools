using CommandLine;
using NbuildTasks;
using Ntools;
using OutputColorizer;
using System.Diagnostics;
using System.Reflection;

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
        var ParserOptions = new ParserOptions
        {
            LogParseErrorToConsole = false,
        };
        var optionsParsed = Parser.TryParse(args, out Cli? options, ParserOptions);
        if (!optionsParsed)
        {
            GitWrapper gitWrapper = new();
            if (gitWrapper.IsGitConfigured())
            {
                result = RunBuildTargets(args, out options);
            }
            else
            {
                return (int)result.Code;
            }
        }
        else
        {
            ParserOptions.LogParseErrorToConsole = true;
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

        DisplayGitInfo(options!.Verbose);

        return (int)result.Code;
    }

    /// <summary>
    /// Run the build targets based on the provided arguments.  This method is added for convenience so that 
    /// nb.exe can be run from the command line without having to specify the command line options.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="options">The parsed command line options.</param>
    /// <returns>A ResultHelper object indicating the result of the build process. 
    ///     Otherwise, int.MaxValue -1 to indicate that target is not running</returns>
    private static ResultHelper RunBuildTargets(string[] args, out Cli options)
    {
        options = new Cli();
        var verbose = options.Verbose;
        string? target = null;

        switch (args.Length)
        {
            case 0:
                options = new Cli() { Verbose = true };

                return BuildStarter.Build(target, verbose);
         
            case 1 when args[0].Equals(CmdHelp, StringComparison.InvariantCultureIgnoreCase):
                return ResultHelper.Success("");
            
            case 1 when !args[0].Contains(CmdHelp, StringComparison.InvariantCultureIgnoreCase) && !args[0].StartsWith("-"):
                target = args[0];
                return BuildStarter.Build(target, verbose);

            case 3 when args[1].Equals("-v", StringComparison.InvariantCultureIgnoreCase) && (bool.TryParse(args[2], out verbose)):
                options.Verbose = verbose;
                target = args[0];
                return BuildStarter.Build(target, verbose);

            default:
                return ResultHelper.Fail(int.MaxValue, "Display Help");
        }
    }

    /// <summary>
    /// Updates the Json property of the provided Cli options object.
    /// If the Json property is null or empty and the Command property matches certain values, 
    /// the Json property is set to a default path.
    /// If the Json property contains a placeholder for the ProgramFiles environment variable, 
    /// this placeholder is replaced with the actual value of the environment variable.
    /// If a file does not exist at the path specified by the Json property, a FileNotFoundException is thrown.
    /// </summary>
    /// <param name="options">The Cli options object to update.</param>
    /// <returns>The updated Json property of the Cli options object.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no file exists at the path specified by the Json property.</exception>
    private static string? UpdateJsonOption(Cli options)
    {
        if ((string.IsNullOrEmpty(options.Json) && !string.IsNullOrEmpty(options.Command)) &&
                    (options.Command.Equals(CmdInstall, StringComparison.InvariantCultureIgnoreCase) ||
                    options.Command.Equals(CmdUninstall, StringComparison.InvariantCultureIgnoreCase) ||
                    options.Command.Equals(CmdList, StringComparison.InvariantCultureIgnoreCase) ||
                    options.Command.Equals(CmdDownload, StringComparison.InvariantCultureIgnoreCase)))
        {
            options.Json = $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Nbuild\\ntools.json";
        }

        if (!string.IsNullOrEmpty(options.Json))
        {
            options.Json = options.Json.Replace("$(ProgramFiles)", Environment.GetEnvironmentVariable("ProgramFiles"));
        }

        if (!File.Exists(options.Json))
        {
            throw new FileNotFoundException($"Json File '{options.Json}' not found");
        }

        return options.Json;
    }

    /// <summary>
    /// Displays git information if git is configured and folder is git repository.
    /// </summary>
    /// <param name="verbose">Flag indicating whether to display verbose output.</param>
    private static void DisplayGitInfo(bool verbose)
    {
        GitWrapper gitWrapper = new(project:null,verbose:verbose);
        if (!gitWrapper.IsGitConfigured(silent:true) || !gitWrapper.IsGitRepository(Environment.CurrentDirectory)) return;

        // Get the directory of the current process
        var executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var process = new Process
        {
            StartInfo =
            {
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Path.Combine(executableDirectory!, NgitAssemblyExe),
                Arguments = $"-c branch",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
            },
        };

        var resultHelper = process.LockStart(verbose);
        if (!resultHelper.IsSuccess())
        {
            if (verbose) Console.WriteLine($"==> Failed to display git info:{resultHelper.GetFirstOutput()}");
        }
    }
}