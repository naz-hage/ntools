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

---

## Quick Start

1. **List installed tools:**
   ```cmd
   nb.exe list
   ```
2. **Install tools from a manifest:**
   ```cmd
   nb.exe install --json "C:\Path\To\tools.json"
   ```
3. **Build a target:**
   ```cmd
   nb.exe [TargetName]
   # Example:
   nb.exe stage
   ```
4. **See all available commands:**
   ```cmd
   nb.exe --help
   ```

---


## Usage

```cmd
Nbuild - Build and DevOps Utility

Usage:
  nb [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  install             Install tools from a JSON manifest
  uninstall           Uninstall tools from a JSON manifest
  list                List all tools and their versions
  download            Download tools from a JSON manifest
  path                Display path segments
  git_info            Display current git info
  git_settag          Set a git tag
  git_autotag         Set the next git tag (STAGE or PROD)
  git_push_autotag    Set and push the next git tag
  git_branch          Display the current git branch
  git_clone           Clone a git repository
  git_deletetag       Delete a git tag
  release_create      Create a GitHub release
  pre_release_create  Create a GitHub pre-release
  release_download    Download a release asset from GitHub
  list_release        List latest releases for a repository
  targets             Display build targets
```

> **Tip:** If the `--json` option is not specified, the default manifest file `C:\Program Files\NBuild\ntools.json` is used.

---

## nbuild.targets
See [nbuild.targets](../setup.md#nbuildtargets) for more information on how to create a `nbuild.targets` file.
                    
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

## Command Examples

Below are practical examples for using `nb.exe`. These examples assume you are running in a PowerShell terminal.


### 1. List Installed Applications
```cmd
nb.exe list
nb.exe list --json "C:\Program Files\NBuild\ntools.json"
```
Lists all applications specified in the provided JSON file. If no `--json` option is specified, the default file is used.

## 1. List Installed Applications
```cmd
nb.exe list --json "C:\Program Files\NBuild\ntools.json"
```
Lists all applications specified in the provided JSON file. If no `--json` option is specified, the default file `C:\Program Files\NBuild\ntools.json` is used.


### 2. Download Applications
```cmd
nb.exe download --json "C:\Program Files\NBuild\ntools.json"
```
Downloads tools and applications specified in the manifest file.

### 3. Install and Uninstall Applications
```cmd
nb.exe install --json "C:\Program Files\tools.json"
nb.exe uninstall --json "C:\Program Files\example-tool.json"
```
Installs or uninstalls applications as specified in the manifest file. (Requires admin privileges.)

### 4. Display Git Information
```cmd
nb.exe git_info
```
Displays the current Git branch and tag information for the local repository.


### 5. Run a Build Target
```cmd
nb.exe stage --verbose true
```
Runs the `stage` target defined in the nbuild.targets file with verbose output enabled.


### 6. List nbuild targets
```cmd
nb.exe targets
```
Lists all available build targets defined in the nbuild.targets file.


### 7. Set a Specific Git Tag
```cmd
nb.exe git_settag --tag 1.0.0
```
Sets the specified Git tag (`1.0.0`) in the local repository.


### 8. Automatically Set the Next Git Tag
```cmd
nb.exe git_autotag --buildtype stage
```
Automatically generates and sets the next Git tag based on the specified build type (`stage` or `prod`).


### 9. Push the Next Git Tag to Remote
```cmd
nb.exe git_push_autotag --buildtype prod
```
Automatically generates the next Git tag based on the specified build type (`prod`) and pushes it to the remote repository.


### 10. Display the Current Git Branch
```cmd
nb.exe git_branch
```
Displays the current Git branch in the local repository.


### 11. Clone a Git Repository
```cmd
nb.exe git_clone --url https://github.com/example/repo --path C:\Projects
```
Clones the specified Git repository into the specified path.


### 12. Delete a Specific Tag
```cmd
nb.exe git_deletetag --tag 1.0.0
```
Deletes the specified Git tag.


### 13. Creating a Release
```cmd
nb.exe release_create --repo userName/my-repo --tag 1.0.0 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub release for the specified repository, tag, branch, and asset file.


### 14. Creating a Pre-Release
```cmd
nb.exe pre_release_create --repo userName/my-repo --tag 1.0.0 --branch main --file C:\Releases\1.0.0.zip
```
Creates a GitHub pre-release for the specified repository, tag, branch, and asset file.


### 15. Downloading an Asset
```cmd
nb.exe release_download --repo userName/my-repo --tag 1.0.0 --path C:\Downloads
```
Downloads an asset from the specified release to the given path.


### 16. Creating a Release with Full GitHub URL
```cmd
nb.exe release_create --repo https://github.com/userName/my-repo --tag 1.0.0 --branch main --file C:\Releases\1.0.0.zip
```


### 17. Downloading an Asset with Full GitHub URL
```cmd
nb.exe release_download --repo https://github.com/userName/my-repo --tag 1.0.0 --path C:\Downloads
```


### 18. List Latest Releases
```cmd
nb.exe list_release --repo https://github.com/userName/my-repo
```
Lists the latest 3 releases and the newest pre-release (if newer than the latest release).

---

## Troubleshooting

- **Admin Privileges:** Some commands (like install/uninstall) may require running your terminal as administrator.
- **Default Manifest:** If you do not specify `--json`, the default manifest is `C:\Program Files\NBuild\ntools.json`.
- **Path with Spaces:** Always wrap file paths with spaces in double quotes.
- **GitHub Authentication:** For release-related commands, ensure you have the correct permissions and authentication set up for your GitHub account.

---
