using CommandLine.Attributes;
using NbuildTasks;

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
            pre_release,
            download,
        }

        /// <summary>
        /// Gets or sets the command to execute.
        /// Possible values: create, download.
        /// </summary>
        [RequiredArgument(0, "command", "Specifies the command to execute.\n" +
            "\t create \t -> Create a release. Requires repo, tag, branch and file.\n" +
            "\t pre_release \t -> Create a pre-release. Requires repo, tag, branch and file.\n" +
            "\t download \t -> Download an asset. Requires repo, tag, and path (optional)\n" +
            "\t ----\n")]
        public CommandType Command { get; set; }

        /// <summary>
        /// Gets or sets the repository name in the format userName/repoName.
        /// </summary>
        [OptionalArgument("", "repo", "Specifies the Git repository in the format any of the following formats: \n" +
            "\t repoName  (UserName is declared the `OWNER` environment variable) \n"+
            "\t userName/repoName\n" +
            "\t https://github.com/userName/repoName (Full URL to the repository on GitHub). This is applicable to all commands.")]
        public string? Repo { get; set; }

        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        [OptionalArgument("", "tag", "Specifies the tag name. Applicable for all commands")]
        public string? Tag { get; set; }

        /// <summary>
        /// Gets or sets the branch name.
        /// </summary>
        [OptionalArgument("main", "branch", "Specifies the branch name. Applicable for create, pre_release commands")]
        public string? Branch { get; set; }

        /// <summary>
        /// Gets or sets the asset file name for `create` command.
        /// </summary>
        [OptionalArgument("", "file", "Specifies the asset file name. Must include full path. Applicable for create, pre_release commands")]
        public string? AssetFileName { get; set; }

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
                    { "pre_release", CommandType.pre_release },
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
                throw new ArgumentException("The 'repo' option is required for all commands and must be in the format userName/repoName.");
            }

            // Use the new ValidateRepo method
            ValidateRepo().GetAwaiter().GetResult();


            if (string.IsNullOrEmpty(Tag))
            {
                throw new ArgumentException("The 'tag' option is required for all commands.");
            }

            if (IsValidTag(Tag) == false)
            {
                throw new ArgumentException($"The 'tag' option '{Tag}' is not a valid tag format.");
            }

            if (Command == CommandType.create && string.IsNullOrEmpty(AssetFileName))
            {
                throw new ArgumentException("The 'file' option is required for the 'create' command.");

            }

            if (Command == CommandType.pre_release && string.IsNullOrEmpty(AssetFileName))
            {
                throw new ArgumentException("The 'file' option is required for the 'pre_release' command.");

            }

            if (Command != CommandType.download && string.IsNullOrEmpty(Branch))
            {
                throw new ArgumentException("The 'branch' option is required for commands other than 'download'.");
            }

            if (Command == CommandType.download && string.IsNullOrEmpty(AssetPath))
            {
                // Default to the current directory if AssetPath is not provided
                AssetPath = Directory.GetCurrentDirectory();
            }

            if (Command == CommandType.download && !Path.IsPathRooted(AssetPath))
            {
                throw new ArgumentException("The 'path' option is required for the download commands and must be an absolute path.");
            }


        }

        /// <summary>
        /// Validates the repository format and accessibility.
        /// </summary>
        /// <remarks>
        /// If only the repository name (repoName) is provided without a userName, the method checks for the 
        /// 'OWNER' environment variable. If 'OWNER' is set, it combines the OWNER value with the repoName 
        /// to form the full repository string in the format userName/repoName. If 'OWNER' is not set, 
        /// an exception is thrown.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the repository format is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the OWNER environment variable is required but not set.</exception>
        public async Task ValidateRepo()
        {
            // Validate the repo format
            // 1. userName/repoName
            // 2. repoName (OWNER environment variable must be set)
            // https://github.com/{repo} must be accessible
            // If the repo contains a slash, it must be in the format userName/repoName
            // otherwise, it is expected to be a repoName, in which case
            // the UserName is derived from the OWNER environment variable

            // Check if the input is a full URL
            if (Repo!.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the userName/repoName portion
                var uri = new Uri(Repo);
                if (uri.Host != "github.com")
                {
                    throw new ArgumentException("Only repositories hosted on github.com are supported.");
                }

                Repo = uri.AbsolutePath.Trim('/'); // Extracts "userName/repoName"
            }

            var repoParts = Repo!.Split('/');
            if (repoParts.Length == 1)
            {
                // If only the repoName is provided, ensure OWNER is set
                var owner = Environment.GetEnvironmentVariable("OWNER");
                if (string.IsNullOrEmpty(owner))
                {
                    throw new InvalidOperationException("The 'OWNER' environment variable is required when only the repository name is provided.");
                }

                // Combine OWNER and repoName to form userName/repoName
                Repo = $"{owner}/{Repo}";

            }
            else if (repoParts.Length != 2 || string.IsNullOrEmpty(repoParts[0]) || string.IsNullOrEmpty(repoParts[1]))
            {
                throw new ArgumentException("The 'repo' option must be in the format userName/repoName.");
            }

            // Validate that the repository exists
            await ValidateRepositoryExists();
        }

        public async Task ValidateRepositoryExists()
        {
            using var httpClient = new HttpClient();
            var apiUrl = $"https://api.github.com/repos/{Repo}";
            Console.WriteLine($"Validating repository via API: {apiUrl}");

            try
            {
                // Add authentication if a GitHub token is available
                var token = Credentials.GetToken();
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Using GitHub token for authentication.");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // Add required headers for GitHub API
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubRelease/1.0");
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                // Send a GET request to the GitHub API
                var response = await httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException($"The repository '{Repo}' does not exist or is not accessible. HTTP Status: {response.StatusCode}");
                }

                Console.WriteLine($"Repository '{Repo}' is valid and accessible.");
            }
            catch (HttpRequestException ex)
            {
                throw new ArgumentException($"Failed to validate the repository '{Repo}'. Error: {ex.Message}", ex);
            }
        }

        private bool IsValidTag(string tag)
        {
            GitWrapper git = new();
            return git.IsValidTag(tag);
        }
    }
}

