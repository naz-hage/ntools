"""
Test runner for SDO tests.
"""

import sys
import subprocess
from pathlib import Path


def run_tests():
    """Run all SDO tests."""
    test_dir = Path(__file__).parent

    # Run pytest with coverage
    cmd = [
        sys.executable, "-m", "pytest",
        str(test_dir),
        "-v",
        "--tb=short",
        "--cov=sdo_package",
        "--cov-report=term-missing"
    ]

    print("Running SDO test suite...")
    result = subprocess.run(cmd)
    return result.returncode


if __name__ == "__main__":
    exit_code = run_tests()
    sys.exit(exit_code)
