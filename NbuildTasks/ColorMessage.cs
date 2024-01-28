using System;

namespace NbuildTasks
{
    public class ColorMessage : Microsoft.Build.Utilities.Task
    {
        public string Message { get; set; }
        public string Color { get; set; }
        public override bool Execute()
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), Color);
            Console.WriteLine(Message);
            Console.ForegroundColor = previousColor;

            return true;
        }
    }
}
