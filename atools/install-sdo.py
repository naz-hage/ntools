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
import shutil
import subprocess
import sys
import time
from pathlib import Path


def parse_args():
    parser = argparse.ArgumentParser(
        description="Install SDO tool in isolated virtual environment",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    python install_sdo.py --local                           # Install locally for development (.venv)
    python install_sdo.py --local --uninstall               # Delete local .venv
    python install_sdo.py                                   # Install system-wide to NBuild
    python install_sdo.py --uninstall                       # Remove system-wide installation
    python install_sdo.py --dry-run                         # Show what would be done
    python install_sdo.py --source-path ../sdo-source       # Custom source location
    python install_sdo.py --nbuild-path "C:\\Program Files\\NBuild"  # Custom NBuild path
        """
    )

    parser.add_argument(
        '--local',
        action='store_true',
        help='Install locally in .venv for development (instead of system-wide)'
    )

    parser.add_argument(
        '--uninstall',
        action='store_true',
        help='Remove/delete the virtual environment'
    )

    parser.add_argument(
        '--source-path',
        default=str(Path(__file__).resolve().parent),
        help='Path to SDO source directory (default: script directory)'
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


def uninstall_venv(venv_path: Path, dry_run: bool = False) -> bool:
    """Remove virtual environment directory."""
    
    if not venv_path.exists():
        print(f"⚠️  Virtual environment not found at {venv_path}")
        return True
    
    if dry_run:
        print(f"Would delete virtual environment at: {venv_path}")
        return True
    
    # Stop any running Python processes using this virtual environment
    print("Stopping any running Python processes using the virtual environment...")
    try:
        if sys.platform == 'win32':
            # Use PowerShell to stop processes - look for the full venv path
            venv_path_str = str(venv_path).replace("'", "''")
            ps_cmd = f"Get-Process | Where-Object {{ $_.Path -and $_.Path -like '*{venv_path_str}*' }} | Stop-Process -Force -ErrorAction SilentlyContinue"
            result = subprocess.run(['powershell', '-Command', ps_cmd], 
                                  capture_output=True, text=True, timeout=30)
            if result.returncode == 0:
                print("✓ Process stopping command completed")
            else:
                print(f"⚠️  Warning: Could not stop processes: {result.stderr}")
        else:
            # For Linux/Mac, use pkill
            result = subprocess.run(['pkill', '-f', str(venv_path)], 
                                  capture_output=True, text=True, timeout=10)
            if result.returncode == 0:
                print("✓ Stopped running processes")
            elif result.returncode == 1:
                print("ℹ️  No running processes found to stop")
    except subprocess.TimeoutExpired:
        print("⚠️  Warning: Process stopping timed out")
    except Exception as e:
        print(f"⚠️  Warning: Could not stop processes: {e}")
    
    # Small delay to ensure processes are fully stopped
    time.sleep(1.0)
    
    try:
        print(f"Deleting virtual environment at {venv_path}...")
        shutil.rmtree(venv_path)
        print("✓ Virtual environment deleted successfully")
        return True
    except Exception as e:
        print(f"❌ Failed to delete virtual environment: {e}")
        return False


def install_sdo(sdo_source_path: Path, target_path: Path, is_local: bool = False, dry_run: bool = False) -> bool:
    """Install SDO in isolated virtual environment."""

    print(f"Installing SDO from {sdo_source_path} to {target_path}")

    # For local install, venv_path IS target_path (.venv)
    # For system install, venv_path is target_path/venv (NBuild/venv)
    if is_local:
        venv_path = target_path
    else:
        venv_path = target_path / "venv"
        # Ensure target directory exists for system install
        if not target_path.exists():
            if dry_run:
                print(f"Would create directory: {target_path}")
            else:
                target_path.mkdir(parents=True, exist_ok=True)
                print(f"✓ Created directory: {target_path}")

    # Create virtual environment
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

    # Get venv python and pip paths
    if sys.platform == 'win32':
        venv_python = venv_path / "Scripts" / "python.exe"
        venv_pip = venv_path / "Scripts" / "pip.exe"
    else:
        venv_python = venv_path / "bin" / "python"
        venv_pip = venv_path / "bin" / "pip"

    # Upgrade pip in venv using the venv's python (as pip recommends)
    if not dry_run:
        print("Upgrading pip in virtual environment...")
        cmd = [str(venv_python), "-m", "pip", "install", "--upgrade", "pip"]
        result = subprocess.run(cmd, capture_output=True, text=True, check=False)
        if result.returncode != 0:
            print(f"⚠️  Warning: Failed to upgrade pip: {result.stderr}")
        else:
            print("✓ Pip upgraded successfully")

    # Install SDO in virtual environment
    if dry_run:
        print(f"Would install SDO from {sdo_source_path} using {venv_python}")
    else:
        print(f"Installing SDO from {sdo_source_path}...")
        cmd = [str(venv_python), "-m", "pip", "install", "-e", str(sdo_source_path)]
        result = subprocess.run(cmd, capture_output=True, text=True, check=False)
        if result.returncode != 0:
            print(f"❌ SDO installation failed: {result.stderr}")
            return False
        print("✓ SDO installed successfully")

    # Copy mapping.md file to target directory
    mapping_source = sdo_source_path / "sdo_package" / "mapping.md"
    if is_local:
        mapping_dest = target_path.parent / "mapping.md"  # Copy to atools directory for local install
    else:
        mapping_dest = target_path / "mapping.md"  # Copy to NBuild directory for system install

    if mapping_source.exists():
        if dry_run:
            print(f"Would copy mapping.md from {mapping_source} to {mapping_dest}")
        else:
            try:
                import shutil
                shutil.copy2(mapping_source, mapping_dest)
                print(f"✓ Copied mapping.md to {mapping_dest}")
            except Exception as e:
                print(f"⚠️  Warning: Failed to copy mapping.md: {e}")
    else:
        print(f"⚠️  Warning: mapping.md not found at {mapping_source}")

    # Create launcher script (only for system-wide install, not local)
    if not is_local:
        if not create_launcher_script(target_path, venv_path, dry_run):
            return False

    return True


def main():
    args = parse_args()

    print("SDO Installation Script")
    print("=" * 40)

    # Resolve paths
    sdo_source_path = Path(args.source_path).resolve()
    
    # Determine target path based on --local flag
    if args.local:
        target_path = sdo_source_path / ".venv"
        install_type = "local development"
    else:
        target_path = Path(args.nbuild_path).resolve()
        install_type = "system-wide"

    print(f"Installation Type: {install_type}")
    print(f"SDO Source: {sdo_source_path}")
    print(f"Target Path: {target_path}")
    print(f"Dry Run: {args.dry_run}")
    print()

    # Handle uninstall
    if args.uninstall:
        if args.local:
            venv_path = sdo_source_path / ".venv"
        else:
            venv_path = target_path / "venv"
            # Also remove launcher script
            if sys.platform == 'win32':
                launcher_path = target_path / "sdo.cmd"
            else:
                launcher_path = target_path / "sdo"
            
            if launcher_path.exists() and not args.dry_run:
                try:
                    launcher_path.unlink()
                    print(f"✓ Removed launcher script: {launcher_path}")
                except Exception as e:
                    print(f"⚠️  Could not remove launcher script: {e}")
        
        success = uninstall_venv(venv_path, args.dry_run)
        if success:
            print(f"\n✅ SDO {install_type} environment removed successfully")
        return 0 if success else 1

    # Validate source path for installation
    if not sdo_source_path.exists():
        print(f"❌ SDO source path does not exist: {sdo_source_path}")
        print("Please specify the correct path with --source-path")
        return 1

    sdo_package_path = sdo_source_path / "sdo_package"
    if not sdo_package_path.exists():
        print(f"❌ SDO package not found at: {sdo_package_path}")
        print("Please ensure you're pointing to the directory containing sdo_package")
        return 1

    # For system-wide install, try to find NBuild in PATH if default doesn't exist
    if not args.local and not target_path.exists():
        found_nbuild = find_nbuild_in_path()
        if found_nbuild:
            print(f"Found NBuild in PATH: {found_nbuild}")
            target_path = found_nbuild
        else:
            print(f"⚠️  NBuild directory not found at {target_path}")
            if not args.dry_run:
                print("Installation will create the directory")

    # Perform installation
    success = install_sdo(sdo_source_path, target_path, args.local, args.dry_run)

    if success:
        if args.dry_run:
            print("\n✅ Dry run completed successfully")
            print("Run without --dry-run to perform actual installation")
        else:
            print(f"\n✅ SDO {install_type} installation completed successfully!")
            if args.local:
                print(f"To activate the virtual environment, run:")
                if sys.platform == 'win32':
                    print(f"  .venv\\Scripts\\Activate.ps1")
                else:
                    print(f"  source .venv/bin/activate")
                print(f"Then use: sdo --help")
            else:
                print(f"You can now use 'sdo' command from any location")
                launcher_path = target_path / ('sdo.cmd' if sys.platform == 'win32' else 'sdo')
                print(f"Launcher script created at: {launcher_path}")

                # Test the installation
                print("\nTesting SDO installation...")
                try:
                    if sys.platform == 'win32':
                        if launcher_path.exists():
                            print("✓ Launcher script exists and is ready")
                        else:
                            print("⚠️  Launcher script not found")
                    else:
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
        print(f"\n❌ SDO {install_type} installation failed")
        return 1


if __name__ == '__main__':
    sys.exit(main())