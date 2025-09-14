# atools — Issue creation helpers (add-issue.py)

This small folder contains a Python CLI scaffold, `add-issue.py`, that parses backlog markdown and
creates or previews work items for GitHub and Azure DevOps.

This README documents prerequisites, installation steps, CLI options, and example usage for the tool.

## Purpose

`add-issue.py` is a convenience tool for converting human-friendly backlog markdown files into
GitHub issues or Azure DevOps work items. It supports a dry-run mode that prints the parsed fields
and a concise platform preview so you can review the output before creating items.

## Prerequisites

- Python 3.13+ installed and available on PATH. The tool was developed and tested with Python 3.13 and
  earlier 3.x versions; Python 3.13 is now the minimum supported runtime.
- The `requests` library is required for Azure DevOps API calls. Install via pip (see Installation).
- For GitHub target:
  - The GitHub CLI (`gh`) is required when the tool uses the CLI-based flow to create issues. Install it from
    https://cli.github.com/ and ensure you have authenticated (`gh auth login`). The tool will show a
    preview command when running `--dry-run` and will attempt to use `gh` for creation if present.
- For Azure DevOps target:
  - The tool uses the Azure DevOps REST API. You must set an Azure DevOps Personal Access Token (PAT)
    in an environment variable named `AZURE_DEVOPS_EXT_PAT` or `AZURE_DEVOPS_PAT`.
  - The Azure DevOps organization should be available in the environment variable `AZURE_DEVOPS_ORG` (e.g., `myorg`).
  - The `requests` library is required to talk to the Azure DevOps API.

## Installation

1. (Optional) Create and activate a virtual environment:

```pwsh
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

2. Install runtime dependencies:

```pwsh
pip install -r requirements.txt
```

3. Install `gh` (GitHub CLI) if you plan to create GitHub issues:

 - Download from https://cli.github.com/ and follow the platform-specific steps, or use your package manager.

4. Authenticate `gh` (if creating GitHub issues interactively):

```pwsh
gh auth login
```

5. Ensure your Azure DevOps PAT and org are exported in your shell (when using `azdo` target):

```pwsh
$env:AZURE_DEVOPS_EXT_PAT = '...your PAT...'
$env:AZURE_DEVOPS_ORG = 'your-org'
```

Note: For CI, configure secure pipeline variables/secrets for the PAT and set them in the environment.

## Usage examples

- Preview (dry-run) a GitHub issue from a markdown file:

```pwsh
python add-issue.py --file-path "path\to\issue-gh-example.md" --target github --dry-run
```

- Create a GitHub issue (will use `gh` if available):

```pwsh
python add-issue.py --file-path "path\to\issue-gh-example.md" --target github
```

- Preview an Azure DevOps work item (dry-run):

```pwsh
python add-issue.py --file-path "path\to\issue-azdo-example.md" --target azdo --dry-run
```

- Create an Azure DevOps work item (requires `AZURE_DEVOPS_EXT_PAT` and `AZURE_DEVOPS_ORG`):

```pwsh
python add-issue.py --file-path "path\to\issue-azdo-example.md" --target azdo --project "MyProject"
```

## Important flags

- `--dry-run` — Parse and display the extracted fields and a concise platform preview, but do not create items.
- `--file-path` — Path to the markdown file. A positional file-path is supported, but using `--file-path` is recommended.
- `--require-criteria` — Fail if the Acceptance Criteria section is missing in the markdown.

## CLI options (detailed)

Below are all supported CLI options for `add-issue.py`. For each option we list whether it is required, optional, and whether it overrides metadata found inside the markdown file.

- `--file-path, --file, -f` (optional but recommended)
  - Purpose: Path to the markdown file containing the work item.
  - Required: Not enforced by argparse so help can be shown, but the script will error if no file-path is provided when creating or previewing items.
  - Overrides file metadata: N/A — this is the source file itself (positional or named). If both positional and `--file-path` are provided, `--file-path` wins.

- Positional `file_path_pos` (optional)
  - Purpose: Convenience positional argument for the markdown file.
  - Required: Optional. If provided and `--file-path` is not specified, it will be used as `--file-path`.
  - Overrides file metadata: N/A — same as `--file-path`.

- `--target` (optional)
  - Purpose: Which platform to create the work item on. Choices: `github`, `azdo`.
  - Required: Optional; defaults to `github`.
  - Overrides file metadata: Yes — this will override a `Target:` or `## Target` metadata heading inside the markdown if present. If the markdown contains a target, the file metadata is used unless the CLI explicitly sets `--target`.

- `--dry-run` (optional)
  - Purpose: Parse and preview output without creating items.
  - Required: Optional.
  - Overrides file metadata: No — read-only preview only.

- `--query, -q` (optional)
  - Purpose: Query an existing work item by ID (GitHub issue number or AzDo work item id) and display its details.
  - Required: Optional.
  - Overrides file metadata: No.

- `--help-template` (optional)
  - Purpose: Print an example markdown template for the specified target (`azdo` or `github`).
  - Required: Optional.
  - Overrides file metadata: No.

- `--work-item-type` (optional)
  - Purpose: For AzDo, choose the work item type (`PBI`, `Bug`, `Task`).
  - Required: Optional; defaults to `PBI`.
  - Overrides file metadata: Yes — will override a `Work Item Type` or `## Work Item Type` heading in the markdown if provided.

- `--project` (optional; required for AzDo writes)
  - Purpose: Azure DevOps project name when targeting `azdo`.
  - Required: Optional for parsing/dry-run, but required at creation time for `azdo` unless the markdown provides a `Project:` metadata heading.
  - Overrides file metadata: Yes — if provided it overrides `## Project` in the markdown.

- `--assignee` (optional)
  - Purpose: Assignee/user for the created item.
  - Required: Optional.
  - Overrides file metadata: Yes — overrides `## Assignee` in the markdown.

- `--labels` (optional)
  - Purpose: Comma-separated labels/tags for GitHub or tags for AzDo.
  - Required: Optional.
  - Overrides file metadata: Yes — overrides `## Labels` in the markdown.

- `--area` (optional)
  - Purpose: Azure DevOps area path override.
  - Required: Optional.
  - Overrides file metadata: Yes — overrides `## Area` in the markdown.

- `--iteration` (optional)
  - Purpose: Azure DevOps iteration path override.
  - Required: Optional.
  - Overrides file metadata: Yes — overrides `## Iteration` in the markdown.

- `--require-criteria` (optional)
  - Purpose: Fail if Acceptance Criteria section is missing in the markdown.
  - Required: Optional.
  - Overrides file metadata: No — it enforces the presence of the AC section in the file.

- `--repo` (optional)
  - Purpose: GitHub repository owner/name (`owner/repo`).
  - Required: Optional for parsing/dry-run; required when creating GitHub issues unless the markdown provides a `Repository:` or `Repo:` metadata heading.
  - Overrides file metadata: Yes — overrides `## Repository` or `## Repo` in the markdown.

- `--config` (optional)
  - Purpose: Optional path to a config file for tokens/credentials.
  - Required: Optional.
  - Overrides file metadata: If the markdown includes a `Config` heading, CLI `--config` will override it.

Notes on precedence and behavior
- CLI flags win over markdown metadata for fields where both exist (for example `--labels`, `--assignee`, `--repo`, `--area`, `--iteration`, `--project`, `--work-item-type`).
- Metadata fields found in the markdown (for example `## Repository`, `## Project`, `## Area`) are used as defaults when CLI flags are not provided.
- The script prints the parsed fields first when using `--dry-run`; CLI flags used to override metadata are shown in the Parameters block under the parsed fields so you can confirm effective values.


## Troubleshooting

- If the tool reports `requests` is missing, run `pip install -r requirements.txt`.
- If GitHub operations fail, confirm `gh` is installed and authenticated (`gh auth status`).
- If Azure DevOps operations fail, confirm:
  - `AZURE_DEVOPS_EXT_PAT` (or `AZURE_DEVOPS_PAT`) is set and has enough scopes to create work items.
  - `AZURE_DEVOPS_ORG` is set.
  - The `--project` parameter matches an existing Azure DevOps project when targeting `azdo`.

## Development notes

- The tool is a scaffold and does not yet implement full API error handling or retries for transient errors.
- Contributions welcome: consider adding `--json` output for machine-readable dry-run previews or an interactive
  mode that confirms creation before calling `gh` or the Azure DevOps API.

