[Next](#next)

## Version 1.2.0 - 22-jan-24
- See [issue#5](https://github.com/naz-hage/ntools/issues/5)
- Add NbuildTask to download and install applications from the web.
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
    - Sample json file: [NbuildAppListTest](./Nbuild/resources/NbuildAppListTest.json)
        
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

