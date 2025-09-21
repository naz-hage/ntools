#!/usr/bin/env python3
"""Install NTools release from GitHub asset ZIP.

This script is a cross-platform replacement for the PowerShell Install-NTools function.

Features
- Read release metadata from dev-setup/ntools.json
- Build download URL for the requested version
- Verify the asset exists (HEAD request)
- Download the ZIP to downloads directory
- Extract into deployment path
- Update PATH or print guidance

Usage: run with --help for options
"""
import argparse
import json
import os
import sys
from pathlib import Path
from urllib.parse import urlparse
import requests
import zipfile
import shutil

# Tool identity/version - update __version__ when releasing a new tool version
__version__ = '0.1.0'
TOOL_NAME = Path(__file__).stem
TOOL_VERSION = __version__
def _print_header_local(tool_name: str, tool_version: str, start_year: int = 2020, owner: str = 'naz-ahmad'):
    from datetime import datetime
    start_year = int(start_year)
    current_year = datetime.now().year
    years = f"{start_year}-{current_year}" if current_year != start_year else f"{start_year}"
    print(f"*** {tool_name} (experimental), Build automation, {owner}, {years} -  Version: {tool_version} ***")
    print('')


_print_header_local(TOOL_NAME, TOOL_VERSION)

def parse_args():
    parser = argparse.ArgumentParser(description="Install NTools from release ZIP (cross-platform)")
    parser.add_argument('--version', required=True, help='Release version to install (e.g. 1.32.0)')
    default_downloads = 'C:\\NToolsDownloads' if os.name == 'nt' else '/tmp/NToolsDownloads'
    parser.add_argument('--downloads-dir', default=default_downloads, help=f'Download directory (default: {default_downloads})')
    parser.add_argument('--json', '--ntools-json-path', dest='ntools_json_path', default=str(Path(__file__).resolve().parents[1] / 'dev-setup' / 'ntools.json'), help='Path to ntools.json (default: ./dev-setup/ntools.json)')
    parser.add_argument('--deploy-path', default=None, help='Deployment path (default from ntools.json InstallPath or platform default)')
    parser.add_argument('--dry-run', action='store_true', help='Do not perform network calls or write actions; print what would be done')
    parser.add_argument('--no-path-update', action='store_true', help='Do not attempt to update PATH; print instructions instead')
    return parser.parse_args()


def load_ntools_json(path: Path):
    if not path.exists():
        raise FileNotFoundError(f"ntools.json not found at: {path}")
    with path.open('r', encoding='utf-8') as f:
        data = json.load(f)
    return data


def build_asset_url(ntools_json: dict, version: str):
    # find first entry in NbuildAppList with Name matching Ntools or first entry
    apps = ntools_json.get('NbuildAppList') or ntools_json.get('NtoolsAppList') or []
    if not apps:
        raise ValueError('No apps defined in ntools.json')
    app = apps[0]
    tmpl = app.get('WebDownloadFile')
    if not tmpl:
        raise ValueError('WebDownloadFile template missing in ntools.json')
    url = tmpl.replace('$(Version)', version)
    # Support $(InstallPath) etc not used in URL
    return url, app


def expand_install_path(raw_path: str) -> Path:
    """Expand placeholders like $(ProgramFiles) used in ntools.json InstallPath.

    If $(ProgramFiles) is present on Windows, use the actual ProgramFiles environment
    folder. For other placeholders, expand environment variables and return a Path.
    """
    if not raw_path:
        return Path('')

    path = raw_path
    # Replace $(ProgramFiles) with system ProgramFiles
    if '$(ProgramFiles)' in path:
        if os.name == 'nt':
            pf = os.environ.get('ProgramFiles') or os.environ.get('PROGRAMFILES') or r'C:\Program Files'
        else:
            # on Unix, map to /usr/local
            pf = '/usr/local'
        path = path.replace('$(ProgramFiles)', pf)

    # Replace other $(...) placeholders by attempting to expand env vars
    # convert $(VAR) to ${VAR} then expand
    import re
    def repl(m):
        var = m.group(1)
        return os.environ.get(var, '')

    path = re.sub(r'\$\(([^)]+)\)', repl, path)

    return Path(path)


def verify_url_exists(url: str):
    # Use HEAD request to verify asset presence
    r = requests.head(url, allow_redirects=True, timeout=10)
    return r.status_code == 200


def download_file(url: str, dest: Path):
    dest.parent.mkdir(parents=True, exist_ok=True)
    with requests.get(url, stream=True) as r:
        r.raise_for_status()
        with dest.open('wb') as f:
            for chunk in r.iter_content(chunk_size=8192):
                if chunk:
                    f.write(chunk)


def extract_zip(zip_path: Path, dest_dir: Path):
    dest_dir.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(str(zip_path), 'r') as zf:
        zf.extractall(str(dest_dir))


def safe_remove_deploy_path(path: Path):
    """Safely remove the deploy path before reinstalling.

    Safety checks:
    - Path must exist and be a directory
    - Path must not be root (C:\\ or /)
    - Path must contain the expected app folder name 'Nbuild' or 'NTools' to reduce risk
    - Raise an exception if checks fail
    """
    if not path.exists():
        return
    if not path.is_dir():
        raise Exception(f"Deploy path exists but is not a directory: {path}")

    # Avoid removing root or very short paths
    resolved = path.resolve()
    if resolved.drive == '' and str(resolved) in ('/',):
        raise Exception(f"Refusing to remove root path: {resolved}")
    if resolved.drive and (str(resolved).rstrip('\\/') in (resolved.drive + ':',)):
        raise Exception(f"Refusing to remove drive root: {resolved}")

    # Basic name check to reduce risk
    if 'nbuild' not in str(resolved).lower() and 'ntools' not in str(resolved).lower():
        raise Exception(f"Deploy path '{resolved}' does not look like ntools install path; refusing to remove")

    # Perform removal
    shutil.rmtree(str(resolved))


def update_path(deploy_path: Path, no_update: bool = False):
    if no_update:
        print(f"ADD TO PATH: please add {deploy_path} to your PATH environment variable")
        return False
    # On CI or non-windows, prefer printing instructions
    if os.name != 'nt' or os.geteuid() != 0 if hasattr(os, 'geteuid') else False:
        print(f"To use ntools, add the deployment path to your PATH. Example:\n  export PATH=\"{deploy_path}:$PATH\"")
        return False

    # Windows: try to modify machine PATH via user environment if possible
    try:
        import winreg
        with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", 0, winreg.KEY_READ) as key:
            current, _ = winreg.QueryValueEx(key, 'Path')
    except Exception:
        # fallback to user PATH
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Environment", 0, winreg.KEY_READ) as key:
                current, _ = winreg.QueryValueEx(key, 'Path')
        except Exception:
            print(f"Unable to modify PATH automatically. Please add {deploy_path} to your PATH manually.")
            return False

    if str(deploy_path) in current:
        print("Deployment path already in PATH")
        return True

    # Try to write to user environment PATH
    try:
        import winreg
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Environment", 0, winreg.KEY_SET_VALUE) as key:
            new = current + ';' + str(deploy_path)
            winreg.SetValueEx(key, 'Path', 0, winreg.REG_EXPAND_SZ, new)
        print(f"Updated user PATH to include {deploy_path}. You may need to sign out and sign in for changes to take effect.")
        return True
    except Exception as ex:
        print(f"Failed to update PATH automatically: {ex}\nPlease add {deploy_path} to your PATH manually.")
        return False


def main():
    args = parse_args()
    downloads_dir = Path(args.downloads_dir).expanduser().resolve()

    ntools_json_path = Path(args.ntools_json_path).expanduser()
    # If relative path given, resolve relative to repo root (script's parent parent)
    if not ntools_json_path.is_absolute():
        ntools_json_path = (Path(__file__).resolve().parents[1] / ntools_json_path).resolve()

    if args.dry_run:
        print("DRY RUN: inputs:")
        print(f" version: {args.version}")
        print(f" downloads_dir: {downloads_dir}")
        print(f" ntools_json_path: {ntools_json_path}")
        print(f" deploy_path: {args.deploy_path}")
    # Validate ntools.json
    data = load_ntools_json(ntools_json_path)
    url, app = build_asset_url(data, args.version)
    parsed = urlparse(url)
    if not parsed.scheme.startswith('http'):
        raise ValueError(f"Unsupported URL scheme in generated asset URL: {url}")

    zip_name = app.get('DownloadedFile') or f"{args.version}.zip"
    zip_name = zip_name.replace('$(Version)', args.version)
    download_dest = downloads_dir / zip_name

    if args.dry_run:
        print(f"Would verify URL: {url}")
        print(f"Would download to: {download_dest}")
        deploy_path = Path(args.deploy_path) if args.deploy_path else expand_install_path(app.get('InstallPath', ''))
        if not deploy_path or str(deploy_path) == '':
            # choose reasonable defaults
            deploy_path = Path('C:/Program Files/Nbuild') if os.name == 'nt' else Path('/usr/local/bin')
        print(f"Would extract zip to: {deploy_path}")
        print("Dry run complete. No network calls were made.")
        return 0

    # Verify URL exists
    ok = verify_url_exists(url)
    if not ok:
        print(f"ERROR: Release asset not found at {url}")
        return 2

    print(f"Downloading {url} to {download_dest}")
    download_file(url, download_dest)

    # Determine deploy path
    if args.deploy_path:
        deploy_path = Path(args.deploy_path)
    else:
        deploy_path = expand_install_path(app.get('InstallPath', ''))
        if not deploy_path or str(deploy_path) == '':
            deploy_path = Path('C:/Program Files/Nbuild') if os.name == 'nt' else Path('/usr/local/lib/ntools')

    # Remove existing installation before extracting new one (explicit policy: always replace)
    try:
        safe_remove_deploy_path(deploy_path)
    except Exception as ex:
        print(f"Refusing to remove deploy path: {ex}")
        return 3

    extract_zip(download_dest, deploy_path)

    updated = update_path(deploy_path, args.no_path_update)
    if updated:
        print("Install complete and PATH updated.")
    else:
        print("Install complete. Please ensure the deployment path is on PATH to use ntools.")

    return 0


if __name__ == '__main__':
    try:
        code = main()
        sys.exit(code)
    except Exception as e:
        print(f"Fatal error: {e}")
        sys.exit(1)
