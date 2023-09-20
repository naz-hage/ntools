using CommandLine.Attributes;

namespace nbackup
{
    public class Cli
    {
        [OptionalArgument("", "src", "Source Folder")]
        public string? Source { get; set; }

        [OptionalArgument("", "dest", "Destination folder")]
        public string? Destination { get; set; }

        [OptionalArgument("/s /XD .git /XD .vs /XD TestResults /XF *.exe /XF *.dll /XF *.pdb /e", "opt", "Backup Options")]
        public string? Backup { get; set; }

        [OptionalArgument("", "input", "input backup file which specifies source, destination and backup options")] 
        public string? Input { get; set; }

        [OptionalArgument(false, "verbose", "Values: true | false.  Default is false")]
        public bool Verbose { get; internal set; }

        [OptionalArgument(true, "performbackup", "Values: true | false. default is true")]
        public bool PerformBackup { get; internal set; }
    }
}
