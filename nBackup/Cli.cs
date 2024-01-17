using CommandLine.Attributes;

namespace Nbackup
{
    public class Cli
    {
        [OptionalArgument("", "i", "input json file which specifies source, destination and backup options.")]
        public string? Input { get; set; }

        [OptionalArgument("", "e", "Extract input json example file to current directory.")]
        public string? Extract { get; set; }

        [OptionalArgument(false, "v", "Verbose level")]
        public bool Verbose { get; internal set; }

        [OptionalArgument(true, "performbackup", " Set to false to verify json file without backup")]
        public bool PerformBackup { get; internal set; }
    }
}
