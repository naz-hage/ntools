using System.CommandLine;
using Nbuild.Helpers;
using Nbuild.Services;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for environment utilities.
    /// </summary>
    public class EnvironmentCommand : Command
    {
        /// <summary>
        /// Initializes a new instance of the EnvironmentCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public EnvironmentCommand(Option<bool> verboseOption) : base("env", "Environment and local system utilities")
        {
            AddPathCommand(verboseOption);
        }

        private void AddPathCommand(Option<bool> verboseOption)
        {
            var pathCommand = new Command(
                "path",
                "Display each segment of the effective PATH environment variable on a separate line, with duplicates removed.");

            pathCommand.Add(verboseOption);
            pathCommand.SetAction(parseResult =>
            {
                PathManager.DisplayPathSegments();
                if (parseResult.GetValue(verboseOption))
                {
                    ConsoleHelper.WriteLine("Displaying PATH segments.", ConsoleColor.Gray);
                }

                return 0;
            });

            Subcommands.Add(pathCommand);
        }
    }
}