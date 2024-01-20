using CommandLine.Attributes;

namespace Nbuild;

public class Cli
{
    [OptionalArgument("", "c", "command. value = [targets | install | download | list | ]\n" +
        "\t targets \t -> List available targets and save in targets.md file\n" +
        "\t install \t -> Download and install apps specified in -json option, requires admin priviledges\n" +
        "\t download \t -> Download apps specified in -json option, requires admin priviledges\n" +
        "\t list    \t -> List apps specified in -json option")]
    public string? Command { get; set; }

    [OptionalArgument("", "json", "json file which holds apps list. Valid only for -c install | download | list option\n" +
        "\t sample json file: https://github.com/naz-hage/ntools/blob/main/Nbuild/resources/NbuildAppListTest.json\"")]
    public string? Json { get; set; }

    public bool Verbose { get; set; }
}