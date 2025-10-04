- [Usage](#usage)
- [nbuild.targets](#nbuildtargets)
- [common.targets](#commontargets)
- [Examples](#examples)
---


# Nbuild (`nb.exe`)

`Nbuild` (`nb.exe`) is a powerful command-line utility for .NET developers. It wraps [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild) to simplify building solutions, running custom targets, and managing your development toolchain.

**Key Features:**
- Build and run custom targets with a single command
- Install, uninstall, and list development tools from a manifest file
- Download tools and assets for your environment
- Integrate with Git for tagging, branching, and release automation
- Automate GitHub releases and asset downloads

> **Note:** `nb.exe` expects the [nbuild.targets](#nbuildtargets) file to be present in your solution folder for build-related commands.

## Usage

```cmd
Description:
  Nbuild - Build and DevOps Utility

Usage:
  nb [command] [options] [[--] <additional arguments>...]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  install                Install tools and applications specified in the manifest file.
  uninstall              Uninstall tools and applications specified in the manifest file.
  list                   Display a formatted table of all tools and their versions.
                         Use this command to audit, compare, or document the state of your development environment.
  download               Download tools and applications specified in the manifest file.
  path                   Display each segment of the effective PATH environment variable on a separate line, with duplicates removed. Shows the complete PATH that processes actually use (Machine + User PATH combined).

                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb path --verbose
  git_info               Displays the current git information for the local repository, including branch, and latest
                         tag.

                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb git_info --verbose
  git_settag             Sets a git tag in the local repository.

                         Required option:
                           --tag   The tag to set (e.g., 1.24.33)
                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb git_settag --tag 1.24.33 --verbose
  auto_tag, git_autotag  Automatically sets the next git tag based on build type.

                         Required option:
                           --buildtype   Build type (STAGE or PROD)
                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb git_autotag --buildtype STAGE --verbose
  git_push_autotag       Sets the next git tag based on build type and pushes to remote.

                         Required option:
                           --buildtype   Build type (STAGE or PROD)
                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb git_push_autotag --buildtype PROD --verbose
  git_branch             Displays the current git branch in the local repository.

                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb git_branch --verbose
  git_clone              Clones a Git repository to a specified path.

                         Required option:
                           --url   Git repository URL
                         Optional options:
                           --path      Path to clone into (default: current directory)
                           --verbose   Verbose output

                         Example:
                           nb git_clone --url https://github.com/user/repo --path ./repo --verbose
  git_deletetag          Deletes a git tag from the local repository.

                         Required option:
                           --tag   The tag to delete (e.g., 1.24.33)
                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb git_deletetag --tag 1.24.33 --verbose
  release_create         Creates a GitHub release.

                         Required options:
                           --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)
                           --tag    Tag to use for the release (e.g., 1.24.33)
                           --branch Branch name to release from (e.g., main)
                           --file   Asset file name (full path required)
                         Optional option:
                           --verbose   Verbose output

                         Examples:
                           nb release_create --repo user/repo --tag 1.24.33 --branch main --file C:\path\to\asset.zip --verbose
                           nb release_create --repo https://github.com/user/repo --tag 1.24.33 --branch main --file ./asset.zip --verbose
  pre_release_create     Creates a GitHub pre-release.

                         Required options:
                           --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)
                           --tag    Tag to use for the pre-release (e.g., 1.24.33)
                           --branch Branch name to release from (e.g., main)
                           --file   Asset file name (full path required)
                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb pre_release_create --repo user/repo --tag 1.24.33 --branch main --file C:\path\to\asset.zip --verbose
  release_download       Downloads a specific asset from a GitHub release.

                         Required options:
                           --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)
                           --tag    Tag to use for the release (e.g., 1.24.33)
                         Optional option:
                           --path   Path to download asset to (default: current directory)
                           --verbose   Verbose output

                         Example:
                           nb release_download --repo user/repo --tag 1.24.33 --path C:\downloads --verbose
  list_release           Lists the latest 3 releases for the specified repository, and the latest pre-release if newer.

                         Required option:
                           --repo   Git repository (formats: repoName, userName/repoName, or full GitHub URL)
                         Optional option:
                           --verbose   Verbose output

                         Example:
                           nb list_release --repo user/repo --verbose
  targets                Displays all available build targets for the current solution or project.

                         Optional option:
                           --verbose   Verbose output

                         You can run any listed target directly using nb.exe.
                         Example: If 'core' is listed, you can run:
                           nb core

                         To list all targets:
                           nb targets --verbose
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
> **Tip:** If the `--json` option is not specified, the default manifest file `C:\Program Files\NBuild\ntools.json` is used.

---

## nbuild.targets
See [`nbuild.targets`](https://github.com/naz-hage/ntools/blob/main/Nbuild/resources/nbuild.targets) for more information and checkout other targets in [`Nbuild/resources`](https://github.com/naz-hage/ntools/blob/main/Nbuild/resources).
                    
### common.targets
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
```cmd
nb.exe install --json "C:\Program Files\tools.json"
```
Installs applications specified in the manifest file. (Requires admin privileges.)

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

### 5. Display Path Segments
```cmd
nb.exe path
nb.exe path --verbose
```
Displays each segment of the effective PATH environment variable on a separate line, with duplicates removed. Shows the complete PATH that processes actually use (Machine + User PATH combined). Use `--verbose` for additional output.

### 6. Display Git Information
```cmd
nb.exe git_info
```
Displays the current git branch and latest tag information for the local repository.

### 7. Set a Specific Git Tag
```cmd
nb.exe git_settag --tag 1.24.33
```
Sets the specified git tag in the local repository.

### 8. Automatically Set the Next Git Tag
```cmd
nb.exe git_autotag --buildtype STAGE
```
Automatically sets the next git tag based on the specified build type (`STAGE` or `PROD`).

### 9. Push the Next Git Tag to Remote
```cmd
nb.exe git_push_autotag --buildtype PROD
```
Sets the next git tag based on build type and pushes it to the remote repository.

### 10. Display the Current Git Branch
```cmd
nb.exe git_branch
```
Displays the current git branch in the local repository.

### 11. Clone a Git Repository
```cmd
nb.exe git_clone --url https://github.com/example/repo --path C:\Projects --verbose
```
Clones the specified git repository into the specified path. Use `--verbose` for detailed output.

### 12. Delete a Specific Tag
```cmd
nb.exe git_deletetag --tag 1.24.33
```
Deletes the specified git tag from the local repository.

### 13. Creating a Release
```cmd
nb.exe release_create --repo userName/my-repo --tag 1.24.33 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub release for the specified repository, tag, branch, and asset file.

### 14. Creating a Pre-Release
```cmd
nb.exe pre_release_create --repo userName/my-repo --tag 1.24.33 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub pre-release for the specified repository, tag, branch, and asset file.

### 15. Downloading an Asset
```cmd
nb.exe release_download --repo userName/my-repo --tag 1.24.33 --path C:\Downloads
```
Downloads an asset from the specified release to the given path.

### 16. Creating a Release with Full GitHub URL
```cmd
nb.exe release_create --repo https://github.com/userName/my-repo --tag 1.24.33 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub release using the full GitHub repository URL.

### 17. Downloading an Asset with Full GitHub URL
```cmd
nb.exe release_download --repo https://github.com/userName/my-repo --tag 1.24.33 --path C:\Downloads
```
Downloads an asset using the full GitHub repository URL.

### 18. List Latest Releases
```cmd
nb.exe list_release --repo https://github.com/userName/my-repo --verbose
```
Lists the latest 3 releases and the newest pre-release (if newer than the latest release). Use `--verbose` for detailed output.

### 19. List Build Targets
```cmd
nb.exe targets
```
Lists all available build targets for the current solution or project.

### 20. Run Any Listed Target
```cmd
nb.exe core
```
Runs the target named `core` if it is listed by `nb targets`.

---
