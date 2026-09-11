using System.CommandLine;
using Sdo.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for file and folder listing utilities.
    /// </summary>
    public class FileCommand : System.CommandLine.Command
    {
        /// <summary>
        /// Initializes a new instance of the FileCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public FileCommand(Option<bool> verboseOption) : base("file", "File and folder listing utilities")
        {
            AddFilesCommand(verboseOption);
            AddFoldersCommand(verboseOption);
        }

        private void AddFilesCommand(Option<bool> verboseOption)
        {
            var filesCommand = new System.CommandLine.Command(
                "files",
                "List files with specified extensions in a directory (recursively).");
            var directoryPathOption = new Option<string>("--directoryPath", ["-d"])
            {
                Description = "Directory path to search in",
                DefaultValueFactory = _ => Directory.GetCurrentDirectory()
            };
            var extensionsOption = new Option<string>("--extensions", new[] { "-e" })
            {
                Description = "Comma-separated file extensions to search for (e.g., .yml,.yaml)"
            };
            extensionsOption.DefaultValueFactory = _ => ".yml,.yaml";

            filesCommand.Options.Add(directoryPathOption);
            filesCommand.Options.Add(extensionsOption);
            filesCommand.Add(verboseOption);
            filesCommand.SetAction(parseResult =>
            {
                var directoryPath = parseResult.GetValue(directoryPathOption) ?? Directory.GetCurrentDirectory();
                var extensions = (parseResult.GetValue(extensionsOption) ?? ".yml,.yaml")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"Searching for files with specified extensions in {directoryPath} recursively");
                FileListSearcher.ListFiles(directoryPath, extensions);
                return 0;
            });

            Subcommands.Add(filesCommand);
        }

        private void AddFoldersCommand(Option<bool> verboseOption)
        {
            var foldersCommand = new System.CommandLine.Command(
                "folders",
                "List folders containing specified names in a directory (recursively).");
            var directoryPathOption = new Option<string>("--directoryPath", new[] { "-d" })
            {
                Description = "Directory path to search in"
            };
            directoryPathOption.DefaultValueFactory = _ => Directory.GetCurrentDirectory();
            var namesOption = new Option<string>("--name", new[] { "-n" })
            {
                Description = "Comma-separated list of folder names to search for"
            };
            namesOption.DefaultValueFactory = _ => string.Empty;

            foldersCommand.Options.Add(directoryPathOption);
            foldersCommand.Options.Add(namesOption);
            foldersCommand.Add(verboseOption);
            foldersCommand.SetAction(parseResult =>
            {
                var directoryPath = parseResult.GetValue(directoryPathOption) ?? Directory.GetCurrentDirectory();
                var names = (parseResult.GetValue(namesOption) ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"Searching for folders containing specified names in {directoryPath} recursively");
                FileListSearcher.ListFoldersContaining(directoryPath, names);
                return 0;
            });

            Subcommands.Add(foldersCommand);
        }
    }
}