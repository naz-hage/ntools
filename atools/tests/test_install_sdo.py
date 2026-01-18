import json
import subprocess
import sys
from pathlib import Path
import tempfile
import os
import pytest


SCRIPT = Path(__file__).resolve().parents[1] / "install-sdo.py"


class TestUninstallVenv:
    """Test cases for the uninstall_venv function via script execution."""

    def test_uninstall_nonexistent_venv(self, tmp_path):
        """Test uninstalling a virtual environment that doesn't exist."""
        nonexistent_venv = tmp_path / "nonexistent_venv"

        cmd = [
            sys.executable,
            str(SCRIPT),
            "--uninstall",
            "--source-path", str(tmp_path),  # dummy source path
            "--nbuild-path", str(tmp_path),
        ]
        env = os.environ.copy()
        env['PYTHONIOENCODING'] = 'utf-8'
        res = subprocess.run(cmd, capture_output=True, text=True, cwd=str(tmp_path), env=env, encoding='utf-8', errors='replace')
        assert res.returncode == 0
        assert "Virtual environment not found" in res.stdout

    def test_uninstall_dry_run(self, tmp_path):
        """Test dry-run mode for uninstall."""
        venv_path = tmp_path / "venv"  # Use the expected path
        venv_path.mkdir()  # Create empty directory

        cmd = [
            sys.executable,
            str(SCRIPT),
            "--uninstall",
            "--dry-run",
            "--source-path", str(tmp_path),
            "--nbuild-path", str(tmp_path),  # target_path is tmp_path
        ]
        env = os.environ.copy()
        env['PYTHONIOENCODING'] = 'utf-8'
        res = subprocess.run(cmd, capture_output=True, text=True, cwd=str(tmp_path), env=env, encoding='utf-8', errors='replace')
        assert res.returncode == 0
        assert f"Would delete virtual environment at: {venv_path}" in res.stdout

    def test_uninstall_with_processes_windows(self, tmp_path, monkeypatch):
        """Test process stopping on Windows (mocked)."""
        if sys.platform != 'win32':
            pytest.skip("Windows-specific test")

        venv_path = tmp_path / "venv"
        venv_path.mkdir()

        # Mock subprocess.run for PowerShell
        original_run = subprocess.run

        def mock_run(cmd, **kwargs):
            if cmd[0] == 'powershell':
                # Simulate successful process stopping
                result = subprocess.CompletedProcess(
                    cmd, 0, stdout="Stopped processes", stderr=""
                )
                return result
            return original_run(cmd, **kwargs)

        monkeypatch.setattr(subprocess, 'run', mock_run)

        cmd = [
            sys.executable,
            str(SCRIPT),
            "--uninstall",
            "--source-path", str(tmp_path),
            "--nbuild-path", str(tmp_path),
        ]
        env = os.environ.copy()
        env['PYTHONIOENCODING'] = 'utf-8'
        res = subprocess.run(cmd, capture_output=True, text=True, cwd=str(tmp_path), env=env, encoding='utf-8', errors='replace')
        assert res.returncode == 0
        assert "Process stopping command completed" in res.stdout

    def test_uninstall_basic_functionality(self, tmp_path):
        """Test basic uninstall functionality works."""
        venv_path = tmp_path / "venv"
        venv_path.mkdir()

        cmd = [
            sys.executable,
            str(SCRIPT),
            "--uninstall",
            "--source-path", str(tmp_path),
            "--nbuild-path", str(tmp_path),
        ]
        env = os.environ.copy()
        env['PYTHONIOENCODING'] = 'utf-8'
        res = subprocess.run(cmd, capture_output=True, text=True, cwd=str(tmp_path), env=env, encoding='utf-8', errors='replace')
        assert res.returncode == 0
        # Just verify the uninstall completed successfully
        assert "Virtual environment deleted successfully" in res.stdout


def test_update_sdo_version(tmp_path):
    """Test the update_sdo_version function."""
    import importlib.util
    from pathlib import Path
    
    # Import the install-sdo module
    script_path = Path(__file__).parent.parent / "install-sdo.py"
    spec = importlib.util.spec_from_file_location("install_sdo", script_path)
    install_sdo = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(install_sdo)
    
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
    install_sdo.update_sdo_version(new_version, pyproject_path)
    
    # Read back and check
    with open(pyproject_path, 'r', encoding='utf-8') as f:
        updated_content = f.read()
    
    assert f'version = "{new_version}"' in updated_content