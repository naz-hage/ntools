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
        create_release,
        create_pre_release,
        download_release,
    }

    /// <summary>
    /// Gets or sets the command to execute.
    /// Possible values: targets, install, uninstall, download, list, path, git_info, git_settag, git_autotag, git_push_autotag, git_branch, git_clone, git_deletetag.
    /// </summary>
    [RequiredArgument(0, "command", "Specifies the command to execute.\n" +
        "\t list \t\t\t -> Lists apps specified in the -json option.\n" +
        "\t install \t\t -> Downloads and installs apps specified in the -json option (require admin privileges to run).\n" +
        "\t uninstall \t\t -> Uninstalls apps specified in the -json option (require admin privileges to run).\n" +
        "\t download \t\t -> Downloads apps specified in the -json option (require admin privileges to run).\n" +
        "\t targets \t\t -> Lists available targets and saves them in the targets.md file.\n" +
        "\t path \t\t\t -> Displays environment path in local machine.\n" +
        "\t git_info \t\t -> Displays the current git information in the local repository.\n" +
        "\t git_settag \t\t -> Set specified tag with -tag option\n" +
        "\t git_autotag \t\t -> Set next tag based on the build type: STAGE | PROD\n" +
        "\t git_push_autotag \t -> Set next tag based on the build type and push to remote repo\n" +
        "\t git_branch \t\t -> Displays the current git branch in the local repository\n" +
        "\t git_clone \t\t -> Clone specified Git repo in the -url option\n" +
        "\t git_deletetag \t\t -> Delete specified tag in -tag option\n" +
        "\t create_release \t -> Create a release. Requires repo, tag, branch and file.\n" +
        "\t create_pre_release \t -> Create a pre-release. Requires repo, tag, branch and file.\n" +
        "\t download_release \t -> Download an asset. Requires repo, tag, and path (optional)\n" +
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

    [OptionalArgument("", "path", "Specifies the path used for git_clone command. If not specified, the current directory will be used.")]
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
        "\t https://github.com/userName/repoName (Full URL to the repository on GitHub). This is applicable to all commands.")]
    public string? Repo { get; set; }

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

            case CommandType.create_pre_release:
            case CommandType.create_release:
            case CommandType.download_release:
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

        if (Command == CommandType.create_release && string.IsNullOrEmpty(AssetFileName))
        {
            throw new ArgumentException("The 'file' option is required for the 'create' command.");

        }

        if (Command == CommandType.create_pre_release && string.IsNullOrEmpty(AssetFileName))
        {
            throw new ArgumentException("The 'file' option is required for the 'pre_release' command.");

        }

        if (Command != CommandType.download && string.IsNullOrEmpty(Branch))
        {
            throw new ArgumentException("The 'branch' option is required for commands other than 'download'.");
        }

        if (Command == CommandType.download && string.IsNullOrEmpty(Path))
        {
            // Default to the current directory if Path is not provided
            Path = Directory.GetCurrentDirectory();
        }

        if (Command == CommandType.download && !System.IO.Path.IsPathRooted(Path))
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
