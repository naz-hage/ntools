using Nbuild.Helpers;

namespace Sdo.Services
{
    /// <summary>
    /// Provides recursive file and folder search operations for SDO.
    /// </summary>
    public static class FileListSearcher
    {
        public static void ListFoldersContaining(string directoryPath, string[] folderNames)
        {
            try
            {
                var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var folderName in folderNames)
                {
                    var matchingDirectories = directories
                        .Where(directory => directory.Contains(folderName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (matchingDirectories.Length > 0)
                    {
                        ConsoleHelper.WriteLine($"Found {matchingDirectories.Length} folders containing '{folderName}':");
                        foreach (var directory in matchingDirectories)
                        {
                            ConsoleHelper.WriteSuccess(directory);
                        }
                    }
                    else
                    {
                        ConsoleHelper.WriteError($"No folders found containing the name '{folderName}'.");
                    }
                }
            }
            catch (Exception exception)
            {
                ConsoleHelper.WriteError($"An error occurred while searching for folders: {exception.Message}");
            }
        }

        public static void ListFiles(string directory, string[] extensions)
        {
            foreach (var extension in extensions)
            {
                var foundCount = 0;
                ListFilesRecursive(directory, extension, ref foundCount);
                if (foundCount == 0)
                {
                    ConsoleHelper.WriteLine($"No files found with {extension} extension");
                }
                else
                {
                    ConsoleHelper.WriteLine($"Found {foundCount} files with {extension} extension.");
                }
            }
        }

        private static void ListFilesRecursive(string directory, string extension, ref int foundCount)
        {
            try
            {
                var files = Directory.GetFiles(directory, $"*{extension}", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    ConsoleHelper.WriteSuccess(file);
                    foundCount++;
                }

                foreach (var subdirectory in Directory.GetDirectories(directory))
                {
                    ListFilesRecursive(subdirectory, extension, ref foundCount);
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                ConsoleHelper.WriteLine($"Access denied to a directory: {exception.Message}", ConsoleColor.Yellow);
            }
            catch (DirectoryNotFoundException exception)
            {
                ConsoleHelper.WriteLine($"Directory not found: {exception.Message}", ConsoleColor.Yellow);
            }
            catch (Exception exception)
            {
                ConsoleHelper.WriteError($"An error occurred: {exception.Message}");
            }
        }
    }
}