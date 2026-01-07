

Once Ntools is installed, Open a Developer Command prompt for Visual Studio 2022 and navigate to your solution folder (i.e. `./ntools`).  The [`nb.exe`](./ntools/nbuild.md) is the main executable for the Ntools.  The following are some examples of how to use the Ntools:

## Global Options

Ntools supports global options that work across all commands:
- `--dry-run`: Preview changes without applying them
- `--verbose`: Enable verbose output for detailed information

## Build & Test Commands

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

- Run specific unit test suites:
```cmd
nb UNIT_TEST_CLI_VALIDATION    # CLI validation tests
nb UNIT_TEST_GIT_CLONE_COMMAND # Git clone command tests
nb UNIT_TEST_ALL              # All unit tests (except long-running ones)
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

- Comprehensive smoke test: Validates published artifacts and build system integrity

```cmd
nb.exe smoke_test
```

- Display available targets:  Lists all the available targets in the targets file
    
```cmd
nb.exe targets
```

- See the complete list of available targets at [Nbuild Targets](./ntools/nbuild-targets.md)
- Learn more about code coverage at [Code Coverage](./ntools/code-coverage.md)

