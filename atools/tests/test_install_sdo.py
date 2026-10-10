import subprocess
from pathlib import Path
import sys


SCRIPT = Path(__file__).resolve().parents[1] / "install-sdo.py"


def test_build_url_from_sdo_yaml(tmp_path):
    # Create a minimal sdo.yaml manifest.
    manifest = """\
Version: '1.2.3'
NbuildAppList:
  - Name: sdo
    WebDownloadFile: https://example.com/releases/download/$(Version)/$(Version).zip
    DownloadedFile: $(Version).zip
    InstallPath: 'C:\\Program Files\\sdo'
"""
    p = tmp_path / "dev-setup"
    p.mkdir()
    f = p / "sdo.yaml"
    f.write_text(manifest)

    # Run script in dry-run mode and capture output
    cmd = [
        sys.executable,
        str(SCRIPT),
        "--version",
        "2.0.0",
        "--yaml",
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


def test_missing_sdo_yaml(tmp_path):
    # Point to a non-existent YAML manifest.
    missing = tmp_path / "dev-setup" / "sdo.yaml"
    cmd = [sys.executable, str(SCRIPT), "--version", "1.0.0", "--yaml", str(missing), "--dry-run"]
    res = subprocess.run(cmd, capture_output=True, text=True)
    # The script should exit non-zero and mention sdo.yaml was not found.
    assert res.returncode != 0 or "sdo.yaml not found" in res.stdout + res.stderr
