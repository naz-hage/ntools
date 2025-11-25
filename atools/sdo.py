#!/usr/bin/env python3
"""
SDO - Simple DevOps Operations Tool
Main entry point for the command-line interface.
"""

import os
import sys
sys.path.insert(0, os.path.dirname(__file__))

from sdo_package.cli import main, cli

# Expose the CLI app for tests
app = cli

if __name__ == "__main__":
    main()