namespace Nbackup
{
    public class NBackupOptions
    {
        public string? Input { get; set; }

        public string? Extract { get; set; }

        public bool Verbose { get; set; }

        public bool PerformBackup { get; set; } = true;
    }
}