using CommandLine;
using NbuildTasks;
using OutputColorizer;

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

            try
            {
                options.Validate();
            }
            catch (ArgumentException ex)
            {
                HandleError($"Invalid arguments: {ex.Message}", 1);
            }

            bool result = false;
            try
            {
                result = await ExecuteCommand(options);
            }
            catch (Exception ex)
            {
                HandleError($"Exception: {ex.Message}", -1);
            }

            DisplayResult(result, options.Command);
            Environment.Exit(result ? 0 : -1);
        }

        private static async Task<bool> ExecuteCommand(Cli options)
        {
            return options.Command switch
            {
                Cli.CommandType.create => await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetFileName!),
                Cli.CommandType.pre_release => await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetFileName!, true),
                Cli.CommandType.download => await Command.DownloadAsset(options.Repo!, options.Tag!, options.AssetPath!),
                _ => throw new InvalidOperationException("Invalid command")
            };
        }

        private static void HandleError(string message, int exitCode)
        {
            Colorizer.WriteLine($"[{ConsoleColor.Red}!× '{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}': {message}]");
            Environment.Exit(exitCode);
        }

        private static void DisplayResult(bool result, Cli.CommandType command)
        {
            Colorizer.WriteLine(result
                ? $"[{ConsoleColor.Green}!√ '{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}': {command} completed successfully]"
                : $"[{ConsoleColor.Red}!× '{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}': {command} failed]");
        }
    }
}
