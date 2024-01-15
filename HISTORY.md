[Next](#next)

## Version 1.2.0 - 05-jan-24
- See [issue#5](https://github.com/naz-hage/ntools/issues/5)
- The following lines belong in dotnet.targets file
        <!-- 
            <Exec Command='"$(DotNetExe)" --version' Condition="Exists('$(DotNetExe)')" />
        <RedError Condition="'$(IsAdmin)' == false" Message="Must be an admin to install dotnet" />
        <FileVersion Name="$(DotNetExe)" Condition="Exists('$(DotNetExe)')" >
            <Output TaskParameter="Output" PropertyName="DotNetVersion" />
        </FileVersion>
        <Message Text="dotnet is not installed" Condition="'$(DotNetVersion)' == ''" />
        <!-- Display the file version -->
        <Message Text="dotnet Version is $(DotNetVersion)" Condition="'$(DotNetVersion)' != ''" />
        <Message Text="Installing dotnet" Condition="!Exists('$(DotNetExe)') or '$(DotNetVersion)' == '' or '$(DotNetVersion)' &lt; '$(DotNetTargetVersion)'"/>
        <Exec Command='"$(FileName)" /SILENT /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /SP- /LOG' Condition="'$(DotNetVersion)' == '' Or '$(DotNetVersion)' &lt; '$(DotNetTargetVersion)'" />
        <Delete Files="$(FileName)" Condition="Exists('$(FileName)') == true" /> 
        -->
    - if dotnet is not installed, the dotnet --version command will fail and the file version will not be displayed. in this release , we always install dotnet.  This is a temporary fix until we can figure out how to get the file version of dotnet.
- Add NbuildTask to download and install applications from the web.

 Add targets to install
    - Git
    - Nodejs
    - VSCode
    - Windows Terminal
    - VS2020
    - VS Build Tools
    - Postman
    - Docker Desktop
    - WSL
    - .NET Core SDK
    - .NET Core Runtime
- Add Ninstallexe to download and install applications from the web.
    - Use json input metadata.json file is used to specify the application to download and install.
    - Input json specify 
        - Name
        - Version
        - Url
        - InstallDir
        - InstallFile
        - InstallArgs
    - Output json specify 
        - Name
        - InstalledVersion
        - InstalledDir
        - InstallationStatus
-
    


## Version 1.1.0 - 05-jan-24
- Move Launcher project to its own public repo [ntools-launcher](https://github.com/naz-hage/ntools-launcher). Publish Launcher 1.1.0 to nuget.org and unlist 1.0.0.5
  - Target .netstandard2.0 project to supprt MS build tasks.  MS Build tasks only support .netstandard2.0. 
- Add `Nbuild` project to streamline the building process of ntools and other projects, renaming it yo `Nb.exe` for convenience.
- Refactor Launcher tests
- Introduce `Nbuild` project to streamline the building process of ntools and other projects, renaming it yo `Nb.exe` for convenience.
- Add `NbuildTasks` project that exposes MS build tasks.  Introduce Git Wrapper class to streamline git operations including:
    - Git.GetTag
    - Git.SetTag
    - Git.Autotag
    - Git.PushTag
    - Git.DeleteTag
    - Git.GetBranch
- Introduce `Ngit` project to provide a simplified and automated interface for git operations, renaming it to `Ng.exe` for convenience.
    - Depends on DevDrive and MainDir environment variables.  Default values are used if they don't exist. DevDrive defaults to `C:` and MainDir defaults to `C:\source`.

- Use DevDrive and MainDir from environment variables if they exist.  Otherwise, use default values.
    - This applies to common.targets and NbuildTasks

- Refactor Nbackup - remove cli options src, dest, and options and use json input only file instead.
- 
- Update documentation
- Reference: [issue#23](https://github.com/naz-hage/ntools/issues/23)

