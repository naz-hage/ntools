## Version 1.4.0 - 25-apr-24

- Complete [issue #37](https://github.com/naz-hage/ntools/issues/37)
- Complete [issue #38](https://github.com/naz-hage/ntools/issues/38)
- Complete [issue #34](https://github.com/naz-hage/ntools/issues/34)
- Complete [issue #44](https://github.com/naz-hage/ntools/issues/44)
- Complete [issue #41](https://github.com/naz-hage/ntools/issues/41)
 
## Version 1.3.0 - 15-feb-24
- Fix [issue #29](https://github.com/naz-hage/ntools/issues/29)
- Add default value for -json option
- Fix specifiying the process start info for ntools-launcher.
  - if StartInfo.FileName uses executable name only that is in the system path, then the StartInfo.FileName witll be replaced with the full pathName.
  - See FileMapping.cs file
  - Add install.ps1 which is equivalent to install.bat.
  - update to ntools-launcher 1.3.0

## Version 1.2.0 - 22-jan-24
- Fix [issue #27](https://github.com/naz-hage/ntools/issues/27)
- Update to ntools-launcher 1.2.0
- Use $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}" for c:\windows\system32
- When Using ntools-launcher use default Process::StartInfo
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = false,
                UseShellExecute = false,

## Version 1.2.0 - 22-jan-24
- See [issue#5](https://github.com/naz-hage/ntools/issues/5)
- Add NbuildTask to download and install applications from the web..j
 - Add targets to install
    - Git
    - Nodejs
    - VSCode
    - VS2020
    - Postman
    - Docker Desktop
    - WSL
    - Dotnet Core SDK
    - Dotnet Core Runtime
- Add -i [list | download | install] and -json option to Nb.exe to download and install applications from the web.
    - Use json input applist.json file to specify the applications to download and install.
        
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
- Update documentation
- Reference: [issue#23](https://github.com/naz-hage/ntools/issues/23)

## Next
- Delete non-prod releases from Artifacts folder
- Add https://github.com/naz-hage/learn/tree/main/dotnet/cleanup-non-prod project to NbuildTasks.  