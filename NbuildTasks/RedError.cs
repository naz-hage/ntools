using System;

namespace NbuildTasks
{
    public class RedError : Microsoft.Build.Utilities.Task
    {
        public string Message { get; set; }

        public override bool Execute()
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ForegroundColor = previousColor;

            Log.LogError(Message);

            return false;
        }
    }
}
