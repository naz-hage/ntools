using CommandLine;
using NbuildTasks;
using OutputColorizer;
using System.Xml.Linq;

namespace GitHubRelease
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"{Nversion.Get()}\n");

            if (!Parser.TryParse(args, out Cli options))
            {
                Environment.Exit(-1);
            }

            // Validate the CLI arguments
            try
            {
                options.Validate();
            }
            catch (ArgumentException ex)
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Invalid arguments: {ex.Message}]");
                Parser.DisplayHelp<Cli>();
                Environment.Exit(1);
            }

            // Print the command line values and exit
            Console.WriteLine($"Repo: {options.Repo}");
            Console.WriteLine($"Tag: {options.Tag}");
            Console.WriteLine($"Branch: {options.Branch}");
            Console.WriteLine($"Asset Path: {options.AssetPath}");

            bool result = false;
            try
            {
                switch (options.Command)
                {
                    // create a release
                    case Cli.CommandType.create:
                        result = await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetPath!);
                        break;

                    // download an asset
                    case Cli.CommandType.download:
                        result = await Command.DownloadAsset(options.Repo!, options.Tag!, options.AssetPath!);
                        break;

                    default:

                        Environment.Exit(1);
                        break;
                }
            }
            catch (Exception ex)
            {
                // log the type of exception Is it a NullReferenceException, ArgumentException, etc.
                Console.WriteLine(ex.ToString());

                // log exception
                Console.WriteLine($"Exception {ex.Message}");
                Environment.Exit(1);
            }

            Colorizer.WriteLine(result
                ? $"[{ConsoleColor.Green}!√ 'gitbubrelease.exe': {options.Command} completed successfully]"
                : $"[{ConsoleColor.Red}!× 'gitbubrelease.exe': {options.Command} failed]");
            Environment.Exit(result ? 0 : -1);
        }
    }
}
