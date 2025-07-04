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
                int foundCount = 0;
                ListFilesRecursive(directory, ext, ref foundCount);
                if (foundCount == 0)
                {
                    ConsoleHelper.WriteLine($"No files found with {ext} extension");
                }
                else
                {
                    Console.WriteLine($"Found {foundCount} files with {ext} extension.");
                }
            }
        }

        // Recursively search for files, skipping inaccessible directories
        private static void ListFilesRecursive(string directory, string ext, ref int foundCount)
        {
            try
            {
                var files = Directory.GetFiles(directory, $"*{ext}", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    ConsoleHelper.WriteLine(file, ConsoleColor.Green);
                    foundCount++;
                }
                var subdirs = Directory.GetDirectories(directory);
                foreach (var subdir in subdirs)
                {
                    ListFilesRecursive(subdir, ext, ref foundCount);
                }
            }
            catch (UnauthorizedAccessException uaex)
            {
                ConsoleHelper.WriteLine($"Access denied to a directory: {uaex.Message}", ConsoleColor.Yellow);
            }
            catch (DirectoryNotFoundException dnfx)
            {
                ConsoleHelper.WriteLine($"Directory not found: {dnfx.Message}", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteLine($"An error occurred: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}
