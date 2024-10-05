using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class ColorMessage : Task
{
    [Required]
    public string Message { get; set; }

    public string Color { get; set; }

    public override bool Execute()
    {
        // Save the current console color
        var previousColor = Console.ForegroundColor;

        // Set the console color based on the Color property
        if (!string.IsNullOrEmpty(Color))
        {
            try
            {
                Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), Color, true);
            }
            catch (ArgumentException)
            {
                // If the color is invalid, log a warning and reset the color
                Log.LogWarning($"Invalid color '{Color}'. Using default console color.");
                Console.ForegroundColor = previousColor;
            }
        }

        // Log the message using MSBuild's logging mechanism
        // Log.LogMessage(MessageImportance.High, Message)
        // The above line is commented out because it does not change color
        Console.WriteLine(Message);

        // Reset the console color
        Console.ForegroundColor = previousColor;

        return true;
    }
}