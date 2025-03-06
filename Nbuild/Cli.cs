using CommandLine.Attributes;

namespace Nbuild;

public class Cli
{
    [OptionalArgument("", "c", "Specifies the command to execute. Possible values: targets, install, uninstall, download, list.\n" +
        "\t targets \t -> Lists available targets and saves them in the targets.md file.\n" +
        "\t install \t -> Downloads and installs apps specified in the -json option.\n" +
        "\t uninstall \t -> Uninstalls apps specified in the -json option.\n" +
        "\t download \t -> Downloads apps specified in the -json option.\n" +
        "\t list \t\t -> Lists apps specified in the -json option.\n" +
        "\t ----\n" +
        "\t - By default, the -json option points to the ntools deployment folder: $(ProgramFiles)\\build\\ntools.json.\n" +
        "\t - The install, uninstall, and download commands require admin privileges to run.")]
    public string? Command { get; set; }

    [OptionalArgument("$(ProgramFiles)\\nbuild\\ntools.json", "json", "Specifies the JSON file that holds the list of apps. Only valid for the install, download, and list commands.\n" +
        "\t Sample JSON file: https://github.com/naz-hage/ntools/blob/main/dev-setup/app-Ntools.json\n" +
        "\t ")]
    public string? Json { get; set; }

    [OptionalArgument(false, "v", "Optional parameter which sets the console output verbose level\n" +
        "\t ----\n" +
        "\t - if no command line options are specified with the -v option , i.e.: 'Nb.exe stage -v true` \n" +
        "\t   `Nb` will run an MSbuild target `stage` defined in a `nbuild.targets` file which present in the solution folder.\n" +
        "\t   Run `Nb.exe -t Targets` to list the available targets. \n" +
        "\t -v Possible Values:")]
    public bool Verbose { get; set; }
}