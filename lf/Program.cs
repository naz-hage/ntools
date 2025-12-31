// --------------------------------------------------------------------------------------
// File: Program.cs
// Description: Entry point for the lf utility. Provides commands for listing files and folders
//              in a directory tree with flexible filtering options using System.CommandLine.
// 
// Usage:
//   lf files   -d <directory> -e <extensions>
//   lf folders -d <directory> -n <folder names>
//
// Dependencies:
//   - NbuildTasks.ListSearcher: Provides static methods for file and folder searching.
//
// Author: [Your Name]
// --------------------------------------------------------------------------------------

using lf;
using NbuildTasks;
using System.CommandLine;

/// <summary>
/// Entry point for the lf utility. Sets up the root command and subcommands for file and folder listing.
/// </summary>
string nv;
try { nv = Nversion.Get(); } catch { nv = "ntools (version unknown)"; }
var rootCommand = new RootCommand($"File and folder listing utility {Environment.NewLine} {nv}");

/// <summary>
/// Command: files
/// Lists files with specified extensions in a directory (recursively).
/// </summary>
var listFilesCommand = new Command("files", "List files with specified extensions in a directory (recursively).");

/// <summary>
/// Option: --directoryPath | -d
/// The directory path to search in. Defaults to the current directory.
/// </summary>
var filesDirectoryPathOption = new Option<string>("--directoryPath", "-d")
{
    Description = "Directory path to search in"
};
filesDirectoryPathOption.DefaultValueFactory = _ => Directory.GetCurrentDirectory();

/// <summary>
/// Option: --extensions | -e
/// Comma-separated file extensions to search for. Defaults to ".yml,.yaml".
/// </summary>
var extensionsOption = new Option<string>("--extensions", "-e")
{
    Description = "Comma-separated file extensions to search for (e.g., .yml,.yaml)"
};
extensionsOption.DefaultValueFactory = _ => ".yml,.yaml";

listFilesCommand.Options.Add(filesDirectoryPathOption);
listFilesCommand.Options.Add(extensionsOption);

/// <summary>
/// Handler for the 'files' command.
/// Splits the extensions string, searches for files, and prints the results.
/// </summary>
listFilesCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
{
    var extensions = parseResult.GetValue(extensionsOption);
    var directoryPath = parseResult.GetValue(filesDirectoryPathOption);
    string[] extensionsArray = (extensions ?? ".yml,.yaml").Split(',', StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"Searching for files with specified extensions in {directoryPath} recursively");
    ListSearcher.ListFiles(directoryPath ?? Directory.GetCurrentDirectory(), extensionsArray);
    return 0;
});

rootCommand.Subcommands.Add(listFilesCommand);

/// <summary>
/// Command: folders
/// Lists folders containing specified names in a directory (recursively).
/// </summary>
var listFoldersCommand = new Command("folders", "List folders containing specified names in a directory (recursively).");

/// <summary>
/// Option: --directoryPath | -d
/// The directory path to search in. Defaults to the current directory.
/// </summary>
var foldersDirectoryPathOption = new Option<string>("--directoryPath", "-d")
{
    Description = "Directory path to search in"
};
foldersDirectoryPathOption.DefaultValueFactory = _ => Directory.GetCurrentDirectory();

/// <summary>
/// Option: --name | -n
/// Comma-separated list of folder names to search for.
/// </summary>
var folderNamesOption = new Option<string>("--name", "-n")
{
    Description = "Comma-separated list of folder names to search for"
};
folderNamesOption.DefaultValueFactory = _ => string.Empty;

listFoldersCommand.Options.Add(foldersDirectoryPathOption);
listFoldersCommand.Options.Add(folderNamesOption);

/// <summary>
/// Handler for the 'folders' command.
/// Splits the names string, searches for folders, and prints the results.
/// </summary>
listFoldersCommand.SetAction((System.CommandLine.ParseResult parseResult) =>
{
    var directoryPath = parseResult.GetValue(foldersDirectoryPathOption);
    var names = parseResult.GetValue(folderNamesOption);
    string[] folderNames = (names ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"Searching for folders containing specified names in {directoryPath} recursively");
    ListSearcher.ListFoldersContaining(directoryPath ?? Directory.GetCurrentDirectory(), folderNames);
    return 0;
});

rootCommand.Subcommands.Add(listFoldersCommand);

/// <summary>
/// Program entry point. Invokes the root command with the provided arguments.
/// </summary>
var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
