

Once Ntools is installed, Open a Developer Command prompt for Visual Studio 2022 and navigate to your solution folder (i.e. `c:\source\ntools`).  The [`nb.exe`](./ntools/nbuild.md) is the main executable for the Ntools.  The following are some examples of how to use the Ntools:

-   Build a solution: Compiles the solution in the solution directory

```cmd
nb.exe solution
```
- Clean a solution:  Deletes the release/Debug, bin and obj folders in the solution directory

```cmd
nb.exe clean
```
- Test solution: runs all the tests in the solution

```cmd
nb.exe test
```
- Create a stage release: Creates a stage build which includes the following steps:
    - Clean the solution
    - Build the solution
    - Run tests
    - Create a stage build
    - Publish the stage build
    - Create a zip file of the stage build file

```cmd
nb.exe stage
```
- Display available targets:  Lists all the available targets in the targets file
    
```cmd
nb.exe targets
```
- See the list of available targets at [Nbuild Targets](./ntools/nbuild-targets.md)

