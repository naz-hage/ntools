using CommandLine.Attributes;

namespace Nbuild;

public class Cli
{
    [OptionalArgument("", "c", "command. value = [targets]\n" +
        "\t targets \t -> List available targets and save in targets.md file\n" +
        "\t install \t -> Download and install app specified in -json option. requires admin priviledges")]
    public string? Command { get; set; }

    [OptionalArgument("", "json", "json file to install. Only for -c install option")]
    public string? Json { get; set; }

    public bool Verbose { get; set; }
}