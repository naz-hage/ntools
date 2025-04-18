﻿using CommandLine.Attributes;

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
        path
    }

    /// <summary>
    /// Gets or sets the command to execute.
    /// Possible values: targets, install, uninstall, download, list, path.
    /// </summary>
    [RequiredArgument(0, "command", "Specifies the command to execute.\n" +
        "\t list \t\t -> Lists apps specified in the -json option.\n" +
        "\t install \t -> Downloads and installs apps specified in the -json option (require admin privileges to run).\n" +
        "\t uninstall \t -> Uninstalls apps specified in the -json option (require admin privileges to run).\n" +
        "\t download \t -> Downloads apps specified in the -json option (require admin privileges to run).\n" +
        "\t targets \t -> Lists available targets and saves them in the targets.md file.\n" +
        "\t path \t\t -> Displays environment path in local machine.\n" +
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

    private static readonly Dictionary<string, CommandType> CommandMap = new()
        {
            { "targets", CommandType.targets },
            { "install", CommandType.install },
            { "uninstall", CommandType.uninstall },
            { "download", CommandType.download },
            { "list", CommandType.list },
            { "path", CommandType.path }
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
            default:
                // For all other commands, no additional validation is required.
                break;
        }
    }
}
