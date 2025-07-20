namespace Nbuild;

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
}
