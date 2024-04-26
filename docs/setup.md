### [DevSetup](devsetup.md)
DevSetup Folder contains the `apps.json` file and the `DevSetup.ps1` file. The `apps.json` file contains the list of development tools required for the project. The `DevSetup.ps1` file is a PowerShell script that installs the development tools and sets up the development environment for the project.

- After installing `ntools`, two environment variables, `DEVDRIVE` and `MAINDIR`, are created.
- When you create a new project, for example `MyProject`, clone the project into the `%DEVDRIVE%\%MAINDIR%` directory. 
- In your project, create a `DevSetup` folder and add the `apps.json` file to it.
- In the `DevSetup`folder, create a [DevSetup.ps1](./ntools/devsetup.md) file.

Your file structure should look like this:
```cmd
c:\source\MyProject
c:\source\MyProject\DevSetup
c:\source\MyProject\DevSetup\apps.json
c:\source\MyProject\DevSetup\DevSetup.ps1
c:\source\MyProject\... other project and test files
```
### Add nbuild.targets file

A file called [`nbuild.targets`](./ntools/nbuild-targets.md) is required in the solution folder. This file imports the `common.targets` file located in the `$(ProgramFiles)\Nbuild` folder. The `ntools` repository includes multiple target files, which can be found in the `nbuild-targets.md` file.

Your file structure should look like this:
```cmd
c:\source\MyProject
c:\source\MyProject\nbuild.targets
c:\source\MyProject\DevSetup
c:\source\MyProject\DevSetup\apps.json
c:\source\MyProject\DevSetup\DevSetup.ps1
c:\source\MyProject\... other project test files
```
### nbuild.targets
- `nbuild.targets` is a MSBuild project file that imports `common.targets`
- `nbuild.targets` must include the `SolutionName` and `DeploymentFolder` [property](#required-properties). It should also define the [ARTIFACTS(](#artifacts) target. 
- `nbuild.targets` imports the [common.targets](#commontargets) file located in the `$(ProgramFiles)\Nbuild` folder. The `ntools` repository includes multiple target files, which can be found in the [targets](nbuild-targets.md) file.
- `nbuild.targets` can include any additional properties and targets that are specific to the solution.  
- `nbuild.targets` file is located in the solution folder.

### Required Properties
- The following properties are required in `nbuild.targets`:
    - SolutionName: The name of the solution file.
```xml
<PropertyGroup>
  <!--The GUID should be replaced with the solution name-->
  <SolutionName>$([System.IO.Path]::GetFileNameWithoutExtension('$(MSBuildProjectDirectory)'))</SolutionName>
  <DeploymentFolder>$(ProgramFiles)\Nbuild</DeploymentFolder>
</PropertyGroup>
```

### Artifacts
- The following target is required in `nbuild.targets`:
    - ARTIFACTS: The folder where the artifacts are copied to.
```xml
<!--Setup the ARTIFACTS folders for binaries and test results - override -->
    <Target Name="ARTIFACTS" DependsOnTargets="TAG">
      <PropertyGroup>
		 <ArtifactsSolutionFolder>$(ArtifactsDir)\$(SolutionName)</ArtifactsSolutionFolder>
		 <SetupFolder>$(ArtifactsSolutionFolder)\release</SetupFolder>
        <ArtifactsFolder>$(ArtifactsSolutionFolder)\$(TargetRelease)\$(ProductVersion)</ArtifactsFolder>
		<ArtifactsTestResultsFolder>$(ArtifactsSolutionFolder)\TestResults\$(ProductVersion)</ArtifactsTestResultsFolder>
      </PropertyGroup>  
      <ItemGroup>
            <BinaryFiles 
						Exclude="
						 $(SolutionDir)\$(TargetRelease)\**\*.pdb;
						 $(SolutionDir)\$(TargetRelease)\test.*;
						 $(SolutionDir)\$(TargetRelease)\*test*;
						 $(SolutionDir)\$(TargetRelease)\Nuget*;
						 $(SolutionDir)\$(TargetRelease)\*CodeCoverage*"

						Include="
                        $(SolutionDir)\$(TargetRelease)\*.exe;
                        $(SolutionDir)\$(TargetRelease)\*.exe.config;
                        $(SolutionDir)\$(TargetRelease)\*.json;
						$(SolutionDir)\Nbuild\resources\*.targets;
						$(SolutionDir)\Nbuild\resources\*.ps1;
						$(SolutionDir)\Nbuild\resources\*.json;
                        $(SolutionDir)\$(TargetRelease)\*.dll"
						/>

            <RunTimesNetStandard20 Include = "
								   $(SolutionDir)\$(TargetRelease)\netstandard2.0\*.*"
                                    Exclude="
						            $(SolutionDir)\$(TargetRelease)\**\*.pdb"
						            />
        </ItemGroup>
		
        <Message Text="==> DONE"/>
    </Target>
```

### Add a new tool

- When looking for new development tool for your project, your need the following:
    - Web location to download the tool and the name of the downloaded file.  This file will be used to install the tool
    - Command and arguments to install and uninstall the tool
    - Location where the tool will be installed
    - Location of the tool File name.  This file name will be used to check if the tool is already installed
    - Version of the tool
    - Name of the tool
    
- To add a new tool to your project which can be installed by `ntools`, you need to define json file.  Below is an example of the json file for 7-zip development tool
```json
{
  "Version": "1.2.0",
  "NbuildAppList": [
    {
      "Name": "7-zip",
      "Version": "23.01",
      "AppFileName": "$(InstallPath)\\7z.exe",
      "WebDownloadFile": "https://www.7-zip.org/a/7z2301-x64.exe",
      "DownloadedFile": "7zip.exe",
      "InstallCommand": "$(DownloadedFile)",
      "InstallArgs": "/S /D=\u0022$(ProgramFiles)\\7-Zip\u0022",
      "InstallPath": "$(ProgramFiles)\\7-Zip",
      "UninstallCommand": "$(InstallPath)\\Uninstall.exe",
      "UninstallArgs": "/S"
    }
  ]
}
```
- By convention, the json file is named apps.json and is located in the DevSetup folder of your project

- Use Nb.exe to install the tool
```cmd
cd DevSetup
Nb.exe -c install -json apps.json
```

## List of Environment Variables
| Variable Name | Description |
| --- | --- |
| Name | The name of the tool | 
| Version | The version of the tool |
| AppFileName | The file name of the tool.  This file name will be used to check if the tool is already installed |
| WebDownloadFile | The web location to download the tool |
| DownloadedFile | The name of the downloaded file.  This file will be used to install the tool |
| InstallCommand | The command to install the tool |
| InstallArgs | The arguments to install the tool |
| InstallPath | The location where the tool will be installed |
| UninstallCommand | The command to uninstall the tool |
| UninstallArgs | The arguments to uninstall the tool |


### Add a new property
In MSBuild, a project file is an XML file that describes a software build process. It includes three important elements: Property, PropertyGroup, and Item.

- **Property**: A property in MSBuild is a named value that you can refer to in the project file. Properties are key-value pairs and are defined inside PropertyGroup elements.  In the example below, the Configuration property is set to Debug.

```xml
<PropertyGroup>
    <Configuration>Debug</Configuration>
</PropertyGroup>
```

- **PropertyGroup**: A PropertyGroup is a container for properties. It can contain one or more Property elements. PropertyGroup elements can appear anywhere in the project file, and you can have multiple PropertyGroup elements in a project file.  In the example below, the Configuration and Platform properties are set to Debug and AnyCPU, respectively.

```xml
<PropertyGroup>
    <Configuration>Debug</Configuration>
    <Platform>AnyCPU</Platform>
</PropertyGroup>
```

- **Item**: An item in MSBuild is a piece of data that has a type and can be a file, a directory, or any other piece of data that you want to operate on. Items are grouped into ItemGroup elements.  In the example below, the Program.cs file is included in the project.

```xml
<ItemGroup>
    <Compile Include="Program.cs" />
</ItemGroup>
```

For more detailed information, you can refer to the official MSBuild documentation: [MSBuild Project File Schema Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-project-file-schema-reference?view=vs-2019)
### Add a new target

A target is a named sequence of tasks that represents something to be built or done. For more information, see [Targets](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets?view=vs-2022).

**Example of a target:**
```xml
<Target Name="MyTarget">
  <Message Text="Hello, world!" />
</Target>
```

### Add a new task

A task is the smallest unit of work in a build. Tasks are independent executable components with inputs and outputs. You can add tasks to the project file in different sections. For more information, see [Tasks](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-tasks?view=vs-2022).

**Example of a task:**
```xml
<Target Name="MyTarget">
  <Message Text="Hello, world!" />
</Target>
```

See [Nbuild Tasks](./ntools/nbuild-tasks.md) for more information on `ntools` built-in tasks.

### Add a new condition
Conditions in MSBuild allow you to specify whether a particular task, property, or target should be executed based on certain conditions. You can use conditions to control the flow of your build process and make it more flexible and dynamic. Conditions are expressed as Boolean expressions that evaluate to true or false. If the condition evaluates to true, the associated task, property, or target is executed; otherwise, it is skipped. You can use various operators and functions to create complex conditions. For more information on conditions in MSBuild, you can refer to the [Conditions](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-conditions?view=vs-2022) documentation.

**Example of a condition:**
```xml
<PropertyGroup>
  <IsAdmin Condition="'$(IsAdmin)' == ''">false</IsAdmin>
</PropertyGroup>
```