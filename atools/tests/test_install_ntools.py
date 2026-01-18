import json
import subprocess
from pathlib import Path
import sys


SCRIPT = Path(__file__).resolve().parents[1] / "install-ntools.py"


def test_build_url_from_ntools_json(tmp_path, monkeypatch):
    # create a minimal ntools.json
    cfg = {
        "Version": "1.2.3",
        "NbuildAppList": [
            {
                "Name": "Ntools",
                "WebDownloadFile": (
                    "https://example.com/releases/download/" "$(Version)/$(Version).zip"
                ),
                "DownloadedFile": "$(Version).zip",
                "InstallPath": "C:\\Nbuild",
            }
        ],
    }
    p = tmp_path / "dev-setup"
    p.mkdir()
    f = p / "ntools.json"
    f.write_text(json.dumps(cfg))

    # Run script in dry-run mode and capture output
    cmd = [
        sys.executable,
        str(SCRIPT),
        "--version",
        "2.0.0",
        "--json",
        str(f),
        "--downloads-dir",
        str(tmp_path),
        "--dry-run",
    ]
    res = subprocess.run(cmd, capture_output=True, text=True)
    assert res.returncode == 0
    out = res.stdout
    assert "Would verify URL" in out
    assert "https://example.com/releases/download/2.0.0/2.0.0.zip" in out


def test_missing_ntools_json(tmp_path):
    # point to non-existent json
    missing = tmp_path / "dev-setup" / "ntools.json"
    cmd = [sys.executable, str(SCRIPT), "--version", "1.0.0", "--json", str(missing), "--dry-run"]
    res = subprocess.run(cmd, capture_output=True, text=True)
    # script should exit non-zero and mention ntools.json not found
    assert res.returncode != 0 or "ntools.json not found" in res.stdout + res.stderr


def test_update_sdo_version(tmp_path):
    """Test the update_sdo_version function."""
    import importlib.util
    from pathlib import Path
    
    # Import the install-ntools module
    script_path = Path(__file__).parent.parent / "install-ntools.py"
    spec = importlib.util.spec_from_file_location("install_ntools", script_path)
    install_ntools = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(install_ntools)
    
    # Create a sample pyproject.toml
    pyproject_content = '''[build-system]
requires = ["setuptools>=61.0", "wheel"]
build-backend = "setuptools.build_meta"

[project]
name = "sdo"
version = "1.59.0"
description = "Simple DevOps CLI tool"
'''
    pyproject_path = tmp_path / "pyproject.toml"
    with open(pyproject_path, 'w', encoding='utf-8') as f:
        f.write(pyproject_content)
    
    # Test updating the version
    new_version = "2.0.0"
    install_ntools.update_sdo_version(new_version, pyproject_path)
    
    # Read back and check
    with open(pyproject_path, 'r', encoding='utf-8') as f:
        updated_content = f.read()
    
    assert f'version = "{new_version}"' in updated_content
