using CommandLine.Attributes;

namespace nbackup
{
    public class Cli
    {
        [OptionalArgument("", "i", "input backup file which specifies source, destination and backup options")] 
        public string? Input { get; set; }

        [OptionalArgument(false, "v", "Verbose Values: true | false.  Default is false")]
        public bool Verbose { get; internal set; }

        [OptionalArgument(true, "performbackup", "Values: true | false. default is true")]
        public bool PerformBackup { get; internal set; }
    }
}
