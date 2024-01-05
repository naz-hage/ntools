using System.Collections.Generic;

namespace Nbackup
{
    public class Backup
    {
        public string? Source { get; set; }
        public string? Destination { get; set; }
        public string? BackupOptions { get; set; }
        public List<string>? ExcludeFolders { get; set; }
        public List<string>? ExcludeFiles { get; set; }
        public string? LogFile { get; set; }
        public List<Backup>? BackupsList { get; set; }
    }
}
