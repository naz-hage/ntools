## NbuildTasks
`NbuildTasks` is a class library that exposes custom `MSBuild` tasks. It is used by `Nbuild` to perform various tasks such as web download and tools installation during the build of any project.

Here is an example custom Tasks that can be used during builds:
```xml
<Target Name="TAG">
    <-- This target uses the `GetTag` task to display the tag from a branch -->
    <GetTag Branch="$(Branch)" BuildType="$(BuildType)">
        <Output TaskParameter="Tag" PropertyName="Tag" />
    </GetTag>
    <Message Text="Tag: $(Tag)" Importance="high" />
</Target>

<Target Name="REDERROR">
    <-- This target uses the `RedError` task to display an error message in red color -->
    <RedError Message="This is an error message displayed in Red" />
</Target>
```
- The `RedError` task will display an error message in red color in the console output.
- The `GetTag` task will get the tag from the branch and build type and display it in the console output.

You can also find the complete list of predefined [MSBuild properties in the Microsoft documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties?view=vs-2022).

- Here are few examples:

- `$(MSBuildProjectFile)`: The file name of the project file.
- `$(MSBuildProjectName)`: The file name of the project file without the extension.
- `$(MSBuildProjectExtension)`: The extension of the project file.
- `$(MSBuildProjectFullPath)`: The absolute path of the project file.
- `$(MSBuildThisFileDirectory)`: The directory of the MSBuild file that is currently being processed.
