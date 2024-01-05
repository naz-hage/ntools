using CommandLine;
using Launcher;
using NbuildTasks;
using OutputColorizer;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Nbackup
{
    class Program
    {
        private const string ResourceLocation = "Nbackup.Resources.Nbackup.json";
        
        public static int ReturnCode { get; private set; }

        static int Main(string[] args)
        {
            Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{Nversion.Get()}]\n");

            if (!Parser.TryParse(args, out Cli options))
            {
                ReturnCode = ResultHelper.InvalidParameter;
                if (!args[0].Equals("--help", StringComparison.CurrentCultureIgnoreCase)) Console.WriteLine($"backup completed with '{ReturnCode}'");
                return ReturnCode;
            }

            

            

            if (!string.IsNullOrEmpty(options.Extract))
            {
                if (File.Exists(options.Extract))
                {
                    File.Delete(options.Extract);
                }
                
                if (ResourceHelper.RessourceExistInCallingAssembly(ResourceLocation))
                {
                    if (!ResourceHelper.ExtractEmbeddedResourceFromCallingAssembly(ResourceLocation, options.Extract))
                    {
                        Console.WriteLine($"Could not extract nbackup.json to {Environment.CurrentDirectory}");
                        return ResultHelper.InvalidParameter;
                    }
                }

                if (File.Exists(options.Extract))
                {
                    Console.WriteLine($"Extracted sample nbackup.json to {options.Extract}");
                    // display file contents od options.Extract in console
                    Console.WriteLine(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, options.Extract)));
                }

                return 0;
            }

            var watch = Stopwatch.StartNew();
            ResultHelper result = NBackup.Perform(options);

            watch.Stop();

            Console.WriteLine($"Backup completed in {watch.ElapsedMilliseconds/1000.00} s with {result.Code}");
            // display elapsed time in HH:MM:SS.MS format
            Console.WriteLine($"Backup completed in {watch.Elapsed:hh\\:mm\\:ss\\.ff} (hh:mm:ss.ff) with {result.Code}");
            return result.Code;
        }
    }
}
