using GitHubRelease;
using NbuildTasks;

namespace Nbuild;

/// <summary>
/// Represents the command-line interface (CLI) options for the Nbuild application.
/// </summary>
public class Cli
{
    /// <summary>
    /// Enum representing the possible command types.
    /// </summary>
    public enum CommandType
    {
        list,
        install,
        uninstall,
        download,
        targets,
        path,
        git_info,
        git_settag,
        git_autotag,
        git_push_autotag,
        git_branch,
        git_clone,
        git_deletetag,
        release_create,
        pre_release_create,
        release_download,
        list_release,
    }

    /// <summary>
    /// Gets or sets the command to execute.
    /// Possible values: targets, install, uninstall, download, list, path, git_info, git_settag, git_autotag, git_push_autotag, git_branch, git_clone, git_deletetag.
    /// </summary>
    public CommandType Command { get; set; }

    /// <summary>
    /// Gets or sets the JSON file that holds the list of apps.
    /// Only valid for the install, download, and list commands.
    /// </summary>
    public string? Json { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the console output verbose level.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets the Git repository URL.
    /// </summary>
    public string? Url { get; set; }

    public string? Tag { get; set; }

    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the build type used for git_autotag and git_push_autotag commands.
    /// Possible values: STAGE, PROD.
    /// </summary>
    public string? BuildType { get; internal set; }

    /// <summary>
    /// Gets or sets the repository name in the format userName/repoName.
    /// </summary>
    public string? Repo { get; set; }

    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the asset file name for `create` command.
    /// </summary>
    public string? AssetFileName { get; set; }

    ///// <summary>
    ///// Gets or sets the asset path.
    ///// </summary>
    //[OptionalArgument("", "path", "Specifies the asset path. Must be an absolute path.")]
    //public string? AssetPath { get; set; }


    private static readonly Dictionary<string, CommandType> CommandMap = new()
        {
            { "targets", CommandType.targets },
            { "install", CommandType.install },
            { "uninstall", CommandType.uninstall },
            { "download", CommandType.download },
            { "list", CommandType.list },
            { "path", CommandType.path },
            { "git_info", CommandType.git_info },
            { "git_settag", CommandType.git_settag },
            { "git_autotag", CommandType.git_autotag },
            { "git_push_autotag", CommandType.git_push_autotag },
            { "git_branch", CommandType.git_branch },
            { "git_clone", CommandType.git_clone },
            { "git_deletetag", CommandType.git_deletetag },
            { "release_create", CommandType.release_create },
            { "pre_release_create", CommandType.pre_release_create },
            { "release_download", CommandType.release_download },
            { "list_release", CommandType.list_release }
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
        switch (Command)
        {
            case CommandType.install:
            case CommandType.uninstall:
            case CommandType.download:
            case CommandType.list:
                if (string.IsNullOrEmpty(Json))
                {
                    throw new ArgumentException("The 'json' option is required for the 'install', 'uninstall', 'download', and 'list' commands.");
                }
                break;
            case CommandType.git_settag:
            case CommandType.git_deletetag:
                if (string.IsNullOrEmpty(Tag))
                {
                    throw new ArgumentException("The 'tag' option is required for the 'git_settag' and 'git_deletetag' commands.");
                }
                break;
            case CommandType.git_autotag:
            case CommandType.git_push_autotag:
                if (string.IsNullOrEmpty(BuildType))
                {
                    throw new ArgumentException("The 'buildtype' option is required for the 'git_autotag' and 'git_push_autotag' commands.");
                }
                break;
            case CommandType.git_clone:
                if (string.IsNullOrEmpty(Url))
                {
                    throw new ArgumentException("The 'url' option is required for the 'clone' command.");
                }
                break;

            case CommandType.pre_release_create:
            case CommandType.release_create:
            case CommandType.release_download:
            case CommandType.list_release:
                ValidateReleaseOptions();
                break;

            default:
                // For all other commands, no additional validation is required.
                break;
        }
    }

    /// <summary>
    /// Validates the CLI release arguments to ensure required options are provided for specific commands.
    /// </summary>
    public void ValidateReleaseOptions()
    {
        if (string.IsNullOrEmpty(Repo))
        {
            throw new ArgumentException("The 'repo' option is required for release_create, pre_release_create and release_download commands and must be in the format userName/repoName.");
        }

        if (Command != CommandType.list_release)
        {
            if (string.IsNullOrEmpty(Tag))
            {
                throw new ArgumentException("The 'tag' option is required for release_create, pre_release_create and release_download commands.");
            }

            if (IsValidTag(Tag) == false)
            {
                throw new ArgumentException($"The 'tag' option '{Tag}' is not a valid tag format.");
            }

            if (Command == CommandType.release_create && string.IsNullOrEmpty(AssetFileName))
            {
                throw new ArgumentException("The 'file' option is required for the release_create command.");
            }

            if (Command == CommandType.pre_release_create && string.IsNullOrEmpty(AssetFileName))
            {
                throw new ArgumentException("The 'file' option is required for the pre_release_create command.");
            }

            if (Command != CommandType.release_download && string.IsNullOrEmpty(Branch))
            {
                throw new ArgumentException("The 'branch' option is required for release_create, pre_release_create commands.");
            }

            if (Command == CommandType.release_download && string.IsNullOrEmpty(Path))
            {
                // Default to the current directory if Path is not provided
                Path = Directory.GetCurrentDirectory();
            }

            if (Command == CommandType.release_download && !System.IO.Path.IsPathRooted(Path))
            {
                throw new ArgumentException("The 'path' option is required for the release_download command and must be an absolute path.");
            }
        }

        // Use the new ValidateRepo method
        ValidateRepo().GetAwaiter().GetResult();
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

        bool verbose = Verbose;
        if (verbose) Console.WriteLine($"[VERBOSE] ValidateRepo: Initial Repo argument: {Repo}");

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
            if (verbose) Console.WriteLine($"[VERBOSE] ValidateRepo: Repo converted from URL: {Repo}");
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

            Repo = $"{owner}/{Repo}";
            if (verbose) Console.WriteLine($"[VERBOSE] ValidateRepo: Repo resolved using OWNER: {Repo}");
        }
        else if (repoParts.Length != 2 || string.IsNullOrEmpty(repoParts[0]) || string.IsNullOrEmpty(repoParts[1]))
        {
            throw new ArgumentException("The 'repo' option must be in the format userName/repoName.");
        }

        if (verbose) Console.WriteLine($"[VERBOSE] ValidateRepo: Final resolved Repo: {Repo}");

        // Validate that the repository exists
        await ValidateRepositoryExists();
    }

    /// <summary>
    /// Validates that the GitHub repository exists and is accessible via the GitHub API.
    /// </summary>
    /// <remarks>
    /// If a GitHub token is available, it is used for authentication. If the token is missing and the repository is private,
    /// the method will throw an exception. For public repositories, the check may succeed without a token, but access to private
    /// repositories always requires authentication. Throws an exception if the repository does not exist or is inaccessible.
    /// </remarks>
    public async Task ValidateRepositoryExists()
    {
        using var httpClient = new HttpClient();
        var apiUrl = $"https://api.github.com/repos/{Repo}";
        Console.WriteLine($"Validating repository via API: {apiUrl}");

        // Add required headers for GitHub API
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubRelease/1.0");
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        // Add authentication if a GitHub token is available
        var token = Credentials.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Using GitHub token for authentication.");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            Console.WriteLine("No GitHub token found. Only public repository validation is possible.");
        }

        try
        {
            var response = await httpClient.GetAsync(apiUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new ArgumentException($"The repository '{Repo}' does not exist or is not accessible. HTTP Status: {response.StatusCode}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new ArgumentException($"Access denied to repository '{Repo}'. A valid GitHub token is required for private repositories.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentException($"Failed to validate the repository '{Repo}'. HTTP Status: {response.StatusCode}");
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
