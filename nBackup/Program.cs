using CommandLine;
using launcher;
using OutputColorizer;
using System;
using System.Diagnostics;

namespace nbackup
{
    class Program
    {
        public static int ReturnCode { get; private set; }

        static int Main(string[] args)
        {
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");

            if (!Parser.TryParse(args, out Cli options))
            {
                ReturnCode = Result.InvalidParameter;
                if (args[0].ToLower() != "--help") Console.WriteLine($"backup completed with '{ReturnCode}'");
                return ReturnCode;
            }

            var watch = Stopwatch.StartNew();
            Result result = new();
            if (!string.IsNullOrEmpty(options.Source) &&
                !string.IsNullOrEmpty(options.Destination) &&
                !string.IsNullOrEmpty(options.Backup))
            {
                result = NBackup.Perform(options.Source, options.Destination, options.Backup);
            }
            else if (!string.IsNullOrEmpty(options.Input))
            {
                result = NBackup.Perform(options);
            }

            watch.Stop();

            Console.WriteLine($"Backup completed in {watch.ElapsedMilliseconds/1000.00} s with {ReturnCode}");
            return result.Code;
        }
    }
}
