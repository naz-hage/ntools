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

                //Console.WriteLine("Debug: Validated CLI arguments successfully.");
                //Environment.Exit(0);
            }
            catch (ArgumentException ex)
            {
                Colorizer.WriteLine($"[{ConsoleColor.Red}!Invalid arguments: {ex.Message}]");
                Parser.DisplayHelp<Cli>();
                Environment.Exit(1);
            }

            bool result = false;
            try
            {
                result = options.Command switch
                {
                    Cli.CommandType.create => await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetFileName!),
                    Cli.CommandType.pre_release => await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetFileName!, true),
                    Cli.CommandType.download => await Command.DownloadAsset(options.Repo!, options.Tag!, options.AssetPath!),
                    _ => throw new InvalidOperationException("Invalid command")
                };
            }
            catch (Exception ex)
            {
                // log exception
                Colorizer.WriteLine($"[{ConsoleColor.Red}!× " +
                    $"'{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}': Exception: {ex.Message}]");
                Environment.Exit(-1);
            }

            Colorizer.WriteLine(result
                ? $"[{ConsoleColor.Green}!√ '{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}': {options.Command} completed successfully]"
                : $"[{ConsoleColor.Red}!× '{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}': {options.Command} failed]");
            Environment.Exit(result ? 0 : -1);
        }
    }
}
