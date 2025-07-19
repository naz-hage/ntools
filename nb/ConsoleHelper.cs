namespace homecli;

public static class ConsoleHelper
{
    /// <summary>
    /// Writes a message to the console with an optional color.
    /// </summary>
    /// <param name="message">The message to write to the console.</param>
    /// <param name="color">The optional color for the message text.</param>
    public static void WriteLine(string message, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
        }

        Console.WriteLine(message);

        // Reset the console color to the default
        if (color.HasValue)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Helper method to handle the old Colorizer format with embedded color codes
    /// </summary>
    /// <param name="message">Message in old format like "[{ConsoleColor.Red}!Error message]"</param>
    public static void WriteLineColored(string message)
    {
        // For now, just extract the ConsoleColor if present and clean up the message
        if (message.Contains("ConsoleColor.Red"))
        {
            var cleanMessage = message.Replace($"[{{{nameof(ConsoleColor)}.Red}}", "[")
                                     .Replace($"{{{nameof(ConsoleColor)}.Red}}", "");
            WriteLine(cleanMessage, ConsoleColor.Red);
        }
        else if (message.Contains("ConsoleColor.Green"))
        {
            var cleanMessage = message.Replace($"[{{{nameof(ConsoleColor)}.Green}}", "[")
                                     .Replace($"{{{nameof(ConsoleColor)}.Green}}", "");
            WriteLine(cleanMessage, ConsoleColor.Green);
        }
        else if (message.Contains("ConsoleColor.Yellow"))
        {
            var cleanMessage = message.Replace($"[{{{nameof(ConsoleColor)}.Yellow}}", "[")
                                     .Replace($"{{{nameof(ConsoleColor)}.Yellow}}", "");
            WriteLine(cleanMessage, ConsoleColor.Yellow);
        }
        else
        {
            WriteLine(message);
        }
    }
}
