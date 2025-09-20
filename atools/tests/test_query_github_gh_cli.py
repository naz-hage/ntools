import json
import subprocess
from unittest import mock
from pathlib import Path

import importlib.util


# Load the add-issue script as a module (same approach as existing tests)
def _load_add_issue_module():
    base = Path(__file__).resolve().parents[1]
    script_path = base / 'add-issue.py'
    spec = importlib.util.spec_from_file_location('add_issue_module', str(script_path))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


ai = _load_add_issue_module()


def test_query_github_uses_gh_cli_and_parses_output(tmp_path, monkeypatch, capsys):
    # Prepare fake gh JSON output
    fake_output = json.dumps({
        "number": 123,
        "title": "Test issue",
        "body": "This is the body of the issue.",
        "labels": [{"name": "bug"}],
        "assignees": [{"login": "alice"}]
    })

    # Mock subprocess.run to return the fake JSON on stdout
    fake_proc = mock.Mock()
    fake_proc.stdout = fake_output
    fake_proc.returncode = 0

    def fake_run(cmd, capture_output, text, check):
        return fake_proc

    monkeypatch.setattr('subprocess.run', fake_run)

    # Monkeypatch environment to set repository
    monkeypatch.setenv('GITHUB_REPOSITORY', 'naz-hage/ntools')

    # Call query_github_issue
    ai.query_github_issue('123', mock.Mock())

    captured = capsys.readouterr()
    assert 'GitHub Issue #123' in captured.out
    assert 'Test issue' in captured.out
    assert 'bug' in captured.out
    assert 'alice' in captured.out
