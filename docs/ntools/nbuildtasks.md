## NbuildTasks
`NbuildTasks` (nbuildtasks.dll) is a class library that exposes custom `MSBuild` tasks. It is used by `Nbuild` to perform various tasks such as web download and tools installation during the build of any project.

### List of Tasks
| Task Name | Description |
| --- | --- |
| [RedError](#rederror) | Displays an error message in red color in the console output. |
| [Git](#git) | Git commands such as get or set the tag from the branch or build type and displays it in the console output. |
| [ColorMessage](#colormessage) | Displays a message in the console output with the specified color. |
| [FileVersion](#fileversion) | Gets the version of the specified file. |
| [WebDownload](#webdownload)  | Downloads a file from the specified URL. |
| [Unzip](#unzip)  | Unzips the specified file. |
| [Zip](#zip)  | Zips the specified file. |
| [Pwsh](#pwsh) | Runs a PowerShell Core script from MSBuild. |
| [UpdateVersionsInDocs](#updateversionsindocs) | Updates version numbers in documentation files. |

Here are examples of custom Tasks that can be used during builds:

### RedError
```xml
<!-- This target uses the `RedError` task to display an error message in red color -->
<Target Name="RED_ERROR">
	<RedError Message="This is an error message displayed in Red" />
</Target>
```
### Git
```xml
<Target Name="TAG">
	<-- This target uses the `GetTag` task to display the tag from a branch -->
	<GetTag Branch="$(Branch)" BuildType="$(BuildType)">
		<Output TaskParameter="Tag" PropertyName="Tag" />
	</GetTag>
	<Message Text="Tag: $(Tag)" Importance="high" />
</Target>
```
### ColorMessage
```xml
<!-- This target uses the `ColorMessage` task to display a message with a specified color -->
<Target Name="COLOR_MESSAGE">
	<ColorMessage Message="This is a message displayed in Yellow" Color="Yellow" />
</Target>
```
### FileVersion
```xml
	<!-- This target uses the `FileVersion` task to file version of specified file -->
	<Target Name="FILE_VERSION">
		<PropertyGroup>
			<FileExe>$(ProgramFiles)\Nbuild\nb.exe</FileExe>
		</PropertyGroup>

		<FileVersion Name="$(FileExe)" Condition="Exists('$(FileExe)')" >
			<Output TaskParameter="Output" PropertyName="Version" />
		</FileVersion>
	</Target>
```
### WebDownload
```xml
<!-- This target uses the `WebDownload` task to download a file from a specified URL -->
<Target Name="WEB_DOWNLOAD" DependsOnTargets="IS_ADMIN" >
	<PropertyGroup>
		<!-- visit https://nodejs.org/dist/ to get the latest stable version -->
		<DownloadsDirectory>c:\NtoolsDownloads</DownloadsDirectory>
		<NodeAppName>Node.js</NodeAppName>
		<NodeTargetVersion>21.5.0</NodeTargetVersion>
		<WebUri>https://nodejs.org/dist/v$(NodeTargetVersion)/node-v$(NodeTargetVersion)-x64.msi</WebUri>
		<FileName>$(DownloadsDirectory)\node-v$(NodeTargetVersion)-x64.msi</FileName>
	</PropertyGroup>
	<RedError Condition="'$(IsAdmin)' == false" Message="Must be an admin to install $(NodeAppName)" />
	<Delete Files="$(FileName)" Condition="Exists('$(FileName)') == true" />
	<WebDownload WebUri="$(WebUri)" FileName="$(FileName)" />
	<Message Text="==> NODE_DONE"/>
</Target>
```

### Unzip
```xml
<!-- This target uses the `Unzip` task to decompress a specified file -->
<Target Name="UNZIP">
	<PropertyGroup>
		<FileName>c:\temp\source.zip</FileName>
		<Path>c:\temp\test1</Path>
	</PropertyGroup>
	<Unzip FileName="$(FileName)" Destination="$(Path)" />
	<Message Text="==> ZIP_DONE"/>
</Target>
```
### Zip
```xml
<!-- This target uses the `Zip` task to compress a specified file -->
<Target Name="ZIP">
	<PropertyGroup>
		<FileName>c:\temp\source.zip</FileName>
		<Path>c:\temp\test</Path>
	</PropertyGroup>
	<Zip FileName="$(FileName)" Path="$(Path)" />
	<Message Text="==> ZIP_DONE"/>
</Target>
```
### Pwsh
```xml
<!-- This target uses the `Pwsh` task to run a PowerShell Core script from MSBuild -->
<Target Name="RUN_PWSH">
	<PropertyGroup>
		<ScriptPath>$(SolutionDir)scripts\build\custom-script.ps1</ScriptPath>
		<ScriptArgs>--example true</ScriptArgs>
	</PropertyGroup>

	<!-- Pwsh is a custom MSBuild task that executes a PowerShell Core script -->
	<Pwsh Script="$(ScriptPath)" Arguments="$(ScriptArgs)" NoProfile="true" ExecutionPolicy="Bypass" />

	<Message Text="==> PWSH_DONE" />
</Target>
```
### UpdateVersionsInDocs
```xml
<!-- This target uses the `UpdateVersionsInDocs` task to update version numbers in documentation files -->
<Target Name="UPDATE_DOC_VERSIONS">
	<UpdateVersionsInDocs DocsPath="$(SolutionDir)docs" Version="$(Version)" />
</Target>
```

### Modern Approach with ntools-scripts module
```xml
<Target Name="PUBLISH">
	<Exec Command='pwsh -NoProfile -ExecutionPolicy Bypass -Command "Import-Module &apos;$(BuildTools)\modules\ntools-scripts\ntools-scripts.psm1&apos; -Force; Publish-AllProjects -OutputDir &apos;$(ArtifactsFolder)&apos; -Version &apos;$(ProductVersion)&apos; -RepositoryRoot &apos;$(SolutionDir)&apos;"' WorkingDirectory="$(SolutionDir)" />
</Target>
```

**Benefits of the Module Approach:**
- **Deterministic paths**: Explicit `RepositoryRoot` parameter eliminates path detection issues
- **Centralized functionality**: All PowerShell functions in one module
- **Better error handling**: Consistent error reporting and logging
- **Easier maintenance**: Single module file instead of dozens of scripts
- **Integration ready**: Automatically installed and available in build process

For complete module documentation, see [ntools-scripts Module](ntools-scripts-module.md).
You can also find the complete list of predefined [MSBuild properties in the Microsoft documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties?view=vs-2022).

- Here are few examples:

- `$(MSBuildProjectFile)`: The file name of the project file.
- `$(MSBuildProjectName)`: The file name of the project file without the extension.
- `$(MSBuildProjectExtension)`: The extension of the project file.
- `$(MSBuildProjectFullPath)`: The absolute path of the project file.
- `$(MSBuildThisFileDirectory)`: The directory of the MSBuild file that is currently being processed.
