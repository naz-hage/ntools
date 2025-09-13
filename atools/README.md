# atools — Issue creation helpers (add-issue.py)

This small folder contains a Python CLI scaffold, `add-issue.py`, that parses backlog markdown and
creates or previews work items for GitHub and Azure DevOps.

This README documents prerequisites, installation steps, and example usage for the tool.

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

## License

See the repository `LICENSE` (if present) for licensing details.
