

Once Ntools is installed, Open a Developer Command prompt for Visual Studio 2022 and navigate to your solution folder (i.e. `./ntools`). The [.NET SDK](https://dotnet.microsoft.com/download) must be installed and available in your PATH for build operations. The [`nb.exe`](./nbuild.md) is the main executable for the Ntools.  The following are some examples of how to use the Ntools:

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

- See the complete list of available targets at [Nbuild Targets](./nbuild-targets.md)
- Learn more about code coverage at [Code Coverage](./code-coverage.md)

## E2E Testing Commands (Advanced Automation Features)

Run end-to-end tests for validating cross-platform SDO operations:

- Run Azure DevOps work item filtering tests:

```cmd
nb RUN_AZDO_WI_ASSIGNED_TO_ME_TEST
```

- Run GitHub issue filtering tests:

```cmd
nb RUN_GITHUB_WI_ASSIGNED_TO_ME_TEST
```

- Run Azure DevOps pipeline operation tests:

```cmd
nb RUN_AZDO_PIPELINE_TEST
```

- Run GitHub Actions operation tests:

```cmd
nb RUN_GITHUB_PIPELINE_TEST
```

**Output**: Color-coded console (green for [SUCCESS], red for [ERROR]) with detailed logging to `sdo-e2e-test.log`

- See the complete list of E2E targets at [Nbuild Targets - E2E Testing](./nbuild-targets.md#e2e-testing-targets)

## SDO Configuration Management (Advanced Automation Features)

Work item queries can be standardized using YAML configuration files. See [SDO Configuration System](./sdo-net.md#configuration-system-yaml-based) for detailed documentation.

**Quick Start**:

1. Create `sdo-config.yaml` in your project's `.temp` folder:

```yaml
commands:
  wi:
    list:
      area_path: "MyProject\\Backend"
      state: "In Progress"
      top: 20
```

2. Run `sdo wi list` from the project directory to use configuration defaults automatically

**Configuration Priority** (highest to lowest):
1. CLI parameters: `sdo wi list --state "Done"`
2. Config file defaults: `sdo-config.yaml`
3. Hard-coded defaults in code

For more details, see:
- [Configuration System Documentation](./sdo-net.md#configuration-system-yaml-based)
- [Markdown Parser for Content Creation](./sdo-net.md#markdown-parser-for-content-creation)

