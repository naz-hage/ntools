

Once Ntools is installed, open a Developer Command Prompt for Visual Studio 2026 and navigate to your solution folder (for example, `./ntools`). The [.NET SDK](https://dotnet.microsoft.com/download) must be installed and available in your PATH for build operations. The `sdo` executable is the primary command-line interface for Ntools. The following are examples of how to use it:

## Global Options

Ntools supports global options that work across all commands:
- `--dry-run`: Preview changes without applying them
- `--verbose`: Enable verbose output for detailed information

## Build & Test Commands

-   Build a solution: Compiles the solution in the solution directory

```cmd
sdo solution
```
- Clean a solution:  Deletes the release/Debug, bin and obj folders in the solution directory

```cmd
sdo clean
```
- Run ntools-launcher YAML test metadata with `sdo e2e`:

```cmd
sdo e2e
```

For solution unit tests, use the repository test target or `dotnet test`, for
example `nb UNIT_TEST_SDOTESTS`.

## YAML Commands

The SDO CLI keeps three YAML formats separate:

- `sdo run --manifest <file>` executes generic YAML Launcher workflows.
- `sdo tool <operation> --manifest <file>` manages application manifests.
- `sdo e2e --test-case <path-or-name>` executes test metadata with assertions
  and extracted variables.

### Workflow YAML

```cmd
sdo run --manifest .\workflow.yaml
```

Workflow files define `steps`, optional `variables`, and `execution` settings.
Variable references use `$(NAME)`. Tool manifests are rejected by `sdo run`.

### Tool manifests

```cmd
sdo tool list --manifest .\apps.yaml
sdo tool install --manifest .\apps.yaml --dry-run
sdo tool download --manifest .\apps.yaml --dry-run
sdo tool uninstall --manifest .\apps.yaml --dry-run
```

JSON remains supported with `--json`, and named installs remain available with
`--name` and optional `--appversion`.

### Test metadata

```cmd
sdo e2e --test-case .\metadata\Test_Validate_AzureDevOps_CreateBugFromMarkdown.yaml
sdo e2e --metadata-path .\metadata
```

Test metadata uses launcher steps with `assertions` and `extractVariables`.
The command reports each metadata result and an aggregate pass/fail summary.
Generic workflows and tool manifests are rejected with guidance to use
`sdo run` or `sdo tool`. The legacy `sdo-e2e-test test` command remains
available during migration.

- Run specific unit test suites:
```cmd
sdo UNIT_TEST_CLI_VALIDATION    # CLI validation tests
sdo UNIT_TEST_GIT_CLONE_COMMAND # Git clone command tests
sdo UNIT_TEST_ALL              # All unit tests (except long-running ones)
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
sdo stage
```

- Comprehensive smoke test: Validates published artifacts and build system integrity

```cmd
sdo smoke_test
```

- Display available targets:  Lists all the available targets in the targets file
    
```cmd
sdo targets
```

- See the complete list of available targets at [Nbuild Targets](./nbuild-targets.md)
- Learn more about code coverage at [Code Coverage](./code-coverage.md)

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

