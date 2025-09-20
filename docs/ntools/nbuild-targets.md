The table below lists all available targets for the Nbuild tool. 

- **./ntools\nbuild.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| ARTIFACTS           | Setup the ARTIFACTS folders for binaries and test results - override |
| FILE_VERSIONS       | Test for FileVersion task and powershell file-version.ps1 |
| NBUILD_DOWNLOAD     | Download Nbuild specified in the NbuildTargetVersion |
| MKDOCS              | Build docs locally for testing |
| MKDOCS_DEPLOY       | mkdocs deploy locally |
| GET_PRODUCT_CODES   | Example to get the installation Product code used for uninstallation of product |
| CORE                | Display core properties |
| UPDATE_NTOOLS       | Update ntools locally for testing |
| NUGET_UPDATE        | Update the ntools-launcher nuget package in the local feed for testing - not needed for normal builds |
| YELLOW_MESSAGE      | Example of a target that displays a yellow color message |
| RED_MESSAGE         | Example of a target that displays a red color message |
| GITHUB_RELEASE      | Creates a stage or prod release |
| GITHUB_PRE_RELEASE  | Creates a stage or prod pre-release |
| INSTALL_DOTNET_OUTDATED_TOOL | Install dotnet-outdated-tool globally |
| UPDATE_NUGET_PACKAGES | Update all NuGet packages to the latest version |
| LIST_NUGET_SOURCES  | List all NuGet sources |
| UPDATE_DOC_VERSIONS | Update documentation versions from JSON configuration files |
| GENERATE_COMMIT_MESSAGE | Intelligent commit message generation |
| GIT_COMMIT_INFRASTRUCTURE | Automated Git commit with intelligent message generation |
| UPDATE_AND_COMMIT   | Combined target: Update versions and commit with smart message |
| INFRASTRUCTURE_COMMIT | Full infrastructure update and commit with intelligent analysis |
| PREVIEW_COMMIT_MESSAGE | Preview commit message without committing |


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
| GITHUB_RELEASE      | Creates a stage or prod release |
| GITHUB_PRE_RELEASE  | Creates a stage or prod pre-release |
| STAGE_DEPLOY        | Create a STAGE package and deploy for testing |
| PROD_DEPLOY         | Create a PROD package and deploy for release |
| SOLUTION            | Build the solution Release configuration  using dotnet build |
| SOLUTION_MSBUILD    | Build the solution Release configuration  using MSBuild |
| PACKAGE             | Create a package for the solution default is a zip file of all artifacts |
| COPY_ARTIFACTS      | Save the artifacts to the artifacts folder with organized structure |
| DEPLOY              | Deploy the package. default is to extract artifacts into DeploymentProperty folder |
| TEST                | Run all tests using dotnet test in Release mode with conditional code coverage |
| TEST_DEBUG          | Run all tests using dotnet test in Debug mode |
| COVERAGE            | Generate comprehensive code coverage reports using ReportGenerator |
| COVERAGE_SUMMARY    | Display high-level code coverage summary |
| SMOKE_TEST          | **Comprehensive smoke test**: Validates published artifacts (4+ executables) AND build system integrity (target delegation). Consolidated from TEST_TARGET_DELEGATION |
| SMOKE_TEST_PWSH     | REMOVED: functionality consolidated into `SMOKE_TEST` |
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
