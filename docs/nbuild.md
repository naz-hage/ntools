# Nbuild (`nb.exe`)

`Nbuild` (`nb.exe`) is a powerful command-line utility for .NET developers. It wraps the [.NET SDK](https://dotnet.microsoft.com/download) to simplify building solutions, running custom targets, and managing your development toolchain.

> **⚠️ BREAKING CHANGE (v1.76+):** The `nb install --name` command now searches **only for `apps.json` files** instead of all JSON files in a directory. If you have multiple JSON files with application definitions, consolidate them into a single `apps.json` file. See [Install by name](#install-by-name-from-current-directory-and-default-location) for details.

**Key Features:**
- Build and run custom targets with a single command
- Install, uninstall, and list development tools from a manifest file or by name/version from `apps.json` in current directory and `C:\program files\nbuild`
- Download tools and assets for your environment
- Integrate with Git for tagging, branching, and release automation
- Automate GitHub releases and asset downloads
- **Global options** (`--dry-run`, `--verbose`) available for all commands

## Why Use nb.exe?

While you could run `dotnet build` or `dotnet msbuild` directly, `nb.exe` provides practical benefits for .NET development workflows:

- **Simplified commands**: Instead of complex MSBuild parameters, use simple commands like `nb solution` or `nb test`
- **Environment awareness**: Automatically finds dotnet.exe, manages versions, and handles dependencies
- **Git integration**: Seamlessly works with git tags for versioning and release management
- **Cross-project consistency**: Standardized build processes that work the same way across different projects
- **DevOps automation**: Streamlined workflows for testing, packaging, and deployment

For example, `nb solution` doesn't just run `dotnet build`—it ensures dependencies are restored, applies proper versioning from git tags, and uses consistent build configurations.

> **Note:** `nb.exe` expects the [nbuild.targets](#nbuildtargets) file to be present in your solution folder for build-related commands.

## Prerequisites

- **.NET SDK**: The .NET SDK must be installed and available in your PATH. If not found, `nb.exe` will display an error message with installation instructions.
- **Git**: Git for Windows is required for Git-related operations.

## Usage

```cmd
Description:
  Nbuild - Build and DevOps Utility

Usage:
  nb [command] [options] [[--] <additional arguments>...]

Options:
  --dry-run       Perform a dry run: show actions but do not perform side effects
  --verbose       Verbose output
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  install                Install tools and applications specified in the manifest file or by name/version from apps.json in current directory and C:\program files\nbuild.
  uninstall              Uninstall tools and applications specified in the manifest file.
  list                   Display a formatted table of all tools and their versions.
                         Use this command to audit, compare, or document the state of your development environment.
  download               Download tools and applications specified in the manifest file.
  path                   Display each segment of the effective PATH environment variable on a separate line, with duplicates removed. Shows the complete PATH that processes actually use (Machine + User PATH combined).
  git_info               Displays the current git information for the local repository, including branch, and latest tag.
  git_settag             Sets a git tag in the local repository.
  git_autotag            Automatically sets the next git tag based on build type.
  git_push_autotag       Sets the next git tag based on build type and pushes to remote.
  git_branch             Displays the current git branch in the local repository.
  git_clone              Clones a Git repository to a specified path.
  git_deletetag          Deletes a git tag from the local repository.
  release_create         Creates a GitHub release.
  pre_release_create     Creates a GitHub pre-release.
  release_download       Downloads a specific asset from a GitHub release.
  list_release           Lists the latest releases for the specified repository.
  targets                Displays all available build targets for the current solution or project.

Additional Arguments:
  Arguments passed to the application that is being run.
```

## Dry-run contract

When `--dry-run` is supplied to `nb.exe` the CLI will not perform any state-changing
operations. The intent of `--dry-run` is to provide a safe, predictable preview of
what the CLI would do without modifying remote services, local files, system
configuration, or registry state.

Key points:
- `--dry-run` must never upload files, create or modify GitHub releases, write to
  Program Files, change PATH, edit the registry, or delete files.
- For destructive commands (for example `release_create`, `pre_release_create`,
  `install`, `uninstall`, `upload`) the command will short-circuit and print a
  concise action summary prefixed with `DRY-RUN:` (for example: `DRY-RUN: would
  upload asset X to release Y`).
- For read-only commands (for example `list_release`) the default Behavior is to
  avoid network access in dry-run and print a short simulated message. If a
  project requires read-only network access during dry-run, it should be made
  explicit (for example `--dry-run=fetch`) in a follow-up PBI.

---

## nbuild.targets {#nbuildtargets}
See [`nbuild.targets`](https://github.com/naz-hage/ntools/blob/main/Nbuild/resources/nbuild.targets) for more information and checkout other targets in [`Nbuild/resources`](https://github.com/naz-hage/ntools/blob/main/Nbuild/resources).
                    
### common.targets {#commontargets}
- The `common.targets` file includes all the defaults targets needed to build, test and deploy a solution.  The `common.targets` file is located in the `$(ProgramFiles)\Nbuild` folder.  The `nbuild.targets` file in the solution folder imports the `common.targets` file

Below is a list of common targets defined in the `common.targets` file:

| **Target Name** | **Description** |
| --- | --- |
| PROPERTIES          | Common properties that will be used by all targets |
| CLEAN               | Clean up the project and artifacts folder |
| INSTALL_DEP         | Install dependencies |
| TELEMETRY_OPT_OUT   | Opt out of the DOTNET_CLI_TELEMETRY_OPTOUT - move to common |
| STAGE             | Create a stage package for testing |
| PROD          | Create a production package for release |
| STAGE_DEPLOY      | Create a stage package and deploy for testing |
| PROD_DEPLOY   | Create a production package and deploy for release |
| SOLUTION            | Build the solution Release configuration  using dotnet build |
| SOLUTION_MSBUILD    | Build the solution Release configuration  using MSBuild |
| PACKAGE             | Create a package for the solution default is a zip file of all artifacts |
| COPY_ARTIFACTS      | Save the artifacts to the artifacts folder |
| DEPLOY              | Deploy the package. default is to extract artifacts into DeploymentProperty folder |
| TEST                | Run all tests using dotnet test in Release mode |
| TEST_DEBUG          | Run all tests using dotnet test in Debug mode |
| IS_ADMIN            | Check if current process is running in admin mode AdminCheckExitCode property is set |
| SingleProject       | Example how to build a single project |
| HandleError         | Error handling placeholder |

---

## Examples

Below are practical examples for using `nb.exe`. These examples assume you are running in a PowerShell terminal.

### 1. Install Applications

#### Install from JSON file (optional):
```cmd
nb.exe install --json "C:\Program Files\tools.json"
```
Installs applications specified in the manifest file. The `--json` parameter is optional. If not specified, the command defaults to searching in `C:\program files\nbuild`. (Requires admin privileges.)

#### Install by name from current directory and default location:
```cmd
nb.exe install --name "MyApp"
nb.exe install --name "MyApp" --appversion "1.2.3"
```
Searches for `apps.json` in both the current directory and `C:\program files\nbuild`, then installs the application matching the specified name. The `--appversion` parameter is optional and overrides the version specified in the JSON file.

**BREAKING CHANGE (v1.76+):** This command now searches ONLY for `apps.json` files, not all JSON files. You must consolidate your application definitions into a single `apps.json` file in the target directory or move it to `C:\program files\nbuild\apps.json`.

**Search order:** Current directory is searched first for `apps.json`, then `C:\program files\nbuild\apps.json`. If an app is found in the current directory, it takes precedence over the same app in the default location.

**Note:** If you specify both `--json` and `--name`, the command is allowed, but `--json` takes precedence and a warning is emitted. The `--name` method provides a more convenient way to install applications without needing to know the exact path to the JSON configuration file.

#### Dry-run mode for install:
```cmd
nb.exe install --name "MyApp" --dry-run
nb.exe install --name "MyApp" --appversion "1.2.3" --dry-run
```

**Behavior in dry-run mode:**
- Searches for `apps.json` in both the current directory and `C:\program files\nbuild` (search order: current directory first)
- If app is found: displays `DRY-RUN: would install app 'MyApp'` in yellow and lists version details
- If app is not found: displays `No apps found matching 'MyApp'` in red, lists the search directories (current directory and `C:\program files\nbuild`), and lists available applications found in those `apps.json` files
- Dry-run always returns exit code 0 (success), even when app is not found, as it is a preview/simulation mode
- Always succeeds (exit code 0) because dry-run is a preview, not actual installation
- No files are downloaded, installed, or modified
- Output uses color coding: yellow for dry-run messages, red for not-found messages

### 2. Uninstall Applications
```cmd
nb.exe uninstall --json "C:\Program Files\example-tool.json"
```
Uninstalls applications as specified in the manifest file. (Requires admin privileges.)

### 3. List Installed Applications
```cmd
nb.exe list
nb.exe list --json "C:\Program Files\NBuild\ntools.json"
```
Lists all applications specified in the provided JSON file. If no `--json` option is specified, the default file is used.

### 4. Download Applications
```cmd
nb.exe download --json "C:\Program Files\NBuild\ntools.json"
```
Downloads tools and applications specified in the manifest file.

### 5. Error Handling for JSON Manifest Files

The `list`, `install`, `uninstall`, and `download` commands require valid JSON manifest files. The following errors may occur:

#### File Not Found
```
Error: JSON file not found: 'C:\invalid\path\apps.json'. Please provide a valid path to the apps.json file.
Exit code: -1
```

**Resolution**: Verify the file path is correct. Common locations:
- Current directory: `.\apps.json`
- Program Files: `C:\Program Files\nbuild\apps.json`
- Relative path: `.\dev-setup\apps.json`

#### Invalid JSON Format
```
Error: Invalid JSON format: '.' is an invalid start of a value. Please check the JSON file for proper escaping of backslashes and quotes.
Exit code: -1
```

**Resolution**: Validate your JSON file:
- Use a JSON validator tool (e.g., jsonlint.com)
- Ensure backslashes in Windows paths are escaped: `C:\\Program Files\\...`
- Ensure quotes in JSON strings are properly escaped: `\"text\"`

#### Unsupported Version
```
Error: Json Version 1.0.0 is not supported. Please use version 1.2.0
Exit code: -1
```

**Resolution**: Update your manifest file to use the correct version in the `"version"` field.

### 6. Display Path Segments
```cmd
nb.exe path
```
Displays each segment of the effective PATH environment variable on a separate line, with duplicates removed. Shows the complete PATH that processes actually use (Machine + User PATH combined). Use `--verbose` for additional output.

### 7. Display Git Information
```cmd
nb.exe git_info
```
Displays the current git branch and latest tag information for the local repository.

### 8. Set a Specific Git Tag
```cmd
nb.exe git_settag --tag 1.24.33
```
Sets the specified git tag in the local repository.

### 9. Automatically Set the Next Git Tag
```cmd
nb.exe git_autotag --buildtype STAGE
```
Automatically sets the next git tag based on the specified build type (`STAGE` or `PROD`).

### 10. Push the Next Git Tag to Remote
```cmd
nb.exe git_push_autotag --buildtype PROD
```
Sets the next git tag based on build type and pushes it to the remote repository.

### 11. Display the Current Git Branch
```cmd
nb.exe git_branch
```
Displays the current git branch in the local repository.

### 12. Clone a Git Repository
```cmd
nb.exe git_clone --url https://github.com/example/repo --path C:\Projects
```
Clones the specified git repository into the specified path. Use `--verbose` for detailed output.

### 13. Delete a Specific Tag
```cmd
nb.exe git_deletetag --tag 1.24.33
```
Deletes the specified git tag from the local repository.

### 14. Creating a Release
```cmd
nb.exe release_create --repo userName/my-repo --tag 1.24.33 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub release for the specified repository, tag, branch, and asset file.

### 15. Creating a Pre-Release
```cmd
nb.exe pre_release_create --repo userName/my-repo --tag 1.24.33 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub pre-release for the specified repository, tag, branch, and asset file.

### 16. Downloading an Asset
```cmd
nb.exe release_download --repo userName/my-repo --tag 1.24.33 --path C:\Downloads
```
Downloads an asset from the specified release to the given path.

### 17. Creating a Release with Full GitHub URL
```cmd
nb.exe release_create --repo https://github.com/userName/my-repo --tag 1.24.33 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub release using the full GitHub repository URL.

### 18. Downloading an Asset with Full GitHub URL
```cmd
nb.exe release_download --repo https://github.com/userName/my-repo --tag 1.24.33 --path C:\Downloads
```
Downloads an asset using the full GitHub repository URL.

### 19. List Latest Releases
```cmd
nb.exe list_release --repo https://github.com/userName/my-repo
```
Lists the latest 3 releases and the newest pre-release (if newer than the latest release). Use `--verbose` for detailed output.

### 20. List Build Targets
```cmd
nb.exe targets
```
Lists all available build targets for the current solution or project.

### 21. Run Any Listed Target
```cmd
nb.exe core
```
Runs the target named `core` if it is listed by `nb targets`.

---

