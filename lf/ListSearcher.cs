using System;
using System.IO;

public static class ListSearcher
{
    public static void ListFoldersContaining(string directoryPath, string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            ConsoleHelper.WriteLine("No folder name specified.", ConsoleColor.Red);
            return;
        }

        try
        {
            var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
            bool found = false;

            foreach (var dir in directories)
            {
                if (dir.Contains(folderName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleHelper.WriteLine(dir, ConsoleColor.Green);
                    found = true;
                }
            }

            if (!found)
            {
                ConsoleHelper.WriteLine($"No folders found containing the name '{folderName}'.", ConsoleColor.Red);
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteLine($"An error occurred while searching for folders: {ex.Message}",ConsoleColor.Red);
        }
    }
}
