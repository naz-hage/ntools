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
        "\t - By default, the -json option points to the ntools deployment folder: $(ProgramFiles)\\build\\tools.json.\n" +
        "\t - The install, uninstall, and download commands require admin privileges to run.")]
    public string? Command { get; set; }

    [OptionalArgument("$(ProgramFiles)\\nbuild\\ntools.json", "json", "Specifies the JSON file that holds the list of apps. Only valid for the install, download, and list commands.\n" +
        "\t Sample JSON file: https://github.com/naz-hage/ntools/blob/main/Nbuild/resources/NbuildAppListTest.json")]
    public string? Json { get; set; }

    [OptionalArgument(false, "v", "Sets the verbose level.")]
    public bool Verbose { get; set; }
}