# SDO

SDO (Simple DevOps Operations) is a single command-line utility for .NET
development and repository automation. It brings build and test execution,
Git and GitHub operations, workflows, and Azure DevOps or
GitHub project management together under one consistent `sdo` command.

## Authentication

- **Azure DevOps**: Set the `AZURE_DEVOPS_PAT` environment variable.
- **GitHub**: Use GitHub CLI authentication or set the `GITHUB_TOKEN`
  environment variable.

- Checkout the [documentation](https://naz-hage.github.io/sdo/) for more information.

- The [installation](https://naz-hage.github.io/sdo/installation/) process is straightforward, and the tools are highly reliable and efficient, ensuring the safety and integrity of your data.

- See the [SDO features](https://naz-hage.github.io/sdo/features/) for an overview of the utility's capabilities.

- See the [`sdo` usage documentation](https://naz-hage.github.io/sdo/usage/) for command details and examples.

- Don't hesitate to write an [issue](https://github.com/naz-hage/sdo/issues) if you have any questions or suggestions.

## Workflows and Metadata

### YAML Workflows

`sdo.yaml` is being introduced to replace `sdo.targets` as the basis for the
build and test lifecycle. Run an ordered YAML Launcher workflow with
`sdo run`. Without `--manifest`, it loads `sdo.yaml` from the current
directory. The manifest must define at least one step; variables use
`$(NAME)` placeholders.

```bash
sdo run
sdo run --manifest workflow.yaml
sdo run --manifest workflow.yaml --stage release
sdo run --manifest workflow.yaml --step build
sdo run --manifest workflow.yaml --step-index 2 0
```

```yaml
version: '1.0'
description: Build the project
variables:
  CONFIGURATION: Release
execution:
  timeout: 00:10:00
  stopOnFirstError: true
steps:
  - name: Build
    path: dotnet
    arguments: build --configuration $(CONFIGURATION)
    workingDirectory: .
    expectedReturnCode: 0
```

`execution.stopOnFirstError` controls workflow continuation. It defaults to `true`, so a failed step stops the workflow. Set it to `false` to run subsequent steps; the workflow still returns a non-zero exit code when any step fails.

Use one selection option to execute only part of a workflow:

- `--stage <name>` executes a named stage.
- `--step <name>` executes one uniquely named step.
- `--step-index <index>...` executes multiple zero-based step indices in the
  order supplied.

Stages are ordered groups of existing step names defined beside `steps`:

```yaml
stages:
  - name: release
    steps:
      - clean
      - build
      - publish
```

The selected stage or steps are validated before execution. Do not combine
`--stage`, `--step`, and `--step-index`.

Tool manifests such as `apps.yaml` are handled by `sdo tool`, not `sdo run`.

#### YAML Tool Manifests

`sdo tool` accepts the existing JSON manifest format and the equivalent YAML
format. Use `--manifest` for either format; `--json` remains available for
existing scripts.

```bash
sdo tool list --manifest .\dev-setup\apps.yaml
sdo tool install --manifest .\dev-setup\apps.yaml --dry-run
sdo tool download --manifest .\dev-setup\apps.yaml --dry-run
sdo tool uninstall --manifest .\dev-setup\apps.yaml --dry-run

sdo tool install --name "Git for Windows" --appversion 2.51.1 --dry-run
```

Tool manifests contain `Version`, `DownloadPath`, and `NbuildAppList`.
Use `sdo run` for workflow YAML and `sdo e2e` for test metadata YAML.

#### YAML Test Metadata

Run one test metadata file by path or name, or discover the migrated suite:

```bash
sdo e2e --test-case .\metadata\Test_Validate_AzureDevOps_CreateBugFromMarkdown.yaml
sdo e2e --metadata-path .\metadata
sdo e2e
```

Test metadata is distinct from workflow YAML because its steps may define
assertions and extracted variables such as `{bug_id}`. Use `sdo run` for
generic workflows and `sdo tool` for application manifests. The legacy
`sdo-e2e-test test` command remains available during migration; new scripts
and CI should use `sdo e2e`. The compatibility executable can be retired
after existing callers have migrated.

**Example GitHub Actions workflow:**
```yml
- name: Build using sdo
  run: |
    & "$env:ProgramFilesPath/nbuild/sdo.exe" ${{ env.Build_Type }} -v ${{ env.Enable_Logging }}
  shell: pwsh
  working-directory: ${{ github.workspace }}
```

### Next Ideas
- e2e testing framework integration
  - Implement e2e testing framework integration.
  - Note: This integration will leverage the existing StepExecution mechanism in SDO for seamless e2e testing.
- `sdo` commands/cli options enhancements
   `sdo run` command
    - define where comments are added for stage or step
    - add option to list steps and stage
    - add alias -s for --step
    - add alias -g for --stage
    - define what step is used as the default step when --step is not specified
- `sdo tool` command
    - Add deprecation warning for json file input
    - Deprecate json file input after one release cycle
    - Make yaml the default input format
- `sdo setup` command
    - Add setup option to configure a new machine for development
    - Add option to install required dependencies automatically
    - Add option to configure environment variables for development
    - Add option to set up project-specific configurations
    - Add option to configure development tools and IDE settings
    - Add option to set up version control and repository settings
    - Add option to configure project-specific scripts and automation