# ntools
Collection of useful tools which automates various tasks on Windows client.

- [**nBackup**](./Nbackup/README.md) - A tool which relies on `robocopy` to backup a list of files and folders from source and destination.

 
- [**Nbuild**](./Nbuild/README.md) - A tool which launches MSBuild with a target to build.
- [**Launcher**](./launcher/README.md) - The launcher class is used by nBackup to launch robocopy and wait for it to complete.

## Additional information:
- There are several predefined MSBuild properties that can be used during builds. Here are a few examples:
    - `$(MSBuildProjectFile)`: The file name of the project file.
    - `$(MSBuildProjectName)`: The file name of the project file without the extension.
    - `$(MSBuildProjectExtension)`: The extension of the project file.
    - `$(MSBuildProjectFullPath)`: The absolute path of the project file.
    - `$(MSBuildThisFileDirectory)`: The directory of the MSBuild file that is currently being processed.
 
You can find the complete list of predefined [MSBuild properties in the Microsoft documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties?view=vs-2022).
