// --------------------------------------------------------------------------------------
// File: Program.cs
// Description: File and folder listing utility using System.CommandLine.
// 
// Commands:
//   files   - Lists files with specified extensions in a directory (recursively).
//             Options:
//               -d, --directoryPath   Directory path to search in (default: current directory)
//               -e, --extensions      Comma-separated file extensions (default: .yml,.yaml)
//   folders - Lists folders containing specified names in a directory (recursively).
//             Options:
//               -d, --directoryPath   Directory path to search in (default: current directory)
//               -n, --name            Comma-separated list of folder names to search for
//
// Usage Examples:
//   lf files -d C:\Projects -e .cs,.md
//   lf folders -d C:\Projects -n bin,obj
// --------------------------------------------------------------------------------------
using NbuildTasks;
using System.CommandLine;

/// <summary>
/// Initializes a new instance of the <see cref="RootCommand"/> class with a description
/// for the file and folder listing utility.
/// </summary>
var rootCommand = new RootCommand($"File and folder listing utility {Environment.NewLine} {Nversion.Get()}");

/// <summary>
/// This command searches for files with given extensions recursively in the specified directory.
/// </summary>
var listFilesCommand = new Command("files", $"List files with specified extensions in a directory  {Environment.NewLine} {Nversion.Get()}");

var filesDirectoryPathOption = new Option<string>(
    name: "--directoryPath",
    description: "Directory path to search in",
    getDefaultValue: () => Directory.GetCurrentDirectory()
);
filesDirectoryPathOption.AddAlias("-d");

var extensionsOption = new Option<string>(
   name: "--extensions",
   description: "Comma-separated file extensions to search for (e.g., .yml,.yaml)",
   getDefaultValue: () => ".yml,.yaml"
);
extensionsOption.AddAlias("-e");

listFilesCommand.AddOption(filesDirectoryPathOption);
listFilesCommand.AddOption(extensionsOption);

/// <summary>
/// Sets the handler for the listFilesCommand to search for files with specified extensions.
/// This method retrieves the file extensions from the command line arguments,
/// searches for files with those extensions in the specified directory,
/// and displays the results.
/// /// If no files are found, it outputs a message indicating that no files were found.
/// </summary>
listFilesCommand.SetHandler((string extensions, string directoryPath) =>
{
    string[] extensionsArray = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"Searching for files with specified extensions in {directoryPath} recursively");
    ListSearcher.ListFiles(directoryPath, extensionsArray);
   
}, extensionsOption, filesDirectoryPathOption);

rootCommand.AddCommand(listFilesCommand);

/// <summary>
/// This command searches for folders
/// containing specified names recursively in the given directory.
/// </summary>
var listFoldersCommand = new Command("folders", $"List folders containing specified names in a directory {Environment.NewLine} {Nversion.Get()}");

/// <summary>
/// Option to specify the directory path to search in.
/// </summary>
var foldersDirectoryPathOption = new Option<string>(
    name: "--directoryPath",
    description: "Directory path to search in",
    getDefaultValue: () => Directory.GetCurrentDirectory()
);
foldersDirectoryPathOption.AddAlias("-d");

/// <summary>
/// Option to specify a comma-separated list of folder names to search for.
/// This option allows the user to specify multiple folder names,
/// which will be used to search for folders containing any of those names.
/// </summary>
var folderNamesOption = new Option<string>(
    name: "--name",
    description: "Comma-separated list of folder names to search for",
    getDefaultValue: () => string.Empty
);
folderNamesOption.AddAlias("-n");

// Add options to the listFoldersCommand
listFoldersCommand.AddOption(foldersDirectoryPathOption);
listFoldersCommand.AddOption(folderNamesOption);

/// <summary>
/// Sets the handler for the listFoldersCommand to search for folders containing specified names.
/// </summary>
listFoldersCommand.SetHandler((string directoryPath, string names) =>
{
    string[] folderNames = names.Split(',', StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"Searching for folders containing specified names in {directoryPath} recursively");
    ListSearcher.ListFoldersContaining(directoryPath, folderNames);

}, foldersDirectoryPathOption, folderNamesOption);

rootCommand.AddCommand(listFoldersCommand);

return await rootCommand.InvokeAsync(args);
