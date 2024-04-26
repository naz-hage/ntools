

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
- Create a staging release: Creates a staging build which includes the following steps:
    - Clean the solution
    - Build the solution
    - Run tests
    - Create a staging build
    - Publish the staging build
    - Create a zip file of the staging build file

```cmd
Nb.exe staging
```
- Display available targets:  Lists all the available targets in the targets file
    
```cmd
Nb.exe -c targets
```
- See the list of available targets at [Nbuild Targets](./ntools/nbuild-targets.md)

