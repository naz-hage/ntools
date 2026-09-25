using System.Text.RegularExpressions;


namespace rb_anlyze
{
    public enum RobocopyFileStatus
    {
        Copied,
        Skipped,
        Mismatched,
        Failed
    }

    public class RobocopyLogProcessor
    {
        private readonly List<string> _lines = new();
        private readonly Regex _filterRegex;

        public RobocopyLogProcessor(string filterPattern = "(same|tweaked)")
        {
            _filterRegex = new Regex(filterPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        // -------------------------------
        // READ (Sync + Async)
        // -------------------------------

        public void ReadRobocopyLog(string filePath)
        {
            _lines.Clear();
            _lines.AddRange(File.ReadAllLines(filePath));
        }

        public async Task ReadRobocopyLogAsync(string filePath)
        {
            _lines.Clear();
            _lines.AddRange(await File.ReadAllLinesAsync(filePath));
        }

        // -------------------------------
        // FILTER
        // -------------------------------

        private IEnumerable<string> GetTrimmedLines(RobocopyFileStatus status = RobocopyFileStatus.Copied)
        {
            return _lines.Where(line =>
                IsStatusLine(line, status) &&
                (status == RobocopyFileStatus.Skipped || !IsFilteredStatus(line, status)));
        }

        private bool IsFilteredStatus(string line, RobocopyFileStatus status)
        {
            var statusPattern = status switch
            {
                RobocopyFileStatus.Copied => @"^\s+(New File|Newer|Changed)\s+",
                RobocopyFileStatus.Mismatched => @"^\s+(Mismatch|Mismatched)\s+",
                _ => string.Empty
            };

            var statusMatch = Regex.Match(line, statusPattern, RegexOptions.IgnoreCase);
            return statusMatch.Success && _filterRegex.IsMatch(statusMatch.Groups[1].Value);
        }

        private bool IsStatusLine(string line, RobocopyFileStatus status)
        {
            return status switch
            {
                RobocopyFileStatus.Copied => Regex.IsMatch(line, @"^\s+(New File|Newer|Changed)\s+", RegexOptions.IgnoreCase),
                RobocopyFileStatus.Skipped => Regex.IsMatch(
                    line,
                    @"^\s+(Same|Tweaked|Older|named|\*named file)\s+",
                    RegexOptions.IgnoreCase),
                RobocopyFileStatus.Mismatched => Regex.IsMatch(
                    line,
                    @"^\s+(Mismatch|Mismatched)\s+",
                    RegexOptions.IgnoreCase),
                RobocopyFileStatus.Failed => Regex.IsMatch(
                    line,
                    @"ERROR\s+\d+.*(?:Copying (?:File|Directory)|Accessing (?:Source|Destination) Directory)\s+",
                    RegexOptions.IgnoreCase),
                _ => false
            };
        }

        // -------------------------------
        // SUMMARY STATISTICS
        // -------------------------------

        public (int Copied, int Skipped, int Mismatched, int Failed) GetSummary()
        {
            int copied = 0;
            int skipped = 0;
            int mismatched = 0;
            int failed = 0;

            foreach (var line in _lines)
            {
                var columns = Regex.Match(
                    line,
                    @"^\s*(?:Dirs|Files)\s*:\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+\d+\s*$",
                    RegexOptions.IgnoreCase);

                if (!columns.Success)
                    continue;

                copied += int.Parse(columns.Groups[2].Value);
                skipped += int.Parse(columns.Groups[3].Value);
                mismatched += int.Parse(columns.Groups[4].Value);
                failed += int.Parse(columns.Groups[5].Value);
            }

            return (copied, skipped, mismatched, failed);
        }

        // -------------------------------
        // DISPLAY (Color-coded)
        // -------------------------------

        public void DisplayTrimmedRobocopyLog(RobocopyFileStatus status = RobocopyFileStatus.Copied)
        {
            foreach (var line in GetTrimmedLines(status))
            {
                if (line.Contains("Copied", StringComparison.OrdinalIgnoreCase))
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (line.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                    Console.ForegroundColor = ConsoleColor.Red;
                else if (line.Contains("Mismatch", StringComparison.OrdinalIgnoreCase))
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ResetColor();

                Console.WriteLine(line);
            }

            Console.ResetColor();
        }

        // -------------------------------
        // WRITE (Sync + Async)
        // -------------------------------

        public void WriteUpdatedRobocopyLog(
            string outputFilePath,
            RobocopyFileStatus status = RobocopyFileStatus.Copied)
        {
            File.WriteAllLines(outputFilePath, GetTrimmedLines(status));
        }

        public async Task WriteUpdatedRobocopyLogAsync(
            string outputFilePath,
            RobocopyFileStatus status = RobocopyFileStatus.Copied)
        {
            await File.WriteAllLinesAsync(outputFilePath, GetTrimmedLines(status));
        }

        // -------------------------------
        // BOXBUILDER INTEGRATION
        // -------------------------------

        public string BuildSummaryBox()
        {
            var (copied, skipped, mismatched, failed) = GetSummary();

            var box = new BoxBuilder(
                "Metric",
                "Count"
            );

            box.AddRow("Copied", copied.ToString());
            box.AddRow("Skipped", skipped.ToString());
            box.AddRow("Mismatched", mismatched.ToString());
            box.AddRow("Failed", failed.ToString());

            return box.Build();
        }
    }
}
