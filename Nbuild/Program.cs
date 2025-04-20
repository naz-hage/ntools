using CommandLine;
using NbuildTasks;
using Ntools;
using OutputColorizer;

namespace Nbuild;

public class Program
{
    private const string CmdTargets = "targets";
    private const string CmdInstall = "install";
    private const string CmdUninstall = "uninstall";
    private const string CmdList = "list";
    private const string CmdDownload = "download";
    private const string CmdPath = "path";
    private const string CmdHelp = "--help";
    
    private static readonly int linesToDisplay = 10;

    static int Main(string[] args)
    {
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
            if (options!.Verbose)
            {
                Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
            }

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
                if (options != null && Enum.IsDefined(options.Command))
                {
                    options.Json = UpdateJsonOption(options);


                    result = options.Command switch
                    {
                        Cli.CommandType.targets => BuildStarter.DisplayTargets(Environment.CurrentDirectory),
                        Cli.CommandType.install => Command.Install(options.Json, options.Verbose),
                        Cli.CommandType.uninstall => Command.Uninstall(options.Json, options.Verbose),
                        Cli.CommandType.list => Command.List(options.Json, options.Verbose),
                        Cli.CommandType.download => Command.Download(options.Json, options.Verbose),
                        Cli.CommandType.path => Command.DisplayPathSegments(),
                        Cli.CommandType.git_tag => Command.DisplayGitInfo(options!.Verbose),
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

        if (options == null || !Enum.IsDefined(typeof(Cli.CommandType), options.Command))
        {
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

            Command.DisplayGitInfo(options!.Verbose);
        }
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
        if ((string.IsNullOrEmpty(options.Json) && !string.IsNullOrEmpty(options.Command.ToString())) &&
                (options.Command.Equals(Cli.CommandType.install) ||
                options.Command.Equals(Cli.CommandType.uninstall) ||
                options.Command.Equals(Cli.CommandType.list) ||
                options.Command.Equals(Cli.CommandType.download)))
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


}