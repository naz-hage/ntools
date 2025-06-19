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

using NbuildTasks;
using System.CommandLine;

/// <summary>
/// Entry point for the lf utility. Sets up the root command and subcommands for file and folder listing.
/// </summary>
var rootCommand = new RootCommand($"File and folder listing utility {Environment.NewLine} {Nversion.Get()}");

/// <summary>
/// Command: files
/// Lists files with specified extensions in a directory (recursively).
/// </summary>
var listFilesCommand = new Command("files", "List files with specified extensions in a directory (recursively).");

/// <summary>
/// Option: --directoryPath | -d
/// The directory path to search in. Defaults to the current directory.
/// </summary>
var filesDirectoryPathOption = new Option<string>(
    name: "--directoryPath",
    description: "Directory path to search in",
    getDefaultValue: () => Directory.GetCurrentDirectory()
);
filesDirectoryPathOption.AddAlias("-d");

/// <summary>
/// Option: --extensions | -e
/// Comma-separated file extensions to search for. Defaults to ".yml,.yaml".
/// </summary>
var extensionsOption = new Option<string>(
   name: "--extensions",
   description: "Comma-separated file extensions to search for (e.g., .yml,.yaml)",
   getDefaultValue: () => ".yml,.yaml"
);
extensionsOption.AddAlias("-e");

listFilesCommand.AddOption(filesDirectoryPathOption);
listFilesCommand.AddOption(extensionsOption);

/// <summary>
/// Handler for the 'files' command.
/// Splits the extensions string, searches for files, and prints the results.
/// </summary>
/// <param name="extensions">Comma-separated list of file extensions.</param>
/// <param name="directoryPath">Directory to search in.</param>
listFilesCommand.SetHandler((string extensions, string directoryPath) =>
{
    string[] extensionsArray = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"Searching for files with specified extensions in {directoryPath} recursively");
    ListSearcher.ListFiles(directoryPath, extensionsArray);
}, extensionsOption, filesDirectoryPathOption);

rootCommand.AddCommand(listFilesCommand);

/// <summary>
/// Command: folders
/// Lists folders containing specified names in a directory (recursively).
/// </summary>
var listFoldersCommand = new Command("folders", "List folders containing specified names in a directory (recursively).");

/// <summary>
/// Option: --directoryPath | -d
/// The directory path to search in. Defaults to the current directory.
/// </summary>
var foldersDirectoryPathOption = new Option<string>(
    name: "--directoryPath",
    description: "Directory path to search in",
    getDefaultValue: () => Directory.GetCurrentDirectory()
);
foldersDirectoryPathOption.AddAlias("-d");

/// <summary>
/// Option: --name | -n
/// Comma-separated list of folder names to search for.
/// </summary>
var folderNamesOption = new Option<string>(
    name: "--name",
    description: "Comma-separated list of folder names to search for",
    getDefaultValue: () => string.Empty
);
folderNamesOption.AddAlias("-n");

listFoldersCommand.AddOption(foldersDirectoryPathOption);
listFoldersCommand.AddOption(folderNamesOption);

/// <summary>
/// Handler for the 'folders' command.
/// Splits the names string, searches for folders, and prints the results.
/// </summary>
/// <param name="directoryPath">Directory to search in.</param>
/// <param name="names">Comma-separated list of folder names.</param>
listFoldersCommand.SetHandler((string directoryPath, string names) =>
{
    string[] folderNames = names.Split(',', StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"Searching for folders containing specified names in {directoryPath} recursively");
    ListSearcher.ListFoldersContaining(directoryPath, folderNames);
}, foldersDirectoryPathOption, folderNamesOption);

rootCommand.AddCommand(listFoldersCommand);

/// <summary>
/// Program entry point. Invokes the root command with the provided arguments.
/// </summary>
return await rootCommand.InvokeAsync(args);
