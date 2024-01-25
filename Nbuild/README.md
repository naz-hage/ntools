
# Nb.exe (Nbuild) 
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
         sample json file: https://github.com/naz-hage/ntools/blob/main/Nbuild/resources/NbuildAppListTest.json" (string, default=)
```

  
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
