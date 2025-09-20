import os
import tempfile
from pathlib import Path
import pytest

import importlib.util
from pathlib import Path


# Load the script directly (no package shim required). This allows tests to import
# the `add-issue.py` file even though its filename contains a hyphen.
def _load_add_issue_module():
    # tests are located at atools/tests; add-issue.py is at atools/add-issue.py
    base = Path(__file__).resolve().parents[1]
    script_path = base / 'add-issue.py'
    spec = importlib.util.spec_from_file_location('add_issue_module', str(script_path))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


ai = _load_add_issue_module()


def write_md(content: str, tmp_path: Path) -> Path:
    p = tmp_path / 'issue.md'
    p.write_text(content, encoding='utf-8')
    return p


def test_github_happy_path_dry_run(tmp_path, capsys):
    md = '''# Sample GitHub Issue

## Target: github
## Repository: naz-hage/naz
## Labels: test, automated

## Description

This is a test issue.

## Acceptance Criteria
- [ ] sample item
'''
    p = write_md(md, tmp_path)

    import sys as _sys
    # Ensure the parser does not accidentally consume pytest's CLI args
    _sys_argv_backup = _sys.argv
    _sys.argv = ['add-issue.py']
    try:
        parser, args = ai.parse_args()
    finally:
        _sys.argv = _sys_argv_backup
    args.file_path = str(p)
    args.target = 'github'
    args.dry_run = True

    # Call core functions: validate + extract + dry-run writer
    validated = ai.validate_file(args.file_path)
    fields = ai.extract_fields_from_markdown(validated)

    # Should not raise; create_github_issue should return True in dry-run
    res = ai.create_github_issue(fields, args, dry_run=True)
    assert res is True

    captured = capsys.readouterr()
    assert 'Sample GitHub Issue' in captured.out
    assert '[dry-run] Command to execute' in captured.out or 'Would create GitHub issue' in captured.out


def test_azdo_missing_ac_field_warns_and_dry_run(tmp_path, capsys, monkeypatch):
    md = '''# PBI-123: AzDO item without AC field

## Target: azdo
## Project: Proto
## Area: Proto\\Warriors
## Iteration: Proto\\Sprint 1

## Description

Short description here.

## Acceptance Criteria
- [ ] Do something
'''
    p = write_md(md, tmp_path)

    # Ensure AZURE_DEVOPS_ORG env is present for dry-run URL generation
    monkeypatch.setenv('AZURE_DEVOPS_ORG', 'nazh')

    import sys as _sys
    _sys_argv_backup = _sys.argv
    _sys.argv = ['add-issue.py']
    try:
        parser, args = ai.parse_args()
    finally:
        _sys.argv = _sys_argv_backup
    args.file_path = str(p)
    args.target = 'azdo'
    args.dry_run = True

    validated = ai.validate_file(args.file_path)
    fields = ai.extract_fields_from_markdown(validated)

    # Dry-run should return True and print acceptance criteria and a notice
    res = ai.create_azdo_workitem(fields, args, dry_run=True)
    assert res is True

    captured = capsys.readouterr()
    assert 'Acceptance Criteria' in captured.out
    assert 'Request URL' in captured.out


def test_github_missing_ac_handled_gracefully(tmp_path, capsys):
    md = '''# Missing AC GitHub Issue

## Target: github
## Repository: naz-hage/naz

## Description

No acceptance criteria here.
'''
    p = write_md(md, tmp_path)

    import sys as _sys
    _sys_argv_backup = _sys.argv
    _sys.argv = ['add-issue.py']
    try:
        parser, args = ai.parse_args()
    finally:
        _sys.argv = _sys_argv_backup
    args.file_path = str(p)
    args.target = 'github'
    args.dry_run = True

    validated = ai.validate_file(args.file_path)
    fields = ai.extract_fields_from_markdown(validated)

    # AC list should be empty
    assert fields.get('acceptance_criteria') == []

    # Print parsed fields first so the writer will show a concise command preview
    ai.dry_run_print(fields, args)
    res = ai.create_github_issue(fields, args, dry_run=True)
    assert res is True

    captured = capsys.readouterr()
    # parsed-fields summary prints Acceptance Criteria and shows '(none)'
    assert 'Acceptance Criteria' in captured.out
    assert '(none)' in captured.out


def test_azdo_missing_ac_handled_gracefully(tmp_path, capsys, monkeypatch):
    md = '''# Missing AC AzDO

## Target: azdo
## Project: Proto
## Area: Proto\\Warriors
## Iteration: Proto\\Sprint 1

## Description

No acceptance criteria present in this file.
'''
    p = write_md(md, tmp_path)

    monkeypatch.setenv('AZURE_DEVOPS_ORG', 'nazh')

    import sys as _sys
    _sys_argv_backup = _sys.argv
    _sys.argv = ['add-issue.py']
    try:
        parser, args = ai.parse_args()
    finally:
        _sys.argv = _sys_argv_backup
    args.file_path = str(p)
    args.target = 'azdo'
    args.dry_run = True

    validated = ai.validate_file(args.file_path)
    fields = ai.extract_fields_from_markdown(validated)

    # AC list should be empty
    assert fields.get('acceptance_criteria') == []

    # Print parsed fields first so the writer will show the concise payload/URL
    ai.dry_run_print(fields, args)
    res = ai.create_azdo_workitem(fields, args, dry_run=True)
    assert res is True

    captured = capsys.readouterr()
    # parsed-fields summary prints Acceptance Criteria and shows '(none)'
    assert 'Acceptance Criteria' in captured.out
    assert '(none)' in captured.out
