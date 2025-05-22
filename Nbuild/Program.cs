using CommandLine;
using NbuildTasks;
using Ntools;
using OutputColorizer;

namespace Nbuild;

public class Program
{
    private const string CmdHelp = "--help";
    private static readonly int LinesToDisplay = 10;

    static int Main(string[] args)
    {
        DisplayVersion();

        var result = ResultHelper.New();
        var parserOptions = new ParserOptions { LogParseErrorToConsole = false };

        if (!TryParseArguments(args, parserOptions, out Cli? options))
        {
            result = HandleUnparsedArguments(args, out options);
            DisplayResult(result, value:"Build", runTarget:true);
        }
        else
        {
            result = ExecuteCommandAsync(args, options);
            DisplayResult(result);
        }

        return (int)result.Code;
    }

    private static void DisplayVersion()
    {
        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");
    }

    private static bool TryParseArguments(string[] args, ParserOptions parserOptions, out Cli? options)
    {
        return Parser.TryParse(args, out options, parserOptions);
    }

    private static ResultHelper HandleUnparsedArguments(string[] args, out Cli options)
    {
        options = new Cli();
        var gitWrapper = new GitWrapper();

        if (gitWrapper.IsGitConfigured())
        {
            return RunBuildTargets(args, out options);
        }

        return ResultHelper.Fail(-1, "Git is not configured.");
    }

    private static ResultHelper ExecuteCommandAsync(string[] args, Cli? options)
    {
        var result = ResultHelper.New();
        var currentDirectory = Environment.CurrentDirectory;

        try
        {
            if (options != null && Enum.IsDefined(options.Command))
            {
                options.Json = UpdateJsonOption(options);

                options.Validate();

                result = ExecuteCommandSwitchAsync(options).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Colorizer.WriteLine($"[{ConsoleColor.Red}!X Error occurred: {ex.Message}.]");
        }
        finally
        {
            // Restore the original working directory
            Environment.CurrentDirectory = currentDirectory;
        }

        return result;
    }

    private static async Task<ResultHelper> ExecuteCommandSwitchAsync(Cli options)
    {
        return options.Command switch
        {
            Cli.CommandType.targets => BuildStarter.DisplayTargets(Environment.CurrentDirectory),
            Cli.CommandType.install => Command.Install(options.Json, options.Verbose),
            Cli.CommandType.uninstall => Command.Uninstall(options.Json, options.Verbose),
            Cli.CommandType.list => Command.List(options.Json, options.Verbose),
            Cli.CommandType.download => Command.Download(options.Json, options.Verbose),
            Cli.CommandType.path => Command.DisplayPathSegments(),
            Cli.CommandType.git_info => Command.DisplayGitInfo(),
            Cli.CommandType.git_settag => Command.SetTag(options.Tag),
            Cli.CommandType.git_autotag => Command.SetAutoTag(options.BuildType),
            Cli.CommandType.git_push_autotag => Command.SetAutoTag(options.BuildType, push: true),
            Cli.CommandType.git_branch => Command.DisplayGitBranch(),
            Cli.CommandType.git_clone => Command.Clone(options.Url, options.Path, options.Verbose),
            Cli.CommandType.git_deletetag => Command.DeleteTag(options.Tag),
            Cli.CommandType.release_create => await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetFileName!),
            Cli.CommandType.pre_release_create => await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetFileName!,true),
            Cli.CommandType.release_download => await Command.DownloadAsset(options.Repo!, options.Tag!, options.Path!),
            Cli.CommandType.list_release => await Command.ListReleases(options.Repo!, options.Verbose),
            _ => ResultHelper.Fail(-1, $"Invalid Command: '{options.Command}'"),
        };
    }

    private static void DisplayResult(ResultHelper result, string value = "Command", bool runTarget = false)
    {
        if (runTarget) Command.DisplayGitInfo();

        if (result.IsSuccess())
        {
            Colorizer.WriteLine($"[{ConsoleColor.Green}!√ {value} completed.]");
        }
        else
        {
            if (result.Code == int.MaxValue)
            {
                Parser.DisplayHelp<Cli>(HelpFormat.Full);
            }
            else
            {
                foreach (var item in result.Output.TakeLast(LinesToDisplay))
                {
                    Colorizer.WriteLine($"[{ConsoleColor.Red}! {item}]");
                }

                Colorizer.WriteLine($"[{ConsoleColor.Red}!X {value} failed!]");
            }
        }
    }

    private static ResultHelper RunBuildTargets(string[] args, out Cli options)
    {
        options = new Cli();
        var verbose = options.Verbose;
        string? target = null;

        return args.Length switch
        {
            0 => BuildStarter.Build(target, verbose),
            1 when args[0].Equals(CmdHelp, StringComparison.InvariantCultureIgnoreCase) =>
                ResultHelper.Success(""),
            1 when !args[0].Contains(CmdHelp, StringComparison.InvariantCultureIgnoreCase) &&
                     !args[0].StartsWith("-") =>
                BuildStarter.Build(args[0], verbose),
            3 when args[1].Equals("-v", StringComparison.InvariantCultureIgnoreCase) &&
                     bool.TryParse(args[2], out verbose) =>
                BuildStarter.Build(args[0], verbose),
            _ => ResultHelper.Fail(int.MaxValue, "Display Help"),
        };
    }

    private static string? UpdateJsonOption(Cli options)
    {
        if (string.IsNullOrEmpty(options.Json) &&
            (options.Command == Cli.CommandType.install ||
             options.Command == Cli.CommandType.uninstall ||
             options.Command == Cli.CommandType.list ||
             options.Command == Cli.CommandType.download))
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
