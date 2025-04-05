﻿using CommandLine;
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

            try
            {
                switch (options.Command)
                {
                    // create a release
                    case Cli.CommandType.create:
                        await Command.CreateRelease(options.Repo!, options.Tag!, options.Branch!, options.AssetPath!);
                        break;

                    // download an asset
                    case Cli.CommandType.download:
                        await Command.DownloadAsset(options.Repo!, options.Tag!, options.AssetPath!);
                        break;

                    default:
                        Console.WriteLine($"Invalid command '{options.Command}'. Please use ");
                        Console.WriteLine("     'notes' get release notes since tag");
                        Console.WriteLine("     'upload' upload an asset");
                        Console.WriteLine("     'create' create a release");
                        Console.WriteLine("     'update' update a release");

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

            Environment.Exit(0);
        }

        private static void DisplayHelp()
        {
            Parser.DisplayHelp<Cli>();
        }
    }
}
