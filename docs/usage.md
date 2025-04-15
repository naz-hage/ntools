Once Ntools is installed, Open a Developer Command prompt for Visual Studio 2022 and navigate to your solution folder (i.e. `c:\source\ntools`).  The [`Nb.exe`](./ntools/nbuild.md) is the main executable for the Ntools.  The following are some examples of how to use the Ntools:

-   Build a solution: Compiles the solution in the solution directory

```cmd
Nb.exe solution
```
- Clean a solution:  Deletes the release/Debug, bin and obj folders in the solution directory

```cmd
Nb.exe clean
```
- Test solution: runs all the tests in the solution

```cmd
Nb.exe test
```
- Create a stage release: Creates a stage build which includes the following steps:
    - Clean the solution
    - Build the solution
    - Run tests
    - Create a stage build
    - Publish the stage build
    - Create a zip file of the stage build file

```cmd
Nb.exe stage
```
- Display available targets:  Lists all the available targets in the targets file
    
```cmd
Nb.exe targets
```
- See the list of available targets at [Nbuild Targets](./ntools/nbuild-targets.md)

## Unified Tool Usage

The `nb.exe` tool now combines functionalities for build automation, Git enhancements, and GitHub release management. Below are examples of how to use the unified tool:

### Build Commands
```plaintext
nb list -json tools.json
nb install -json tools.json
nb download -json tools.json
nb targets
nb path
```

### Git Commands
```plaintext
nb git tag
nb git settag --tag 1.0.0
nb git autotag --buildtype STAGE
nb git branch
nb git clone --url https://github.com/example/repo.git
nb git deletetag --tag 1.0.0
```

### Release Commands
```plaintext
nb release create --repo userName/my-repo --tag 1.0.0 --branch main --file C:\Releases\1.0.0.zip
nb release download --repo userName/my-repo --tag 1.0.0 --path C:\Downloads
nb release list --repo userName/my-repo
```

For more details, use the `--help` option with any command or subcommand (e.g., `nb git --help`, `nb release --help`).

