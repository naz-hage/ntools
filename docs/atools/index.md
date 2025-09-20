---
title: atools — Issue creation helpers
---


<!-- Full README content for atools/add-issue.py -->

# atools — Issue creation helpers (add-issue.py)

This folder contains a Python CLI tool, `add-issue.py`, that parses backlog markdown and
creates or previews work items for GitHub and Azure DevOps.

This README documents prerequisites, installation steps, the tool behavior and example usage.

## Purpose

`add-issue.py` is a convenience tool for converting human-friendly backlog markdown files into
GitHub issues or Azure DevOps work items. It supports a dry-run mode that prints the parsed fields
and a concise platform preview so you can review the output before creating items.

## Experimental

- This tool is experimental. It is functional but may change and currently lacks full API error handling,
	retries for transient errors, and other production hardening.
- Contributions welcome: consider adding `--json` output for machine-readable dry-run previews, improved
	retries/error-handling, or an interactive mode that confirms creation before calling `gh` or the Azure DevOps API.

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
$env:AZURE_DEVOPS_PROJECT = '...your Project...'
```

Note: For CI, configure secure pipeline variables/secrets for the PAT and set them in the environment.

### Removing the virtual environment

If you created a `.venv` during testing and want to remove it, simply delete the folder.

PowerShell (Windows):

```pwsh
# Deactivate first if active
deactivate
Remove-Item -Recurse -Force .venv
```

POSIX (macOS / Linux):

```bash
# Deactivate first if active
deactivate
rm -rf .venv
```

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

## Example markdown files

The repository includes example markdown files you can use with `add-issue.py`. These are not copied into the docs — use the files directly from the `atools/` folder:

- [issue-gh-example.md](https://github.com/naz-hage/ntools/blob/main/atools/issue-gh-example.md) — GitHub issue example (contains `Repository:` heading)
- [issue-azdo-example.md](https://github.com/naz-hage/ntools/blob/main/atools/issue-azdo-example.md) — Azure DevOps work item example (contains `Project:`, `Area:`, and `Iteration:` headings)

- In GitHub, labels are not created automatically. You must create any labels you want to use in the target repository before running the tool to create issues with those labels.

## Troubleshooting

- If the tool reports `requests` is missing, run `pip install -r requirements.txt`.
- If GitHub operations fail, confirm `gh` is installed and authenticated (`gh auth status`).
- If Azure DevOps operations fail, confirm:
	- `AZURE_DEVOPS_EXT_PAT` (or `AZURE_DEVOPS_PAT`) is set and has enough scopes to create work items.
	- `AZURE_DEVOPS_ORG` is set.
	- The `--project` parameter matches an existing Azure DevOps project when targeting `azdo`.


## Running the tests

From the repository root (`./ntools`) you can run the new tests with Python's pytest. Example PowerShell commands:

Note: avoid using `-q` twice (for example `-qq`) — that suppresses the final summary line. Use a single `-q` for concise output with the final summary.

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
```


## CI-safe live-create example

If you want CI to create work items (GitHub issues or AzDO work items) automatically, it's important to gate this capability and avoid exposing credentials to untrusted pull requests. The pattern below shows a guarded job that only runs on the default branch (or after manual approval) and reads secrets from the repository/organization secrets store.

Example GitHub Actions job snippet (add to `.github/workflows/ntools.yml` as a guarded job):

```yaml
jobs:
	create-issues:
		name: Create Issues (gated)
		runs-on: ubuntu-latest
		if: github.ref == 'refs/heads/main' # only run on main branch; change per your policy
		steps:
			- uses: actions/checkout@v4
			- name: Set up Python
				uses: actions/setup-python@v5
				with:
					python-version: '3.12'
			- name: Install deps
				run: |
					python -m pip install --upgrade pip
					pip install -r atools/requirements.txt
			- name: Create issues (guarded)
				env:
					AZURE_DEVOPS_EXT_PAT: ${{ secrets.AZURE_DEVOPS_EXT_PAT }}
					AZURE_DEVOPS_ORG: ${{ secrets.AZURE_DEVOPS_ORG }}
					API_GITHUB_KEY: ${{ secrets.API_GITHUB_KEY }}
				run: |
					# Example: create a GitHub issue from a file
					python atools/add-issue.py --file-path atools/issue-gh-example.md --target github

			# Secure practice: require manual approval using environments or GitHub Environments protection rules
			# (set environment protection to require reviewers) to gate any job that exposes credentials.
```

Required secrets and PAT scopes

- For Azure DevOps:
	- `AZURE_DEVOPS_EXT_PAT` or `AZURE_DEVOPS_PAT` (recommended secure secret)
	- Required scopes: Work Items (read/write) and potentially Full Access if you need to add links/attachments. Prefer minimal scopes.
	- `AZURE_DEVOPS_ORG` — organization name (string)

- For GitHub:
	- `API_GITHUB_KEY` (personal access token) — if using `gh` for creation you can also rely on `GITHUB_TOKEN` provided by GitHub Actions but be aware `GITHUB_TOKEN` has repo-scoped permissions and may be fine for creating issues; use a PAT if you need broader scopes.
	- Required scopes for creating issues: repo (or public_repo for public repos), and workflow if triggering workflows.

Guarded step guidance

- Use GitHub Environments with required reviewers to gate jobs that have access to secrets. This prevents PRs from untrusted forks from receiving secrets.
- Prefer running live-create jobs only on `main` or via manually triggered workflow_dispatch after a review.
- Avoid using `pull_request` events for live-create jobs unless you explicitly filter or require approvals.

