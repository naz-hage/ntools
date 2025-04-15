- [Usage](#usage)
- [nbuild.targets](#nbuildtargets)
- [common.targets](#commontargets)
- [Examples](../usage.md)
---

# Unified `nb.exe` Tool

The `nb.exe` tool now serves as a unified interface for:
- Build automation
- Git enhancements
- GitHub release management

### Command Structure
The tool supports the following top-level commands:
- `build`: Build-related commands (e.g., `list`, `install`, `uninstall`, etc.).
- `git`: Git-related commands (e.g., `tag`, `branch`, `clone`, etc.).
- `release`: GitHub release-related commands (e.g., `create`, `download`, `list`, etc.).

### Examples
Refer to the [Usage Guide](./usage.md) for detailed examples of each command.

### nbuild.targets
See [nbuild.targets](../setup.md#nbuildtargets) for more information on how to create a `nbuild.targets` file.
                    
### common.targets
- The `common.targets` file includes all the defaults targets needed to build, test and deploy a solution.  The `common.targets` file is located in the `$(ProgramFiles)\Nbuild` folder.  The `nbuild.targets` file in the solution folder imports the `common.targets` file

Below is list of common targets that are defined in the `common.targets` file

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

