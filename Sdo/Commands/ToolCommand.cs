using System.CommandLine;
using Nbuild.Helpers;
using NbuildCommand = Nbuild.Command;

namespace Sdo.Commands
{
    /// <summary>
    /// Command handler for environment tool management.
    /// </summary>
    public class ToolCommand : System.CommandLine.Command
    {
        /// <summary>
        /// Initializes a new instance of the ToolCommand class.
        /// </summary>
        /// <param name="verboseOption">Option for verbose output.</param>
        public ToolCommand(Option<bool> verboseOption) : base("tool", "Environment tool installation, auditing, and manifest management")
        {
            AddListCommand(verboseOption);
        }

        private void AddListCommand(Option<bool> verboseOption)
        {
            var listCommand = new System.CommandLine.Command(
                "list",
                "Display a formatted table of all tools and their versions.");
            listCommand.TreatUnmatchedTokensAsErrors = true;

            var jsonOption = new Option<string>("--json")
            {
                Description = "Full path to the manifest file containing your tool definitions."
            };
            jsonOption.DefaultValueFactory = _ =>
            {
                if (Environment.GetEnvironmentVariable("ProgramFiles") is string programFiles)
                {
                    return $"\"{Path.Combine(programFiles, "nbuild", "apps.json")}\"";
                }

                return $"\"{NbuildCommand.DefaultAppsFile}\"";
            };

            listCommand.Options.Add(jsonOption);
            listCommand.Add(verboseOption);
            listCommand.SetAction(parseResult =>
            {
                var json = parseResult.GetValue(jsonOption) ?? string.Empty;
                var verbose = parseResult.GetValue(verboseOption);

                try
                {
                    return NbuildCommand.List(json, verbose).Code;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Error: {exception.Message}");
                    return -1;
                }
            });

            Subcommands.Add(listCommand);
        }
    }
}