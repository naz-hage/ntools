## NbuildTasks
`NbuildTasks` is a class library that exposes `MSBuild` tasks. It is used by `Nbuild` to perform various tasks such as web download and tools installation during the build of any project.

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

