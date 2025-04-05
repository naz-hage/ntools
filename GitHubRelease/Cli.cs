using CommandLine.Attributes;

namespace GitHubRelease
{
    /// <summary>
    /// Represents the command-line interface (CLI) options for the GitHubRelease application.
    /// </summary>
    public class Cli
    {
        /// <summary>
        /// Enum representing the possible command types.
        /// </summary>
        public enum CommandType
        {
            create,
            download,
        }

        /// <summary>
        /// Gets or sets the command to execute.
        /// Possible values: create, download.
        /// </summary>
        [RequiredArgument(0, "command", "Specifies the command to execute.\n" +
            "\t create \t -> Create a release. Requires repo, tag, branch and path.\n" +
            "\t download \t -> Download an asset. Requires repo, tag, and path\n" +
            "\t ----\n")]
        public CommandType Command { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        [OptionalArgument("", "repo", "Specifies the repository name.")]
        public string? Repo { get; set; }

        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        [OptionalArgument("", "tag", "Specifies the tag name.")]
        public string? Tag { get; set; }

        /// <summary>
        /// Gets or sets the branch name.
        /// </summary>
        [OptionalArgument("main", "branch", "Specifies the branch name.")]
        public string? Branch { get; set; }

        /// <summary>
        /// Gets or sets the asset path.
        /// </summary>
        [OptionalArgument("", "path", "Specifies the asset path. Must be an absolute path.")]
        public string? AssetPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to set the console output verbose level.
        /// </summary>
        [OptionalArgument(false, "v", "Optional parameter which sets the console output verbose level.")]
        public bool Verbose { get; set; }

        private static readonly Dictionary<string, CommandType> CommandMap = new()
            {
                { "create", CommandType.create },
                { "download", CommandType.download },
            };

        /// <summary>
        /// Gets the command type from the command string.
        /// </summary>
        /// <returns>The command type.</returns>
        /// <exception cref="ArgumentException">Thrown when the command is invalid.</exception>
        public CommandType GetCommandType()
        {
            if (CommandMap.TryGetValue(Command.ToString().ToLower(), out var commandType))
            {
                return commandType;
            }
            throw new ArgumentException($"Invalid command: {Command}");
        }

        /// <summary>
        /// Validates the CLI arguments to ensure required options are provided for specific commands.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Repo))
            {
                throw new ArgumentException("The 'repo' option is required for all commands.");
            }

            if (string.IsNullOrEmpty(Tag))
            {
                throw new ArgumentException("The 'tag' option is required for all commands.");
            }

            if (Command != CommandType.download && string.IsNullOrEmpty(Branch))
            {
                throw new ArgumentException("The 'branch' option is required for commands other than 'download'.");
            }

            if (string.IsNullOrEmpty(AssetPath) || !Path.IsPathRooted(AssetPath))
            {
                throw new ArgumentException("The 'path' option is required for all commands and must be an absolute path.");
            }
        }
    }
}
