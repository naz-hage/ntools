---
title: atools — Helper tools
---

# atools — Helper tools (overview)

This section gives a short overview of the helper tools in the `atools/` folder.

Tools

- [install-ntools.py](install-ntools.md) — Cross-platform installer for NTools; downloads and extracts NTools release ZIPs and handles deployment; supports `--dry-run` and includes comprehensive safety checks.

## Note: SDO (Simple DevOps Operations)

The Python-based SDO tool has been deprecated in favor of the modern C# implementation. For DevOps operations across Azure DevOps and GitHub platforms, please use **sdo.net** (C# version), which provides:

- Full feature parity with the Python version
- 2x+ performance improvement
- Better integration with .NET ecosystem
- No additional Python runtime dependency

See the main [ntools documentation](../index.md) for sdo.net usage.
