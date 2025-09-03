

Once Ntools is installed, Open a Developer Command prompt for Visual Studio 2022 and navigate to your solution folder (i.e. `c:\source\ntools`).  The [`nb.exe`](./ntools/nbuild.md) is the main executable for the Ntools.  The following are some examples of how to use the Ntools:

-   Build a solution: Compiles the solution in the solution directory

```cmd
nb.exe solution
```
- Clean a solution:  Deletes the release/Debug, bin and obj folders in the solution directory

```cmd
nb.exe clean
```
- Test solution: runs all the tests in the solution with optional code coverage

```cmd
nb.exe test
```
- Run tests with code coverage disabled (for faster execution):

```cmd
nb.exe test -p:EnableCodeCoverage=false
```
- Generate code coverage reports:

```cmd
nb.exe coverage
```
- View code coverage summary:

```cmd
nb.exe coverage_summary
```
- Verify build artifacts:

```cmd
nb.exe smoke_test
```
- Create a stage release: Creates a stage build which includes the following steps:
    - Clean the solution
    - Build the solution
    - Run tests with code coverage
    - Generate coverage reports
    - Publish the stage build
    - Verify artifacts with smoke tests
    - Create a zip file of the stage build file

```cmd
nb.exe stage
```
- Display available targets:  Lists all the available targets in the targets file
    
```cmd
nb.exe targets
```

## Enhanced Features

### Code Coverage
The build system now includes comprehensive code coverage support:
- Automatic coverage collection during test runs
- HTML and text coverage reports
- Configurable assembly and class filters
- CI/CD integration support

### Artifact Verification
- Automated smoke testing of build artifacts
- PowerShell-based verification scripts
- Comprehensive artifact structure validation

### Build Pipeline
The enhanced build pipeline provides:
- Integrated test execution with coverage
- Automated artifact verification
- Streamlined publishing process
- Better error handling and diagnostics

- See the complete list of available targets at [Nbuild Targets](./ntools/nbuild-targets.md)
- Learn more about code coverage at [Code Coverage](./ntools/code-coverage.md)

