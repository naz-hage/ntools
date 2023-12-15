# ntools
Collection of useful tools which automates various tasks on Windows client.

- **nBackup** - A .NET executable which relies on robocopy to backup a list of folders from source and destination.
nBackup command line options:
 
```
 nBackup.exe [-src value] [-dest value] [-opt value] [-input value] [-verbose value] [-performbackup value]

 
      - `src`           : Source Folder (string, default=)
      - `dest`          : Destination folder (string, default=)
      - `opt`           : Backup Options (string, default=/s /XD .git /XD .vs /XD TestResults /XF *.exe /XF *.dll /XF *.pdb /e)
      - `input`         : input backup file which specifies source, destination and backup options. See [backup.json](./nBackup/Data/backup.json) for a sample input backup json file. (string, default=)
      - `verbose`       : Values: true | false.  Default is false (true or false, default=False)
      - `performbackup` : Values: true | false.  false displays options and does not perform backup (default=True)

            - if `input` option is specified, the `src`, `dest`, and `opt` options are ignored.
            - if `input` option is not specified, the `src`, `dest`, and `opt` options are required.

```
- [**Launcher**](./launcher/README.md) - The launcher class is used by nBackup to launch robocopy and wait for it to complete.

## Additional information:
- There are several predefined MSBuild properties that can be used during builds. Here are a few examples:
    - `$(MSBuildProjectFile)`: The file name of the project file.
    - `$(MSBuildProjectName)`: The file name of the project file without the extension.
    - `$(MSBuildProjectExtension)`: The extension of the project file.
    - `$(MSBuildProjectFullPath)`: The absolute path of the project file.
    - `$(MSBuildThisFileDirectory)`: The directory of the MSBuild file that is currently being processed.
 
You can find the complete list of predefined [MSBuild properties in the Microsoft documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties?view=vs-2022).
