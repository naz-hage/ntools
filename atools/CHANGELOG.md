# Changelog — atools/add-issue.py

All notable changes to the `add-issue.py` helper are recorded in this file.

## [Unreleased]

- 2025-09-17: Enforce "file-first" policy: the CLI no longer accepts overrides for project/work-item-type/assignee/labels/area/iteration/require-criteria/repo/config. All creation metadata must be provided in the markdown file passed with `--file-path`.
- 2025-09-17: GitHub Projects CLI wiring removed to avoid ambiguity with Projects V2; users should manage Projects via `gh` directly.
- 2025-09-17: Acceptance Criteria handling for Azure DevOps: AC is written only into a dedicated Acceptance Criteria field when present; otherwise AC will not be written to the created work item.

## 0.1.0 - initial

- Initial scaffold and experimental behavior.
