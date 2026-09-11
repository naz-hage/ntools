using System.CommandLine;
using Nbuild;
using Nbuild.Helpers;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for build automation and target management.
    /// </summary>
    public class BuildCommand : System.CommandLine.Command
    {
        /// <summary>
        /// Initializes a new instance of the BuildCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public BuildCommand(Option<bool> verboseOption) : base("build", "Build automation and target management")
        {
            AddTargetsCommand(verboseOption);
        }

        private void AddTargetsCommand(Option<bool> verboseOption)
        {
            var targetsCommand = new System.CommandLine.Command(
                "targets",
                "Displays all available build targets for the current solution or project.");

            targetsCommand.Add(verboseOption);
            targetsCommand.SetAction(parseResult =>
            {
                if (parseResult.GetValue(verboseOption))
                {
                    ConsoleHelper.WriteLine("[VERBOSE] Displaying build targets.", ConsoleColor.Gray);
                }

                var result = BuildStarter.DisplayTargets(Environment.CurrentDirectory);
                return result.Code;
            });

            Subcommands.Add(targetsCommand);
        }
    }
}