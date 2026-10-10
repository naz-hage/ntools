
# SDO Usage

SDO provides the `sdo` command-line tool for build automation, YAML
workflows, tool management, and DevOps operations.

## Prerequisites

- Install the [.NET SDK](https://dotnet.microsoft.com/download).
- Install SDO and open a Developer Command Prompt for Visual Studio.
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

- `sdo run [--manifest <file>]` runs a YAML Launcher workflow. The default
  manifest is `sdo.yaml` in the current directory.
- `sdo tool <operation> --manifest <file>` manages JSON or YAML tool manifests.
- `sdo e2e --test-case <path-or-name>` runs test metadata with assertions.

To execute a named stage or selected steps, use exactly one selection option:

```cmd
sdo run
sdo run --manifest workflow.yaml --stage release
sdo run --manifest workflow.yaml --step build
sdo run --manifest workflow.yaml --step-index 2 0
```

`--stage` executes a named, ordered group of step names. `--step` executes
one uniquely named step. `--step-index` accepts multiple zero-based indices
and preserves their requested order. Stage names, referenced step names, and
indices are validated before any selected step starts. These options are
mutually exclusive.

Stages are defined beside the top-level `steps` collection:

```yaml
stages:
  - name: release
    steps:
      - clean
      - build
      - publish
```

## Next Steps

See the [sdo CLI reference](./sdo-net.md) for all commands, options, examples,
configuration, build targets, and troubleshooting guidance.

- [SDO targets](./sdo-targets.md)
- [Code coverage](./code-coverage.md)
