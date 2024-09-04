The table below lists all available targets for the Nbuild tool. 

- **c:\source\ntools\nbuild.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| ARTIFACTS           | Setup the ARTIFACTS folders for binaries and test results - override |
| CLEAN_ARTIFACTS     | Delete the ARTIFACTS folder after PACKAGE target is completed |
| TEST_GIT            | Temporary Target to test the Git Task |
| LOCAL               | Build local stage without incrementing the version |
| FILE_VERSIONS       | Test for FileVersion task and powershell file-version.ps1 |
| NBUILD_DOWNLOAD     | Download Nbuild specified in the NbuildTargetVersion |
| NBUILD_INSTALL      | Install Nbuild specified in the NbuildTargetVersion |
| DEV_SETUP           | Setup Development Environment |
| MKDOCS              | Build docs locally for testing |
| NBUILD_DOWNLOAD     | Download Nbuild specified in the NbuildTargetVersion |
|                     | Update the ntools-launcher nuget package in the local feed fot testing - not needed for normal builds |
| NBUILD_INSTALL      | Install Nbuild specified in the NbuildTargetVersion |
| UPDATE_APPS         | Update App metadata json files in nbuild\resources |
| GET_PRODUCT_CODES   | Example to get the installation Product code used for uninstallation of product |


- **C:\Program Files\Nbuild\common.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| PROPERTIES          | Common properties that will be used by all targets |
| CLEAN               | Clean up the project and artifacts folder |
| INSTALL_DEP         | Install dependencies |
| TELEMETRY_OPT_OUT   | Opt out of the DOTNET_CLI_TELEMETRY_OPTOUT - move to common |
| DEV                 | Create a development package for testing |
| STAGE               | Create a stage package for testing |
| PROD                | Create a production package for release |
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
| AUTOTAG_STAGE     | Increment version for a stage build |
| SET_TAG             | Set version for a stage build |
| GIT_PULL            | Get the latest tag from git |
| AUTOTAG_PROD  | Increment version for a production build |
| TAG                 | Get the tag from git |
| PUSH_TAG            | Push the tag to the remote repo |
| GIT_BRANCH          | Get the current git branch |


