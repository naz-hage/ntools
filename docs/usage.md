
# Ntools Usage

Ntools provides the `sdo` command-line tool for build automation, YAML
workflows, tool management, and DevOps operations.

## Prerequisites

- Install the [.NET SDK](https://dotnet.microsoft.com/download).
- Install Ntools and open a Developer Command Prompt for Visual Studio.
- Change to the solution or repository directory.

## First Run

Build and clean the current solution:

```cmd
sdo solution
sdo clean
```

Run unit tests with the repository target or `dotnet test`:

```cmd
sdo TEST
dotnet test
```

Use `--verbose` for diagnostic output and `--dry-run` to preview supported
operations.

## YAML Workflows

The YAML command families have separate purposes:

- `sdo run --manifest <file>` runs a YAML Launcher workflow.
- `sdo tool <operation> --manifest <file>` manages JSON or YAML tool manifests.
- `sdo e2e --test-case <path-or-name>` runs test metadata with assertions.

## Next Steps

See the [sdo CLI reference](./sdo-net.md) for all commands, options, examples,
configuration, build targets, and troubleshooting guidance.

- [Nbuild targets](./nbuild-targets.md)
- [Code coverage](./code-coverage.md)

