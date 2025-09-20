from pathlib import Path
from unittest import mock
import importlib.util


def _load_add_issue_module():
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


def test_azdo_requests_post_raises_exception(tmp_path, monkeypatch, capsys):
    md = '''# Test AzDO

## Target: azdo
## Project: Proto

## Description
Sample description.
'''
    p = write_md(md, tmp_path)

    validated = ai.validate_file(str(p))
    fields = ai.extract_fields_from_markdown(validated)

    args = mock.Mock()
    args.file_path = str(p)
    args.target = 'azdo'
    args.dry_run = False

    # Ensure org and PAT are present so function proceeds to POST
    monkeypatch.setenv('AZURE_DEVOPS_ORG', 'myorg')
    monkeypatch.setenv('AZURE_DEVOPS_EXT_PAT', 'fakepat')

    # Monkeypatch requests.post to raise an exception (network failure)
    def fake_post(*a, **kw):
        raise Exception('network failure')

    monkeypatch.setattr(ai.requests, 'post', fake_post)

    res = ai.create_azdo_workitem(fields, args, dry_run=False)
    captured = capsys.readouterr()

    assert res is False
    assert 'ERROR: HTTP request failed' in captured.out


def test_azdo_requests_post_returns_4xx(tmp_path, monkeypatch, capsys):
    md = '''# Test AzDO

## Target: azdo
## Project: Proto

## Description
Sample description.
'''
    p = write_md(md, tmp_path)

    validated = ai.validate_file(str(p))
    fields = ai.extract_fields_from_markdown(validated)

    args = mock.Mock()
    args.file_path = str(p)
    args.target = 'azdo'
    args.dry_run = False

    monkeypatch.setenv('AZURE_DEVOPS_ORG', 'myorg')
    monkeypatch.setenv('AZURE_DEVOPS_EXT_PAT', 'fakepat')

    class FakeResp:
        def __init__(self):
            self.status_code = 400
        def json(self):
            return {'message': 'bad request'}
        @property
        def text(self):
            return 'bad request'

    def fake_post(*a, **kw):
        return FakeResp()

    monkeypatch.setattr(ai.requests, 'post', fake_post)

    res = ai.create_azdo_workitem(fields, args, dry_run=False)
    captured = capsys.readouterr()

    assert res is False
    assert 'ERROR creating work item: HTTP 400' in captured.out


def test_github_subprocess_calledprocesserror(tmp_path, monkeypatch, capsys):
    md = '''# Test GH

## Target: github
## Repository: naz-hage/ntools

## Description
Sample description.
'''
    p = write_md(md, tmp_path)

    validated = ai.validate_file(str(p))
    fields = ai.extract_fields_from_markdown(validated)

    args = mock.Mock()
    args.file_path = str(p)
    args.target = 'github'
    args.dry_run = False

    import subprocess

    def fake_run(cmd, capture_output, text, check):
        raise subprocess.CalledProcessError(returncode=2, cmd=cmd, stderr='simulated gh error')

    monkeypatch.setattr('subprocess.run', fake_run)

    res = ai.create_github_issue(fields, args, dry_run=False)
    captured = capsys.readouterr()

    assert res is False
    assert 'ERROR: GitHub CLI command failed' in captured.out
