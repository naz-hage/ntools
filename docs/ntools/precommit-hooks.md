# Pre-commit Hooks for ntools

This guide explains how to enable, use, and troubleshoot pre-commit hooks in the ntools project.

## What are Pre-commit Hooks?
Pre-commit hooks are scripts that run automatically before each commit to enforce code quality, documentation, and version consistency. They help catch issues early and keep the repository clean.

## How to Enable Pre-commit Hooks

1. Ensure you have Python and `pip` installed.
2. From the repo root or `dev-setup` folder, run:
   ```powershell
   ./dev-setup/install-precommit-hooks.ps1
   ```
   This will install the pre-commit framework and set up the hooks defined in `.pre-commit-config.yaml`.

## What Hooks Are Included?
- Trailing whitespace, end-of-file, and merge conflict checks
- JSON/YAML validation
- C# and PowerShell formatting
- Automated documentation version updates (via `nb update_doc_versions`)
- Commit message validation

## Usage
- Hooks run automatically on `git commit`.
- If a hook fails, fix the reported issue and re-commit.
- To skip hooks (not recommended):
  ```sh
  git commit --no-verify
  ```

## Troubleshooting
- If you see errors about missing tools, ensure you have Python, pip, and .NET SDK installed.
- To debug hooks:
  ```sh
  pre-commit run --all-files --verbose
  ```
- For more info, see [pre-commit.com](https://pre-commit.com/).

## Updating Hooks
- To update hook versions or add new checks, edit `.pre-commit-config.yaml` and re-run:
  ```sh
  pre-commit autoupdate
  pre-commit install
  ```

---
For questions, see the PBI or ask in the project chat.
