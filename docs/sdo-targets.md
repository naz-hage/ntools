- **C:\source\ntools\sdo.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| ARTIFACTS           | Setup the ARTIFACTS folders for binaries and test results - override |
| FILE_VERSIONS       | Test for FileVersion task and powershell file-version.ps1 |
| NBUILD_DOWNLOAD     | Download Nbuild specified in the NbuildTargetVersion |
| SETUP_ENVIRONMENT   | Setup development environment by importing ntools-scripts and calling Set-DevelopmentEnvironment |
| TEST_NTOOLS_SCRIPTS | Test that ntools-scripts module is installed and can report a version |
| MKDOCS              | Build docs locally for testing |
| RUN_NBTESTS_COVERAGE | Run nbTests and generate code coverage report |
| MKDOCS_DEPLOY       | mkdocs deploy locally with live reload fixes |
| INSTALL_PYTHON_TEST_DEPS | Install python test dependencies for atools and run pytest (verbose) |
| RUN_PYTESTS_VERBOSE | Run Python pytest for atools package (verbose) |
| RUN_SDO_TESTS       | Run SDO tests specifically |
| GET_PRODUCT_CODES   | Example to get the installation Product code used for uninstallation of product |
| CORE                | Display core properties |
| BUILD               | Build the solution alias solution target |
| CHECK_GITHUB_KEY    | Check for API_GITHUB_KEY environment variable and print its length |
| UPDATE_NTOOLS       | Update ntools locally for testing |
| NUGET_UPDATE        | Update the ntools-launcher nuget package in the local feed for testing - not needed for normal builds |
| YELLOW_MESSAGE      | Example of a target that displays a yellow color message |
| RED_MESSAGE         | Example of a target that displays a red color message |
| INSTALL_DOTNET_OUTDATED_TOOL | Install dotnet-outdated-tool globally |
| UPDATE_NUGET_PACKAGES | Update all NuGet packages to the latest version |
| LIST_NUGET_SOURCES  | List all NuGet sources |
| UPDATE_DOC_VERSIONS |  |
| GENERATE_COMMIT_MESSAGE | Intelligent commit message generation |
| GIT_COMMIT_INFRASTRUCTURE | Automated Git commit with intelligent message generation |
| UPDATE_AND_COMMIT   | Combined target: Update versions and commit with smart message |
| INFRASTRUCTURE_COMMIT | Full infrastructure update and commit with intelligent analysis |
| PREVIEW_COMMIT_MESSAGE | Preview commit message without committing |
| INSTALL_REPORTGENERATOR | Install ReportGenerator tool globally |
| GITHUB_RELEASE      | Creates a stage or prod release Add for testing and remove after success |
| GITHUB_PRE_RELEASE  | Creates a stage or prod pre-release Add for testing and remove after success |
| RUN_RELEASESERVICEFACTORYTESTS | Run the focused ReleaseServiceFactoryTests via dotnet test |
| NB_PRECHECK         | Pre-check: ensure no machine-level Debug/Release folders are present before running tests |
| NB_TEST             | Run the full solution test suite (used by 'nb test') |
| NB_TEST_DIAGNOSTICS | Diagnostic target to run automatically after NB_TEST to list TestFramework DLLs |
| NB_DIAG_NbuildTasksTests | Targets auto-generated: one per Tests.csproj in the solution |
| NB_DIAG_NbuildTests |  |
| NB_DIAG_lfTests     |  |
| NB_DIAG_nbTests     |  |
| NB_DIAG_nBackupTests |  |
| NB_DIAG_GitHubReleaseTests |  |
| NB_TEST_DIAG_ALL    | Aggregate target: run all per-project diagnostics sequentially |
| STAGE               | Create a stage package for testing |
| PROD                | Create a PROD package for release (delegates to PROD_NEW in common.targets) |
| SMOKE_TEST          | Comprehensive smoke test to verify published artifacts and build system integrity |
| TEST                | This prevents double test execution that causes conflicts |
| PUBLISH             | Publish all non-test projects to artifacts folder |


- **C:\Program Files\Nbuild\common.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| PROPERTIES          | Common properties that will be used by all targets |
| CLEAN               | Clean up the project and artifacts folder |
| INSTALL_DEP         | Install dependencies |
| TELEMETRY_OPT_OUT   | Opt out of the DOTNET_CLI_TELEMETRY_OPTOUT - move to common |
| DEV                 | Create a development package for testing without incrementing the version |
| STAGE               | Create a stage package for testing |
| PROD                | Create a PROD package for release |
| GITHUB_RELEASE      | Creates a prod release |
| GITHUB_PRE_RELEASE  | Creates a pre-release |
| STAGE_DEPLOY        | Create a STAGE package and deploy for testing |
| PROD_DEPLOY         | Create a PROD package and deploy for release |
| SOLUTION            | Build the solution Release configuration  using dotnet build |
| SOLUTION_MSBUILD    | Build the solution Release configuration  using MSBuild |
| PACKAGE             | Create a package for the solution default is a zip file of all artifacts |
| COPY_ARTIFACTS      | Save the artifacts to the artifacts folder |
| DEPLOY              | Deploy the package. default is to extract artifacts into DeploymentProperty folder |
| TEST                | Run all tests using dotnet test in Release mode |
| TEST_DEBUG          | Run all tests using dotnet test in Debug mode |
| PUBLISH             | Publish all non-test projects to artifacts folder |
| COVERAGE            | Generate code coverage reports |
| COVERAGE_SUMMARY    | Display high-level code coverage summary |
| STAGE_NEW           | Enhanced stage package for testing with coverage and smoke tests |
| PROD_NEW            | Enhanced prod package for release with coverage and smoke tests |
| IS_ADMIN            | Check if current process is running in admin mode AdminCheckExitCode property is set |
| SingleProject       | Example how to build a single project |
| HandleError         | Error handling placeholder |


- **C:\Program Files\nbuild\apps-versions.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| APP_COMMON          | Defines the download location for the apps |


- **C:\Program Files\nbuild\git.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| GIT_DOWNLOAD        | Download Git For Windows version specified in GitTargetVersion - Requires admin mode |
| GIT_INSTALL         | Download Git For Windows version specified in GitTargetVersion property and install |
| GIT_UPDATE          | Update the current Git for Windows |


- **C:\Program Files\nbuild\dotnet.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| DOTNET_SDK_DOWNLOAD | Download dotnet Core sdk |
| DOTNET_SDK_INSTALL  | Download and install dotnet Core sdk |
| DOTNET_DOWNLOAD     | Download DotNet Core |
| DOTNET_INSTALL      | Download and install DotNet Core |


- **C:\Program Files\nbuild\code.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| CODE_DOWNLOAD       | Download node version specified in CodeTargetVersion - Requires admin mode |
| CODE_INSTALL        | Download node version specified in CodeTargetVersion property and install |


- **C:\Program Files\nbuild\node.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| NODE_DOWNLOAD       | Download node version specified in NodeTargetVersion - Requires admin mode |
| NODE_INSTALL        | Download node version specified in NodeTargetVersion property and install |
| NODE_VERSION        | Display the installed note version |


- **C:\Program Files\nbuild\mongodb.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| MONGODB_INSTALL     | Display mongodb version specified in TargetNodeVersion property and install |


- **C:\Program Files\nbuild\nuget.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| NUGET_VERSION       | Display the installed nuget version |
| NUGET_INSTALL       | Download latest nuget.exe and install |


- **C:\Program Files\nbuild\ngit.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| GIT_STATUS          | Display the current git status |
| AUTOTAG_STAGE       | Increment version for a stage build |
| SET_TAG             | Set version for a stage build |
| GIT_PULL            | Get the latest tag from git |
| AUTOTAG_PROD        | Increment version for a production build |
| TAG                 | Get the tag from git |
| PUSH_TAG            | Push the tag to the remote repo |
| GIT_BRANCH          | Get the current git branch |


- **C:\Program Files\nbuild\docker.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| DOCKER_DOWNLOAD     | Download Docker version specified in DockerTargetVersion - Requires admin mode |
| DOCKER_INSTALL      | Download Docker version specified in DockerTargetVersion property and install |


- **C:\Program Files\nbuild\terraform.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| TF_WORKSPACE        | Create a new terraform workspace `dev` and select it |
| TF_INIT             | Init terraform |
| TF_PLAN             | terraform plan |
| TF_APPLY            | terraform apply |
| TF_DESTROY          | terraform destroy |


- **unit-tests.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| UNIT_TEST_CLI_VALIDATION | Unit tests for CLI validation |
| UNIT_TEST_BUILD_STARTER | Unit tests for BuildStarter |
| UNIT_TEST_CLI       | Unit tests for CLI |
| UNIT_TEST_COMMAND   | Unit tests for Command |
| UNIT_TEST_NB_COMMAND | Unit tests for NbCommand |
| UNIT_TEST_GIT_CLONE_COMMAND | Unit tests for GitCloneCommand |
| UNIT_TEST_NTOOLS_JSON | Unit tests for NtoolsJson |
| UNIT_TEST_PATH_MANAGER | UNIT_TEST_PATH_MANAGER is excluded from UNIT_TEST_ALL and should be run separately with: nb UNIT_TEST_PATH_MANAGER |
| UNIT_TEST_RELEASE_SERVICE_FACTORY | Unit tests for ReleaseServiceFactory |
| UNIT_TEST_RESOURCE_HELPER | Unit tests for ResourceHelper |
| UNIT_TEST_WORKITEM_COMMAND | Unit tests for WorkItemCommand |
| UNIT_TEST_REPOSITORY_COMMAND | Unit tests for WorkItemCommand |
| UNIT_TEST_PIPELINE_COMMAND | Unit tests for PipelineCommand |
| UNIT_TEST_ADHOC_GET_PIPELINE_ASYNC | Ad hoc integration-style test for AzureDevOpsClient.GetPipelineAsync |
| UNIT_TEST_PULL_REQUEST_COMMAND | Unit tests for PullRequestCommand |
| UNIT_TEST_WORKITEM_STATE_TRANSLATOR | Unit tests for WorkItemStateTranslator |
| UNIT_TEST_MAPPING_GENERATOR | Unit tests for MappingGenerator |
| UNIT_TEST_SDOTESTS  | Unit tests for all SDO tests with code coverage |
| UNIT_TEST_NBTESTS   | Unit tests for all nbtests |
| UNIT_TEST_ALL       | Run all unit tests |


