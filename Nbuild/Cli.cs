using CommandLine.Attributes;
using GitHubRelease;
using NbuildTasks;
using System.IO;

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
    [RequiredArgument(0, "command", "Specifies the command to execute.\n" +
        "\t list \t\t\t -> Lists apps specified in the -json option.\n" +
        "\t install \t\t -> Downloads and installs apps specified in the -json option (requires admin privileges).\n" +
        "\t uninstall \t\t -> Uninstalls apps specified in the -json option (requires admin privileges).\n" +
        "\t download \t\t -> Downloads tools or apps listed in the -json option (requires admin privileges).\n" +
        "\t targets \t\t -> Lists available build targets and saves them in the targets.md file.\n" +
        "\t path \t\t\t -> Displays the environment PATH variable for the local machine.\n" +
        "\t git_info \t\t -> Displays the current git information for the local repository.\n" +
        "\t git_settag \t\t -> Sets the specified tag using the -tag option.\n" +
        "\t git_autotag \t\t -> Sets the next tag based on the build type: STAGE or PROD.\n" +
        "\t git_push_autotag \t -> Sets the next tag based on the build type and pushes to the remote repository.\n" +
        "\t git_branch \t\t -> Displays the current git branch in the local repository.\n" +
        "\t git_clone \t\t -> Clones the specified Git repository using the -url option.\n" +
        "\t git_deletetag \t\t -> Deletes the specified tag using the -tag option.\n" +
        "\t release_create \t -> Creates a GitHub release. Requires -repo, -tag, -branch, and -file options.\n" +
        "\t pre_release_create \t -> Creates a GitHub pre-release. Requires -repo, -tag, -branch, and -file options.\n" +
        "\t release_download \t -> Downloads a specific asset from a GitHub release. Requires -repo, -tag, and -path (optional, defaults to current directory).\n" +
        "\t list_release \t\t -> Lists latest 3 releases for the specified repository (and latest pre-release if newer). Requires -repo.\n" +
        "\t ----\n" +
        "\t  The nbuild.exe can also execute targets defined in an nbuild.targets file if one " +
        "\t exists in the current folder.\n" +
        "\t To execute a target defined in nbuild.targets, simply use its name as the command.\n" +
        "\t For example, if nbuild.targets defines a target named 'build', you can run it" +
        "\t  with: `nb.exe build`\n")]
    public CommandType Command { get; set; }

    /// <summary>
    /// Gets or sets the JSON file that holds the list of apps.
    /// Only valid for the install, download, and list commands.
    /// </summary>
    [OptionalArgument("$(ProgramFiles)\\nbuild\\ntools.json", "json", "Specifies the JSON file that holds the list of apps. Only valid for the install, download, and list commands.\n" +
        "\t - By default, the -json option points to the ntools deployment folder: $(ProgramFiles)\\build\\ntools.json.\n" +
        "\t Sample JSON file: https://github.com/naz-hage/ntools/blob/main/dev-setup/ntools.json\n" +
        "\t ")]
    public string? Json { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the console output verbose level.
    /// </summary>
    [OptionalArgument(false, "v", "Optional parameter which sets the console output verbose level\n" +
        "\t ----\n" +
        "\t - if no command line options are specified with the -v option , i.e.: 'Nb.exe stage -v true` \n" +
        "\t   `Nb` will run an MSbuild target `stage` defined in a `nbuild.targets` file which present in the solution folder.\n" +
        "\t   Run `Nb.exe Targets` to list the available targets. \n" +
        "\t -v Possible Values:")]
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets the Git repository URL.
    /// </summary>
    [OptionalArgument("", "url", "Specifies the Git repository URL.")]
    public string? Url { get; set; }

    [OptionalArgument("", "tag", "Specifies the tag used for git_settag and git_deletetag commands.")]
    public string? Tag { get; set; }

    [OptionalArgument("", "path", "Specifies the path used for git_clone, pre_release_create and release_create commands. If not specified, the current directory will be used.\n" +
        "\t for pre_release_create and release_create commands, it must be an absolute path")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the build type used for git_autotag and git_push_autotag commands.
    /// Possible values: STAGE, PROD.
    /// </summary>
    [OptionalArgument("", "buildtype", "Specifies the build type used for git_autotag and git_push_autotag commands. Possible values: STAGE, PROD.")]
    public string? BuildType { get; internal set; }

    /// <summary>
    /// Gets or sets the repository name in the format userName/repoName.
    /// </summary>
    [OptionalArgument("", "repo", "Specifies the Git repository in the format any of the following formats: \n" +
        "\t repoName  (UserName is declared the `OWNER` environment variable) \n" +
        "\t userName/repoName\n" +
        "\t https://github.com/userName/repoName (Full URL to the repository on GitHub). This is applicable to release_create, pre_release_create and release_download commands.")]
    public string? Repo { get; set; }

    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    [OptionalArgument("main", "branch", "Specifies the branch name. Applicable for release_create, pre_release_create commands")]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the asset file name for `create` command.
    /// </summary>
    [OptionalArgument("", "file", "Specifies the asset file name. Must include full path. Applicable for release_create, pre_release_create commands")]
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
