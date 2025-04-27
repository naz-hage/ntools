using CommandLine.Attributes;

namespace Nbuild;

/// <summary>
/// Represents the command-line interface (CLI) options for the Nbuild application.
/// </summary>
public class Cli
{
    /// <summary>
    /// Enum representing the possible command types.
    /// </summary>
    public enum CommandType
    {
        list,
        install,
        uninstall,
        download,
        targets,
        path,
        git_info,
        git_settag,
        git_autotag,
        git_push_autotag,
        git_branch,
    }

    /// <summary>
    /// Gets or sets the command to execute.
    /// Possible values: targets, install, uninstall, download, list, path.
    /// </summary>
    [RequiredArgument(0, "command", "Specifies the command to execute.\n" +
        "\t list \t\t\t -> Lists apps specified in the -json option.\n" +
        "\t install \t\t -> Downloads and installs apps specified in the -json option (require admin privileges to run).\n" +
        "\t uninstall \t\t -> Uninstalls apps specified in the -json option (require admin privileges to run).\n" +
        "\t download \t\t -> Downloads apps specified in the -json option (require admin privileges to run).\n" +
        "\t targets \t\t -> Lists available targets and saves them in the targets.md file.\n" +
        "\t path \t\t\t -> Displays environment path in local machine.\n" +
        "\t git_info \t\t -> Displays the current git information in the local repository.\n" +
        "\t git_settag \t\t -> Set specified tag with -tag option\n" +
        "\t git_autotag \t\t -> Set next tag based on the build type: STAGE | PROD\n" +
        "\t git_push_autotag \t -> Set next tag based on the build type and push to remote repo\n" +
        "\t git_branch \t\t -> Displays the current git branch in the local repository\n" +
        "\t ----\n")]
    public CommandType Command { get; set; }

    /// <summary>
    /// Gets or sets the JSON file that holds the list of apps.
    /// Only valid for the install, download, and list commands.
    /// </summary>
    [OptionalArgument("$(ProgramFiles)\\nbuild\\ntools.json", "json", "Specifies the JSON file that holds the list of apps. Only valid for the install, download, and list commands.\n" +
        "\t - By default, the -json option points to the ntools deployment folder: $(ProgramFiles)\\build\\ntools.json.\n" +
        "\t Sample JSON file: https://github.com/naz-hage/ntools/blob/main/dev-setup/ntools.json\n" +
        "\t ")]
    public string? Json { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the console output verbose level.
    /// </summary>
    [OptionalArgument(false, "v", "Optional parameter which sets the console output verbose level\n" +
        "\t ----\n" +
        "\t - if no command line options are specified with the -v option , i.e.: 'Nb.exe stage -v true` \n" +
        "\t   `Nb` will run an MSbuild target `stage` defined in a `nbuild.targets` file which present in the solution folder.\n" +
        "\t   Run `Nb.exe Targets` to list the available targets. \n" +
        "\t -v Possible Values:")]
    public bool Verbose { get; set; }

    [OptionalArgument("", "tag", "Specifies the tag used for git_settag command.")]
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the build type used for git_autotag and git_push_autotag commands.
    /// Possible values: STAGE, PROD.
    /// </summary>
    [OptionalArgument("", "buildtype", "Specifies the build type used for git_autotag and git_push_autotag commands. Possible values: STAGE, PROD.")]
    public string? BuildType { get; internal set; }

    private static readonly Dictionary<string, CommandType> CommandMap = new()
        {
            { "targets", CommandType.targets },
            { "install", CommandType.install },
            { "uninstall", CommandType.uninstall },
            { "download", CommandType.download },
            { "list", CommandType.list },
            { "path", CommandType.path },
            { "git_info", CommandType.git_info },
            { "git_settag", CommandType.git_settag },
            { "git_autotag", CommandType.git_autotag },
            { "git_push_autotag", CommandType.git_push_autotag },
            { "git_branch", CommandType.git_branch },
        };

    /// <summary>
    /// Gets the command type from the command string.
    /// </summary>
    /// <returns>The command type.</returns>
    /// <exception cref="ArgumentException">Thrown when the command is invalid.</exception>
    public CommandType GetCommandType()
    {
        if (CommandMap.TryGetValue(Command.ToString().ToLower(), out var commandType))
        {
            return commandType;
        }
        throw new ArgumentException($"Invalid command: {Command}");
    }

    /// <summary>
    /// Validates the CLI arguments to ensure required options are provided for specific commands.
    /// </summary>
    public void Validate()
    {
        switch (Command)
        {
            case CommandType.install:
            case CommandType.uninstall:
            case CommandType.download:
            case CommandType.list:
                if (string.IsNullOrEmpty(Json))
                {
                    throw new ArgumentException("The 'json' option is required for the 'install', 'uninstall', 'download', and 'list' commands.");
                }
                break;
            case CommandType.git_settag:
                if (string.IsNullOrEmpty(Tag))
                {
                    throw new ArgumentException("The 'tag' option is required for the 'git_settag' command.");
                }
                break;
            case CommandType.git_autotag:
            case CommandType.git_push_autotag:
                if (string.IsNullOrEmpty(BuildType))
                {
                    throw new ArgumentException("The 'buildtype' option is required for the 'git_autotag' and 'git_push_autotag' commands.");
                }
                break;
            default:
                // For all other commands, no additional validation is required.
                break;
        }
    }
}
