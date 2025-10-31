from unittest import mock
from pathlib import Path
import importlib.util


def _load_add_issue_module():
    base = Path(__file__).resolve().parents[1]
    script_path = base / 'add-issue.py'
    spec = importlib.util.spec_from_file_location('add_issue_module', str(script_path))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


ai = _load_add_issue_module()


def test_create_github_invokes_gh_with_expected_args(tmp_path, monkeypatch, capsys):
    # Create a minimal markdown file with repository metadata
    md = '''# Test Issue

## Target: github
## Repository: naz-hage/ntools

## Description

Sample description.
'''
    p = tmp_path / 'issue.md'
    p.write_text(md, encoding='utf-8')

    # Prepare fields via the real extraction path
    validated = ai.validate_file(str(p))
    fields = ai.extract_fields_from_markdown(validated)

    # Build args object similar to parse_args()
    args = mock.Mock()
    args.file_path = str(p)
    args.target = 'github'
    args.dry_run = False

    # Capture the command that subprocess.run receives by mocking it
    captured_cmd = {}

    def fake_run(cmd, capture_output, text, check):
        # Record the command for assertions
        captured_cmd['cmd'] = cmd
        # Simulate successful gh output (URL)
        fake = mock.Mock()
        fake.stdout = 'https://github.com/naz-hage/ntools/issues/999'
        return fake

    monkeypatch.setattr('subprocess.run', fake_run)

    # Call the create function (non-dry-run path should call subprocess.run)
    res = ai.create_github_issue(fields, args, dry_run=False)

    assert res is True
    assert 'cmd' in captured_cmd
    cmd = captured_cmd['cmd']
    # first elements should indicate the gh invocation
    assert cmd[0] == 'gh'
    assert cmd[1] == 'issue'
    assert cmd[2] == 'create'
    # ensure title and repo flags are present
    assert '--title' in cmd
    assert '--body-file' in cmd
    assert '--repo' in cmd
