using CommandLine.Attributes;

namespace Ngit;

/// <summary>
/// Represents the command-line interface (CLI) options for the Ngit application.
/// </summary>
public class Cli
{
    /// <summary>
    /// Enum representing the possible command types.
    /// </summary>
    public enum CommandType
    {
        tag,
        setTag,
        autoTag,
        setAutoTag,
        deleteTag,
        branch,
        clone,
        pushTag
    }

    /// <summary>
    /// Gets or sets the git command to execute.
    /// Possible values: tag, settag, autotag, setautotag, deletetag, branch, clone.
    /// </summary>
    [RequiredArgument(0, "command", "Specifies the git command to execute.\n" +
        "\t tag \t\t -> Get the current tag\n" +
        "\t settag \t -> Set specified tag in -tag option\n" +
        "\t autotag \t -> Set next tag based on the build type: STAGE vs. PROD\n" +
        "\t setautotag \t -> Set next tag based on the build type: STAGE vs. PROD\n" +
        "\t deletetag \t -> Delete specified tag in -tag option\n" +
        "\t branch \t -> Get the current branch\n" +
        "\t clone \t\t -> Clone specified Git repo in the -url option\n" +
        "\t ----\n")]
    public CommandType Command { get; set; }

    /// <summary>
    /// Gets or sets the Git repository URL.
    /// </summary>
    [OptionalArgument("", "url", "Specifies the Git repository URL.")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the tag used for settag and deletetag commands.
    /// </summary>
    [OptionalArgument("", "tag", "Specifies the tag used for settag and deletetag commands.")]
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the build type used for autotag and setautotag commands.
    /// Possible values: STAGE, PROD.
    /// </summary>
    [OptionalArgument("", "buildtype", "Specifies the build type used for autotag and setautotag commands. Possible values: STAGE, PROD.")]
    public string? BuildType { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the console output verbose level.
    /// </summary>
    [OptionalArgument(false, "v", "Specifies whether to print additional information.")]
    public bool Verbose { get; set; }

    private static readonly Dictionary<string, CommandType> CommandMap = new()
    {
        { "tag", CommandType.tag },
        { "settag", CommandType.setTag },
        { "autotag", CommandType.autoTag },
        { "setautotag", CommandType.setAutoTag },
        { "deletetag", CommandType.deleteTag },
        { "branch", CommandType.branch },
        { "clone", CommandType.clone },
        { "pushtag", CommandType.pushTag }
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
            case CommandType.setTag:
            case CommandType.deleteTag:
                if (string.IsNullOrEmpty(Tag))
                {
                    throw new ArgumentException("The 'tag' option is required for the 'settag' and 'deletetag' commands.");
                }
                break;
            case CommandType.clone:
                if (string.IsNullOrEmpty(Url))
                {
                    throw new ArgumentException("The 'url' option is required for the 'clone' command.");
                }
                break;
            case CommandType.autoTag:
            case CommandType.setAutoTag:
                if (string.IsNullOrEmpty(BuildType))
                {
                    throw new ArgumentException("The 'buildtype' option is required for the 'autotag' and 'setautotag' commands.");
                }
                break;
            default:
                // For all other commands, no additional validation is required.
                break;
        }
    }
}
