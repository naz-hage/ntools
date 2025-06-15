public static class ListSearcher
{
    public static void ListFoldersContaining(string directoryPath, string[] folderNames)
    {
        try
        {
            foreach (string folderName in folderNames)
            {
                var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
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
    /// Finds files with specified extensions in a directory recursively.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="extensions">The array of file extensions to search for.</param>
    /// <returns>An array of file paths matching the specified extensions.</returns>
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
                ConsoleHelper.WriteLine($"No files found wih {ext} extension");
            }
        }
    }
}
