using CommandLine.Attributes;

namespace Nbuild;

public class Cli
{
    [OptionalArgument("", "c", "command. value = [targets]\n" +
        "\t targets \t -> list targets in nbuild.targets and common.targets files")]
    public string? Command { get; set; }

    public bool Verbose { get; set; }
}