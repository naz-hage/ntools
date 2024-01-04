[Next](#next)

## Version 1.1.0 - 05-jan-24
- 
- Add `Nbuild` project to facilitate building of ntools and other projects.
- Add `NbuildTasks` project that exposed MS build tasks.
    - Tasks include:
        - Git.GetTag
        - Git.SetTag
        - Git.Autotag
        - Git.PushTag
        - Git.DeleteTag
        - Git.GetBranch
- Add Git Wrapper class to facilitate git operations.
- Target .netstandard2.0 for Launcher project.  This allows NbuildTasks to use it 
- Refactor Launcher tests
- Refactor Nbackup - remove cli options src, dest, and options and use json input only file instead.
- Publish Launcher 1.1.0 to nuget.org and unlist 1.0.0.5
- Update documentation
- Reference: [issue#23](https://github.com/naz-hage/ntools/issues/23)

