
## [Latest Release](https://github.com/naz-hage/ntools/releases)

## Version 1.7.0 - oct-24
- [issue #78](https://github.com/naz-hage/ntools/issues/78) - Feature: Add Hash Checks for File Integrity after app installation enhancement
- [issue #76](https://github.com/naz-hage/ntools/issues/76) - Feature: Enhanced Build Process with Verbose Logging and Terraform Support enhancement
- [issue #74](https://github.com/naz-hage/ntools/issues/74) - Feature: Add pwsh as MS Build task to execute PowerShell scripts with validation and logging. enhancement
- [issue #69](https://github.com/naz-hage/ntools/issues/69) - Feature: Add common devops scripts enhancement
- [issue #67](https://github.com/naz-hage/ntools/issues/67) - Feature: Add new msbuild tasks to common.targets that supports terraform enhancement
- [issue #65](https://github.com/naz-hage/ntools/issues/65) - Feature: Update development tools to latest stable versions enhancement

## Version 1.6.0 - 21-jun-24
- [issue #62](https://github.com/naz-hage/ntools/issues/62) - Feature: Rename ng.exe to ngit.exe
- 
## Version 1.5.0 - 03-may-24
- [issue #56](https://github.com/naz-hage/ntools/issues/56) - Feature: Remove the dependency on the $(DevDrive) and $(MainDir) environment variables
- [issue #55](https://github.com/naz-hage/ntools/issues/55) - Feature: Add documentation of staging and production releases with ntools

## Version 1.4.0 - 25-apr-24

- Complete [issue #37](https://github.com/naz-hage/ntools/issues/37)
- Complete [issue #38](https://github.com/naz-hage/ntools/issues/38)
- Complete [issue #34](https://github.com/naz-hage/ntools/issues/34)
- Complete [issue #44](https://github.com/naz-hage/ntools/issues/44)
- Complete [issue #41](https://github.com/naz-hage/ntools/issues/41)
 
## Version 1.3.0 - 15-feb-24
- Fix [issue #29](https://github.com/naz-hage/ntools/issues/29)
- Add default value for -json option
- Fix specifying the process start info for ntools-launcher.
  - if StartInfo.FileName uses executable name only that is in the system path, then the StartInfo.FileName will be replaced with the full pathName.
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
  - Target .netstandard2.0 project to support MS build tasks.  MS Build tasks only support .netstandard2.0. 
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
- Introduce `Ngit` project to provide a simplified and automated interface for git operations.
    - Depends on DevDrive and MainDir environment variables.  Default values are used if they don't exist. DevDrive defaults to `C:` and MainDir defaults to `C:\source`.

- Use DevDrive and MainDir from environment variables if they exist.  Otherwise, use default values.
    - This applies to common.targets and NbuildTasks
- Refactor Nbackup - remove cli options src, dest, and options and use json input only file instead.
- Update documentation
- Reference: [issue#23](https://github.com/naz-hage/ntools/issues/23)

## Next
- Delete non-prod releases from Artifacts folder
- Add https://github.com/naz-hage/learn/tree/main/dotnet/cleanup-non-prod project to NbuildTasks.  