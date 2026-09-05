using System;
using System.Collections.Generic;
using System.Text;

namespace NbuildTasks
{
    public static class DeprecationNotice
    {
        private const string DeprecationMessage =
            "NOTICE: This tool will be deprecated soon. Please familiarize yourself with using sdo.";

        /// <summary>
        /// Displays the gentle deprecation warning to stderr or stdout.
        /// </summary>
        public static void DisplayWarning()
        {
            var previousColor = Console.ForegroundColor;

            // Use Yellow for visibility without looking like a breaking error
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(DeprecationMessage);
            Console.Error.WriteLine(); // Blank line for spacing
            Console.ForegroundColor = previousColor;
        }
    }
}
