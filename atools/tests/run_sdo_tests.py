"""
Test runner for SDO tests.
"""

import sys
import subprocess
from pathlib import Path


def install_dependencies():
    """Install test dependencies."""
    atools_dir = Path(__file__).parent.parent
    requirements_dev = atools_dir / "requirements-dev.txt"
    
    if not requirements_dev.exists():
        print(f"Warning: {requirements_dev} not found, skipping dependency installation")
        return True
    
    print("Installing test dependencies...")
    cmd = [sys.executable, "-m", "pip", "install", "-q", "-r", str(requirements_dev)]
    result = subprocess.run(cmd)
    
    if result.returncode == 0:
        print("✓ Dependencies installed")
        return True
    else:
        print("❌ Failed to install dependencies")
        return False


def run_tests():
    """Run all SDO tests."""
    test_dir = Path(__file__).parent

    # Install dependencies first
    if not install_dependencies():
        return 1

    # Run pytest with coverage
    cmd = [
        sys.executable, "-m", "pytest",
        str(test_dir),
        "-v",
        "--tb=short",
        "--cov=sdo_package",
        "--cov-report=term-missing"
    ]

    print("\nRunning SDO test suite...")
    result = subprocess.run(cmd)
    return result.returncode


if __name__ == "__main__":
    exit_code = run_tests()
    sys.exit(exit_code)
