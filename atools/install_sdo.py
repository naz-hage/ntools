#!/usr/bin/env python3
"""
Install SDO (Simple DevOps) tool in an isolated virtual environment.

This script creates a virtual environment in the NBuild directory and installs
SDO with all its dependencies, ensuring complete isolation from system Python.

Usage:
    python install_sdo.py [--source-path PATH] [--nbuild-path PATH] [--dry-run]

Requirements:
    - Python 3.8+ installed
    - NBuild directory exists and is in PATH
    - Internet connection for pip installs (unless using cached packages)
"""

import argparse
import subprocess
import sys
from pathlib import Path


def parse_args():
    parser = argparse.ArgumentParser(
        description="Install SDO tool in isolated virtual environment",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    python install_sdo.py                                    # Install with defaults
    python install_sdo.py --dry-run                         # Show what would be done
    python install_sdo.py --source-path ../sdo-source       # Custom source location
    python install_sdo.py --nbuild-path "C:\\Program Files\\NBuild"  # Custom NBuild path
        """
    )

    parser.add_argument(
        '--source-path',
        default='../atools',
        help='Path to SDO source directory (default: ../atools)'
    )

    parser.add_argument(
        '--nbuild-path',
        default=r'C:\Program Files\NBuild',
        help='Path to NBuild installation directory (default: C:\\Program Files\\NBuild)'
    )

    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Show what would be done without making changes'
    )

    return parser.parse_args()


def find_nbuild_in_path():
    """Find NBuild directory in PATH."""
    import os

    path_dirs = os.environ.get('PATH', '').split(os.pathsep)
    for dir_path in path_dirs:
        dir_path = Path(dir_path).resolve()
        if dir_path.name.lower() in ['nbuild', 'n-build'] and dir_path.exists():
            return dir_path

    # Fallback to common locations
    common_paths = [
        r'C:\Program Files\NBuild',
        r'C:\Program Files (x86)\NBuild',
        Path.home() / 'NBuild'
    ]

    for path in common_paths:
        if Path(path).exists():
            return Path(path)

    return None


def create_launcher_script(nbuild_path: Path, venv_path: Path, dry_run: bool = False) -> bool:
    """Create launcher script that activates venv and runs SDO."""

    if sys.platform == 'win32':
        launcher_path = nbuild_path / "sdo.cmd"
        launcher_content = f'''@echo off
call "{venv_path}\\Scripts\\activate.bat"
python -m sdo_package.cli %*
'''
    else:
        launcher_path = nbuild_path / "sdo"
        launcher_content = f'''#!/bin/bash
source "{venv_path}/bin/activate"
exec python -m sdo_package.cli "$@"
'''
        launcher_path.chmod(0o755)

    if dry_run:
        print(f"Would create launcher script at: {launcher_path}")
        print(f"Launcher content:\n{launcher_content}")
        return True

    try:
        launcher_path.write_text(launcher_content)
        print(f"✓ Created SDO launcher at {launcher_path}")
        return True
    except Exception as e:
        print(f"❌ Failed to create launcher script: {e}")
        return False


def install_sdo(sdo_source_path: Path, nbuild_path: Path, dry_run: bool = False) -> bool:
    """Install SDO in isolated virtual environment."""

    print(f"Installing SDO from {sdo_source_path} to {nbuild_path}")

    # Ensure NBuild directory exists
    if not nbuild_path.exists():
        if dry_run:
            print(f"Would create directory: {nbuild_path}")
        else:
            nbuild_path.mkdir(parents=True, exist_ok=True)
            print(f"✓ Created directory: {nbuild_path}")

    # Create virtual environment
    venv_path = nbuild_path / "venv"
    if dry_run:
        print(f"Would create virtual environment at: {venv_path}")
    else:
        print(f"Creating virtual environment at {venv_path}...")
        cmd = [sys.executable, "-m", "venv", str(venv_path)]
        result = subprocess.run(cmd, capture_output=True, text=True, check=False)
        if result.returncode != 0:
            print(f"❌ Failed to create virtual environment: {result.stderr}")
            return False
        print("✓ Virtual environment created")

    # Get venv pip path
    if sys.platform == 'win32':
        venv_pip = venv_path / "Scripts" / "pip.exe"
    else:
        venv_pip = venv_path / "bin" / "pip"

    # Upgrade pip in venv
    if not dry_run:
        print("Upgrading pip in virtual environment...")
        cmd = [str(venv_pip), "install", "--upgrade", "pip"]
        result = subprocess.run(cmd, capture_output=True, text=True, check=False)
        if result.returncode != 0:
            print(f"⚠️  Warning: Failed to upgrade pip: {result.stderr}")

    # Install SDO in virtual environment
    if dry_run:
        print(f"Would install SDO from {sdo_source_path} using {venv_pip}")
    else:
        print(f"Installing SDO from {sdo_source_path}...")
        cmd = [str(venv_pip), "install", "-e", str(sdo_source_path)]
        result = subprocess.run(cmd, capture_output=True, text=True, check=False)
        if result.returncode != 0:
            print(f"❌ SDO installation failed: {result.stderr}")
            return False
        print("✓ SDO installed successfully")

    # Create launcher script
    if not create_launcher_script(nbuild_path, venv_path, dry_run):
        return False

    return True


def main():
    args = parse_args()

    print("SDO Installation Script")
    print("=" * 40)

    # Resolve paths
    sdo_source_path = Path(args.source_path).resolve()
    nbuild_path = Path(args.nbuild_path).resolve()

    # Validate source path
    if not sdo_source_path.exists():
        print(f"❌ SDO source path does not exist: {sdo_source_path}")
        print("Please specify the correct path with --source-path")
        return 1

    sdo_package_path = sdo_source_path / "sdo_package"
    if not sdo_package_path.exists():
        print(f"❌ SDO package not found at: {sdo_package_path}")
        print("Please ensure you're pointing to the directory containing sdo_package")
        return 1

    # Try to find NBuild in PATH if default path doesn't exist
    if not nbuild_path.exists():
        found_nbuild = find_nbuild_in_path()
        if found_nbuild:
            print(f"Found NBuild in PATH: {found_nbuild}")
            nbuild_path = found_nbuild
        else:
            print(f"⚠️  NBuild directory not found at {nbuild_path}")
            print("Installation will proceed, but you may need to create the directory manually")

    print(f"SDO Source: {sdo_source_path}")
    print(f"NBuild Path: {nbuild_path}")
    print(f"Dry Run: {args.dry_run}")
    print()

    # Perform installation
    success = install_sdo(sdo_source_path, nbuild_path, args.dry_run)

    if success:
        if args.dry_run:
            print("\n✅ Dry run completed successfully")
            print("Run without --dry-run to perform actual installation")
        else:
            print("\n✅ SDO installation completed successfully!")
            print(f"You can now use 'sdo' command from any location")
            print(f"Launcher script created at: {nbuild_path / ('sdo.cmd' if sys.platform == 'win32' else 'sdo')}")

            # Test the installation
            print("\nTesting SDO installation...")
            try:
                if sys.platform == 'win32':
                    launcher_path = nbuild_path / "sdo.cmd"
                    # For Windows, we can't easily test the batch file, so just check it exists
                    if launcher_path.exists():
                        print("✓ Launcher script exists and is ready")
                    else:
                        print("⚠️  Launcher script not found")
                else:
                    # For Unix, test by running the launcher
                    launcher_path = nbuild_path / "sdo"
                    cmd = [str(launcher_path), "--help"]
                    result = subprocess.run(cmd, capture_output=True, text=True, timeout=10)
                    if result.returncode == 0:
                        print("✓ SDO launcher works correctly")
                    else:
                        print(f"⚠️  SDO launcher test failed: {result.stderr}")
            except Exception as e:
                print(f"⚠️  Could not test SDO launcher: {e}")

        return 0
    else:
        print("\n❌ SDO installation failed")
        return 1


if __name__ == '__main__':
    sys.exit(main())