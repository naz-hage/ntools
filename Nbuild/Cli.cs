using CommandLine.Attributes;

namespace nbuild;

public class Cli
{
    [OptionalArgument("", "cmd", "command. value = [targets]\n" +
        "\t targets \t -> list targets in nbuild.targets and common.targets files")]
    public string? Command { get; set; }

    public bool Verbose { get; set; }
}