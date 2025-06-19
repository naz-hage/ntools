namespace lf
{
    /// <summary>
    /// Provides static methods for searching and listing files and folders in a directory tree.
    /// </summary>
    public static class ListSearcher
    {
        /// <summary>
        /// Lists all folders within the specified directory (recursively) that contain any of the provided folder names.
        /// </summary>
        /// <param name="directoryPath">The root directory to search in.</param>
        /// <param name="folderNames">An array of folder name substrings to search for.</param>
        public static void ListFoldersContaining(string directoryPath, string[] folderNames)
        {
            try
            {
                var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                foreach (string folderName in folderNames)
                {
                    var matchingDirs = directories
                        .Where(dir => dir.Contains(folderName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (matchingDirs.Length > 0)
                    {
                        Console.WriteLine($"Found {matchingDirs.Length} folders containing '{folderName}':");
                        foreach (var dir in matchingDirs)
                        {
                            ConsoleHelper.WriteLine(dir, ConsoleColor.Green);
                        }
                    }
                    else
                    {
                        ConsoleHelper.WriteLine($"No folders found containing the name '{folderName}'.", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"An error occurred while searching for folders: {ex.Message}", ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Lists all files with the specified extensions in the given directory and its subdirectories.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="extensions">An array of file extensions to search for (e.g., ".yml", ".yaml").</param>
        public static void ListFiles(string directory, string[] extensions)
        {
            foreach (var ext in extensions)
            {
                var filesWithExt = Directory.GetFiles(directory, $"*{ext}", SearchOption.AllDirectories);
                if (filesWithExt.Length > 0)
                {
                    Console.WriteLine($"Found {filesWithExt.Length} files with {ext} extension:");
                    foreach (string file in filesWithExt)
                    {
                        ConsoleHelper.WriteLine(file, ConsoleColor.Green);
                    }
                }
                else
                {
                    ConsoleHelper.WriteLine($"No files found with {ext} extension");
                }
            }
        }
    }
}
