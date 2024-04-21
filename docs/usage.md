## Usage (examples)

Once Ntools is installed, Open a Developer Command prompt for Visual Studio 2022 and navigate to your solution folder (i.e. `c:\source\ntools`).  The [`Nb.exe`](./ntools/nbuild.md) is the main executable for the Ntools.  The following are some examples of how to use the Ntools:

-   Build a solution:

```cmd
Nb.exe solution
```
- Clean a solution:

```cmd
Nb.exe clean
```
- Run tests on a solution:

```cmd
Nb.exe test
```
- Create a staging build which includes the following steps:
    - Clean the solution
    - Build the solution
    - Run tests
    - Create a staging build
    - Publish the staging build
    - Create a zip file of the staging build file

```cmd
Nb.exe staging
```
- Display available command line options:
    
```cmd
Nb.exe -c targets
```
- See the list of available targets at [Nbuild Targets](./ntools/nbuild-targets.md)

