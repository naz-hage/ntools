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
