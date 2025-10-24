---
title: add-issue — atools
---

# add-issue.py

This page documents the `add-issue.py` helper. Use this page for detailed prerequisites, installation, usage examples, troubleshooting, and testing.

## Purpose

`add-issue.py` parses backlog-style markdown files and creates GitHub issues or Azure DevOps work items (PBIs, Bugs, or Tasks). It supports dry-run previews and a file-first metadata policy.

## What's New in v1.38.0

- **Azure DevOps Task Support**: Create Tasks with parent PBI linking
- **Connectivity Validation**: Automatic Azure DevOps API connectivity checks
- **Enhanced Error Handling**: Better validation for required Task fields
- **Template System**: Built-in templates for PBIs, Tasks, and GitHub issues

## Prerequisites

- Python 3.13+ installed and available on PATH.
- The `requests` library is required for Azure DevOps API calls. Install via pip (see Installation).
- For GitHub target:
	- The GitHub CLI (`gh`) is required when the tool uses the CLI-based flow to create issues. Install it from https://cli.github.com/ and ensure you have authenticated (`gh auth login`).
- For Azure DevOps target:
	- Set an Azure DevOps Personal Access Token (PAT) in an environment variable named `AZURE_DEVOPS_EXT_PAT` or `AZURE_DEVOPS_PAT`.
	- Set `AZURE_DEVOPS_ORG` in your environment (e.g., `myorg`).

## Installation

1. (Optional) Create and activate a virtual environment:

```pwsh
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

2. Install runtime dependencies:

```pwsh
pip install -r atools/requirements.txt
```

3. Install `gh` (GitHub CLI) if you plan to create GitHub issues.

4. Authenticate `gh` (if creating GitHub issues interactively):

```pwsh
gh auth login
```

5. Ensure your Azure DevOps PAT and org are exported in your shell (when using `azdo` target):

```pwsh
$env:AZURE_DEVOPS_EXT_PAT = '...your PAT...'
$env:AZURE_DEVOPS_ORG = 'your-org'
$env:AZURE_DEVOPS_PROJECT = '...your Project...'
```

## Supported Templates

The tool includes built-in markdown templates for different work item types:

- **GitHub Issues**: Standard issue template with labels and assignees
- **Azure DevOps PBIs**: Product Backlog Item template with acceptance criteria
- **Azure DevOps Tasks**: Task template with parent PBI linking (new in v1.38.0)

View example templates:

```pwsh
# Show GitHub issue template
python atools/add-issue.py --help-template github

# Show Azure DevOps PBI template  
python atools/add-issue.py --help-template azdo

# Show Azure DevOps Task template (new)
python atools/add-issue.py --help-template azdo-task
```

## Installation
```

## Usage examples

- Preview (dry-run) a GitHub issue from a markdown file:

```pwsh
python atools/add-issue.py --file-path "atools/issue-gh-example.md" --target github --dry-run
```

- Create a GitHub issue (will use `gh` if available):

```pwsh
python atools/add-issue.py --file-path "atools/issue-gh-example.md" --target github
```

- Preview an Azure DevOps work item (dry-run):

```pwsh
python atools/add-issue.py --file-path "atools/issue-azdo-example.md" --target azdo --dry-run
```

- Create an Azure DevOps work item (requires PAT and org):

```pwsh
python atools/add-issue.py --file-path "atools/issue-azdo-example.md" --target azdo --project "MyProject"
```

- Preview an Azure DevOps Task (dry-run):

```pwsh
python atools/add-issue.py --file-path "atools/task-azdo-example.md" --target azdo --dry-run
```

- Create an Azure DevOps Task (requires parent PBI ID):

```pwsh
python atools/add-issue.py --file-path "atools/task-azdo-example.md" --target azdo --project "MyProject"
```

## Troubleshooting

- If the tool reports `requests` is missing, run `pip install -r atools/requirements.txt`.
- If GitHub operations fail, confirm `gh` is installed and authenticated (`gh auth status`).
- If Azure DevOps operations fail, confirm your PAT and `AZURE_DEVOPS_ORG` are set and the `--project` parameter matches an existing project.
- For Azure DevOps Tasks, ensure the `Parent ID` field references a valid PBI work item ID.
- The tool automatically validates Azure DevOps connectivity and area paths before creating work items.
- Use `--verbose` flag for detailed connection testing and API response information.

## Running the tests

From the repository root (`./ntools`) you can run the tests with pytest. Example PowerShell commands:

```pwsh
# (optional) create & activate a virtual environment
python -m venv .venv
.\.venv\Scripts\Activate.ps1

# install runtime + test deps (requests + pytest)
python -m pip install -r atools/requirements-dev.txt

# run the whole test file
python -m pytest -q atools/tests/test_add_issue_file_only.py

# or run just the GitHub dry-run test
python -m pytest -q atools/tests/test_add_issue_file_only.py::test_github_happy_path_dry_run

# or run the new Azure DevOps Task tests
python -m pytest -q atools/tests/test_add_issue_file_only.py -k "azdo"
```

## CI-safe live-create example

If you want CI to create work items automatically, gate the job and read secrets from the repo/organization secrets store. See the example in the original README for a guarded `create-issues` job snippet.
