# Software Tools Collection

This repository contains a collection of software tools specifically designed to automate various build and test tasks on Windows clients. Whether you are a developer working on your local machine or using GitHub Actions for continuous integration, these tools will greatly simplify your workflow and enhance your productivity.

With the NTools, you can effortlessly backup your files and folders, build your projects with ease, perform Git operations seamlessly, and leverage powerful MSBuild tasks. The installation process is straightforward, and the tools are highly reliable and efficient, ensuring the safety and integrity of your data.

Take advantage of the NTools' intuitive command-line interface to streamline your development process. From building solutions to running tests, creating staging builds, and exploring available options, the NTools provide a seamless experience for all your software development needs.

Don't settle for mediocre tools when you can have the NTools at your disposal. Try them out today and witness the difference they can make in your development workflow. Enhancements are added weekly! 

Don't hesitate to write an [issue](https://github.com/naz-hage/ntools/issues) if you have any questions or suggestions.

## Table of Contents
1. [Nbackup](#Nbackup)
2. [Nbuild](#Nbuild)
3. [Ngit](#Ngit)
4. [NbuildTasks](#nbuildtasks)
5. [Installation](#Installation)
6. [Usage](#Usage)

## Nbackup
[Nbackup](./Nbackup/README.md) is a tool that leverages `robocopy` to backup a list of files and folders from a source to a destination. It is designed to be reliable and efficient, ensuring that your data is safe.
- Nbackup command line options:
```
 Nbackup.exe [-i value] [-n value] [-performbackup value]

 
      - `i`       : input backup file which specifies source, destination and backup options. See [backup.json](./nBackup/Data/backup.json) for a sample input backup json file. (string, default=)
      - `v`       : Verbose Values: true | false.  Default is false (true or false, default=False)
      - `performbackup` : Values: true | false.  false displays options and does not perform backup (default=True)
```
## Nbuild (Nb)
[Nbuild](./Nbuild/README.md) is a tool that launches MSBuild with a target to build. It simplifies the build process and makes it easier to manage your projects.


# Nb.exe (Nbuild)
- Nb.exe is a tool that launches MSBuild with a target to build. It simplifies the build process and makes it easier to manage your projects.
  - To run a target typee `nb [Target Name]`
  - The list of targets is generated by running `nb.exe -c targets` command.

- Nb is a command line tool that is used to build, test, and deploy solutions. It is a wrapper for MSBuild and provides a simplified interface for building solutions. It also provides a way to define and run custom targets.
- Nb install the development tools and runtimes required to build and test the solution. It also provides a way to define and run custom targets.
- Nb.exe generates `nbuild.bat` to the solution folder and uses `common.targets` from $(ProgramFiles)\Nbuild.
- Expects `nbuild.targets` file in the solution folder.
```cmd
 Usage:
 Nb.exe [-c value] [-json value]
  - c    : command. value = [targets | install | download | list | ]
         targets         -> List available targets and save in targets.md file
         install         -> Download and install apps specified in -json option, requires admin priviledges
         download        -> Download apps specified in -json option, requires admin priviledges
         list            -> List apps specified in -json option (string, default=)
  - json : json file which holds apps list. Valid only for -c install | download | list option
         sample json file: https://github.com/naz-hage/ntools/blob/main/Nbuild/resources/app-ntools.json" (string, default=)
```


- **If the -json option is not specified, the default json file `$(ProgramFiles)\Nbuild\ntools.json` is used if it exists**. 

# nbuild.targets
- nbuild.targets is a MSBuild file that is imports `common.targets`.
- The following properties are required in nbuild.targets:
    - SolutionName: The name of the solution file.
    	```xml
        <PropertyGroup>
    		<!--The GUID should be replaced with the solution name-->
        	<SolutionName></SolutionName>
            <DeploymentFolder>$(ProgramFiles)\Nbuild</DeploymentFolder>
    	    </PropertyGroup>
        ```

- The following target is required in nbuild.targets:
    - ARTIFACTS: The folder where the artifacts are copied to.
        ```xml
        <Target Name="ARTIFACTS" DependsOnTargets="TAG">
        <!--The folder where the artifacts are copied to-->
        <ItemGroup>
            <BinaryFiles Include="$(Solut
        ```
                    
# common.targets
- common.targets is imported by nbuild.targets.

- Common TARGETS:

| Target Name | Description |
| --- | --- |
| PROPERTIES | Sets up properties for the build process, reads the Git tag, checks if essential properties are defined, and prints out some properties and git information. |
| CLEAN | Cleans the solution by removing the output directories and deleting the obj directories. |
| STAGING | Executes a series of targets for staging: CLEAN, TAG, AUTOTAG_STAGING, SOLUTION, TEST, SAVE_ARTIFACTS, PACKAGE. |
| STAGING_DEPLOY | Executes the STAGING target and then the DEPLOY target. |
| PRODUCTION | Executes a series of targets for production: CLEAN, TAG, |AUTOTAG_PRODUCTION, SOLUTION, TEST, SAVE_ARTIFACTS, PACKAGE. |
| PRODUCTION_DEPLOY | Executes the PRODUCTION target and then the DEPLOY target. |
| AUTOTAG_STAGING | Sets the build type to STAGING. |
| AUTOTAG_PRODUCTION | Sets the build type to PRODUCTION and updates the main branch of the git repository. |
| AUTOTAG_PRODUCTION | Increments version for a production build, but only if the current branch is 'main' || SIGN_PRODUCT | Placeholder target for signing the product. Currently, it does not perform any actions. |
| GIT_TAG | Commits changes to the git repository, pulls the latest changes, pushes the changes, and creates a tag. |
| AUTOTAG | Gets the git branch, reads the build type, and automatically creates a tag based on the branch and build type. |
| SOLUTION | Builds the solution using the `dotnet build` command with the specified configuration, version, and culture. |
| SOLUTION_MSBUILD | Restores the solution's dependencies with `dotnet restore` and then builds the solution using MSBuild with the specified configuration, platform, version, and culture. |
| PACKAGE | Creates a zip file of the artifacts folder and then removes the artifacts folder. |
| SAVE_ARTIFACTS | Copies various types of files (binary files, EnUSFiles, ref, RunTimesLib, Default, RunTimesLibNet, RunTimesNetStandard20, RunTimesNative) to the artifacts folder. |
| Deploy | Checks if the user is an administrator and if the DeploymentFolder property is defined. If both conditions are met, it extracts the zip file of the artifacts folder to the deployment folder and then removes the setup folder. |
| TEST | Runs tests on the solution with the `dotnet test` command with the specified configuration and logger settings. |
| TEST_RELEASE | Similar to TEST, but uses the release configuration. |
| SingleProject | Demonstrate how to build a single projec. Builds a specific project (`nbuild\nbuild.csproj`) using the `dotnet build` command with the specified configuration, runtime, version. |
| IS_ADMIN | Checks if the user is an administrator by running the `net session` command and sets the `IsAdmin` property based on the exit code. |
| GIT_STATUS | Displays the current git status |
| AUTOTAG_STAGING | Increments version for a staging build |
| SET_TAG | Sets version for a staging build |
| GIT_PULL | Pulls the latest changes from git |
| TAG | Gets the latest tag from git |
| PUSH_TAG | Pushes the current tag to the remote repository |
| GIT_BRANCH | Gets the current branch from git |
| HandleError | Prints a high importance message stating that an error occurred while reading the version file. |

# nbuild.bat
- nbuild.bat is a batch file that is generated by nb.exe.
- nbuild.bat is used to run MSBuild to build any target defined in *.targets files.
# [Targets.md](../targets.md)
- Targets.md is a markdown file that is generated by nb.exe.
- Targets.md lists all the targets defined in *.targets files.

## Ngit (Ng)
[Ngit](./Ngit/README.md) is simple wrapper for `Git` tool that perform simple commands such as get tag and set tag.

Usage:
 ```batch
 Ng.exe [-git value] [-org value] [-url value] [-branch value] [-tag value] [-buildtype value] [-v value]
  - git       : git Command, value= [gettag | settag| autotag| autoversion| deletetag | getbranch | setbranch| createbranch]
         gettag          -> Get tag of a branch for a given project
         settag          -> Set specied tag of a branch for a given project
         autotag                 -> Set next tag based of branch and project on STAGING vs.PRODUCTION build (commit to remote repo)
         autoversion     -> Equivalent to `autotag` cmd (Does not commit to remote repo)
         deletetag       -> Delete specified tag of a branch for a given Project
         getbranch       -> Get the current branch for a given project
         setbranch       -> Set/checkout specified branch for a given project
         createbranch    -> Create specified branch for a given project
         clone           -> Clone a Project (string, default=)
  - org       : Organization Name (string, default=)
  - url       : GitHub Url Name (string, default=)
  - branch    : Branch Name (string, default=)
  - tag       : Tag Name (string, default=)
  - buildtype : Values: STAGING | PRODUCTION (string, default=)
  - v         : verbose. value = [true | false] (true or false, default=False)
 ```
## NbuildTasks
[NbuildTasks](./NbuildTasks/README.md) is a class library that exposes `MSBuild` tasks. It is used by `Nbuild` to perform various tasks such as web download and tools installation during the build of any project.

NbuildTasks defines a few additional Tasks that can be used during builds. Here are a few examples:
```xml
<Target Name="TAG">
    <GetTag Branch="$(Branch)" BuildType="$(BuildType)">
        <Output TaskParameter="Tag" PropertyName="Tag" />
    </GetTag>
    <Message Text="Tag: $(Tag)" Importance="high" />
</Target>

<Target Name="REDERROR">
    <RedError Message="This is an error message displayed in Red" />
</Target>
```

You can find the complete list of predefined [MSBuild properties in the Microsoft documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties?view=vs-2022).

## Additional Information
There are several predefined MSBuild properties that can be used during builds. Here are a few examples:
- `$(MSBuildProjectFile)`: The file name of the project file.
- `$(MSBuildProjectName)`: The file name of the project file without the extension.
- `$(MSBuildProjectExtension)`: The extension of the project file.
- `$(MSBuildProjectFullPath)`: The absolute path of the project file.
- `$(MSBuildThisFileDirectory)`: The directory of the MSBuild file that is currently being processed.

## Installation
To get started with the NTools repository, follow these steps:

1. Clone this repository to your local machine.
2. Open a command prompt in administrative mode and navigate to the root folder of the repository.
3. Change the PowerShell execution policy to allow the installation script to run. Run the following command:

    ```cmd
    Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process
    ```
    
    This command will allow the installation script to run. Once the installation is complete, the execution policy will revert to its original state.
4. Run the following command to install the tools:

    ```cmd
    install.bat
    ```

   This command will install the Dotnet Core Desktop runtime and download the Ntools from GitHub. The tools will be installed in the `C:\Program Files\Nbuild` folder.

Once the installation is complete, you'll be ready to use the NTools from the command line. Refer to the [Usage](#Usage) section for examples of how to use the tools.

## Usage
Once installation is complete, you can use the NTools from the command line.  Open a Terminal and navigate to your solution folder.

- Examples: 
-   Build a solution:

    ```cmd
    nb.exe solution
    ```
- Clean a solution:

    ```cmd
    nb.exe clean
    ```

- Run tests on a solution:

    ```cmd
    nb.exe test
    ```
- Create a staging build:

    ```cmd
    nb.exe staging
    ```
- Display available options:
    
        ```cmd
        nb.exe -cmd targets
        ```