"""
SDO Package - Simple DevOps Operations Tool
A modern CLI tool for Azure DevOps and GitHub operations.
"""

import importlib.metadata

try:
    __version__ = importlib.metadata.version("sdo")
    __version_info__ = tuple(map(int, __version__.split(".")))
except importlib.metadata.PackageNotFoundError:
    # Fallback for development
    __version__ = "0.0.0"
    __version_info__ = (0, 0, 0)

__author__ = "naz-hage"
__description__ = "Simple DevOps Operations Tool"
